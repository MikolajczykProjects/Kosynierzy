# SENDER GLEIWITZ 31.08.1939 — PEŁNY MANIFEST DIALOGÓW I NARRACJI

Dokumentacja przygotowana specjalnie pod wdrożenie w Unity 6 dla programisty lub modelu AI (ROLA B — UNITY).

## 1. Jak Unity odtwarza dialogi?
- **Pliki audio:** Umieszczone w folderze `Assets/_Dialogi/Resources/Audio/` (pliki `.mp3`).
- **Ładowanie w C#:** `Resources.Load<AudioClip>("Audio/" + linia.audio)`
- **Zasada wyświetlania:** Jeśli `czy_to_mysl_wewnetrzna == true`, napis wyświetlany jest kursywą, postać nie rusza ustami, a audio brzmi jak monolog wewnętrzny.
- **Zasada napisów:** Język mówiony w pliku audio może być niemiecki lub gwarowy, ale pole `napis_na_ekranie_pl` ZAWSZE zawiera pełne tłumaczenie po polsku dla gracza.

---

## 2. Katalog Scen i Kwestii Dialogowych

### Scena: `a1_intro`
- **Akt i czas:** Akt 1 (15:30) Dworzec i hotel
- **Lokacja w grze:** Dworzec kolejowy Bahnhof Gleiwitz, peron
- **Punkt w prefabie Mapy Unity:** `spawn_dworzec`
- **Wyzwalacz sceny (Trigger):** Automatycznie po załadowaniu sceny i spawnie gracza
- **Akcja po zakończeniu:** Ustawienie celu w HUD: 'Idź do księgarni na Ringu'
- **Notatka reżyserska:** *Młody chłopak wysiada z pociągu w upalny dzień. Stara się zachować spokój, ale czuje presję samotnej misji.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a1_intro_01.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Gliwice. Wpół do czwartej, a upał jak w piecu. | Gliwice. Wpół do czwartej, a upał jak w piecu. | TAK |
| `a1_intro_02.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Po tej stronie granicy jestem Johann Krause, kupiec z Bytomia. | Po tej stronie granicy jestem Johann Krause, kupiec z Bytomia. | TAK |
| `a1_intro_03.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Centrala wie, że Niemcy coś szykują. Nie wie, gdzie ani kiedy. | Centrala wie, że Niemcy coś szykują. Nie wie, gdzie ani kiedy. | TAK |
| `a1_intro_04.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Najpierw księgarnia na Ringu. Spokojnie. Kupiec się nie spieszy. | Najpierw księgarnia na Ringu. Spokojnie. Kupiec się nie spieszy. | TAK |

---

### Scena: `pob_kolejarz`
- **Akt i czas:** Akt 1 (15:35) Dworzec - misja poboczna
- **Lokacja w grze:** Peron dworca przy lokomotywie
- **Punkt w prefabie Mapy Unity:** `miasto_kolejarz`
- **Wyzwalacz sceny (Trigger):** Klawisz [E] przy postaci kolejarza
- **Akcja po zakończeniu:** Otrzymanie rozkładu transportów wojskowych, wpis do dziennika
- **Notatka reżyserska:** *Gwara śląska, ściszony głos za dymem lokomotywy, lęk przed patrolami Schupo.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `pob_kolejarz_01.mp3` | **kolejarz** | Kolejarz do Kruka / Kruk do Kolejarza | Panoczku, niy stójcie tak na widoku. Podejdźcie bliżej. | Panie, nie stójcie tak na widoku. Podejdźcie bliżej. | NIE |
| `pob_kolejarz_02.mp3` | **kolejarz** | Kolejarz do Kruka / Kruk do Kolejarza | Kożdo noc jadom tu cugi z wojskiym na wschód. Armaty pod plandekami. | Co noc jadą tu pociągi z wojskiem na wschód. Armaty pod plandekami. | NIE |
| `pob_kolejarz_03.mp3` | **kolejarz** | Kolejarz do Kruka / Kruk do Kolejarza | Mom rozpiska, kiedy kery jedzie. Weź pan, ino cicho, bo mie zawrom. | Mam rozkład, kiedy który jedzie. Weź pan, tylko cicho, bo mnie zamkną. | NIE |
| `pob_kolejarz_04.mp3` | **kruk** | Kolejarz do Kruka / Kruk do Kolejarza | Nikt się nie dowie. Dziękuję. | Nikt się nie dowie. Dziękuję. | NIE |
| `pob_kolejarz_05.mp3` | **kolejarz** | Kolejarz do Kruka / Kruk do Kolejarza | Niy dziynkujcie. Jo tu mom dzieci. Idźcie z Bogiym. | Nie dziękujcie. Ja tu mam dzieci. Idźcie z Bogiem. | NIE |

---

### Scena: `a1_zofia`
- **Akt i czas:** Akt 1 (15:45) Księgarnia na Ringu
- **Lokacja w grze:** Księgarnia na Ringu, zaplecze za regałami
- **Punkt w prefabie Mapy Unity:** `ksiegarnia_zofia`
- **Wyzwalacz sceny (Trigger):** Interakcja [E] z panią Zofią w księgarni
- **Akcja po zakończeniu:** Gracz otrzymuje aparat fotograficzny Leica; cel misji: 'Zbadaj hotel Haus Oberschlesien'
- **Notatka reżyserska:** *Konspiracyjny półszept, matczyna opiekuńczość i lęk przed obserwowaniem lokalu przez Gestapo.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a1_zofia_01.mp3` | **kruk** | Zofia do Kruka / Kruk do Zofii | Haben Sie Eichendorff? Die Ausgabe von neunzehnhundertzwölf. | Ma pani Eichendorffa? Wydanie z 1912 roku. | NIE |
| `a1_zofia_02.mp3` | **zofia** | Zofia do Kruka / Kruk do Zofii | Nur die von neunzehnhundertzwanzig. Die ältere ist längst vergriffen. | Tylko z 1920. Starsze dawno się wyczerpało. | NIE |
| `a1_zofia_03.mp3` | **zofia** | Zofia do Kruka / Kruk do Zofii | Dobrze, że pan jest. Mówmy cicho, tutaj ściany mają uszy. | Dobrze, że pan jest. Mówmy cicho, tutaj ściany mają uszy. | NIE |
| `a1_zofia_04.mp3` | **zofia** | Zofia do Kruka / Kruk do Zofii | Od kilku dni w Haus Oberschlesien na Wilhelmstraße mieszkają inżynierowie z Berlina. | Od kilku dni w Haus Oberschlesien na Wilhelmstraße mieszkają „inżynierowie z Berlina”. | NIE |
| `a1_zofia_05.mp3` | **zofia** | Zofia do Kruka / Kruk do Zofii | Nie wyglądają na inżynierów. Ich szef nazywa się Naujocks. | Nie wyglądają na inżynierów. Ich szef nazywa się Naujocks. | NIE |
| `a1_zofia_06.mp3` | **kruk** | Zofia do Kruka / Kruk do Zofii | Gdzie mają pokój? | Gdzie mają pokój? | NIE |
| `a1_zofia_07.mp3` | **zofia** | Zofia do Kruka / Kruk do Zofii | Na piętrze. A w holu stoi telefon. Berlin dzwoni na niego bez przerwy. | Na piętrze. A w holu stoi telefon. Berlin dzwoni na niego bez przerwy. | NIE |
| `a1_zofia_08.mp3` | **zofia** | Zofia do Kruka / Kruk do Zofii | Niech pan to weźmie. Leica, film założony. | Niech pan to weźmie. Leica, film założony. | NIE |
| `a1_zofia_09.mp3` | **zofia** | Zofia do Kruka / Kruk do Zofii | Słowu nikt nie uwierzy. Zdjęciu tak. | Słowu nikt nie uwierzy. Zdjęciu tak. | NIE |
| `a1_zofia_10.mp3` | **kruk** | Zofia do Kruka / Kruk do Zofii | Oddam pani aparat. | Oddam pani aparat. | NIE |
| `a1_zofia_11.mp3` | **zofia** | Zofia do Kruka / Kruk do Zofii | Aparat jest najmniej ważny. Niech pan wróci z filmem. | Aparat jest najmniej ważny. Niech pan wróci z filmem. | NIE |

---

