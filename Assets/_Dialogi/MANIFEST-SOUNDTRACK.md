# SENDER GLEIWITZ 31.08.1939 — SOUNDTRACK & SYSTEM MUZYKI DYNAMICZNEJ

Dokumentacja przygotowana dla programisty i AI Unity (ROLA B — UNITY) do obsługi ścieżki dźwiękowej gry.

## 1. Architektura systemu muzyki w Unity (C#)
Wszystkie utwory zostały wygenerowane i skopiowane do folderu:
`Assets/_Dialogi/Resources/Music/`

### Przykładowy skrypt w C# do płynnego przejścia muzyki:
```csharp
using UnityEngine;
using System.Collections;

public class MuzykaGry : MonoBehaviour
{
    public static MuzykaGry Instancja;
    private AudioSource zrodloA;
    private AudioSource zrodloB;
    private bool uzywaZrodlaA = true;

    void Awake()
    {
        if (Instancja == null) Instancja = this; else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
        zrodloA = gameObject.AddComponent<AudioSource>();
        zrodloB = gameObject.AddComponent<AudioSource>();
        zrodloA.loop = true; zrodloB.loop = true;
    }

    public void ZmienMuzyke(string idUtworu, float czasPrzejscia = 1.5f, float glosnoscDocelowa = 0.8f, bool zapetlij = true)
    {
        AudioClip klip = Resources.Load<AudioClip>("Music/" + idUtworu);
        if (klip == null) { Debug.LogWarning("Brak utworu: " + idUtworu); return; }
        StopAllCoroutines();
        StartCoroutine(PlynnePrzejscie(klip, czasPrzejscia, glosnoscDocelowa, zapetlij));
    }

    private IEnumerator PlynnePrzejscie(AudioClip nowyKlip, float czas, float glosnosc, bool loop)
    {
        AudioSource aktywne = uzywaZrodlaA ? zrodloA : zrodloB;
        AudioSource nowe = uzywaZrodlaA ? zrodloB : zrodloA;
        uzywaZrodlaA = !uzywaZrodlaA;

        nowe.clip = nowyKlip;
        nowe.loop = loop;
        nowe.volume = 0f;
        nowe.Play();

        float timer = 0f;
        float startVolAktywne = aktywne.volume;
        while (timer < czas)
        {
            timer += Time.deltaTime;
            float t = timer / czas;
            aktywne.volume = Mathf.Lerp(startVolAktywne, 0f, t);
            nowe.volume = Mathf.Lerp(0f, glosnosc, t);
            yield return null;
        }
        aktywne.Stop();
        nowe.volume = glosnosc;
    }
}
```

---

## 2. Katalog Utworów i Przeznaczenie w Grze

### `mus_menu_main_theme` — Motyw Przewodni / Menu Główne (Sender Gleiwitz 1939)
- **Plik audio:** `mus_menu_main_theme.mp3`
- **Ścieżka w Unity:** `Resources/Music/mus_menu_main_theme`
- **Typ utworu:** Elegijny / Noir / Motyw Główny
- **Domyślna głośność:** 85%
- **Pętla (Loop):** TAK (odtwarzaj w pętli)
- **Zalecany czas przejścia (Crossfade):** 1.5 s
- **Kiedy odtworzyć:** W Menu Głównym gry (Menu.unity) oraz na ekranie wczytywania
- **Gdzie na mapie:** Menu główne, plansza startowa
- **Wyzwalacz w kodzie Unity:** `Start sceny Menu.unity w funkcji Start() AudioManager.GrajMuzyke('mus_menu_main_theme')`
- **Opis i klimat:** Mroczny, elegijny noir z 1939 roku. Solowe skrzypce, melancholijna wiolonczela i powolne, subtelne pianino. Wprowadza gracza w klimat przedwojennego Śląska i nieuchronnej tragedii.

---

