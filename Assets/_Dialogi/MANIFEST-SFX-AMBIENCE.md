# SENDER GLEIWITZ 31.08.1939 — MANIFEST AMBIENCE & SFX (EFEKTY I POKOJE)

Dokumentacja przygotowana dla programisty i AI (ROLA B — UNITY) do obsługi teł dźwiękowych miasta, pokojów i efektów kroków/przedmiotów.

## 1. Jak działa system tła dźwiękowego (Miasto vs Pokoje)?
- **Miasto (`amb_miasto_wojna_1939`):** Dźwięk tła odtwarza się w pętli na stałym kanale AudioSource. W tle słychać stłumione serie i odległe bomby na granicy.
- **Pokoje i wnętrza:** W momencie przejścia przez drzwi (`miasto_drzwi_*`), ambient miasta jest wyciszany do zera (fade out w 0,4 s), a włącza się dedykowany, cichy ambient danego pokoju (np. `amb_hotel_hol_wnetrze`, `amb_ksiegarnia_wnetrze`). W pokojach panuje intymna cisza.
- **Dźwięk kroków:** Skrypt ruchu pobiera tag nawierzchni (`Bruk` lub `Drewno`) i odpala `sfx_kroki_bruk` lub `sfx_kroki_drewno` zsynchronizowany z prędkością gracza.

---

## 2. Katalog Dźwięków Tła i Efektów

### `amb_miasto_wojna_1939`
- **Plik audio:** `amb_miasto_wojna_1939.mp3`
- **Ścieżka w Unity:** `Resources/Ambience/amb_miasto_wojna_1939`
- **Kategoria:** Ambience (Plener / Miasto)
- **Głośność / Pętla:** 60% | Pętla (Loop)
- **Kiedy odtworzyć:** Gra bez przerwy w tle przez całą grę, dopóki gracz znajduje się na zewnątrz (na ulicy, rynku, peronie, w zagajniku i polach).
- **Gdzie:** Cała otwarta mapa zewnętrzna (Gliwice z 0..400). Wycisza się do zera w momencie wejścia do wnętrz.
- **Wyzwalacz w kodzie:** `AudioSource na obiekcie OtoczenieMiasto z AudioLowPassFilter przy zbliżaniu się do budynków.`
- **Opis efektu:** Ciągły ambient starego miasta 1939 z odgłosami wojny w tle: letni wiatr, odległe głuche bombardowania na granicy, stłumione serie karabinowe w oddali, nerwowy szum miasta, dalekie syreny.

---

### `sfx_kroki_bruk`
- **Plik audio:** `sfx_kroki_bruk.mp3`
- **Ścieżka w Unity:** `Resources/Sfx/sfx_kroki_bruk`
- **Kategoria:** SFX Ruchu (Kroki)
- **Głośność / Pętla:** 50% | Pętla (Loop)
- **Kiedy odtworzyć:** Gdy gracz idzie lub biegnie po jezdni z bruku, chodnikach Wilhelmstraße lub bruku Rynku.
- **Gdzie:** Na powierzchniach z tagiem lub warstwą 'Bruk'.
- **Wyzwalacz w kodzie:** `Wywoływane z animacji chodu/biegu (Animation Events na stopach) lub skryptu ThirdPersonController.`
- **Opis efektu:** Kroki męskich skórzanych półbutów z lat 30. po nierównym bruku miejskim (rytmiczny, naturalny chód i skradanie).

---

### `sfx_kroki_drewno`
- **Plik audio:** `sfx_kroki_drewno.mp3`
- **Ścieżka w Unity:** `Resources/Sfx/sfx_kroki_drewno`
- **Kategoria:** SFX Ruchu (Kroki Wnętrza)
- **Głośność / Pętla:** 45% | Pętla (Loop)
- **Kiedy odtworzyć:** Gdy gracz porusza się we wnętrzach: hotel Haus Oberschlesien, księgarnia, stodoła, schody wieży.
- **Gdzie:** Wnętrza budynków z warstwą 'Drewno'.
- **Wyzwalacz w kodzie:** `Wykrycie raycastem podłoża 'Drewno' pod stopami gracza.`
- **Opis efektu:** Kroki na starej, skrzypiącej podłodze drewnianej (parkiet hotelowy, deski w księgarni i wieży radiostacji).

---

