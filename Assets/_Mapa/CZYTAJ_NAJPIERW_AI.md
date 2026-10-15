# CZYTAJ NAJPIERW — instrukcja dla AI składającego grę w Unity

## 0) Spis treści
1. Co to jest
2. Jak uruchomić
3. Mapa folderów i plików
4. Układ świata
5. AKTY — tabela
6. Konwencje nazw
7. Jak pracować krok po kroku
8. Czego brakuje
9. Checklista

## 1) Co to jest
Gra na hackaton: „Sender Gleiwitz — 31.08.1939”. Gracz (polski agent „Kruk”) przeżywa noc prowokacji gliwickiej w 4 aktach + epilog: dworzec i hotel Naujocksa → miasto w drodze do radiostacji → sama radiostacja i 90 sekund na przecięcie linii → ucieczka z ciałem „Konserwy” i alarmem. Gra nie zmienia historii — chodzi o zebranie dowodów prawdy. Pełna fabuła: `docs/FABULA.md`.

## 2) Jak uruchomić
1. Otwórz/stwórz projekt Unity w **URP** (Universal Render Pipeline) — materiały i `MapaSwiatlo` są pod URP.
2. Wrzuć cały folder `_Mapa` do `Assets/` projektu.
3. Poczekaj na import FBX i tekstur.
4. Menu **Tools → Zbuduj Gliwice**. Skrypt `Editor/ZbudujGliwice.cs` czyta `Dane/manifest.json` + `Dane/paleta.json` (+ opcjonalnie `Dane/tekstury.json`), tworzy materiały, importuje FBX, składa hierarchię `Mapa_Gliwice` (grupy: Teren, Ulice, Budynki, Natura, Wnetrza, Granice, Punkty, Swiatla), dodaje kolidery, punkty (puste GameObjecty z nazwą i yaw) i światła (`Light` + `MapaLampa`), buduje atmosferę (słońce/skybox/mgła/Volume).
5. Uruchom ponownie po każdej zmianie w `Dane/*.json` lub w modelach — skrypt niszczy i odtwarza `Mapa_Gliwice` od zera (bezpieczny do wielokrotnego klikania).

## 3) Mapa folderów i plików

| Ścieżka | Zawartość |
|---|---|
| `Editor/ZbudujGliwice.cs` | Buduje całą scenę z FBX + manifest.json. Jedyne miejsce, które tworzy `Mapa_Gliwice`. |
| `Skrypty/MapaLampa.cs`, `MapaSwiatlo.cs` | Komponenty runtime: zachowanie lamp (dzień/noc), atmosfera światła globalnego. |
| `Skrypty/PunktyGizmos.cs` | Rysuje gizmo w edytorze dla obiektu `Punkty` (podgląd punktów w scenie). |
| `Dane/manifest.json` | Źródło prawdy o scenie: `warstwy` (72 — teren/ulice/natura/rekwizyty sektorami), `obiekty` (9 — budynki + wnętrza z poz./kat.), `punkty` (145 — nazwane miejsca gry z pozycją i yaw), `swiatla` (413 — lampy). |
| `Dane/paleta.json`, `Dane/tekstury.json` | Definicje materiałów i tekstur (nazwa materiału ↔ pliki `T_*_A/_N`). |
| `Modele/*.fbx` | Geometria: `Budynki_i_j.fbx`, `Natura_i_j.fbx`, `Rekwizyty_i_j.fbx` (sektory 500 m), plus pojedyncze budynki (`Dworzec.fbx`, `Hotel.fbx`, `Kosciol.fbx`, `Rynek.fbx`, `Radiostacja.fbx`) i wnętrza (`Wnetrze_*.fbx`). |
| `Tekstury/T_<nazwa>_A.png` / `_N.png` | Albedo + normal per materiał (cegła, tynk, dachówka, bruk, drewno, trawa, itd.). |
| `Dane/akty.json` | Przypisanie obiektów/punktów do aktów 1–4 + epilog. |
| `Skrypty/AktZnacznik.cs` | Komponent `int[] akty`, `string rola`, `string opis_dla_ai` — wpinany na obiekty istotne fabularnie. |