### `mus_eksploracja_spokojna` — Spokojna Eksploracja (Ulice Gleiwitz, Rynek, Księgarnia)
- **Plik audio:** `mus_eksploracja_spokojna.mp3`
- **Ścieżka w Unity:** `Resources/Music/mus_eksploracja_spokojna`
- **Typ utworu:** Spokojny / Vintage Ambient
- **Domyślna głośność:** 65%
- **Pętla (Loop):** TAK (odtwarzaj w pętli)
- **Zalecany czas przejścia (Crossfade):** 2.0 s
- **Kiedy odtworzyć:** Akt 1 (15:30) oraz spokojne momenty Aktu 2 przed zmrokiem
- **Gdzie na mapie:** Dworzec peron, ulica Wilhelmstraße, Ring, wnętrze księgarni Zofii
- **Wyzwalacz w kodzie Unity:** `Po zakończeniu monologu a1_intro lub wejściu na otwartą ulicę w stanie gry Neutralny`
- **Opis i klimat:** Ciepłe, parne popołudnie 31 sierpnia. Stonowany kontrabas pizzicato, cichy klarnet, powolny rytm miejski. Gracz czuje się jak incognito kupiec z Bytomia.

---

### `mus_skradanie_napiecie` — Budowanie Napięcia / Skradanie (Hotel, Cienie, Zakazane Strefy)
- **Plik audio:** `mus_skradanie_napiecie.mp3`
- **Ścieżka w Unity:** `Resources/Music/mus_skradanie_napiecie`
- **Typ utworu:** Budujący Napięcie / Suspens / Skradanie
- **Domyślna głośność:** 70%
- **Pętla (Loop):** TAK (odtwarzaj w pętli)
- **Zalecany czas przejścia (Crossfade):** 1.0 s
- **Kiedy odtworzyć:** Kiedy gracz kuca [C], wchodzi do hotelu Haus Oberschlesien lub na piętro pokoju Naujocksa
- **Gdzie na mapie:** Korytarze hotelu, pokój Naujocksa, za kolumną w holu, teren radiostacji za płotem
- **Wyzwalacz w kodzie Unity:** `Gdy PostacGracza.JestWKucaniu == true LUB gracz wejdzie w trigger strefy zakazanej (Zone_Hotel_Pietro)`
- **Opis i klimat:** Przytłumione smyczki, basowy puls serca, ciche metaliczne skrzypienie. Buduje poczucie bycia w zasięgu wzroku wroga.

---

### `mus_sledzenie_auta` — Śledzenie Samochodu SD (Wilhelmstraße i Zmrok)
- **Plik audio:** `mus_sledzenie_auta.mp3`
- **Ścieżka w Unity:** `Resources/Music/mus_sledzenie_auta`
- **Typ utworu:** Rytmiczny Suspens / Śledzenie
- **Domyślna głośność:** 75%
- **Pętla (Loop):** TAK (odtwarzaj w pętli)
- **Zalecany czas przejścia (Crossfade):** 1.2 s
- **Kiedy odtworzyć:** Akt 2 (16:30–19:45): Podążanie za czarnym autem SD z hotelu do zagajnika
- **Gdzie na mapie:** Trasa auta miasto_auto_sd_trasa_1..5, most na Kłodnicy, zapalające się latarnie
- **Wyzwalacz w kodzie Unity:** `Rozpoczęcie misji a2_start po wyjściu z hotelu i ruszeniu auta`
- **Opis i klimat:** Hipotetyczny, ostrożny rytm szorowanych szczotkami bębnów, nerwowe ostinato na kontrabasie, odgłos tykającego zegara.

---