### `sfx_telefon_stacjonarny_1930`
- **Plik audio:** `sfx_telefon_stacjonarny_1930.mp3`
- **Ścieżka w Unity:** `Resources/Sfx/sfx_telefon_stacjonarny_1930`
- **Kategoria:** SFX Obiektu (Hotel)
- **Głośność / Pętla:** 85% | Jednorazowy
- **Kiedy odtworzyć:** Akt 1 (16:20): Gdy Heydrich dzwoni z Berlina z hasłem 'Großmutter gestorben' do Naujocksa.
- **Gdzie:** Hol hotelu Haus Oberschlesien przy obiekcie OB_Telefon na punkcie hotel_telefon.
- **Wyzwalacz w kodzie:** `AudioSource 3D Spatial Blend na obiekcie OB_Telefon, uruchamiany przed sceną a1_telefon.`
- **Opis efektu:** Głośny, realistyczny dzwonek telefonu stacjonarnego z lat 30. Podwójne metaliczne uderzenia czaszowe z echem w hotelowym holu.

---

### `sfx_dzwonek_recepcja`
- **Plik audio:** `sfx_dzwonek_recepcja.mp3`
- **Ścieżka w Unity:** `Resources/Sfx/sfx_dzwonek_recepcja`
- **Kategoria:** SFX Obiektu (Hotel)
- **Głośność / Pętla:** 70% | Jednorazowy
- **Kiedy odtworzyć:** Podejście do lady recepcji w hotelu lub kliknięcie [E] na ladzie.
- **Gdzie:** Hotel Haus Oberschlesien, lada recepcji OB_Lada_Recepcja.
- **Wyzwalacz w kodzie:** `Trigger wejścia w strefę hotel_recepcja.`
- **Opis efektu:** Dźwięk mosiężnego dzwonka na ladzie hotelowej (klasyczne 'ding' portiera/recepcjonisty).

---

### `amb_hotel_hol_wnetrze`
- **Plik audio:** `amb_hotel_hol_wnetrze.mp3`
- **Ścieżka w Unity:** `Resources/Ambience/amb_hotel_hol_wnetrze`
- **Kategoria:** Ambience (Wnętrze Hotelu)
- **Głośność / Pętla:** 40% | Pętla (Loop)
- **Kiedy odtworzyć:** Po wejściu gracza przez drzwi do hotelu Haus Oberschlesien.
- **Gdzie:** Wnętrze hotelu (x = 1000–1040 w scenie).
- **Wyzwalacz w kodzie:** `Po teleportacji gracza na spawn_hotel_wejscie.`
- **Opis efektu:** Ciche, eleganckie tło luksusowego hotelu z lat 30.: cichy szmer zegara szafowego, wytłumione dywanami echo, cichy szelest gazet.

---

### `amb_ksiegarnia_wnetrze`
- **Plik audio:** `amb_ksiegarnia_wnetrze.mp3`
- **Ścieżka w Unity:** `Resources/Ambience/amb_ksiegarnia_wnetrze`
- **Kategoria:** Ambience (Wnętrze Księgarni)
- **Głośność / Pętla:** 35% | Pętla (Loop)
- **Kiedy odtworzyć:** Podczas pobytu w księgarni pani Zofii.
- **Gdzie:** Wnętrze Buchhandlung (x = 1060–1076).
- **Wyzwalacz w kodzie:** `Po wejściu przez drzwi miasto_drzwi_ksiegarnia.`
- **Opis efektu:** Intymna, cicha atmosfera małej księgarni na Ringu: powolne, głębokie tykanie zegara ściennego, szelest starych kartek papieru, absolutny spokój odcięty od ulicy.

---

### `amb_fotograf_ciemnia`
- **Plik audio:** `amb_fotograf_ciemnia.mp3`
- **Ścieżka w Unity:** `Resources/Ambience/amb_fotograf_ciemnia`
- **Kategoria:** Ambience (Wnętrze Fotografa / Ciemnia)
- **Głośność / Pętla:** 40% | Pętla (Loop)
- **Kiedy odtworzyć:** Misja poboczna u fotografa, za ladą w ciemni OB_Lampa_Ciemnia.
- **Gdzie:** Ciemnia fotograficzna (fotograf_ciemnia).
- **Wyzwalacz w kodzie:** `Wejście gracza do pomieszczenia ciemni.`
- **Opis efektu:** Klimat ciemni fotograficznej: ciche, powolne kapanie wody z kranu do kuwety chemicznej, delikatne buczenie czerwonej lampy ciemniowej, stłumione echo.