## 4) Układ świata
- Jednostki: **1 jednostka Unity = 1 metr**. **Y w górę** (standard Unity).
- Miasto właściwe leży w okolicach współrzędnych mapy 0–1500 w płaszczyźnie X/Z (patrz pozycje obiektów w manifest.json, np. Rynek ok. (−34,−3), Dworzec ok. (718,743), Radiostacja ok. (1012,1517)).
- **Wnętrza** (`STREFA_WNETRZ`, pod grupą `Wnetrza`) są fizycznie odsunięte na bok mapy: **X = 4000…4600**, osobne, nie stykają się z miastem. To celowe — unika się nakładania kolizji z zewnętrzem.
- Wejście do wnętrza = **teleport/ładowanie sceny-sekcji** z punktu „drzwi” w mieście (np. `miasto_drzwi_hotel`) do punktu startowego wewnątrz odpowiedniego `Wnetrze_*`. To nie jest przejście fizyczne przez drzwi — logika gry ma przestawić gracza.

## 5) AKTY — lokacje, obiekty, punkty (wg FABULA.md; ostateczne wiązanie w `Dane/akty.json`)

| Akt | Lokacja | Kluczowe obiekty | Punkty startu/celu (orientacyjnie z manifestu) |
|---|---|---|---|
| 1 — Dworzec i hotel (15:30) | `Dworzec`, `Hotel`, `Wnetrze_Hotel`, `Wnetrze_Ksiegarnia` | Dworzec, Hotel, Fontanna_Fauny, wnętrze hotelu (pokój Naujocksa, hol z hasłem) | `spawn_dworzec`, `miasto_drzwi_hotel`, punkty wewnątrz hotelu (do zweryfikowania w akty.json) |
| 2 — Przez miasto (16:00–19:45) | Ulice, Ring, zagajnik, most | Rekwizyty uliczne, auto SD, Karty historii (6 szt.), skrytka pod mostem, zagajnik | `miasto_auto_sd_trasa_1..5`, `miasto_skrytka_most`, `miasto_most_zejscie`, `miasto_zagajnik_auto`, `miasto_karta_1..6`, `miasto_tlum_*` |
| 3 — Radiostacja (20:00) | `Radiostacja`, `Wnetrze_Radio` | wnętrze stacji: studio, mikrofon burzowy, wzmacniacz (90 s na przecięcie linii), piwnica z pracownikami | punkty wewnątrz `Wnetrze_Radio` (do zweryfikowania w akty.json) |
| 4 — Konserwa i ucieczka (20:10) | okolice radiostacji → wschód, stodoła kuriera | ciało Honioka + kartka, trasa pościgu | punkty wschodnie (stodoła — patrz `gliwice_geo.STODOLA` w generatorze mapy, nie w tym folderze) |
| Epilog | — | brak geometrii w mapie — ekran tekstowy/slajdy | brak punktów w świecie 3D |

Dokładne, wiążące przypisanie obiekt→akt i punkt→akt jest w `Dane/akty.json` — traktuj powyższą tabelę jako mapę orientacyjną z fabuły, a `akty.json` jako źródło prawdy do kodu.