### `mus_akcja_strzelanina` — Dynamiczna Walka / Strzelanina (Zdemaskowanie przez SD)
- **Plik audio:** `mus_akcja_strzelanina.mp3`
- **Ścieżka w Unity:** `Resources/Music/mus_akcja_strzelanina`
- **Typ utworu:** Dynamiczny / Walka / Akcja
- **Domyślna głośność:** 90%
- **Pętla (Loop):** TAK (odtwarzaj w pętli)
- **Zalecany czas przejścia (Crossfade):** 0.5 s
- **Kiedy odtworzyć:** W momencie podniesienia alarmu przez wroga i rozpoczęcia ostrzału
- **Gdzie na mapie:** Dowolne miejsce na mapie w trybie walki (stan AI: Alarm / Combat)
- **Wyzwalacz w kodzie Unity:** `Dowolny wróg przechodzi w stan PostacAI.Stan == StanAI.Alarm (wykrycie gracza)`
- **Opis i klimat:** Agresywna, wojenna perkusja, ostre staccato smyczków, dramatyczne uderzenia sekcji dętej. Potęguje fakt, że Kruk ginie od jednej kuli.

---

### `mus_kulminacja_lacznica` — Wyścig z Czasem / Łącznica (90 Sekund do Przecięcia Kabli)
- **Plik audio:** `mus_kulminacja_lacznica.mp3`
- **Ścieżka w Unity:** `Resources/Music/mus_kulminacja_lacznica`
- **Typ utworu:** Kulminacja / Ticking Clock / Ekstremalne Napięcie
- **Domyślna głośność:** 85%
- **Pętla (Loop):** TAK (odtwarzaj w pętli)
- **Zalecany czas przejścia (Crossfade):** 0.8 s
- **Kiedy odtworzyć:** Akt 3 (20:03): Heinz nadaje odezwę przez mikrofon burzowy, licznik 90 s
- **Gdzie na mapie:** Pokój techniczny radiostacji przy szafach łącznicy (radio_technika_kable)
- **Wyzwalacz w kodzie Unity:** `Wyzwalacz sceny a3_komunikat i start timera 90 sekund`
- **Opis i klimat:** Frantyczny, przyspieszający puls elektroniczno-orkiestrowy, imitacja tykającego detonatora, rosnące crescendo.

---

### `mus_ucieczka_pola` — Wielka Ucieczka (Pościg SD przez Pola do Stodoły)
- **Plik audio:** `mus_ucieczka_pola.mp3`
- **Ścieżka w Unity:** `Resources/Music/mus_ucieczka_pola`
- **Typ utworu:** Ucieczka / Pościg / Adrenalina
- **Domyślna głośność:** 90%
- **Pętla (Loop):** TAK (odtwarzaj w pętli)
- **Zalecany czas przejścia (Crossfade):** 0.6 s
- **Kiedy odtworzyć:** Akt 4 (20:11–20:20): Globalny alarm po zrobieniu zdjęcia ciała Honioka
- **Gdzie na mapie:** Pola na wschód od radiostacji, rowy melioracyjne, sprint do stodoły kuriera Sowy
- **Wyzwalacz w kodzie Unity:** `Po zakończeniu dialogu a4_alarm i włączeniu syren miejskich`
- **Opis i klimat:** Niepowstrzymane, pędzące bębny orkiestrowe, dramatyczne frazy skrzypcowe, odgłos syren w tle, bieg z rolką filmu w kieszeni.

---

### `mus_epilog_pamiec` — Epilog / Pamięć Historii (Norymberga, Westerplatte, Honiok)
- **Plik audio:** `mus_epilog_pamiec.mp3`
- **Ścieżka w Unity:** `Resources/Music/mus_epilog_pamiec`
- **Typ utworu:** Podniosły / Requiem / Zaduma Historyczna
- **Domyślna głośność:** 80%
- **Pętla (Loop):** TAK (odtwarzaj w pętli)
- **Zalecany czas przejścia (Crossfade):** 2.0 s
- **Kiedy odtworzyć:** W trakcie odtwarzania plansz tekstowych Epilogu (epilog_1..6) po ucieczce
- **Gdzie na mapie:** Czarne plansze końcowe epilogu
- **Wyzwalacz w kodzie Unity:** `Start sekwencji epilogu po oddaniu filmu w stodole Sowy`
- **Opis i klimat:** Głębokie requiem symfoniczne, potężny smutek smyczków, daleki żałobny dzwon. Podkreśla zdanie: 'Prowokacja trwała kilka minut. Wojna trwała sześć lat'.

