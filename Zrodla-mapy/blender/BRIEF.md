# BRIEF — modele mapy „SENDER GLEIWITZ 31.08.1939” (Blender 5.2 → Unity 6 URP)

Gra na hackatonie (Unity 6, URP, low poly, kamera z trzeciej osoby). Gliwice, 31.08.1939, popołudnie i wieczór. Styl: **ładne low poly** w duchu epoki,
płaskie kolory (bez tekstur), ostre krawędzie. Ma wyglądać „wow” w kamerze z trzeciej osoby z 3–30 m: bogata sylweta, gzymsy, pilastry, cokoły,
okna z ramami i parapetami, kominy, lukarny, drzwi z daszkiem, schody. Detal robimy BRYŁĄ, nie teksturą. Zero nowoczesności (żadnego plastiku,
neonów, anten satelitarnych, aut). **Zero swastyk, flag III Rzeszy i symboli nazistowskich** (art. 256 kk). Szyldy jako gładkie płyty bez napisów.

## Biblioteka — UŻYWAJ, nie pisz własnej
`/Users/nataniel_bk/Documents/NBK/agent-dashboard/agent-workspace/otton/hackaton-gliwice/mapa/blender/lowpoly.py` — przeczytaj ją najpierw (ok. 400 linii).
- `Siatka(nazwa)` zbiera bryły w jeden obiekt: `box`, `box_od_do`, `belka(a,b,grub)`, `walec`, `stozek`, `ostroslup`, `dach_dwuspadowy`,
  `dach_czterospadowy`, `okna_rzad`, `obrys`, `wielokat`, `poly/quad/tri`, `dolacz`, na końcu `.obiekt(poz, kat)`.
- Kolory TYLKO z `PALETA` (nazwy jak `tynk_ochra`, `cegla`, `dachowka`, `modrzew`, `okno`...). Nieznana nazwa rzuca błąd.
  Potrzebujesz nowego koloru? NIE edytuj lowpoly.py (inni z niej korzystają). Napisz go w raporcie, dodam go ja.
  Do tego czasu użyj najbliższego istniejącego.
- `drzewo(nazwa, typ, ziarno, skala)`, `krzak(...)` to gotowa roślinność.
- `punkt(nazwa, (x,y,z), yaw)`: punkt gry (spawn, NPC, drzwi). `swiatlo(...)`: lampa.
- `eksport_fbx(lista_obiektow, plik)`, `render_podglad(plik, cel, odl, azymut, wysokosc_kat)`, `ziemia_podgladu(r)`, `wyczysc_scene()`, `zapisz_meta(plik)`.

## Układ i konwencje (WAŻNE: Unity od tego zależy)
- Blender: X = wschód, Y = północ, Z = góra. 1 j. = 1 m.
- Każdy obiekt ma **pivot w środku podstawy (0,0,0)** i **front (wejście główne) w stronę +Y**. Na mapie i tak go obrócę.
- **Każda rzecz to osobny obiekt** (budynek, wieża, płot, fontanna, drzewo, ławka itd.), żeby w Unity dało się je przesuwać i skalować pojedynczo.
  Nazwy obiektów po polsku, bez spacji i polskich znaków, np. `Ratusz`, `Ratusz_Wieza`, `Fontanna_Neptun`.
- Sufit, który ma nie mieć kolizji: nazwa kończy się na `_NOCOL`. Niewidzialna bryła tylko do kolizji: `_COL`. Drzewa: nazwa zaczyna się od `Drzewo`.
- Drobny detal (< 0,3 m) wrzucaj do osobnego obiektu `<Nazwa>_Detal_NOCOL`, żeby kolizje nie łapały gracza na gzymsach.
- Wnętrza: podłoga ma być obiektem z kolizją. Ściany grubości ≥ 0,2 m. Drzwi to otwór 1,1 × 2,2 m (prefab drzwi wstawi zespół).
- Liczba trójkątów: duży landmark do 25 tys., zwykły budynek do 3 tys., wnętrze do 30 tys. Low poly, ale z detalem.

## Plik, który oddajesz
`.../mapa/blender/obiekty/<modul>.py`, zawierający:
```python
import sys, os
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from lowpoly import *

def zbuduj():
    """Buduje obiekty w origin (0,0,0). Zwraca listę obiektów bpy. Dodaje punkty/światła przez punkt()/swiatlo()."""
    ...
    return [obiekty]

if __name__ == "__main__":
    wyczysc_scene()
    obs = zbuduj()
    eksport_fbx(obs, os.path.join(UNITY_MODELE, "<Modul>.fbx"))
    zapisz_meta(os.path.join(DANE, "meta_<modul>.json"))
    ziemia_podgladu(120)
    render_podglad(os.path.join(PODGLADY, "<modul>_1.png"), cel=(...), odl=..., azymut=215)   # z frontu
    render_podglad(os.path.join(PODGLADY, "<modul>_2.png"), cel=(...), odl=..., azymut=35)    # z tyłu
    # + zbliżenie z wysokości oczu gracza (wysokosc_kat=8, odl ~15)
```
Uruchom: `blender -b --factory-startup -P <plik>.py 2>&1 | grep -iE "error|Traceback|PODGLAD"`.
Render trwa kilka sekund. **Obejrzyj swoje PNG (narzędziem Read), jeśli możesz, i poprawiaj, aż będzie ładnie.** Minimum 2 iteracje poprawek.

Nie masz prawa zapisu poza swoim katalogiem? Zapisz plik i PNG u siebie (`<twój workspace>/gliwice/`), a w raporcie podaj ścieżki. Przeniosę je.

## Raport (krótko)
Ścieżka pliku, nazwy obiektów, liczba trójkątów, lista punktów, ścieżki PNG, nowe kolory do palety, czego nie zrobiłeś.