## 6) Konwencje nazw
- **`AKTx_`** prefiks (np. `AKT1_Hotel`, `AKT3_Radiostacja`) — oznacza obiekt istotny dla danego aktu/aktów. Obiekt może mieć komponent `AktZnacznik` z `int[] akty` (może należeć do kilku aktów), `rola` (np. "cel", "przeszkoda", "dekoracja_fabularna") i `opis_dla_ai` (krótki opis po polsku do czego służy — czytaj go zamiast zgadywać).
- Wnętrza pod `STREFA_WNETRZ`: nazwy typu **`AKT1_Wnetrze_Hotel`**, **`AKT3_Wnetrze_Radio`** — prefiks aktu + `Wnetrze_` + nazwa lokacji.
- **Punkty** (`Punkty/<nazwa>`): `spawn_*` = miejsce startu gracza, `miasto_*` = punkt w świecie miasta (NPC, cel, trasa, karta kolekcjonerska), `wnetrze_*` (jeśli występuje) = punkt wewnątrz strefy wnętrz. Yaw w stopniach, 0° = patrzy w lokalny +Y/+Z zgodnie z generatorem mapy (nie zmieniaj ręcznie bez potrzeby).
- **`_COL`** sufiks na obiekcie/submeshu = ma/powinien mieć kolizję osobno od wizualnej siatki (drobne detale pomijane w kolizji są bez sufiksu lub oznaczone `_NOCOL`).
- **`M_`** prefiks materiałów (tworzonych w `ZbudujMaterialy`), odpowiadają wpisom w `paleta.json`.
- **`T_<nazwa>_A.png`** = albedo, **`T_<nazwa>_N.png`** = normal map tej samej tekstury.

## 7) Jak AI ma pracować krok po kroku
1. Przeczytaj `Dane/akty.json` i `Skrypty/AktZnacznik.cs`, żeby wiedzieć co do którego aktu należy — nie zgaduj z nazw.
2. **Nie przesuwaj, nie skaluj, nie importuj ponownie ręcznie żadnego FBX z `Modele/`** ani nie edytuj plików w `_Mapa/`. Cała geometria i jej pozycje są generowane z `manifest.json` przez `ZbudujGliwice.cs` — zmiana w edytorze zniknie przy ponownym „Zbuduj Gliwice”.
3. Jeśli coś w mapie trzeba poprawić (pozycja, brakujący punkt), zgłoś to jako poprawkę do generatora/manifestu, nie rób tego ręcznie w scenie.
4. Całą logikę gry (ruch gracza, AI strażników, system aktów, UI, dialogi, save) twórz w **osobnym folderze `Assets/_Gra/`** (stwórz go, jeśli nie istnieje). `_Mapa` jest tylko danymi świata — traktuj go jak reimportowalny asset.
5. Dowiązania do obiektów mapy rób przez nazwę/tag/komponent (`AktZnacznik`, `PunktyGizmos`), nie przez twarde referencje do konkretnych instancji w scenie, bo mapa bywa przebudowywana.
6. Testuj po każdym „Zbuduj Gliwice” — sprawdź w konsoli liczbę obiektów/kolideli/punktów/świateł (log na końcu buildu) i warningi o punktach bez podłoża.

## 8) Czego brakuje (stan na dziś)
- Wyposażenie wnętrza radiostacji (sprzęt, mikrofon burzowy, wzmacniacz jako interaktywny obiekt z minigrą 90 s) — tylko pusta bryła `Wnetrze_Radio`.
- Postacie/NPC (Naujocks, Heinz, Paul Gruber, pani Zofia, kurier „Sowa”, cywile, Schupo) — brak modeli i AI.
- Dźwięk/dialogi — teksty są w `Assets/_Dialogi/` (`dialogi.json`, `teksty_gry.json`); pliki audio z ElevenLabs dojdą osobno, nazwy jak `a1_zofia_01` — podłączyć jako AudioClip, gdy dotrą.
- Ludzie/postacie na ulicach, zapalone latarnie widać dopiero w Unity (robi je `ZbudujGliwice.cs`).

## 9) Szybka checklista
- [ ] Projekt URP, `_Mapa` w `Assets/`
- [ ] Tools → Zbuduj Gliwice bez błędów w konsoli
- [ ] `Dane/akty.json` + `AktZnacznik.cs` obecne i przeczytane
- [ ] Logika gry wyłącznie w `Assets/_Gra/`, zero ręcznych zmian w `_Mapa`
- [ ] Punkty startowe/celowe każdego aktu zidentyfikowane po nazwie z `akty.json`
- [ ] Wnętrza (X=4000–4600) traktowane jako osobne „pokoje” ładowane przez teleport z punktów drzwi w mieście

