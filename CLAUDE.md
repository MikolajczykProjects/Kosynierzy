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

## Postacie (rola C, paczka od Borysa, poprawki 15.10)
- `Assets/_Postacie/`: 21 postaci `CH_*.fbx` (Humanoid, wspólny szkielet), `CH_Animacje.fbx` (13 klipów: Idle, Chod, Bieg, Kucanie_Idle, Kucanie_Chod, Celowanie, Strzal, Trafiony, Rozmowa, RecePodniesione, Siedzi, Lezy, Spadanie), ręce FPP `FP_*.fbx` (Vis, PPK, Luger, Kar98k, wz29, Bagnet, Noz), atlas w `Materialy/`. Parametry animatora są w `Assets/_Postacie/CZYTAJ.txt`.
- Źródła (Blender i skrypty, NIE do Unity): `SourceArt/Postacie_blender/`. Opis Borysa: `INSTRUKCJA_BORYS.txt`.

### Pierwsze kroki w Unity
1. Poczekaj na import, potem **Postacie → Zbuduj wszystko**. `_Obiekty` musi już być zbudowane, bo ręce FPP biorą `Assets/_Obiekty/Efekty/FX_BlyskLufy.prefab`.
2. Wynik: `Assets/_Postacie/Prefaby/CH_*.prefab`, `FP_<broń>.prefab` (+ `FP_Rece.prefab` = kopia `FP_Vis`), `AC_Czlowiek.controller`, `Animacje/AC_FP_<broń>.controller`. Potem **Kosynierzy → Refresh Prefab Registry**, żeby NPC dostały prawdziwe modele zamiast kapsuł.
3. Jeśli Unity zgłosi błąd Avatara: model → Rig → Configure → Enforce T-Pose.
4. Zacommituj od razu wygenerowane `.meta` i prefaby, żeby wszyscy mieli te same GUID-y.

### Ręce FPP (dziecko kamery gracza, pozycja i obrót 0)
- Broń palna, `AC_FP_<n>`: float `Speed`, bool `Celuje`, trigger `Strzal`, trigger `Przeladuj`. Przy strzale: `Lufa/FX_BlyskLufy` → `ParticleSystem.Play()`.
- Noże (`FP_Bagnet`, `FP_Noz`): float `Speed`, trigger `Atak`, trigger `Pchniecie`.
- Jeśli gra ma już własne viewmodele (np. Vis), te prefaby są opcjonalne i nie trzeba ich podpinać.

### Co zmieniono względem paczki Borysa
- `BudujPostacie.cs`: zamiast jednego `FP_Rece.fbx` jest pętla po wszystkich `FP_*.fbx`, z osobnym kontrolerem i prefabem dla każdej broni. Kompilacja poza edytorem: 0 błędów. W Unity skrypt nie był jeszcze uruchamiany.
- Siatki postaci przebudowane (`ch_cialo3.py`): równe rękawy bez „balona” na bicepsie, grubsze przedramiona i nadgarstki, większe dłonie z kciukiem, pełniejsze nogawki, większe buty, szersza klatka u mężczyzn.
- `CH_Honiok`: usunięta płaska płyta „okrycia”, która w każdej animacji była przyklejona do tułowia. Teraz ma kaszkiet i kamizelkę. Scena z okrytym Honiokiem = klip `Lezy` + osobny rekwizyt w scenie.
- Bez zmian: szkielet, nazwy kości, klipy i parametry animatora.
- Przebudowa z Blendera (Mac/Linux): `Blender -b SourceArt/Postacie_blender/obiekty.blend -P SourceArt/Postacie_blender/zbuduj_postacie_mac.py [-- CH_Kruk ...]`. FBX trafiają do `SourceArt/_Postacie/Modele`, skąd trzeba je skopiować do `Assets/_Postacie/Modele`.

### Znane braki postaci
- NPC z karabinem nie ma osobnej pozy celowania (jest tylko z pistoletu).
- Gniazdo broni NPC `RightHand/Bron_P`: offset pistoletu trzeba dopasować w prefabie.
- Przy przeciąganiu zamka pistoletu lewy rękaw FPP może zasłaniać kadr.

Licencje postaci (napisy końcowe): proporcje mierzone na modelach Sketchfab CC BY 4.0: „Simple Low Poly Character” (PIXELOKAY), „Low Poly Female Base Mesh” (mesh-base).
