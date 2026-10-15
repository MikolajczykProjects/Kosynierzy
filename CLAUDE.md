# CLAUDE.md — instrukcja dla Claude'a (przeczytaj przed jakąkolwiek pracą)

Projekt Unity gry „Sender Gleiwitz — 31.08.1939” (hackaton, zespół Kosynierzy).
Wspólne zasady i podział ról: `docs/00-PODZIAL-I-ZASADY.txt`. Fabuła: `docs/FABULA.md`.

## Mapa Gliwic 1939 (rola A) — co gdzie leży
| Ścieżka | Co to jest |
|---|---|
| `Assets/_Mapa/` | Gotowa mapa: 81 FBX, tekstury CC0, skrypt budujący scenę, dane aktów, rendery podglądowe |
| `Assets/_Mapa/CZYTAJ_NAJPIERW_AI.md` | **Główna instrukcja mapy — przeczytaj w całości przed pracą na mapie** |
| `Assets/_Mapa/Dane/akty.json` | Który obiekt i punkt należy do którego aktu. Źródło prawdy, nie zgaduj z nazw |
| `Assets/_Mapa/Podglady_aktow/*.png` | 23 rendery z oczu gracza, podpisane aktem — obejrzyj, żeby wiedzieć, jak wygląda każde miejsce |
| `Assets/_Dialogi/` | 177 kwestii w 56 scenach (`dialogi.json`), menu/przedmioty/dziennik/cele (`teksty_gry.json`), opisy głosów do ElevenLabs |
| `Zrodla-mapy/` | Źródło mapy dla Blendera (`.blend` + skrypty Python) i dane OSM. Poza `Assets/`, żeby Unity tego nie importował. Do gry niepotrzebne |

## Jak podpiąć mapę (pierwsze 5 minut)
1. Otwórz projekt, poczekaj na import `Assets/_Mapa` (pierwszy raz potrwa).
2. Menu **Tools → Zbuduj Gliwice**. Powstaje `Assets/_Mapa/Mapa_Gliwice.prefab`: teren, ulice, budynki, natura, wnętrza, 145 punktów gry, 413 świateł.
3. Sprawdź log w konsoli (liczba obiektów i punktów, ostrzeżenia).
4. **Kosynierzy → Refresh Prefab Registry**, wstaw `Mapa_Gliwice.prefab` do sceny, usuń `Mapa_Testowa`, przelicz NavMesh.

## Twarde zasady
- **Nie edytuj niczego w `Assets/_Mapa/`** i nie przesuwaj ręcznie FBX. Każde „Zbuduj Gliwice” odtwarza mapę od zera — ręczne zmiany znikną.
- Kod i logika gry idą do `Assets/_Gra/` / `Assets/Scripts/` (rola B).
- Do obiektów mapy odwołuj się po nazwie, komponencie `AktZnacznik` albo nazwie punktu (`Punkty/spawn_*`, `Punkty/miasto_*`), nie przez twarde referencje do instancji.
- 1 jednostka = 1 metr, oś Y w górę. Materiały pod URP.
- **Wnętrza stoją osobno** w strefie `STREFA_WNETRZ` (X = 4000…4600), z dala od miasta. Wejście do budynku = teleport z punktu drzwi w mieście (np. `miasto_drzwi_hotel`) do punktu startu we wnętrzu.
- Obiekty fabularne mają prefiks aktu: `AKT1_Hotel`, `AKT3_Radiostacja` itd. Pole `opis_dla_ai` w `AktZnacznik` mówi, do czego obiekt służy.

## Akty w skrócie (szczegóły: `akty.json`)
| Akt | Miejsca |
|---|---|
| 1 | Dworzec, Hotel Haus Oberschlesien (z wnętrzem), księgarnia Zofii (wnętrze) |
| 2 | Rynek z ratuszem, kościół, ulice, most i zagajnik, fotograf (wnętrze) |
| 3–4 | Radiostacja: wieża i wnętrze (studio, sterownia, piwnica), ucieczka na wschód |
| Epilog | Brak geometrii, ekran tekstowy |

## Czego w mapie nie ma
- Postaci i NPC (rola C, `Assets/_Postacie/`).
- Wyposażenia radiostacji (pulpit, mikrofon, wzmacniacz) — są tylko punkty pod nie (rola D, `Assets/_Obiekty/`).
- Plików audio dialogów — dojdą do `Assets/_Dialogi/`, nazwy już są w `dialogi.json` (np. `a1_zofia_01`).

## Znane braki mapy
- Skrypt `ZbudujGliwice.cs` przeszedł kompilację poza edytorem, w samym Unity nie był jeszcze uruchamiany — jeśli coś wywali, popraw w kodzie edytora, nie w scenie.
- Pliki `.meta` w `_Mapa`/`_Dialogi` wygeneruje Unity przy pierwszym imporcie — zacommituj je od razu, żeby wszyscy mieli te same GUID-y.
- Bruk na Rynku z daleka wygląda gładko; latarnie palą się dopiero w Unity (skrypt lamp).

Licencje: dane mapy © współtwórcy OpenStreetMap (ODbL), tekstury ambientCG (CC0).