### Scena: `a1_recepcja`
- **Akt i czas:** Akt 1 (16:00) Hotel Haus Oberschlesien
- **Lokacja w grze:** Hol hotelu, lada recepcji
- **Punkt w prefabie Mapy Unity:** `hotel_recepcja`
- **Wyzwalacz sceny (Trigger):** Podejście do lady recepcji w holu
- **Akcja po zakończeniu:** Odblokowanie schodów na piętro do pokoju Naujocksa
- **Notatka reżyserska:** *Elegancki, formalny język niemiecki, dystyngowany recepcjonista kontra wyluzowany kupiec Krause.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a1_recepcja_01.mp3` | **recepcjonista** | Recepcjonista do Kruka / Kruk do Recepcjonisty | Guten Tag. Wünschen der Herr ein Zimmer? | Dzień dobry. Czy szanowny pan życzy sobie pokój? | NIE |
| `a1_recepcja_02.mp3` | **kruk** | Recepcjonista do Kruka / Kruk do Recepcjonisty | Nein, danke. Ich bin hier mit einem Geschäftsfreund verabredet. | Nie, dziękuję. Jestem tu umówiony ze wspólnikiem. | NIE |
| `a1_recepcja_03.mp3` | **recepcjonista** | Recepcjonista do Kruka / Kruk do Recepcjonisty | Dann darf ich an die Bar bitten. Gleich links, mein Herr. | W takim razie zapraszam do baru. Zaraz na lewo, proszę pana. | NIE |
| `a1_recepcja_04.mp3` | **recepcjonista** | Recepcjonista do Kruka / Kruk do Recepcjonisty | Verzeihen Sie die Unruhe heute. Wir haben Herren aus Berlin im Haus. | Proszę wybaczyć dzisiejszy ruch. Mamy gości z Berlina. | NIE |
| `a1_recepcja_05.mp3` | **kruk** | Recepcjonista do Kruka / Kruk do Recepcjonisty | Ich habe Zeit. Danke sehr. | Mam czas. Dziękuję bardzo. | NIE |

---

### Scena: `a1_mysl_mapa`
- **Akt i czas:** Akt 1 (16:10) Pokój Naujocksa
- **Lokacja w grze:** Piętro hotelu, pokój Naujocksa, biurko
- **Punkt w prefabie Mapy Unity:** `hotel_pokoj_mapa`
- **Wyzwalacz sceny (Trigger):** Zrobienie zdjęcia mapy aparatem Leica [PPM -> LPM]
- **Akcja po zakończeniu:** Zaliczenie dowodu 1/6, cel: 'Odszukaj listę zaopatrzenia'
- **Notatka reżyserska:** *Szybki, przyciszony oddech, świadomość bycia w jaskini lwa, dłonie nie mogą drżeć.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a1_mysl_mapa_01.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Plan Gliwic. Radiostacja przy Tarnowitzer Landstraße, zakreślona na czerwono. | Plan Gliwic. Radiostacja przy Tarnowitzer Landstraße, zakreślona na czerwono. | TAK |
| `a1_mysl_mapa_02.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Obok ołówkiem: dwudziesta. Tylko dzisiaj czy jutro? | Obok ołówkiem: 20:00. Tylko dzisiaj czy jutro? | TAK |
| `a1_mysl_mapa_03.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Zdjęcie. Ręce nie mogą drżeć. | Zdjęcie. Ręce nie mogą drżeć. | TAK |

---

### Scena: `a1_mysl_lista`
- **Akt i czas:** Akt 1 (16:12) Pokój Naujocksa
- **Lokacja w grze:** Piętro hotelu, pokój Naujocksa, stolik
- **Punkt w prefabie Mapy Unity:** `hotel_pokoj_lista`
- **Wyzwalacz sceny (Trigger):** Zrobienie zdjęcia listy zaopatrzenia
- **Akcja po zakończeniu:** Zaliczenie dowodu 2/6, cel: 'Zejdź do holu i podsłuchaj rozmowę'
- **Notatka reżyserska:** *Zrozumienie intrygi: niemieccy dywersanci przebierają się za Polaków.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a1_mysl_lista_01.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Lista zaopatrzenia. Polskie ubrania cywilne, siedem kompletów. | Lista zaopatrzenia. „Polskie ubrania cywilne, siedem kompletów”. | TAK |
| `a1_mysl_lista_02.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Po co inżynierom z Berlina polskie ubrania? | Po co inżynierom z Berlina polskie ubrania? | TAK |
| `a1_mysl_lista_03.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Chcą, żeby to wyglądało na nas. | Chcą, żeby to wyglądało na nas. | TAK |

---

### Scena: `a1_telefon`
- **Akt i czas:** Akt 1 (16:20) Hol hotelu
- **Lokacja w grze:** Hol hotelu, budka telefoniczna na ścianie
- **Punkt w prefabie Mapy Unity:** `hotel_telefon`
- **Wyzwalacz sceny (Trigger):** Gracz kuca za kolumną hotel_kolumna
- **Akcja po zakończeniu:** Rozpoczęcie sceny a1_mysl_haslo, Naujocks wychodzi z hotelu
- **Notatka reżyserska:** *Lodowaty, służbisty pruski ton Naujocksa, krótkie odpowiedzi, padają słowa Großmutter gestorben.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a1_telefon_01.mp3` | **naujocks** | Naujocks do Heydricha (w słuchawce telefonu) | Naujocks. | Naujocks. | NIE |
| `a1_telefon_02.mp3` | **naujocks** | Naujocks do Heydricha (w słuchawce telefonu) | Ja, ich höre. | Tak, słucham. | NIE |
| `a1_telefon_03.mp3` | **naujocks** | Naujocks do Heydricha (w słuchawce telefonu) | Bitte wiederholen. | Proszę powtórzyć. | NIE |
| `a1_telefon_04.mp3` | **naujocks** | Naujocks do Heydricha (w słuchawce telefonu) | Großmutter gestorben. Verstanden. | Babcia umarła. Zrozumiałem. | NIE |
| `a1_telefon_05.mp3` | **naujocks** | Naujocks do Heydricha (w słuchawce telefonu) | Heute Abend, wie besprochen. Meine Leute sind bereit. | Dziś wieczorem, jak ustalono. Moi ludzie są gotowi. | NIE |

---

### Scena: `a1_mysl_haslo`
- **Akt i czas:** Akt 1 (16:22) Hol hotelu
- **Lokacja w grze:** Hol hotelu za kolumną
- **Punkt w prefabie Mapy Unity:** `hotel_kolumna`
- **Wyzwalacz sceny (Trigger):** Po odłożeniu słuchawki przez Naujocksa
- **Akcja po zakończeniu:** Koniec Aktu 1, przejście do Aktu 2: 'Śledź auto SD na północ'
- **Notatka reżyserska:** *Kruk łączy fakty: hasło oznacza start prowokacji dziś o 20:00.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a1_mysl_haslo_01.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Babcia umarła. Nikt nie odbiera takiej wiadomości tak spokojnie. | Babcia umarła. Nikt nie odbiera takiej wiadomości tak spokojnie. | TAK |
| `a1_mysl_haslo_02.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | To hasło. Zaczynają dziś. Na mapie była dwudziesta. | To hasło. Zaczynają dziś. Na mapie była 20:00. | TAK |

---

### Scena: `a2_start`
- **Akt i czas:** Akt 2 (16:30) Przez miasto
- **Lokacja w grze:** Wilhelmstraße przed hotelem
- **Punkt w prefabie Mapy Unity:** `miasto_drzwi_hotel`
- **Wyzwalacz sceny (Trigger):** Wyjście z hotelu na ulicę, start auta SD
- **Akcja po zakończeniu:** Rozpoczęcie sekwencji śledzenia auta SD (dystans 8-40 m)
- **Notatka reżyserska:** *Czujność, wtopienie się w tłum przechodniów.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a2_start_01.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Czarne auto spod hotelu. Rusza na północ. | Czarne auto spod hotelu. Rusza na północ. | TAK |
| `a2_start_02.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Trzymam się za nim. Nie za blisko. | Trzymam się za nim. Nie za blisko. | TAK |

---

### Scena: `pob_fotograf_przed`
- **Akt i czas:** Akt 2 (17:00) Studio fotograficzne
- **Lokacja w grze:** Zakład fotograficzny na Ringu
- **Punkt w prefabie Mapy Unity:** `spawn_fotograf`
- **Wyzwalacz sceny (Trigger):** Wejście do zakładu fotograficznego
- **Akcja po zakończeniu:** Wejście agenta Gestapo do lokalu
- **Notatka reżyserska:** *Fotograf jest spięty, przeczuwa kłopoty.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `pob_fotograf_przed_01.mp3` | **fotograf** | Fotograf do Kruka | Niech pan mówi ciszej. Na zapleczu siedzi jeden z Gestapo. | Niech pan mówi ciszej. Na zapleczu siedzi jeden z Gestapo. | NIE |
| `pob_fotograf_przed_02.mp3` | **fotograf** | Fotograf do Kruka | Pilnuje mnie od rana. Myśli, że robię odbitki dla Polaków. | Pilnuje mnie od rana. Myśli, że robię odbitki dla Polaków. | NIE |
| `pob_fotograf_przed_03.mp3` | **fotograf** | Fotograf do Kruka | I ma rację. Niech pan uważa, idzie tutaj. | I ma rację. Niech pan uważa, idzie tutaj. | NIE |

---

### Scena: `pob_fotograf_gestapo`
- **Akt i czas:** Akt 2 (17:02) Studio fotograficzne
- **Lokacja w grze:** Zakład fotograficzny na Ringu
- **Punkt w prefabie Mapy Unity:** `fotograf_gestapo`
- **Wyzwalacz sceny (Trigger):** Dialog automatyczny w zakładzie fotografa
- **Akcja po zakończeniu:** Gestapo odchodzi przekonane o tożsamości kupca z Bytomia
- **Notatka reżyserska:** *Cyniczne przesłuchanie z drapieżnym spokojem; Kruk blefuje śląskim akcentem.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `pob_fotograf_gestapo_01.mp3` | **gestapo** | Gestapo do Kruka / Kruk do Gestapo | Moment mal. Sie da. Ihre Papiere. | Chwileczkę. Pan tam. Dokumenty. | NIE |
| `pob_fotograf_gestapo_02.mp3` | **kruk** | Gestapo do Kruka / Kruk do Gestapo | Bitte sehr. Krause, Johann. Kaufmann aus Beuthen. | Proszę bardzo. Krause, Johann. Kupiec z Bytomia. | NIE |
| `pob_fotograf_gestapo_03.mp3` | **gestapo** | Gestapo do Kruka / Kruk do Gestapo | Und was sucht ein Kaufmann aus Beuthen beim Fotografen in Gleiwitz? | A czego szuka kupiec z Bytomia u fotografa w Gliwicach? | NIE |
| `pob_fotograf_gestapo_04.mp3` | **kruk** | Gestapo do Kruka / Kruk do Gestapo | Ich hole Bilder ab. Von der Hochzeit meiner Schwester. | Odbieram zdjęcia. Ze ślubu mojej siostry. | NIE |
| `pob_fotograf_gestapo_05.mp3` | **gestapo** | Gestapo do Kruka / Kruk do Gestapo | Sie sprechen wie ein Pole. | Mówi pan jak Polak. | NIE |
| `pob_fotograf_gestapo_06.mp3` | **kruk** | Gestapo do Kruka / Kruk do Gestapo | Ich spreche wie ein Oberschlesier. Wie die halbe Stadt. | Mówię jak Górnoślązak. Jak pół miasta. | NIE |
| `pob_fotograf_gestapo_07.mp3` | **gestapo** | Gestapo do Kruka / Kruk do Gestapo | Na gut. Gehen Sie. Und halten Sie sich von Ärger fern. | No dobrze. Proszę iść. I trzymać się z dala od kłopotów. | NIE |