---

### `mus_porazka_game_over` — Porażka Misji / Zdemaskowanie (Game Over)
- **Plik audio:** `mus_porazka_game_over.mp3`
- **Ścieżka w Unity:** `Resources/Music/mus_porazka_game_over`
- **Typ utworu:** Dramatyczny / Koniec Gry
- **Domyślna głośność:** 85%
- **Pętla (Loop):** NIE (pojedynczy motyw)
- **Zalecany czas przejścia (Crossfade):** 0.2 s
- **Kiedy odtworzyć:** W momencie trafienia kulą, zdemaskowania lub zastrzelenia cywila
- **Gdzie na mapie:** Ekran końca gry (UI Porażka)
- **Wyzwalacz w kodzie Unity:** `Wywołanie funkcji GameManager.KoniecMisji(powod)`
- **Opis i klimat:** Nagły, głęboki dysonans wiolonczeli i kotłów, opadający motyw porażki, po którym zapada głucha cisza.

---


## 3. Dodatkowe Utwory Sytuacyjne i Dźwięk Przestrzenny 3D (Wygenerowane)

### `mus_3d_gramofon_kawiarnia` — Vintage Swing z Kawiarni na Ringu (Gramofon 3D)
- **Plik audio:** `mus_3d_gramofon_kawiarnia.mp3`
- **Ścieżka w Unity:** `Resources/Music/mus_3d_gramofon_kawiarnia`
- **Typ utworu:** Muzyka Przestrzenna 3D / Vintage Szelak 1939
- **Domyślna głośność:** 65%
- **Pętla (Loop):** TAK (odtwarzaj w pętli)
- **Zalecany czas przejścia (Crossfade):** 1.0 s
- **Kiedy odtworzyć:** Przez całe popołudnie (Akt 1 i początek Aktu 2), gdy kawiarnia na Ringu jest otwarta
- **Gdzie na mapie:** Rynek (Ring), stoliki kawiarniane Café przy fontannie, punkt miasto_tlum_2
- **Wyzwalacz w kodzie Unity:** `Komponent AudioSource na obiekcie OB_Gramofon z włączonym Spatial Blend = 1.0 (Dźwięk 3D, zasięg 15 m)`
- **Opis i klimat:** Nostalgiczny, lekki swing kawiarniany z lat 30. odtwarzany ze starej płyty gramofonowej. Słychać ciepłą trąbkę z tłumikiem, gitarę akustyczną i charakterystyczny, miękki trzask igły szelakowej. Tworzy uderzający kontrast beztroskiego życia cywilów ze śmiertelną misją Kruka.

---

### `mus_zagajnik_konserwa` — Tragedia w Zagajniku (Bolesna Bezradność przy Honioku)
- **Plik audio:** `mus_zagajnik_konserwa.mp3`
- **Ścieżka w Unity:** `Resources/Music/mus_zagajnik_konserwa`
- **Typ utworu:** Dramatyczny / Elegijny / Emocjonalna Kulminacja
- **Domyślna głośność:** 75%
- **Pętla (Loop):** TAK (odtwarzaj w pętli)
- **Zalecany czas przejścia (Crossfade):** 1.5 s
- **Kiedy odtworzyć:** Akt 2 (19:40): Kruk kuca w krzakach zagajnika i widzi, jak esesmani wyciągają nieprzytomnego Franciszka Honioka
- **Gdzie na mapie:** Zagajnik na zachód od radiostacji (miasto_zagajnik_krzaki, miasto_zagajnik_auto)
- **Wyzwalacz w kodzie Unity:** `Wejście gracza w strefę miasto_zagajnik_krzaki i kucnięcie`
- **Opis i klimat:** Przejmujący, minimalistyczny motyw na płaczącą wiolonczelę solo i chłodny powiew wiatru. Podkreśla absolutną bezradność agenta: Kruk jest sam na sześciu uzbrojonych esesmanów. Nie może strzelać – może jedynie uwiecznić prawdę na kliszy.