---

### `amb_radiostacja_technika`
- **Plik audio:** `amb_radiostacja_technika.mp3`
- **Ścieżka w Unity:** `Resources/Ambience/amb_radiostacja_technika`
- **Kategoria:** Ambience (Wnętrze Radiostacji / Nadajnik)
- **Głośność / Pętla:** 55% | Pętla (Loop)
- **Kiedy odtworzyć:** Akt 3: Wejście do budynku nadajnika i pokoju technicznego.
- **Gdzie:** Sterownia i łącznica (radio_sterownia_mikrofon, radio_technika_kable).
- **Wyzwalacz w kodzie:** `Teleportacja przez spawn_radio_tyl.`
- **Opis efektu:** Przemysłowe, elektryczne buczenie aparatury nadawczej: szum wielkich lamp nadawczych, elektryczny pomruk łącznicy, buczenie transformatorów 100 kW.

---

### `amb_radiostacja_piwnica`
- **Plik audio:** `amb_radiostacja_piwnica.mp3`
- **Ścieżka w Unity:** `Resources/Ambience/amb_radiostacja_piwnica`
- **Kategoria:** Ambience (Wnętrze Piwnicy Radiostacji)
- **Głośność / Pętla:** 45% | Pętla (Loop)
- **Kiedy odtworzyć:** Akt 3: Zejście schodami do piwnicy z uwięzionymi pracownikami.
- **Gdzie:** Piwnica radiostacji (radio_piwnica_pracownik_1..3).
- **Wyzwalacz w kodzie:** `Zejście po schodach do piwnicy.`
- **Opis efektu:** Zimna, wilgotna piwnica stacji: stłumiony pogłos grubych betonowych murów, ciche kapanie wody z rur, odległe głuche kroki na stropie z góry.

---

### `sfx_drzwi_drewno`
- **Plik audio:** `sfx_drzwi_drewno.mp3`
- **Ścieżka w Unity:** `Resources/Sfx/sfx_drzwi_drewno`
- **Kategoria:** SFX Interakcji (Drzwi)
- **Głośność / Pętla:** 75% | Jednorazowy
- **Kiedy odtworzyć:** Przy każdej interakcji [E] z drzwiami budynków (hotel, księgarnia, fotograf, radiostacja).
- **Gdzie:** Obiekty OB_Drzwi w całej grze.
- **Wyzwalacz w kodzie:** `Wywołanie triggera Otworz w Animatorze drzwi.`
- **Opis efektu:** Otwarcie i zatrzaśnięcie ciężkich drewnianych drzwi z mosiężną klamką i cichym kliknięciem zamka.

---

### `sfx_aparat_leica_migawka`
- **Plik audio:** `sfx_aparat_leica_migawka.mp3`
- **Ścieżka w Unity:** `Resources/Sfx/sfx_aparat_leica_migawka`
- **Kategoria:** SFX Narzędzia (Aparat)
- **Głośność / Pętla:** 90% | Jednorazowy
- **Kiedy odtworzyć:** Zrobienie zdjęcia dowodu klawiszem [LPM] w trybie aparatu [F].
- **Gdzie:** Wszędzie, gdzie gracz fotografuje dowody (mapa, lista, Honiok).
- **Wyzwalacz w kodzie:** `Skrypt AparatFoto.ZrobZdjecie().`
- **Opis efektu:** Metaliczny, sprężynowy dźwięk mechanicznej migawki aparatu Leica z lat 30. z przesunięciem dźwigni przewijania filmu.

---

### `sfx_przeciecie_kabla_iskra`
- **Plik audio:** `sfx_przeciecie_kabla_iskra.mp3`
- **Ścieżka w Unity:** `Resources/Sfx/sfx_przeciecie_kabla_iskra`
- **Kategoria:** SFX Mini-gry (Łącznica)
- **Głośność / Pętla:** 85% | Jednorazowy
- **Kiedy odtworzyć:** Akt 3: Przecięcie kabla na tablicy łącznicy OB_Lacznica.
- **Gdzie:** Pokój techniczny radiostacji.
- **Wyzwalacz w kodzie:** `Animator kabla trigger Przetnij + FX_Iskra.`
- **Opis efektu:** Dźwięk cięcia grubego kabla szczypcami z gwałtownym trzaskiem zwarcia elektrycznego i skwierczeniem iskier.

---