---

### Scena: `pob_fotograf_po`
- **Akt i czas:** Akt 2 (17:05) Studio fotograficzne
- **Lokacja w grze:** Ciemnia fotograficzna na zapleczu
- **Punkt w prefabie Mapy Unity:** `fotograf_ciemnia`
- **Wyzwalacz sceny (Trigger):** Przejście za ladę do ciemni
- **Akcja po zakończeniu:** Zabezpieczenie zapasowej kopii kliszy
- **Notatka reżyserska:** *Cichy szept przy czerwonym świetle ciemni.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `pob_fotograf_po_01.mp3` | **fotograf** | Fotograf do Kruka / Kruk do Fotografa | Poszedł. Myślałem, że już po nas. | Poszedł. Myślałem, że już po nas. | NIE |
| `pob_fotograf_po_02.mp3` | **fotograf** | Fotograf do Kruka / Kruk do Fotografa | Wywołam to w ciemni. Po dwie odbitki z każdej klatki. | Wywołam to w ciemni. Po dwie odbitki z każdej klatki. | NIE |
| `pob_fotograf_po_03.mp3` | **kruk** | Fotograf do Kruka / Kruk do Fotografa | Jedna kopia zostaje u pana. Gdyby mnie złapali. | Jedna kopia zostaje u pana. Gdyby mnie złapali. | NIE |
| `pob_fotograf_po_04.mp3` | **fotograf** | Fotograf do Kruka / Kruk do Fotografa | Schowam ją za ramą portretu ślubnego. Tam nikt nie zagląda. | Schowam ją za ramą portretu ślubnego. Tam nikt nie zagląda. | NIE |

---

### Scena: `pob_skrytka`
- **Akt i czas:** Akt 2 (17:30) Rzeka Kłodnica
- **Lokacja w grze:** Pod mostem przy Tarnowitzer Landstraße
- **Punkt w prefabie Mapy Unity:** `miasto_skrytka_most`
- **Wyzwalacz sceny (Trigger):** Otwarcie skrzynki OB_Skrytka pod mostem
- **Akcja po zakończeniu:** Dodanie 8 nabojów do pistoletu Vis wz. 35
- **Notatka reżyserska:** *Ulga po znalezieniu skrytki wywiadu, nadzieja, że broń nie będzie potrzebna.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `pob_skrytka_01.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Skrytka pod mostem na Kłodnicy. Trzeci kamień od lewej, tak jak mówili w Katowicach. | Skrytka pod mostem na Kłodnicy. Trzeci kamień od lewej, tak jak mówili w Katowicach. | TAK |
| `pob_skrytka_02.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Osiem naboi do Visa. Oby zostały w magazynku. | Osiem naboi do Visa. Oby zostały w magazynku. | TAK |

---

### Scena: `a2_zmrok`
- **Akt i czas:** Akt 2 (19:00) Zmrok w mieście
- **Lokacja w grze:** Tarnowitzer Landstraße, północ Gliwic
- **Punkt w prefabie Mapy Unity:** `miasto_auto_sd_trasa_3`
- **Wyzwalacz sceny (Trigger):** Godzina 19:00 w zegarze gry, włączenie latarni ulicznych
- **Akcja po zakończeniu:** Zmniejszenie zasięgu wzroku wrogów w cieniu, latarnie stają się źródłem ryzyka
- **Notatka reżyserska:** *Niepokój, zmierzch, narastający chłód i świadomość uciekającego czasu.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a2_zmrok_01.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Dziewiętnasta. Zmierzcha, a powietrze dalej stoi. | 19:00. Zmierzcha, a powietrze dalej stoi. | TAK |
| `a2_zmrok_02.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Zapalają latarnie. Każda to świadek. Trzymam się cienia. | Zapalają latarnie. Każda to świadek. Trzymam się cienia. | TAK |

---

### Scena: `a2_auto_daleko`
- **Akt i czas:** Akt 2 (16:30-19:30) Śledzenie auta
- **Lokacja w grze:** Ulica w Gliwicach
- **Punkt w prefabie Mapy Unity:** `miasto_auto_sd_trasa_1`
- **Wyzwalacz sceny (Trigger):** Dystans gracza do auta SD przekracza 40 m
- **Akcja po zakończeniu:** Ostrzeżenie na ekranie, ryzyko zerwania śledzenia
- **Notatka reżyserska:** *Nerwowe ponaglenie samego siebie.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a2_auto_daleko_01.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Tracę ich. Szybciej, zanim skręcą. | Tracę ich. Szybciej, zanim skręcą. | TAK |
| `a2_auto_daleko_02.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Gdzie to auto? Bez niego nie znajdę reszty. | Gdzie to auto? Bez niego nie znajdę reszty. | TAK |

---

### Scena: `a2_auto_blisko`
- **Akt i czas:** Akt 2 (16:30-19:30) Śledzenie auta
- **Lokacja w grze:** Ulica w Gliwicach
- **Punkt w prefabie Mapy Unity:** `miasto_auto_sd_trasa_2`
- **Wyzwalacz sceny (Trigger):** Dystans gracza do auta SD spada poniżej 8 m
- **Akcja po zakończeniu:** Pasek podejrzenia kierowcy rośnie
- **Notatka reżyserska:** *Przymuszenie się do zwolnienia kroku.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a2_auto_blisko_01.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Za blisko. Kierowca patrzy w lusterko. | Za blisko. Kierowca patrzy w lusterko. | TAK |
| `a2_auto_blisko_02.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Zwolnij. Kupiec z Bytomia nie goni samochodów. | Zwolnij. Kupiec z Bytomia nie goni samochodów. | TAK |

---

### Scena: `a2_most`
- **Akt i czas:** Akt 2 (19:15) Most na Kłodnicy
- **Lokacja w grze:** Most przy Tarnowitzer Landstraße
- **Punkt w prefabie Mapy Unity:** `miasto_skrytka_most`
- **Wyzwalacz sceny (Trigger):** Wejście na most, widok na 111-metrową wieżę
- **Akcja po zakończeniu:** Aktualizacja celu: 'Dotrzyj w rejon radiostacji przed 20:00'
- **Notatka reżyserska:** *Widok drewnianej wieży dominującej nad horyzontem.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a2_most_01.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Kłodnica. Nad dachami wieża radiostacji, ponad sto metrów drewna. | Kłodnica. Nad dachami wieża radiostacji, ponad sto metrów drewna. | TAK |
| `a2_most_02.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Do ósmej zostało niewiele. | Do ósmej zostało niewiele. | TAK |

---

### Scena: `a2_zagajnik`
- **Akt i czas:** Akt 2 (19:40) Zagajnik przy radiostacji
- **Lokacja w grze:** Zagajnik na zachód od radiostacji
- **Punkt w prefabie Mapy Unity:** `miasto_zagajnik_auto`
- **Wyzwalacz sceny (Trigger):** Gracz dociera do punktu miasto_zagajnik_krzaki i kuca
- **Akcja po zakończeniu:** Rozpoczęcie sceny a2_mysl_konserwa, możliwość zrobienia zdjęcia
- **Notatka reżyserska:** *Niemieccy żołnierze wyciągają z auta nieprzytomnego Honioka. Szorstkie dialogi.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a2_zagajnik_01.mp3` | **sd** | Żołnierze SD do Naujocksa i między sobą | Er ist bewusstlos. Wohin mit ihm? | Jest nieprzytomny. Gdzie go dać? | NIE |
| `a2_zagajnik_02.mp3` | **naujocks** | Żołnierze SD do Naujocksa i między sobą | Unsere Konserve. Er kommt später zum Eingang. | Nasza konserwa. Później pójdzie pod wejście. | NIE |
| `a2_zagajnik_03.mp3` | **naujocks** | Żołnierze SD do Naujocksa i między sobą | Jetzt umziehen. Die Zivilsachen liegen im Wagen. | Teraz się przebrać. Cywilne rzeczy leżą w aucie. | NIE |
| `a2_zagajnik_04.mp3` | **naujocks** | Żołnierze SD do Naujocksa i między sobą | Ab jetzt seid ihr Polen. Wer Polnisch kann, ruft. Die anderen schweigen. | Od teraz jesteście Polakami. Kto zna polski, krzyczy. Reszta milczy. | NIE |
| `a2_zagajnik_05.mp3` | **sd** | Żołnierze SD do Naujocksa i między sobą | Und wenn uns jemand sieht? | A jeśli ktoś nas zobaczy? | NIE |
| `a2_zagajnik_06.mp3` | **naujocks** | Żołnierze SD do Naujocksa i między sobą | Dann hat er Polen gesehen. Um acht sind wir drin. | To widział Polaków. O ósmej jesteśmy w środku. | NIE |

---

### Scena: `a2_mysl_konserwa`
- **Akt i czas:** Akt 2 (19:45) Zagajnik przy radiostacji
- **Lokacja w grze:** Zagajnik w krzakach
- **Punkt w prefabie Mapy Unity:** `miasto_zagajnik_krzaki`
- **Wyzwalacz sceny (Trigger):** Zrobienie zdjęcia auta z nieprzytomnym Honiokiem
- **Akcja po zakończeniu:** Zaliczenie dowodu 3/6, przejście do Aktu 3: 'Przedostań się do radiostacji'
- **Notatka reżyserska:** *Głęboki szok i bezsilność: Kruk widzi człowieka przeznaczonego na rzeź, ale nie może go odbić w pojedynkę.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a2_mysl_konserwa_01.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Przywieźli go nieprzytomnego. To nie żołnierz, to ktoś stąd. | Przywieźli go nieprzytomnego. To nie żołnierz, to ktoś stąd. | TAK |
| `a2_mysl_konserwa_02.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Ich jest sześciu, ja jeden. Nie odbiję go. | Ich jest sześciu, ja jeden. Nie odbiję go. | TAK |
| `a2_mysl_konserwa_03.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Zostaje mi zdjęcie. Żeby kiedyś ktoś zapytał, kim był. | Zostaje mi zdjęcie. Żeby kiedyś ktoś zapytał, kim był. | TAK |

---

### Scena: `a1_hotel_goscie`
- **Akt i czas:** Akt 1 (16:00) Hol hotelu
- **Lokacja w grze:** Hol hotelu Haus Oberschlesien
- **Punkt w prefabie Mapy Unity:** `hotel_kolumna`
- **Wyzwalacz sceny (Trigger):** Ambientowe dialogi w holu hotelowym
- **Akcja po zakończeniu:** Budowanie tła epoki
- **Notatka reżyserska:** *Plotki o inżynierach z Berlina.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a1_hotel_goscie_01.mp3` | **przechodzien** | Goście hotelowi między sobą | Bei dieser Hitze sollte man gar nicht arbeiten. | W taki upał w ogóle nie powinno się pracować. | NIE |
| `a1_hotel_goscie_02.mp3` | **przechodzien** | Goście hotelowi między sobą | Herr Ober, noch ein Bier, bitte. Aber kalt. | Kelner, jeszcze jedno piwo. Ale zimne. | NIE |
| `a1_hotel_goscie_03.mp3` | **przechodzien** | Goście hotelowi między sobą | Man sagt, die Grenze wird bald ganz geschlossen. | Mówią, że granicę wkrótce całkiem zamkną. | NIE |
| `a1_hotel_goscie_04.mp3` | **przechodzien** | Goście hotelowi między sobą | Meine Frau will morgen nach Breslau, solange die Züge noch fahren. | Żona chce jutro jechać do Wrocławia, póki jeszcze jeżdżą pociągi. | NIE |

