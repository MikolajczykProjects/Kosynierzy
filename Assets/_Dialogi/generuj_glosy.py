#!/usr/bin/env python3
"""Generuje głosy postaci (i opcjonalnie efekty) z ElevenLabs.

Użycie:
  export ELEVENLABS_API_KEY=...            (albo plik .env w katalogu repo)
  python3 tools/generuj_glosy.py --lista-glosow     # Twoje głosy i ich voice_id
  python3 tools/generuj_glosy.py --sucho            # policz znaki, nic nie wysyłaj
  python3 tools/generuj_glosy.py --tylko a1_        # tylko dialogi zaczynające się od a1_
  python3 tools/generuj_glosy.py --wymus a1_zofia   # wygeneruj ten dialog od nowa
  python3 tools/generuj_glosy.py                    # wszystko, czego jeszcze nie ma
  python3 tools/generuj_glosy.py --sfx              # efekty do Assets/_Dialogi/Resources/Sfx

Wersja pod Unity: uruchamiaj z katalogu projektu (tam, gdzie Assets/).
Tylko biblioteka standardowa, nic nie trzeba instalować.
"""
import argparse, json, os, sys, urllib.request, urllib.error

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DIALOGI = os.path.join(ROOT, "Assets", "_Dialogi", "Resources", "dialogi.json")
GLOSY = os.path.join(ROOT, "Assets", "_Dialogi", "glosy.json")
OUT_DIALOGI = os.path.join(ROOT, "Assets", "_Dialogi", "Resources", "Audio")
OUT_SFX = os.path.join(ROOT, "Assets", "_Dialogi", "Resources", "Sfx")
API = "https://api.elevenlabs.io/v1"


def klucz():
    k = os.environ.get("ELEVENLABS_API_KEY")
    if not k and os.path.exists(os.path.join(ROOT, ".env")):
        for line in open(os.path.join(ROOT, ".env"), encoding="utf-8"):
            if line.startswith("ELEVENLABS_API_KEY="):
                k = line.split("=", 1)[1].strip().strip('"')
    if not k:
        sys.exit("Brak ELEVENLABS_API_KEY (export albo plik .env w katalogu repo).")
    return k


def zapytanie(metoda, sciezka, cialo=None):
    req = urllib.request.Request(API + sciezka, method=metoda,
                                 data=json.dumps(cialo).encode() if cialo else None)
    req.add_header("xi-api-key", klucz())
    req.add_header("Content-Type", "application/json")
    try:
        with urllib.request.urlopen(req, timeout=120) as r:
            return r.read()
    except urllib.error.HTTPError as e:
        sys.exit(f"Błąd API {e.code} dla {sciezka}: {e.read().decode(errors='replace')[:400]}")


def lista_glosow():
    dane = json.loads(zapytanie("GET", "/voices"))
    for v in dane.get("voices", []):
        lab = v.get("labels") or {}
        print(f"{v['voice_id']}  {v['name']:<28} {lab.get('gender','')} {lab.get('age','')} {lab.get('accent','')}")


def tts(tekst, glos, model, plik):
    cialo = {"text": tekst, "model_id": model, "voice_settings": {
        "stability": glos.get("stability", 0.5),
        "similarity_boost": glos.get("similarity_boost", 0.75),
        "style": glos.get("style", 0.2),
        "use_speaker_boost": True}}
    audio = zapytanie("POST", f"/text-to-speech/{glos['voice_id']}?output_format=mp3_44100_128", cialo)
    with open(plik, "wb") as f:
        f.write(audio)


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--lista-glosow", action="store_true")
    p.add_argument("--sucho", action="store_true")
    p.add_argument("--tylko", default="")
    p.add_argument("--wymus", action="append", default=[])
    p.add_argument("--sfx", action="store_true")
    a = p.parse_args()

    if a.lista_glosow:
        return lista_glosow()

    glosy = json.load(open(GLOSY, encoding="utf-8"))

    if a.sfx:
        os.makedirs(OUT_SFX, exist_ok=True)
        for nazwa, s in glosy["sfx"].items():
            plik = os.path.join(OUT_SFX, nazwa + ".mp3")
            if os.path.exists(plik) and nazwa not in a.wymus:
                continue
            print("sfx:", nazwa)
            if not a.sucho:
                audio = zapytanie("POST", "/sound-generation",
                                  {"text": s["opis"], "duration_seconds": s.get("sekundy", 2.0)})
                open(plik, "wb").write(audio)
        return

    dialogi = json.load(open(DIALOGI, encoding="utf-8"))
    os.makedirs(OUT_DIALOGI, exist_ok=True)
    brak_glosu, znaki, zrobione = set(), 0, 0
    for did, d in dialogi.items():
        if not did.startswith(a.tylko):
            continue
        for i, linia in enumerate(d["linie"], 1):
            nazwa = f"{did}_{i:02d}"
            plik = os.path.join(OUT_DIALOGI, nazwa + ".mp3")
            sciezka_gry = nazwa  # Unity: Resources.Load<AudioClip>("Audio/" + audio)
            if os.path.exists(plik) and did not in a.wymus:
                linia["audio"] = sciezka_gry
                continue
            glos = glosy["postacie"].get(linia["kto"], {})
            if not glos.get("voice_id"):
                brak_glosu.add(linia["kto"])
                continue
            znaki += len(linia["tekst"])
            if a.sucho:
                zrobione += 1
                continue
            print(f"{nazwa}  [{linia['kto']}]  {linia['tekst'][:60]}")
            tts(linia["tekst"], glos, glosy.get("model", "eleven_multilingual_v2"), plik)
            linia["audio"] = sciezka_gry
            zrobione += 1
            # zapis po każdej linii, żeby przerwanie nic nie gubiło
            json.dump(dialogi, open(DIALOGI, "w", encoding="utf-8"), ensure_ascii=False, indent=2)

    json.dump(dialogi, open(DIALOGI, "w", encoding="utf-8"), ensure_ascii=False, indent=2)
    print(f"\n{'Do wygenerowania' if a.sucho else 'Wygenerowano'}: {zrobione} linii, {znaki} znaków.")
    if brak_glosu:
        print("Pominięte, bo brak voice_id w data/glosy.json:", ", ".join(sorted(brak_glosu)))


if __name__ == "__main__":
    main()
