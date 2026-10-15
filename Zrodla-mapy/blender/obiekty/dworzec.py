# -*- coding: utf-8 -*-
"""
dworzec.py — Dworzec Gleiwitz Hbf 1939 (SENDER GLEIWITZ, hackaton).
Front budynku = +Y (konwencja biblioteki). Na mapie: miasto na POŁUDNIU,
więc ten obiekt trzeba obrócić o 180 stopni (wejście -Y, tory +Y) — patrz raport.
"""
import sys, os
sys.path.insert(0, '/Users/nataniel_bk/Documents/NBK/agent-dashboard/agent-workspace/otton/hackaton-gliwice/mapa/blender')
from lowpoly import *

# Zapisujemy FBX/PNG/meta u siebie, nie w UNITY_MODELE/PODGLADY/DANE z briefu.
WYJSCIE = os.path.join(os.path.dirname(os.path.abspath(__file__)))
PODGLADY_LOK = os.path.join(WYJSCIE, "podglady")
DANE_LOK = os.path.join(WYJSCIE, "dane")


def budynek_dworca():
    """Budynek główny: dwuskrzydłowy, środkowy pawilon wyższy z hallem.
    Lokalnie front = +Y (wejście), tory po stronie -Y (z tyłu budynku)."""
    m = Siatka("Dworzec")
    dl = 90.0          # długość wzdłuż X
    szer = 14.0        # głębokość wzdłuż Y
    h1 = 7.0           # wysokość 2 kondygnacji skrzydeł
    h_pawilon = 10.0   # wysokość pawilonu środkowego (do okapu)
    szer_pawilon = 18.0
    glab_pawilon = 16.0

    # --- SKRZYDŁA (lewe i prawe), dach dwuspadowy, kalenica wzdłuż X
    dl_skrzydlo = (dl - szer_pawilon) / 2 - 1.0
    for znak in (-1, 1):
        cx = znak * (szer_pawilon / 2 + dl_skrzydlo / 2 + 0.5)
        m.box((cx, 0, h1 / 2), (dl_skrzydlo, szer, h1), "cegla_jasna")
        # cokół
        m.box((cx, 0, 0.4), (dl_skrzydlo + 0.1, szer + 0.1, 0.8), "cokol")
        # gzyb / gzyms pod okapem
        m.box((cx, 0, h1 + 0.05), (dl_skrzydlo + 0.3, szer + 0.3, 0.12), "piaskowiec")
        # dach dwuspadowy (kalenica wzdłuż X -> dl_skrzydlo to "dl", szer to "szer")
        m.dach_dwuspadowy((cx, 0, h1), dl_skrzydlo, szer, 3.2, "dachowka_ciemna", m_szczyt="cegla_jasna")
        # okna rząd 1 (front +Y) - parter i piętro
        x0, x1 = cx - dl_skrzydlo / 2 + 1.0, cx + dl_skrzydlo / 2 - 1.0
        m.okna_rzad((x0, szer / 2), (x1, szer / 2), 1.3, 2.2, 1.1, 3.2, m_rama="rama")
        m.okna_rzad((x0, szer / 2), (x1, szer / 2), 4.3, 2.0, 1.1, 3.2, m_rama="rama")
        # okna z tyłu (strona torów, -Y)
        m.okna_rzad((x1, -szer / 2), (x0, -szer / 2), 1.3, 2.2, 1.1, 3.2, m_rama="rama")
        m.okna_rzad((x1, -szer / 2), (x0, -szer / 2), 4.3, 2.0, 1.1, 3.2, m_rama="rama")
        # pilastry (co ok. 3.2 m) na elewacji frontowej - detal bryłą
        n_pil = int(dl_skrzydlo // 3.2)
        for i in range(n_pil + 1):
            px = cx - dl_skrzydlo / 2 + i * (dl_skrzydlo / n_pil)
            m.box((px, szer / 2 + 0.08, h1 / 2), (0.35, 0.16, h1), "piaskowiec")

    # --- PAWILON ŚRODKOWY (hall), wyższy, dach czterospadowy
    m.box((0, 0, h_pawilon / 2), (szer_pawilon, glab_pawilon, h_pawilon), "tynk_ochra")
    m.box((0, 0, 0.45), (szer_pawilon + 0.15, glab_pawilon + 0.15, 0.9), "cokol")
    m.box((0, 0, h_pawilon + 0.06), (szer_pawilon + 0.3, glab_pawilon + 0.3, 0.14), "piaskowiec")
    m.dach_czterospadowy((0, 0, h_pawilon), szer_pawilon, glab_pawilon, 4.5, "lupek")

    # wejście główne na froncie pawilonu (+Y lokalnie) - drzwi + daszek, bez kolizji z oknami hallu
    m.box((0, glab_pawilon / 2 + 0.05, 1.6), (3.2, 0.3, 3.2), "drzwi_ciemne")
    m.box((0, glab_pawilon / 2 + 0.05, 3.3), (3.6, 0.1, 0.6), "piaskowiec", rot_z=0.0)  # nadproże
    # schody przed wejściem
    for i in range(3):
        m.box((0, glab_pawilon / 2 + 0.6 + i * 0.5, 0.1 * (i + 1) - 0.0), (4.4 - i * 0.3, 0.5, 0.18), "kamien")
    # daszek nad wejściem
    m.box((0, glab_pawilon / 2 + 0.9, 3.7), (4.2, 1.6, 0.14), "metal_ciemny")

    # okna hallu - wysokie, z łukiem (prostokąt + trójkątny szczyt), dalej od osi drzwi żeby nie kolidowały
    for znak in (-1, 1):
        wx = znak * (szer_pawilon / 2 - 2.6)
        y = glab_pawilon / 2 + 0.07
        m.box((wx, y, 2.6), (1.6, 0.14, 3.8), "rama")
        m.box((wx, y + 0.02, 2.6), (1.3, 0.06, 3.5), "okno")
        m.tri((wx - 0.8, y, 4.5), (wx + 0.8, y, 4.5), (wx, y, 5.3), "rama")
        m.tri((wx - 0.65, y + 0.02, 4.55), (wx + 0.65, y + 0.02, 4.55), (wx, y + 0.02, 5.2), "okno")
    # okna hallu z tyłu (strona torów)
    for znak in (-1, 1):
        wx = znak * (szer_pawilon / 2 - 2.6)
        y = -glab_pawilon / 2 - 0.07
        m.box((wx, y, 2.6), (1.6, 0.14, 3.8), "rama")
        m.box((wx, y - 0.02, 2.6), (1.3, 0.06, 3.5), "okno")
        m.tri((wx + 0.8, y, 4.5), (wx - 0.8, y, 4.5), (wx, y, 5.3), "rama")

    # zegar nad wejściem - tarcza płasko na ścianie (obudowa + cyferblat + wskazówki)
    zy = glab_pawilon / 2 + 0.06
    m.box((0, zy + 0.02, 6.6), (1.0, 0.08, 1.0), "piaskowiec")
    m.box((0, zy + 0.08, 6.6), (0.78, 0.04, 0.78), "tynk_bialy")
    m.box((0, zy + 0.11, 6.75), (0.05, 0.03, 0.3), "czarny")
    m.box((0.1, zy + 0.11, 6.62), (0.22, 0.03, 0.05), "czarny")

    # kominy na skrzydłach
    for znak in (-1, 1):
        cx = znak * (szer_pawilon / 2 + dl_skrzydlo / 2 + 0.5)
        m.box((cx, 2.5, h1 + 3.6), (0.7, 0.7, 1.4), "cegla_ciemna")

    return m.obiekt((0, 0, 0), kat=0.0)


def peron(nazwa, srodek_x, srodek_y, dlugosc, szerokosc=6.0, wys=0.5):
    m = Siatka(nazwa)
    m.box((srodek_x, srodek_y, wys / 2), (dlugosc, szerokosc, wys), "chodnik")
    # krawędzie (krawężniki) po obu stronach wzdłuż X
    for znak in (-1, 1):
        m.box((srodek_x, srodek_y + znak * szerokosc / 2, wys + 0.05), (dlugosc, 0.15, 0.1), "kraweznik")
    return m.obiekt((0, 0, 0), kat=0.0)


def wiata(nazwa, srodek_x, srodek_y, dlugosc=40.0):
    m = Siatka(nazwa)
    szer = 5.0
    h_slup = 3.4
    h_dach = 0.9
    n_slupow = max(2, int(dlugosc // 8) + 1)
    for i in range(n_slupow):
        px = srodek_x - dlugosc / 2 + i * (dlugosc / (n_slupow - 1))
        for znak in (-1, 1):
            m.walec((px, srodek_y + znak * (szer / 2 - 0.2), 0), 0.12, h_slup, "metal", n=8)
    # dach dwuspadowy płytki na wiacie
    m.dach_dwuspadowy((srodek_x, srodek_y, h_slup), dlugosc, szer, 1.1, "blacha", m_szczyt="metal_ciemny", okap=0.6)
    return m.obiekt((0, 0, 0), kat=0.0)


def tory(nazwa, srodek_x, srodek_y, dlugosc, liczba_torow=4, rozstaw_torow=4.0):
    m = Siatka(nazwa)
    rozstaw_szyn = 1.435
    for t in range(liczba_torow):
        ty = srodek_y + (t - (liczba_torow - 1) / 2.0) * rozstaw_torow
        # podsypka żwirowa
        m.box((srodek_x, ty, 0.1), (dlugosc, 2.4, 0.2), "zwir")
        # podkłady co 0.8 m
        n_podkladow = int(dlugosc // 0.8)
        for i in range(n_podkladow):
            px = srodek_x - dlugosc / 2 + i * 0.8
            m.box((px, ty, 0.22), (0.22, 2.2, 0.16), "podklad")
        # szyny
        for znak in (-1, 1):
            m.box((srodek_x, ty + znak * rozstaw_szyn / 2, 0.3), (dlugosc, 0.08, 0.14), "szyna")
    return m.obiekt((0, 0, 0), kat=0.0)


def postoj_dorozek(srodek_x, srodek_y, dl=10.0, szer=4.0):
    m = Siatka("Dworzec_Postoj_Dorozek")
    m.box((0, 0, 0.08), (dl, szer, 0.16), "bruk")
    for znak in (-1, 1):
        m.box((0, znak * szer / 2, 0.16 + 0.05), (dl, 0.15, 0.1), "kraweznik")
    return m.obiekt((srodek_x, srodek_y, 0), kat=0.0)


def zbuduj():
    """Buduje obiekty w origin (0,0,0), lokalnie front budynku = +Y (strona torów to -Y w tym module).
    UWAGA do złożenia mapy: w mieście wejście ma być od strony miasta (-Y globalnie),
    tory na północy (+Y globalnie) -> przy wstawianiu w scenę obrócić ten moduł o 180 stopni
    (albo zbudować od razu odwrotnie - patrz raport)."""
    obiekty = []

    dworzec = budynek_dworca()
    obiekty.append(dworzec)

    szer_pawilon = 18.0
    glab_pawilon = 16.0
    dl_peronow = 160.0
    odstep_tory = 10.0  # od tyłu budynku (-Y, strona torów) do peronu 1

    y_tory_start = -(glab_pawilon / 2 + odstep_tory)
    # peron 1 (bliżej budynku), peron 2 dalej
    y_peron1 = y_tory_start - 3.0
    y_peron2 = y_peron1 - 10.0

    p1 = peron("Dworzec_Peron_1", 0, y_peron1, dl_peronow)
    p2 = peron("Dworzec_Peron_2", 0, y_peron2, dl_peronow)
    obiekty += [p1, p2]

    w1 = wiata("Dworzec_Wiata_1", 0, y_peron1, dlugosc=40.0)
    w2 = wiata("Dworzec_Wiata_2", 0, y_peron2, dlugosc=40.0)
    obiekty += [w1, w2]

    # tory: 4 tory między/za peronami, środek pomiędzy peron1 i peron2 oraz za peron2
    y_tory_srodek = (y_peron1 - y_peron2) / 2 + y_peron2
    tr = tory("Dworzec_Tory", 0, y_tory_srodek, dl_peronow, liczba_torow=4, rozstaw_torow=4.0)
    obiekty.append(tr)

    # drzewa przed dworcem (strona wejścia, lokalnie +Y)
    drzewa_poz = [(-14, glab_pawilon / 2 + 6, 0), (14, glab_pawilon / 2 + 6, 1),
                  (-22, glab_pawilon / 2 + 9, 2), (22, glab_pawilon / 2 + 9, 3)]
    for i, (dx, dy, seed) in enumerate(drzewa_poz):
        d = drzewo("Drzewo_Dworzec_%d" % (i + 1), typ=0 if i % 2 == 0 else 3, ziarno=seed, skala=1.1)
        ob = d.obiekt((dx, dy, 0), kat=0.0)
        obiekty.append(ob)

    # postój dorożek (krawężnik), z boku przed wejściem
    dorozki = postoj_dorozek(28, glab_pawilon / 2 + 4, dl=10.0, szer=4.0)
    obiekty.append(dorozki)

    # --- PUNKTY GRY ---
    # yaw: 0 = patrzy na +Y (lokalnie "w stronę wyjścia do miasta", bo front modułu = +Y)
    y_peron1_front = glab_pawilon / 2 + 2  # pozycja odniesienia nieużywana, punkty dajemy wprost na peronie
    punkt("spawn_dworzec", (-20, y_peron1, 0.5), 0.0)
    punkt("miasto_kolejarz", (5, y_peron1, 0.5), 180.0)
    punkt("miasto_karta_4", (0, glab_pawilon / 2 + 1.5, 0.0), 0.0)
    punkt("miasto_tlum_1", (-30, y_peron1, 0.5), 90.0)
    punkt("miasto_tlum_2", (0, y_peron1, 0.5), -90.0)
    punkt("miasto_tlum_3", (30, y_peron1, 0.5), 0.0)

    # światła na peronach (lampy wiat)
    for sx in (-30, 0, 30):
        swiatlo((sx, y_peron1, 3.2), kolor="#ffcf8a", zasieg=10, moc=2.5)
        swiatlo((sx, y_peron2, 3.2), kolor="#ffcf8a", zasieg=10, moc=2.5)

    return obiekty


if __name__ == "__main__":
    wyczysc_scene()
    obs = zbuduj()
    eksport_fbx(obs, os.path.join(WYJSCIE, "Dworzec.fbx"))
    zapisz_meta(os.path.join(DANE_LOK, "meta_dworzec.json"))
    ziemia_podgladu(140)
    render_podglad(os.path.join(PODGLADY_LOK, "dworzec_1.png"), cel=(0, 0, 4), odl=70, azymut=195, wysokosc_kat=22)
    render_podglad(os.path.join(PODGLADY_LOK, "dworzec_2.png"), cel=(0, -20, 4), odl=90, azymut=15, wysokosc_kat=20)
    render_podglad(os.path.join(PODGLADY_LOK, "dworzec_3.png"), cel=(0, 20, 2), odl=18, azymut=200, wysokosc_kat=8)