---

### Scena: `bark_przechodnie`
- **Akt i czas:** Akty 1 i 2 (Całe miasto)
- **Lokacja w grze:** Ulice Wilhelmstraße i Ring
- **Punkt w prefabie Mapy Unity:** `miasto_tlum_1`
- **Wyzwalacz sceny (Trigger):** Losowo podczas mijania tłumu na chodnikach
- **Akcja po zakończeniu:** Tło życia miejskiego w 1939 r.
- **Notatka reżyserska:** *Gawędy o pogodzie, zbliżającej się wojnie i cenach.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `bark_przechodnie_01.mp3` | **przechodzien** | Przechodnie między sobą | Was für eine Hitze. Seit Tagen kein Tropfen Regen. | Co za upał. Od dni ani kropli deszczu. | NIE |
| `bark_przechodnie_02.mp3` | **przechodzien** | Przechodnie między sobą | Ale gorko. Bez dyszczu ziymniaki ani niy urosnom. | Ale gorąco. Bez deszczu ziemniaki nawet nie urosną. | NIE |
| `bark_przechodnie_03.mp3` | **przechodzien** | Przechodnie między sobą | Mein Schwager hat gestern seinen Gestellungsbefehl bekommen. | Szwagier dostał wczoraj wezwanie do wojska. | NIE |
| `bark_przechodnie_04.mp3` | **przechodzien** | Przechodnie między sobą | Żniwa jeszcze niy zebrane, a już godajom o wojnie. | Żniwa jeszcze niezebrane, a już mówią o wojnie. | NIE |
| `bark_przechodnie_05.mp3` | **przechodzien** | Przechodnie między sobą | Seit Montag gibt es Butter nur noch auf Karte. | Od poniedziałku masło jest już tylko na kartki. | NIE |
| `bark_przechodnie_06.mp3` | **przechodzien** | Przechodnie między sobą | Ludzie wykupujom cuker i mąka, jakby jutro miało niy być. | Ludzie wykupują cukier i mąkę, jakby jutra miało nie być. | NIE |
| `bark_przechodnie_07.mp3` | **przechodzien** | Przechodnie między sobą | Im Radio sagen sie, an der Grenze wird geschossen. | W radiu mówią, że na granicy strzelają. | NIE |
| `bark_przechodnie_08.mp3` | **przechodzien** | Przechodnie między sobą | Wojna, niy wojna, a na gruba i tak trza iść. | Wojna czy nie wojna, a do kopalni i tak trzeba iść. | NIE |

---

### Scena: `a3_wejscie`
- **Akt i czas:** Akt 3 (20:00) Radiostacja
- **Lokacja w grze:** Teren stacji za szopą, dziura w płocie
- **Punkt w prefabie Mapy Unity:** `spawn_radio_tyl`
- **Wyzwalacz sceny (Trigger):** Przejście przez dziurę w siatce za radiostacją
- **Akcja po zakończeniu:** Cel misji: 'Wejdź do budynku nadajnika'
- **Notatka reżyserska:** *Koncentracja przed skokiem w niebezpieczeństwo.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a3_wejscie_01.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Ósma. Od frontu stoi auto i palą się światła. Wchodzę od tyłu. | Ósma. Od frontu stoi auto i palą się światła. Wchodzę od tyłu. | TAK |
| `a3_wejscie_02.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Płot przy szopie jest niski. Nikt go nie pilnuje, bo oni nie spodziewają się świadków. | Płot przy szopie jest niski. Nikt go nie pilnuje, bo oni nie spodziewają się świadków. | TAK |
| `a3_wejscie_03.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Kupiec z Bytomia został w mieście. Tutaj jestem tylko okiem aparatu. | Kupiec z Bytomia został w mieście. Tutaj jestem tylko okiem aparatu. | TAK |

---

### Scena: `a3_sd_udaja`
- **Akt i czas:** Akt 3 (20:01) Wnętrze radiostacji
- **Lokacja w grze:** Hol budynku nadajnika
- **Punkt w prefabie Mapy Unity:** `radio_drzwi_wyjscie`
- **Wyzwalacz sceny (Trigger):** Wtargnięcie napastników do wnętrza
- **Akcja po zakończeniu:** Obezwładnienie załogi i zamknięcie w piwnicy
- **Notatka reżyserska:** *Komicznie zły, przerysowany 'polski' w wykonaniu Niemców: 'Rence do góry!'.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a3_sd_udaja_01.mp3` | **sd** | SD do załogi stacji (udają powstańców) | Rence do góry. Tu polska powstańcy. | Ręce do góry. Tu polscy powstańcy. | NIE |
| `a3_sd_udaja_02.mp3` | **sd** | SD do załogi stacji (udają powstańców) | Na ziemia, szybko. Niech żyje Polska. | Na ziemię, szybko. Niech żyje Polska. | NIE |
| `a3_sd_udaja_03.mp3` | **sd** | SD do załogi stacji (udają powstańców) | Lauter. Man soll uns bis zur Straße hören. | Głośniej. Mają nas słyszeć aż na ulicy. | NIE |
| `a3_sd_udaja_04.mp3` | **naujocks** | SD do załogi stacji (udają powstańców) | Schneller. Wir haben nur ein paar Minuten. | Szybciej. Mamy tylko kilka minut. | NIE |
| `a3_sd_udaja_05.mp3` | **naujocks** | SD do załogi stacji (udają powstańców) | Zwei Mann in den Senderaum. Der Rest hält die Tür. | Dwóch do sali nadawczej. Reszta pilnuje drzwi. | NIE |

---

### Scena: `a3_brak_sygnalu`
- **Akt i czas:** Akt 3 (20:02) Sterownia radiostacji
- **Lokacja w grze:** Sterownia nadajnika
- **Punkt w prefabie Mapy Unity:** `radio_sterownia_mikrofon`
- **Wyzwalacz sceny (Trigger):** Nieudana próba odpalenia głównego pulpitu
- **Akcja po zakończeniu:** Podłączenie mikrofonu burzowego, przygotowanie odezwy
- **Notatka reżyserska:** *Frustracja i nerwy dywersantów, którzy nie znają obsługi aparatury.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a3_brak_sygnalu_01.mp3` | **sd** | SD do Naujocksa | Der Sender geht nicht. Wir finden den richtigen Schalter nicht. | Nadajnik nie działa. Nie możemy znaleźć właściwego przełącznika. | NIE |
| `a3_brak_sygnalu_02.mp3` | **naujocks** | SD do Naujocksa | Was heißt, er geht nicht? Wo ist der Techniker? | Jak to nie działa? Gdzie jest technik? | NIE |
| `a3_brak_sygnalu_03.mp3` | **sd** | SD do Naujocksa | Im Keller eingesperrt. Wie befohlen. | Zamknięty w piwnicy. Tak jak pan kazał. | NIE |
| `a3_brak_sygnalu_04.mp3` | **naujocks** | SD do Naujocksa | Dann das Gewittermikrofon. Damit warnen sie vor Unwettern. Es geht direkt hinaus. | To mikrofon burzowy. Nim ostrzegają przed burzą. Idzie prosto w eter. | NIE |
| `a3_brak_sygnalu_05.mp3` | **sd** | SD do Naujocksa | Gefunden. Es ist an. | Jest. Włączony. | NIE |

