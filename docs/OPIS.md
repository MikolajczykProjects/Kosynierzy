# Sender Gleiwitz 31.08.1939: opis projektu

## Jednym zdaniem
Skradankowa gra FPP, w której jako polski agent wywiadu zdobywasz dowody, że napad na radiostację w Gliwicach był niemiecką prowokacją.

## Pitch (30 sekund)
31 sierpnia 1939 roku Niemcy upozorowali polski atak na radiostację w Gliwicach i następnego dnia użyli go jako pretekstu do wojny. W naszej grze tej nocy jesteś w Gliwicach: Jan Wrona „Kruk”, agent Ekspozytury nr 4. Historii nie zmienisz, bo wojna i tak wybuchnie. Możesz ocalić prawdę: aparatem Leica fotografujesz kolejne dowody, przecinasz kabel w 90 sekund i uciekasz z filmem do Katowic. Jedno trafienie kończy misję.

## Dlaczego ten temat
- To wydarzenie, od którego zaczęła się II wojna światowa, i rozegrało się tuż obok nas. Radiostacja stoi do dziś przy ul. Tarnogórskiej 129.
- Gra uczy przez działanie: gracz sam zbiera dowody kłamstwa, zamiast czytać o nim w podręczniku.
- Karty historii i epilog oddzielają fakty od fikcji.

## Co działa w demie
- Cała misja w 4 aktach z epilogiem, od dworca i hotelu po ucieczkę.
- Mapa centrum Gliwic 1939 na danych OSM: Rynek, ratusz, dworzec, hotel Haus Oberschlesien, kościół, radiostacja z drewnianą wieżą i wnętrza.
- Skradanie jak w Hitmanie: wskaźnik wykrycia, kryjówki, ciche obezwładnienia, reakcje na znalezione ciała i panika cywilów.
- Strzelanie z celowaniem, przeładowaniem i zbrojownią. Broń z epoki: Vis wz. 35, karabin i bagnet.
- Aparat jako mechanika: zdjęcia to dowody, które popychają misję do przodu.
- 21 postaci z animacjami, dialogi PL/DE z napisami, muzyka zmieniająca się z akcją, dźwięki otoczenia.
- Pora dnia sterowana misją, noc od 19:00, punkty kontrolne, menu, ustawienia i ekran porażki.

## Technologia
- Unity 6.6, URP, nowy Input System, NavMesh.
- Mapa i postacie w Blenderze, generowane skryptami w Pythonie z danych OSM, a w Unity składane jednym kliknięciem.
- Głosy nagrane w ElevenLabs, UI v2 zaprojektowane w Figmie.
- Praca zespołu równolegle na jednym repo: każda rola ma własny folder w `Assets/`.

## Zespół Kosynierzy
| Rola | Zakres |
|---|---|
| A: Mapa i dialogi | Mapa Gliwic 1939, dialogi, nagrania, muzyka i dźwięki |
| B: Unity | Rozgrywka, AI, misja, UI, integracja |
| C: Postacie | 21 postaci, animacje, ręce FPP |
| D: Obiekty | Broń, rekwizyty, efekty |

## Zasady treści
Gra nie pokazuje symboli nazistowskich (art. 256 kk) i domyślnie nie ma w niej krwi (opcja „Krew 16+”). Honiok jest pokazany z szacunkiem. Gracz nie może strzelać do cywilów bez konsekwencji.

## Trailer
[`docs/trailer/Sender-Gleiwitz-trailer.mp4`](trailer/Sender-Gleiwitz-trailer.mp4)