---

### `mus_fotograf_ciemnia_noir` — Klaustrofobia w Ciemni (Spotkanie z Gestapo)
- **Plik audio:** `mus_fotograf_ciemnia_noir.mp3`
- **Ścieżka w Unity:** `Resources/Music/mus_fotograf_ciemnia_noir`
- **Typ utworu:** Klaustrofobiczny Suspens / Noir
- **Domyślna głośność:** 70%
- **Pętla (Loop):** TAK (odtwarzaj w pętli)
- **Zalecany czas przejścia (Crossfade):** 1.2 s
- **Kiedy odtworzyć:** Misja poboczna u fotografa: wejście agenta Gestapo do lokalu i rozmowa z Krukiem w ciemni
- **Gdzie na mapie:** Zakład fotograficzny na Ringu, ciemnia (spawn_fotograf, fotograf_ciemnia)
- **Wyzwalacz w kodzie Unity:** `Wyzwalacz sceny pob_fotograf_gestapo`
- **Opis i klimat:** Duszny, paranoiczny minimalizm w stylu szpiegowskiego noir. Pojedyncze, wyczekujące nuty fortepianu, lodowate flażolety smyczkowe i ciężka cisza. Buduje napięcie podczas przesłuchania pod czerwonym światłem ciemni.

---

### `mus_zmrok_przejscie` — Zmierzch nad Gliwicami (Godzina 19:00 - Przejście w Noc)
- **Plik audio:** `mus_zmrok_przejscie.mp3`
- **Ścieżka w Unity:** `Resources/Music/mus_zmrok_przejscie`
- **Typ utworu:** Sygnał Narracyjny (Transition Stinger)
- **Domyślna głośność:** 85%
- **Pętla (Loop):** NIE (pojedynczy motyw)
- **Zalecany czas przejścia (Crossfade):** 2.0 s
- **Kiedy odtworzyć:** Godzina 19:00 w zegarze gry: zachód słońca, zmiana oświetlenia na granatowe, zapalenie gazowych latarni
- **Gdzie na mapie:** Ulice Gliwic (Wilhelmstraße, most na Kłodnicy, Tarnowitzer Landstraße)
- **Wyzwalacz w kodzie Unity:** `Skrypt ZegarGry po osiągnięciu godziny 19:00 wywołuje MuzykaGry.Instancja.ZmienMuzyke('mus_zmrok_przejscie', 2.0f, 0.85f, false)`
- **Opis i klimat:** Potężny, powolny motyw orkiestrowy z wejściem niskich waltorni i falujących smyczków. Zwiastuje graczowi, że beztroski dzień się skończył i rozpoczyna się niebezpieczna nocna infiltracja.

---

### `mus_ucieczka_rower` — Cichy Pęd Nocą (Ucieczka Rowerem przez Pola)
- **Plik audio:** `mus_ucieczka_rower.mp3`
- **Ścieżka w Unity:** `Resources/Music/mus_ucieczka_rower`
- **Typ utworu:** Rytmiczny / Cichy Pościg / Stealth Sprint
- **Domyślna głośność:** 80%
- **Pętla (Loop):** TAK (odtwarzaj w pętli)
- **Zalecany czas przejścia (Crossfade):** 0.8 s
- **Kiedy odtworzyć:** Akt 4 (20:12): Gdy gracz zamiast biec pieszo wsiada na rower OB_Rower i pędzi polnymi miedzami
- **Gdzie na mapie:** Pola na wschód od radiostacji (miasto_rower -> miasto_stodola)
- **Wyzwalacz w kodzie Unity:** `Interakcja gracza z rowerem OB_Rower (Kruk wsiada na rower)`
- **Opis i klimat:** Szybki, synkopowany, cichy puls akustyczny z szelestem miotełek na bębnie i miękkim basem. Oddaje dynamiczną, ale dyskretną ucieczkę na dwóch kółkach w chłodnym nocnym powietrzu.

---