---

### Scena: `a3_komunikat`
- **Akt i czas:** Akt 3 (20:03) Sterownia radiostacji
- **Lokacja w grze:** Sterownia, mikrofon burzowy
- **Punkt w prefabie Mapy Unity:** `radio_sterownia_mikrofon`
- **Wyzwalacz sceny (Trigger):** Włączenie mikrofonu burzowego, start zegara 90 sekund
- **Akcja po zakończeniu:** Pasek czasu na HUD: 90 s na przecięcie linii wzmacniacza
- **Notatka reżyserska:** *Niemiec czyta po polsku z łamanym, twardym akcentem; trzęsący się ze stresu głos.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a3_komunikat_01.mp3` | **heinz** | Heinz do mikrofonu (w eter radiowy) | Uwaga! Tu Gliwice. Rozgłośnia znajduje się w rękach polskich. | Uwaga! Tu Gliwice. Rozgłośnia znajduje się w rękach polskich. | NIE |
| `a3_komunikat_02.mp3` | **heinz** | Heinz do mikrofonu (w eter radiowy) | Wzywamy wszystkich Polaków na Śląsku. | Wzywamy wszystkich Polaków na Śląsku. | NIE |
| `a3_komunikat_03.mp3` | **heinz** | Heinz do mikrofonu (w eter radiowy) | Godzina wolności wybiła. | Godzina wolności wybiła. | NIE |
| `a3_komunikat_04.mp3` | **heinz** | Heinz do mikrofonu (w eter radiowy) | Reicht das? Hört uns da draußen überhaupt jemand? | Wystarczy? Czy ktoś nas tam w ogóle słyszy? | NIE |

---

### Scena: `a3_mysl_kable`
- **Akt i czas:** Akt 3 (20:03) Pokój techniczny łącznicy
- **Lokacja w grze:** Pokój techniczny z szafami łącznicy
- **Punkt w prefabie Mapy Unity:** `radio_technika_kable`
- **Wyzwalacz sceny (Trigger):** Podejście do tablicy OB_Lacznica i kartki Grubera
- **Akcja po zakończeniu:** Początek mini-gry przecinania kabli (kabel 1, 2, 3)
- **Notatka reżyserska:** *Wyścig z czasem, sekundy uciekają, pomyłka kosztuje życie.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a3_mysl_kable_01.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Kartka przy szafie, podpisana Gruber. Linia, wzmacniacz, zasilanie. W tej kolejności. | Kartka przy szafie, podpisana Gruber. Linia, wzmacniacz, zasilanie. W tej kolejności. | TAK |
| `a3_mysl_kable_02.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Jeśli odepnę ich od linii, nie powiedzą wszystkiego, co chcieli. | Jeśli odepnę ich od linii, nie powiedzą wszystkiego, co chcieli. | TAK |
| `a3_mysl_kable_03.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Dziewięćdziesiąt sekund. Potem ktoś tu zajrzy. | 90 sekund. Potem ktoś tu zajrzy. | TAK |

---

### Scena: `a3_kabel_zly`
- **Akt i czas:** Akt 3 (20:04) Pokój techniczny
- **Lokacja w grze:** Tablica łącznicy
- **Punkt w prefabie Mapy Unity:** `radio_technika_kable`
- **Wyzwalacz sceny (Trigger):** Gracz przeciął niewłaściwy kabel (zwarcie, iskry)
- **Akcja po zakończeniu:** Utrata 15 sekund z licznika, strażnik idzie sprawdzić hałas
- **Notatka reżyserska:** *Panika, snop iskier, kroki na korytarzu.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a3_kabel_zly_01.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Nie ten. Iskra. | Nie ten. Iskra. | TAK |
| `a3_kabel_zly_02.mp3` | **sd** | Kruk do siebie / SD na korytarzu | Was war das? Hinten hat es geknallt. | Co to było? Z tyłu coś trzasnęło. | NIE |
| `a3_kabel_zly_03.mp3` | **sd** | Kruk do siebie / SD na korytarzu | Geh nachsehen. Sofort. | Idź zobacz. Natychmiast. | NIE |

---

### Scena: `a3_sygnal_urwany`
- **Akt i czas:** Akt 3 (20:04) Pokój techniczny i sterownia
- **Lokacja w grze:** Tablica łącznicy
- **Punkt w prefabie Mapy Unity:** `radio_technika_kable`
- **Wyzwalacz sceny (Trigger):** Przecięcie właściwego kabla (sukces mini-gry)
- **Akcja po zakończeniu:** Zatrzymanie licznika, cel: 'Opcjonalnie: uwolnij załogę z piwnicy'
- **Notatka reżyserska:** *Mikrofon gaśnie, Heinz krzyczy w panice, Kruk oddycha z ulgą.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a3_sygnal_urwany_01.mp3` | **heinz** | Heinz, Naujocks, Kruk | Die Leitung ist tot. Ich bin nicht mehr drauf. | Linia padła. Już nie nadaję. | NIE |
| `a3_sygnal_urwany_02.mp3` | **sd** | Heinz, Naujocks, Kruk | Wer war das? Ist hier noch jemand? | Kto to zrobił? Jest tu jeszcze ktoś? | NIE |
| `a3_sygnal_urwany_03.mp3` | **naujocks** | Heinz, Naujocks, Kruk | Egal. Es hat gereicht. Alle raus. | Nieważne. Wystarczyło. Wszyscy wychodzić. | NIE |
| `a3_sygnal_urwany_04.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Urwałem im głos. Ale kilka zdań i tak poszło w eter. | Urwałem im głos. Ale kilka zdań i tak poszło w eter. | TAK |

---

### Scena: `a3_czas_minal`
- **Akt i czas:** Akt 3 (20:05) Pokój techniczny
- **Lokacja w grze:** Pokój techniczny
- **Punkt w prefabie Mapy Unity:** `radio_technika_kable`
- **Wyzwalacz sceny (Trigger):** Upłynięcie 90 sekund przed przecięciem kabla
- **Akcja po zakończeniu:** Komunikat poszedł w całości; misja kontynuowana z gorszym wynikiem
- **Notatka reżyserska:** *Gorzka świadomość porażki technicznej, konieczność ratowania dowodów zdjęciowych.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a3_czas_minal_01.mp3` | **heinz** | Heinz / Kruk do siebie | Fertig. Es ist draußen. | Gotowe. Poszło. | NIE |
| `a3_czas_minal_02.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Za późno. Komunikat poszedł w eter. | Za późno. Komunikat poszedł w eter. | TAK |
| `a3_czas_minal_03.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Historii nie zatrzymam. Ale zdjęcia muszą dotrzeć do Katowic. | Historii nie zatrzymam. Ale zdjęcia muszą dotrzeć do Katowic. | TAK |

---

### Scena: `a3_gruber`
- **Akt i czas:** Akt 3 (20:06) Piwnica radiostacji
- **Lokacja w grze:** Piwnica pod budynkiem stacji
- **Punkt w prefabie Mapy Unity:** `radio_piwnica_drzwi`
- **Wyzwalacz sceny (Trigger):** Otwarcie rygla drzwi piwnicy klawiszem [E]
- **Akcja po zakończeniu:** Uwolnienie zakładników, wyłączenie animacji podniesionych rąk
- **Notatka reżyserska:** *Gruber płacze z wdzięczności, Kruk nakazuje natychmiastową ucieczkę po odjeździe SD.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a3_gruber_01.mp3` | **gruber** | Gruber do Kruka / Kruk do Grubera | Wer ist da? Nicht schießen. Hier unten sind nur Arbeiter. | Kto tam? Nie strzelać. Tu na dole są tylko pracownicy. | NIE |
| `a3_gruber_02.mp3` | **kruk** | Gruber do Kruka / Kruk do Grubera | Cicho. Nie jestem z nimi. Wypuszczę was. | Cicho. Nie jestem z nimi. Wypuszczę was. | NIE |
| `a3_gruber_03.mp3` | **gruber** | Gruber do Kruka / Kruk do Grubera | Godosz po naszymu. Panie, to niy som żodne Polacy. To som Niymcy z Berlina. | Mówisz po naszemu. Panie, to nie są żadni Polacy. To Niemcy z Berlina. | NIE |
| `a3_gruber_04.mp3` | **kruk** | Gruber do Kruka / Kruk do Grubera | Wiem. Wyjdźcie tylnymi drzwiami, jak tylko odjadą. Mnie tu nie było. | Wiem. Wyjdźcie tylnymi drzwiami, jak tylko odjadą. Mnie tu nie było. | NIE |
| `a3_gruber_05.mp3` | **gruber** | Gruber do Kruka / Kruk do Grubera | Jo nic niy widzioł. Ale zapamiyntom. | Ja nic nie widziałem. Ale zapamiętam. | NIE |

---

### Scena: `bark_pracownik`
- **Akt i czas:** Akt 3 (20:06) Piwnica radiostacji
- **Lokacja w grze:** Piwnica radiostacji
- **Punkt w prefabie Mapy Unity:** `radio_piwnica_pracownik_1`
- **Wyzwalacz sceny (Trigger):** Podejście do uwięzionych pracowników
- **Akcja po zakończeniu:** Budowanie dramatyzmu sytuacji zakładników
- **Notatka reżyserska:** *Lęk o rodziny, szept w ciemnej piwnicy.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `bark_pracownik_01.mp3` | **pracownik** | Pracownicy stacji w ciemności | Gruber, co sie tam dzieje na górze? | Gruber, co się tam dzieje na górze? | NIE |
| `bark_pracownik_02.mp3` | **pracownik** | Pracownicy stacji w ciemności | Ich habe Frau und Kinder zu Hause. Bitte. | Mam w domu żonę i dzieci. Proszę. | NIE |
| `bark_pracownik_03.mp3` | **pracownik** | Pracownicy stacji w ciemności | Niy wyłaź jeszcze. Oni majom broń. | Nie wychodź jeszcze. Oni mają broń. | NIE |

---

### Scena: `a4_mysl_honiok`
- **Akt i czas:** Akt 4 (20:10) Wyjście ze stacji
- **Lokacja w grze:** Trawnik przy schodach wyjściowych z radiostacji
- **Punkt w prefabie Mapy Unity:** `radio_cialo_honiok`
- **Wyzwalacz sceny (Trigger):** Zrobienie zdjęcia ciała Franciszka Honioka i kartki 'Konserve'
- **Akcja po zakończeniu:** Zaliczenie dowodu 4/6, włączenie globalnego alarmu SD
- **Notatka reżyserska:** *Głęboki, bolesny szacunek dla zamordowanego Ślązaka: 'Dla nich był rekwizytem, dla mnie jest Franciszkiem Honiokiem'.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a4_mysl_honiok_01.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Przy wejściu leży człowiek przykryty płaszczem. Ten z zagajnika. | Przy wejściu leży człowiek przykryty płaszczem. Ten z zagajnika. | TAK |
| `a4_mysl_honiok_02.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Na kartce nazwisko. Honiok, Franz. I jedno słowo: Konserve. | Na kartce nazwisko. Honiok, Franz. I jedno słowo: „Konserve”. | TAK |
| `a4_mysl_honiok_03.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Dla nich był rekwizytem. Dla mnie jest Franciszkiem Honiokiem. Nikt nie powie, że go tu nie było. | Dla nich był rekwizytem. Dla mnie jest Franciszkiem Honiokiem. Nikt nie powie, że go tu nie było. | TAK |

---

### Scena: `a4_alarm`
- **Akt i czas:** Akt 4 (20:11) Teren radiostacji
- **Lokacja w grze:** Plac wokół stacji i wieży
- **Punkt w prefabie Mapy Unity:** `spawn_radio_brama`
- **Wyzwalacz sceny (Trigger):** Po zrobieniu zdjęcia ciała Honioka
- **Akcja po zakończeniu:** Wszyscy wrogowie w trybie agresywnym, start pościgu na wschód
- **Notatka reżyserska:** *Reflektory, krzyki, syrena alarmowa, komenda 'Strzelać bez ostrzeżenia!'.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a4_alarm_01.mp3` | **sd** | Naujocks i SD do całego oddziału | Da ist einer! Am Zaun! | Tam ktoś jest! Przy płocie! | NIE |
| `a4_alarm_02.mp3` | **naujocks** | Naujocks i SD do całego oddziału | Holt ihn. Lebend. Er hat eine Kamera. | Złapcie go. Żywego. Ma aparat. | NIE |
| `a4_alarm_03.mp3` | **sd** | Naujocks i SD do całego oddziału | Licht! Macht das Licht an! | Światło! Zapalcie światło! | NIE |

---

### Scena: `a4_ucieczka`
- **Akt i czas:** Akt 4 (20:12) Ucieczka przez pola
- **Lokacja w grze:** Pola na wschód od radiostacji
- **Punkt w prefabie Mapy Unity:** `miasto_stodola`
- **Wyzwalacz sceny (Trigger):** Sprint przez pola (opcjonalnie rower OB_Rower)
- **Akcja po zakończeniu:** Kierunek: stodoła kuriera Sowy
- **Notatka reżyserska:** *Zadyszka, bieg w ciemności rowami melioracyjnymi, za plecami światła pościgu.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a4_ucieczka_01.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Na wschód, przez pola. W stodole czeka Sowa. | Na wschód, przez pola. W stodole czeka Sowa. | TAK |
| `a4_ucieczka_02.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Rowem, nie drogą. Na drodze widać każdego. | Rowem, nie drogą. Na drodze widać każdego. | TAK |
| `a4_ucieczka_03.mp3` | **kruk** | Do siebie (Myśl wewnętrzna bohatera - gracz słyszy głos w głowie) | Film jest w kieszeni. Tylko to się teraz liczy. | Film jest w kieszeni. Tylko to się teraz liczy. | TAK |

---

### Scena: `a4_sowa`
- **Akt i czas:** Akt 4 (20:20) Drewniana stodoła
- **Lokacja w grze:** Stodoła na wschodnim krańcu mapy
- **Punkt w prefabie Mapy Unity:** `miasto_stodola`
- **Wyzwalacz sceny (Trigger):** Wejście do stodoły i interakcja z Sową
- **Akcja po zakończeniu:** Przekazanie naświetlonej rolki filmu OB_Film, start plansz epilogu
- **Notatka reżyserska:** *Ulga, pośpiech, ostatnie instrukcje: 'Do Katowic przed północą. Jeśli cię zatrzymają, prześwietl kliszę'.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a4_sowa_01.mp3` | **sowa** | Sowa do Kruka / Kruk do Sowy | Późno dzisiaj na grzyby. | Późno dzisiaj na grzyby. | NIE |
| `a4_sowa_02.mp3` | **kruk** | Sowa do Kruka / Kruk do Sowy | Po deszczu będą lepsze. | Po deszczu będą lepsze. | NIE |
| `a4_sowa_03.mp3` | **kruk** | Sowa do Kruka / Kruk do Sowy | Jeden film. Mapa, lista, zagajnik, radiostacja. I człowiek, którego zostawili przy wejściu. | Jeden film. Mapa, lista, zagajnik, radiostacja. I człowiek, którego zostawili przy wejściu. | NIE |
| `a4_sowa_04.mp3` | **sowa** | Sowa do Kruka / Kruk do Sowy | Schowom go w ramie kola. Do Katowic przed północą. | Schowam go w ramie roweru. Do Katowic przed północą. | NIE |
| `a4_sowa_05.mp3` | **kruk** | Sowa do Kruka / Kruk do Sowy | Jeśli cię zatrzymają, prześwietl go. Lepiej nic, niż żeby trafił do nich. | Jeśli cię zatrzymają, prześwietl go. Lepiej nic, niż żeby trafił do nich. | NIE |
| `a4_sowa_06.mp3` | **sowa** | Sowa do Kruka / Kruk do Sowy | Niy zatrzymajom. A ty idź inszom drogom. Rano bydzie wojna. | Nie zatrzymają. A ty idź inną drogą. Rano będzie wojna. | NIE |

---

### Scena: `a4_radio_spiker`
- **Akt i czas:** Akt 4 (20:25) Epilog radiowy
- **Lokacja w grze:** Odbiornik radiowy w stodole
- **Punkt w prefabie Mapy Unity:** `miasto_stodola`
- **Wyzwalacz sceny (Trigger):** Po przekazaniu filmu Sowie
- **Akcja po zakończeniu:** Rozpoczęcie plansz tekstowych epilogu z głosem lektora
- **Notatka reżyserska:** *Oficjalna niemiecka propaganda obwiniająca Polskę o napaść graniczną.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `a4_radio_spiker_01.mp3` | **spiker** | Spiker niemiecki do radiosłuchaczy | Hier ist der Reichssender Breslau. | Tu Rozgłośnia Rzeszy we Wrocławiu. | NIE |
| `a4_radio_spiker_02.mp3` | **spiker** | Spiker niemiecki do radiosłuchaczy | Gestern gegen zwanzig Uhr haben polnische Aufständische den Sender Gleiwitz überfallen. | Wczoraj około 20:00 polscy powstańcy napadli na radiostację w Gliwicach. | NIE |
| `a4_radio_spiker_03.mp3` | **spiker** | Spiker niemiecki do radiosłuchaczy | Die Polizei hat die Angreifer zurückgeschlagen. Ein Aufständischer wurde getötet. | Policja odparła napastników. Jeden z powstańców zginął. | NIE |
| `a4_radio_spiker_04.mp3` | **spiker** | Spiker niemiecki do radiosłuchaczy | Weitere Meldungen folgen. | Dalsze komunikaty wkrótce. | NIE |

---

### Scena: `bark_sd_podejrzenie`
- **Akt i czas:** Dowolny moment (AI Patrolu)
- **Lokacja w grze:** Wokół patroli SD
- **Punkt w prefabie Mapy Unity:** `miasto_patrol_sd_1`
- **Wyzwalacz sceny (Trigger):** Wykrycie hałasu lub dostrzeżenie gracza w stożku wzroku
- **Akcja po zakończeniu:** Podejście do źródła hałasu
- **Notatka reżyserska:** *Podejrzliwe, twarde niemieckie barks.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `bark_sd_podejrzenie_01.mp3` | **sd** | Żołnierz SD do drugiego strażnika | Hast du das gehört? | Słyszałeś to? | NIE |
| `bark_sd_podejrzenie_02.mp3` | **sd** | Żołnierz SD do drugiego strażnika | Da hinten hat sich etwas bewegt. | Tam z tyłu coś się poruszyło. | NIE |
| `bark_sd_podejrzenie_03.mp3` | **sd** | Żołnierz SD do drugiego strażnika | Wer ist da? Zeig dich. | Kto tam? Pokaż się. | NIE |
| `bark_sd_podejrzenie_04.mp3` | **sd** | Żołnierz SD do drugiego strażnika | Wahrscheinlich eine Katze. Ich sehe trotzdem nach. | Pewnie kot. I tak sprawdzę. | NIE |

---

### Scena: `bark_sd_alarm`
- **Akt i czas:** Dowolny moment (AI Walki)
- **Lokacja w grze:** Wokół patroli SD
- **Punkt w prefabie Mapy Unity:** `miasto_patrol_sd_1`
- **Wyzwalacz sceny (Trigger):** Pełne zdemaskowanie gracza
- **Akcja po zakończeniu:** Otwarcie ognia, wezwanie posiłków w promieniu 25 m
- **Notatka reżyserska:** *Wściekły krzyk bojowy, strzały.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `bark_sd_alarm_01.mp3` | **sd** | Żołnierz SD do innych patroli | Da ist er! Hinterher! | Tam jest! Za nim! | NIE |
| `bark_sd_alarm_02.mp3` | **sd** | Żołnierz SD do innych patroli | Ein Fremder auf dem Gelände! | Obcy na terenie! | NIE |
| `bark_sd_alarm_03.mp3` | **sd** | Żołnierz SD do innych patroli | Er läuft zum Zaun! | Biegnie do płotu! | NIE |
| `bark_sd_alarm_04.mp3` | **sd** | Żołnierz SD do innych patroli | Nicht entkommen lassen! | Nie dajcie mu uciec! | NIE |

---

### Scena: `bark_sd_szukanie`
- **Akt i czas:** Dowolny moment (AI Szukania)
- **Lokacja w grze:** Miejsce ostatniej pozycji gracza
- **Punkt w prefabie Mapy Unity:** `miasto_patrol_sd_1`
- **Wyzwalacz sceny (Trigger):** Gracz zniknął za osłoną, wrogowie przeszukują teren
- **Akcja po zakończeniu:** Sprawdzanie zaułków i krzaków
- **Notatka reżyserska:** *Czujne sprawdzanie cieni z bronią w gotowości.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `bark_sd_szukanie_01.mp3` | **sd** | Żołnierz SD | Durchsucht die Schuppen. | Przeszukać szopy. | NIE |
| `bark_sd_szukanie_02.mp3` | **sd** | Żołnierz SD | Er kann nicht weit sein. | Nie mógł daleko odejść. | NIE |
| `bark_sd_szukanie_03.mp3` | **sd** | Żołnierz SD | Leuchte unter die Bäume. | Poświeć pod drzewa. | NIE |
| `bark_sd_szukanie_04.mp3` | **sd** | Żołnierz SD | Jeden Winkel. Verstanden? | Każdy kąt. Zrozumiano? | NIE |

---

### Scena: `bark_sd_zgubil`
- **Akt i czas:** Dowolny moment (AI Powrót)
- **Lokacja w grze:** Trasa patrolu
- **Punkt w prefabie Mapy Unity:** `miasto_patrol_sd_1`
- **Wyzwalacz sceny (Trigger):** Brak śladu gracza po 15 sekundach szukania
- **Akcja po zakończeniu:** Powrót do standardowej trasy patrolu
- **Notatka reżyserska:** *Zniecierpliwienie, machnięcie ręką.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `bark_sd_zgubil_01.mp3` | **sd** | Żołnierz SD | Weg. Einfach weg. | Zniknął. Po prostu zniknął. | NIE |
| `bark_sd_zgubil_02.mp3` | **sd** | Żołnierz SD | Verdammt, er ist uns entwischt. | Cholera, wymknął się nam. | NIE |
| `bark_sd_zgubil_03.mp3` | **sd** | Żołnierz SD | Zurück zum Wagen. Der Chef wartet nicht. | Wracamy do auta. Szef nie będzie czekał. | NIE |
| `bark_sd_zgubil_04.mp3` | **sd** | Żołnierz SD | Wenn er etwas gesehen hat, haben wir ein Problem. | Jeśli coś widział, mamy problem. | NIE |

---

### Scena: `bark_schupo`
- **Akt i czas:** Dowolny moment (AI Policji)
- **Lokacja w grze:** Ulice Wilhelmstraße i Ring
- **Punkt w prefabie Mapy Unity:** `miasto_schupo_1`
- **Wyzwalacz sceny (Trigger):** Zauważenie biegnącego lub kucającego gracza
- **Akcja po zakończeniu:** Bieg do najbliższego patrolu SD w celu podniesienia alarmu
- **Notatka reżyserska:** *Służbiste pouczenia i wezwania do okazania dokumentów.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `bark_schupo_01.mp3` | **schupo** | Policjant Schupo do gracza | Halt! Stehenbleiben! Polizei! | Stój! Nie ruszać się! Policja! | NIE |
| `bark_schupo_02.mp3` | **schupo** | Policjant Schupo do gracza | Sie da! Hände aus den Taschen! | Pan tam! Ręce z kieszeni! | NIE |
| `bark_schupo_03.mp3` | **schupo** | Policjant Schupo do gracza | Hierher, meine Herren! Hier läuft einer weg! | Tutaj, panowie! Tu ktoś ucieka! | NIE |
| `bark_schupo_04.mp3` | **schupo** | Policjant Schupo do gracza | Holt die Herren vom Sender. Schnell! | Wołajcie panów z radiostacji. Szybko! | NIE |

---

### Scena: `porazka_zdemaskowany`
- **Akt i czas:** Ekran Porażki 1
- **Lokacja w grze:** Ekran gry
- **Punkt w prefabie Mapy Unity:** `brak`
- **Wyzwalacz sceny (Trigger):** Gracz trafiony kulą wroga
- **Akcja po zakończeniu:** Wyświetlenie planszy porażki z przyciskiem powrotu do checkpointa
- **Notatka reżyserska:** *Głęboki, poważny ton dokumentalny: 'Agent Kruk zdemaskowany. Prowokacja przeszła bez świadków'.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `porazka_zdemaskowany_01.mp3` | **lektor** | Lektor do gracza | Agent Kruk zdemaskowany. Prowokacja przeszła bez świadków. | Agent »Kruk« zdemaskowany. Prowokacja przeszła bez świadków. | NIE |

---

### Scena: `porazka_cywil`
- **Akt i czas:** Ekran Porażki 2
- **Lokacja w grze:** Ekran gry
- **Punkt w prefabie Mapy Unity:** `brak`
- **Wyzwalacz sceny (Trigger):** Gracz postrzelił cywila, policjanta Schupo lub pracownika
- **Akcja po zakończeniu:** Plansza porażki
- **Notatka reżyserska:** *Stanowczy, etyczny osąd: 'Polski agent nie strzela do cywilów. Właśnie dałeś im prawdziwy dowód'.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `porazka_cywil_01.mp3` | **lektor** | Lektor do gracza | Polski agent nie strzela do cywilów. Właśnie dałeś im prawdziwy dowód. | Polski agent nie strzela do cywilów. Właśnie dałeś im prawdziwy dowód. | NIE |

---

### Scena: `porazka_czas`
- **Akt i czas:** Ekran Porażki 3
- **Lokacja w grze:** Ekran gry
- **Punkt w prefabie Mapy Unity:** `brak`
- **Wyzwalacz sceny (Trigger):** Komunikat w radiostacji poszedł bez przecięcia kabli i gracz został złapany
- **Akcja po zakończeniu:** Plansza porażki
- **Notatka reżyserska:** *Smutna konstatacja kłamstwa, które stało się faktem medialnym.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `porazka_czas_01.mp3` | **lektor** | Lektor do gracza | Świt. Film nie dotarł do Katowic. Świat usłyszał tylko wersję z Berlina. | Świt. Film nie dotarł do Katowic. Świat usłyszał tylko wersję z Berlina. | NIE |

---

### Scena: `karta_1`
- **Akt i czas:** Karta Historii 1 (Ratusz)
- **Lokacja w grze:** Rynek przy ratuszu
- **Punkt w prefabie Mapy Unity:** `miasto_karta_1`
- **Wyzwalacz sceny (Trigger):** Podniesienie Karty Historii nr 1
- **Akcja po zakończeniu:** Otwarcie karty edukacyjnej w UI
- **Notatka reżyserska:** *Fakty historyczne o Operacji Himmler.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `karta_1_01.mp3` | **lektor** | Lektor | Po podziale Górnego Śląska w tysiąc dziewięćset dwudziestym drugim roku Gliwice zostały w Niemczech, a Katowice znalazły się w Polsce. Na Ringu stał ratusz i fontanna z Neptunem. W mieście mieszkali obok siebie Niemcy, Ślązacy i Polacy. | Po podziale Górnego Śląska w 1922 roku Gliwice zostały w Niemczech, a Katowice znalazły się w Polsce. Na Ringu stał ratusz i fontanna z Neptunem. W mieście mieszkali obok siebie Niemcy, Ślązacy i Polacy. | NIE |

---

### Scena: `karta_2`
- **Akt i czas:** Karta Historii 2 (Kościół)
- **Lokacja w grze:** Kościół Wszystkich Świętych
- **Punkt w prefabie Mapy Unity:** `miasto_karta_2`
- **Wyzwalacz sceny (Trigger):** Podniesienie Karty Historii nr 2
- **Akcja po zakończeniu:** Otwarcie karty w UI
- **Notatka reżyserska:** *Fakty o granicy polsko-niemieckiej w 1939 r.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `karta_2_01.mp3` | **lektor** | Lektor | Gotycki kościół Wszystkich Świętych to jeden z najstarszych budynków Gliwic. W mieście żyła polska mniejszość, która miała swoje organizacje, w tym Związek Polaków w Niemczech. Po wybuchu wojny wielu polskich działaczy ze Śląska aresztowano. | Gotycki kościół Wszystkich Świętych to jeden z najstarszych budynków Gliwic. W mieście żyła polska mniejszość, która miała swoje organizacje, w tym Związek Polaków w Niemczech. Po wybuchu wojny wielu polskich działaczy ze Śląska aresztowano. | NIE |

---

### Scena: `karta_3`
- **Akt i czas:** Karta Historii 3 (Willa)
- **Lokacja w grze:** Tarnowitzer Landstraße
- **Punkt w prefabie Mapy Unity:** `miasto_karta_3`
- **Wyzwalacz sceny (Trigger):** Podniesienie Karty Historii nr 3
- **Akcja po zakończeniu:** Otwarcie karty w UI
- **Notatka reżyserska:** *Fakty o Alfredzie Naujocksie.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `karta_3_01.mp3` | **lektor** | Lektor | Tej samej nocy Niemcy zorganizowali kilka prowokacji wzdłuż granicy. Oprócz radiostacji w Gliwicach napadnięto na komorę celną w Stodołach, po niemiecku Hochlinden, i na leśniczówkę w Byczynie, po niemiecku Pitschen. Każda z nich miała wyglądać na polski atak. | Tej samej nocy Niemcy zorganizowali kilka prowokacji wzdłuż granicy. Oprócz radiostacji w Gliwicach napadnięto na komorę celną w Stodołach (Hochlinden) i na leśniczówkę w Byczynie (Pitschen). Każda z nich miała wyglądać na polski atak. | NIE |

---

### Scena: `karta_4`
- **Akt i czas:** Karta Historii 4 (Dworzec)
- **Lokacja w grze:** Bahnhof Gleiwitz
- **Punkt w prefabie Mapy Unity:** `miasto_karta_4`
- **Wyzwalacz sceny (Trigger):** Podniesienie Karty Historii nr 4
- **Akcja po zakończeniu:** Otwarcie karty w UI
- **Notatka reżyserska:** *Fakty o Ekspozyturze nr 4 polskiego wywiadu w Katowicach.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `karta_4_01.mp3` | **lektor** | Lektor | Granica z Polską biegła zaledwie kilka kilometrów od Gliwic. Między Gliwicami a Katowicami jeździły pociągi, a wielu Ślązaków miało rodzinę i pracę po obu stronach granicy. | Granica z Polską biegła zaledwie kilka kilometrów od Gliwic. Między Gliwicami a Katowicami jeździły pociągi, a wielu Ślązaków miało rodzinę i pracę po obu stronach granicy. | NIE |

---

### Scena: `karta_5`
- **Akt i czas:** Karta Historii 5 (Most)
- **Lokacja w grze:** Most na Kłodnicy
- **Punkt w prefabie Mapy Unity:** `miasto_karta_5`
- **Wyzwalacz sceny (Trigger):** Podniesienie Karty Historii nr 5
- **Akcja po zakończeniu:** Otwarcie karty w UI
- **Notatka reżyserska:** *Fakty o mikrofonie burzowym Gewittermikrofon.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `karta_5_01.mp3` | **lektor** | Lektor | Na przełomie osiemnastego i dziewiętnastego wieku przekopano Kanał Kłodnicki, który połączył Gliwice z Odrą. Barkami płynęły węgiel i żelazo. Gliwice z królewską odlewnią żeliwa i kopalniami w okolicy były jednym z ośrodków górnośląskiego przemysłu. | Na przełomie XVIII i XIX wieku przekopano Kanał Kłodnicki, który połączył Gliwice z Odrą. Barkami płynęły węgiel i żelazo. Gliwice z królewską odlewnią żeliwa i kopalniami w okolicy były jednym z ośrodków górnośląskiego przemysłu. | NIE |

---

### Scena: `karta_6`
- **Akt i czas:** Karta Historii 6 (Stodoła)
- **Lokacja w grze:** Stodoła na wschodzie
- **Punkt w prefabie Mapy Unity:** `miasto_karta_6`
- **Wyzwalacz sceny (Trigger):** Podniesienie Karty Historii nr 6
- **Akcja po zakończeniu:** Otwarcie karty w UI
- **Notatka reżyserska:** *Fakty o drewnianej wieży radiostacji 111 m.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `karta_6_01.mp3` | **lektor** | Lektor | Franciszek Honiok pochodził z Łubia, po niemiecku Hohenlieben, i walczył w powstaniu śląskim po stronie polskiej. Gestapo zatrzymało go trzydziestego sierpnia tysiąc dziewięćset trzydziestego dziewiątego roku, a w planie napastników nazywano go Konserve. Zabito go i zostawiono przy radiostacji jako fałszywy dowód polskiego ataku. | Franciszek Honiok pochodził z Łubia (Hohenlieben) i walczył w powstaniu śląskim po stronie polskiej. Gestapo zatrzymało go 30 sierpnia 1939 roku, a w planie napastników nazywano go „Konserve”. Zabito go i zostawiono przy radiostacji jako fałszywy dowód polskiego ataku. | NIE |

---

### Scena: `epilog_1`
- **Akt i czas:** Epilog (1.09.1939)
- **Lokacja w grze:** Czarna plansza
- **Punkt w prefabie Mapy Unity:** `brak`
- **Wyzwalacz sceny (Trigger):** Plansza 1 po ucieczce
- **Akcja po zakończeniu:** Przejście do planszy 2
- **Notatka reżyserska:** *Westerplatte i wybuch II wojny światowej.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `epilog_1_01.mp3` | **lektor** | Lektor | Pierwszego września o czwartej czterdzieści pięć niemiecki okręt otworzył ogień do Westerplatte. Niemcy zaatakowały Polskę bez wypowiedzenia wojny. | 1 września, 4:45. Niemiecki okręt otworzył ogień do Westerplatte. Niemcy zaatakowały Polskę bez wypowiedzenia wojny. | NIE |

---

### Scena: `epilog_2`
- **Akt i czas:** Epilog (1.09.1939)
- **Lokacja w grze:** Czarna plansza
- **Punkt w prefabie Mapy Unity:** `brak`
- **Wyzwalacz sceny (Trigger):** Plansza 2
- **Akcja po zakończeniu:** Przejście do planszy 3
- **Notatka reżyserska:** *Przemówienie Hitlera w Reichstagu: 'Od 5:45 odpowiadamy ogniem'.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `epilog_2_01.mp3` | **lektor** | Lektor | Tego samego dnia Hitler powiedział w Reichstagu: od piątej czterdzieści pięć odpowiadamy ogniem. Na usprawiedliwienie wojny pokazywano światu napady, które Niemcy sami zorganizowali. | Tego samego dnia Hitler powiedział w Reichstagu: „Od 5:45 odpowiadamy ogniem”. Na usprawiedliwienie wojny pokazywano światu napady, które Niemcy sami zorganizowali. | NIE |

---

### Scena: `epilog_3`
- **Akt i czas:** Epilog (Kim był Honiok)
- **Lokacja w grze:** Czarna plansza
- **Punkt w prefabie Mapy Unity:** `brak`
- **Wyzwalacz sceny (Trigger):** Plansza 3
- **Akcja po zakończeniu:** Przejście do planszy 4
- **Notatka reżyserska:** *Wspomnienie Franciszka Honioka, pierwszej ofiary II wojny światowej.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `epilog_3_01.mp3` | **lektor** | Lektor | Franciszka Honioka nazywa się często pierwszą ofiarą drugiej wojny światowej. Zginął, zanim wojna się zaczęła. | Franciszka Honioka nazywa się często pierwszą ofiarą II wojny światowej. Zginął, zanim wojna się zaczęła. | NIE |

---

### Scena: `epilog_4`
- **Akt i czas:** Epilog (Prawda)
- **Lokacja w grze:** Czarna plansza
- **Punkt w prefabie Mapy Unity:** `brak`
- **Wyzwalacz sceny (Trigger):** Plansza 4
- **Akcja po zakończeniu:** Przejście do planszy 5
- **Notatka reżyserska:** *Zeznania Naujocksa przed Trybunałem w Norymberdze 20.11.1945 r.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `epilog_4_01.mp3` | **lektor** | Lektor | Jan Wrona, Kruk, to postać fikcyjna. Prawdę o prowokacji ujawnił sam Alfred Naujoks w zeznaniu złożonym w Norymberdze w tysiąc dziewięćset czterdziestym piątym roku. | Jan Wrona „Kruk” to postać fikcyjna. Prawdę o prowokacji ujawnił sam Alfred Naujocks w zeznaniu złożonym w Norymberdze w 1945 roku. | NIE |

---

### Scena: `epilog_5`
- **Akt i czas:** Epilog (Pamięć)
- **Lokacja w grze:** Czarna plansza
- **Punkt w prefabie Mapy Unity:** `brak`
- **Wyzwalacz sceny (Trigger):** Plansza 5
- **Akcja po zakończeniu:** Przejście do planszy 6
- **Notatka reżyserska:** *Dziś przy Tarnogórskiej 129 mieści się Muzeum w Gliwicach.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `epilog_5_01.mp3` | **lektor** | Lektor | Drewniana wieża radiostacji stoi do dziś. Można ją zobaczyć w Muzeum Radiostacja Gliwice, przy ulicy Tarnogórskiej sto dwadzieścia dziewięć. | Drewniana wieża radiostacji stoi do dziś. Można ją zobaczyć w Muzeum Radiostacja Gliwice, ul. Tarnogórska 129. | NIE |

---

### Scena: `epilog_6`
- **Akt i czas:** Epilog (Finał)
- **Lokacja w grze:** Czarna plansza
- **Punkt w prefabie Mapy Unity:** `brak`
- **Wyzwalacz sceny (Trigger):** Plansza 6
- **Akcja po zakończeniu:** Napisy końcowe i powrót do Menu
- **Notatka reżyserska:** *Ostatnie zdanie: 'Prowokacja trwała kilka minut. Wojna trwała sześć lat'.*

| Plik Audio | Kto mówi | Do kogo | Tekst wypowiadany | Napis na ekranie (PL) | Myśl? |
| :--- | :--- | :--- | :--- | :--- | :-: |
| `epilog_6_01.mp3` | **lektor** | Lektor | Prowokacja trwała kilka minut. Wojna trwała sześć lat. | Prowokacja trwała kilka minut. Wojna trwała sześć lat. | NIE |

---

