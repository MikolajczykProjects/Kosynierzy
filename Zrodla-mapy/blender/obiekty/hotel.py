# -*- coding: utf-8 -*-
"""
hotel.py — Hotel "Haus Oberschlesien" 1928 (arch. Richard Gaze), hackaton Gliwice.
Monumentalny modernizm z neoklasycyzmem. Front budynku = +Y (konwencja biblioteki,
tak jak w dworzec.py) — wejście od strony ulicy.
"""
import sys, os
sys.path.insert(0, '/Users/nataniel_bk/Documents/NBK/agent-dashboard/agent-workspace/otton/hackaton-gliwice/mapa/blender')
from lowpoly import *

WYJSCIE = os.path.join(os.path.dirname(os.path.abspath(__file__)))
PODGLADY_LOK = os.path.join(WYJSCIE, "podglady")
DANE_LOK = os.path.join(WYJSCIE, "dane")


def lukarna(m, x, y_front, z0, m_dach="dachowka_ciemna"):
    """Mała lukarna w dachu mansardowym: dwuspadowy daszek z oknem."""
    m.box((x, y_front, z0 + 0.5), (1.4, 1.0, 1.0), "tynk_kremowy")
    m.box((x, y_front + 0.02, z0 + 0.5), (0.9, 0.06, 0.7), "okno")
    m.dach_dwuspadowy((x, y_front, z0 + 1.0), 1.5, 1.1, 0.7, m_dach, okap=0.2)


def budynek_hotel():
    """Plan U: fasada 75 m wzdłuż X, dwa skrzydła do tyłu (-Y lokalnie), głębokość 45 m,
    szerokość skrzydeł 14 m. Front (wejście) = +Y."""
    m = Siatka("Hotel")
    fasada = 75.0
    szer_skrzydlo = 14.0
    glebokosc = 45.0
    h_parter = 4.5
    h_pietro = 3.4
    n_pieter = 5
    h_do_dachu = h_parter + n_pieter * h_pietro          # wys. korony skrzydeł
    h_ryzalit = h_do_dachu + h_pietro                    # ryzalit o piętro wyższy
    szer_ryzalit = 20.0
    wysun_ryzalit = 2.0

    # --- SKRZYDŁA BOCZNE (plan U: idą do tyłu, -Y) ---
    # oś fasady na y=0 (front), skrzydła ciągną się od y=0 do y=-głębokość
    for znak in (-1, 1):
        cx = znak * (fasada / 2 - szer_skrzydlo / 2)
        cy = -glebokosc / 2
        # korpus skrzydła
        m.box((cx, cy, h_do_dachu / 2), (szer_skrzydlo, glebokosc, h_do_dachu), "tynk_kremowy")
        # cokół
        m.box((cx, cy, 0.5), (szer_skrzydlo + 0.1, glebokosc + 0.1, 1.0), "piaskowiec")
        # gzyms nad parterem
        m.box((cx, cy, h_parter + 0.05), (szer_skrzydlo + 0.4, glebokosc + 0.4, 0.15), "piaskowiec")
        # gzyms pod dachem
        m.box((cx, cy, h_do_dachu + 0.05), (szer_skrzydlo + 0.4, glebokosc + 0.4, 0.15), "piaskowiec")
        # dach mansardowy czterospadowy
        m.dach_czterospadowy((cx, cy, h_do_dachu), szer_skrzydlo, glebokosc, 3.0, "dachowka_ciemna")
        # lukarny na zewnętrznej ścianie długiej (strona ulicy / zewnątrz)
        y_zewn = znak * 1  # tylko do wyboru ściany zewnętrznej X
        xw = cx + znak * (szer_skrzydlo / 2 + 0.05)
        n_luk = int(glebokosc // 7)
        for i in range(n_luk):
            ly = -1.5 - i * 7.0
            lukarna(m, xw if False else cx, ly, h_do_dachu)  # lukarny patrzą na +X/-X ściankę zewn. - uproszczone jako na osi dachu
        # lizeny na zewnętrznej ścianie (X) co 5 m
        n_liz = int(glebokosc // 5)
        for i in range(n_liz + 1):
            ly = -i * (glebokosc / n_liz)
            m.box((cx + znak * (szer_skrzydlo / 2 + 0.08), ly, h_do_dachu / 2), (0.15, 0.6, h_do_dachu), "piaskowiec")
        # okna na każdym piętrze, ściana zewnętrzna (X) i wewnętrzna (dwór)
        for p in range(n_pieter):
            z0 = h_parter + p * h_pietro + 0.6
            m.okna_rzad((cx + znak * szer_skrzydlo / 2, -1.5), (cx + znak * szer_skrzydlo / 2, -glebokosc + 1.5),
                        z0, 2.0, 1.1, 3.5, m_rama="rama") if False else None
        # (powyżej okna_rzad wymaga a!=b w X lub Y w linii prostej - robimy je wzdłuż Y:)
        for p in range(n_pieter):
            z0 = h_parter + p * h_pietro + 0.6
            xw_ext = cx + znak * (szer_skrzydlo / 2 + 0.03)
            m.okna_rzad((xw_ext, -1.5), (xw_ext, -glebokosc + 1.5), z0, 2.0, 1.1, 3.5, m_rama="rama")
            xw_int = cx - znak * (szer_skrzydlo / 2 + 0.03)
            m.okna_rzad((xw_int, -glebokosc + 1.5), (xw_int, -1.5), z0, 2.0, 1.1, 3.5, m_rama="rama")
        # okna na szczycie skrzydła (ściana końcowa -Y, tył budynku)
        for p in range(n_pieter):
            z0 = h_parter + p * h_pietro + 0.6
            m.okna_rzad((cx - szer_skrzydlo / 2 + 2.0, -glebokosc - 0.03), (cx + szer_skrzydlo / 2 - 2.0, -glebokosc - 0.03),
                        z0, 2.0, 1.1, 4.0, m_rama="rama")

    # --- CZĘŚĆ ŁĄCZĄCA (tył planu U, między skrzydłami, zamyka podwórze) ---
    szer_laczaca = fasada - 2 * szer_skrzydlo
    m.box((0, -glebokosc + szer_skrzydlo / 2, h_do_dachu / 2), (szer_laczaca, szer_skrzydlo, h_do_dachu), "tynk_kremowy")
    m.box((0, -glebokosc + szer_skrzydlo / 2, 0.5), (szer_laczaca + 0.1, szer_skrzydlo + 0.1, 1.0), "piaskowiec")
    m.dach_czterospadowy((0, -glebokosc + szer_skrzydlo / 2, h_do_dachu), szer_laczaca, szer_skrzydlo, 2.6, "dachowka_ciemna")
    for p in range(n_pieter):
        z0 = h_parter + p * h_pietro + 0.6
        m.okna_rzad((-szer_laczaca / 2 + 1.5, -glebokosc - 0.03), (szer_laczaca / 2 - 1.5, -glebokosc - 0.03),
                    z0, 2.0, 1.1, 4.0, m_rama="rama")

    # --- FASADA GŁÓWNA (front, +Y), łącząca skrzydła na linii y=0, poza ryzalitem ---
    dl_fasada_bok = (fasada - szer_ryzalit) / 2
    for znak in (-1, 1):
        cx = znak * (szer_ryzalit / 2 + dl_fasada_bok / 2)
        # korpus fasady frontowej (płytki, szerokość skrzydła, łączy z korpusem skrzydła)
        m.box((cx, -szer_skrzydlo / 2, h_do_dachu / 2), (dl_fasada_bok, szer_skrzydlo, h_do_dachu), "tynk_kremowy")
        m.box((cx, -szer_skrzydlo / 2, 0.5), (dl_fasada_bok + 0.1, szer_skrzydlo + 0.1, 1.0), "piaskowiec")
        # cokół front
        m.box((cx, 0.0, 0.5), (dl_fasada_bok, 0.3, 1.0), "piaskowiec")
        # gzymsy front
        m.box((cx, -0.1, h_parter + 0.05), (dl_fasada_bok, szer_skrzydlo + 0.3, 0.15), "piaskowiec")
        m.box((cx, -0.1, h_do_dachu + 0.05), (dl_fasada_bok, szer_skrzydlo + 0.3, 0.15), "piaskowiec")
        # arkady parteru: filary 1 m co 5 m, podcień 1.5 m głębokości
        n_arkad = max(2, int(dl_fasada_bok // 5))
        x0 = cx - dl_fasada_bok / 2
        for i in range(n_arkad + 1):
            px = x0 + i * (dl_fasada_bok / n_arkad)
            m.box((px, -0.1, h_parter / 2), (1.0, 0.3, h_parter), "piaskowiec")
        # witryny i podcień między filarami (lekko wsunięte, głębokość 1.5 m)
        m.box((cx, -0.75, h_parter / 2), (dl_fasada_bok - 1.2, 1.5, h_parter - 0.3), "okno_swiatlo")
        # markizy nad witrynami
        m.box((cx, 0.65, h_parter - 0.2), (dl_fasada_bok - 1.6, 1.3, 0.08), "markiza_kremowa")
        # lizeny na fasadzie co 5 m
        for i in range(n_arkad + 1):
            px = x0 + i * (dl_fasada_bok / n_arkad)
            m.box((px, 0.12, h_do_dachu / 2), (0.6, 0.15, h_do_dachu), "piaskowiec")
        # okna_rzad na każdym piętrze
        for p in range(n_pieter):
            z0 = h_parter + p * h_pietro + 0.6
            m.okna_rzad((x0 + 0.5, 0.05), (x0 + dl_fasada_bok - 0.5, 0.05), z0, 2.0, 1.1, 3.5, m_rama="rama")
        # dach dwuspadowy łączący z korpusem skrzydła (prosta kontynuacja, pokrywa narożnik)
        m.dach_czterospadowy((cx, -szer_skrzydlo / 2, h_do_dachu), dl_fasada_bok, szer_skrzydlo, 2.4, "dachowka_ciemna")

    # --- RYZALIT ŚRODKOWY: 20 m szer., wysunięty 2 m, o piętro wyższy, wejście ---
    gl_ryz = szer_skrzydlo + wysun_ryzalit
    cy_ryz = -gl_ryz / 2 + wysun_ryzalit  # środek bryły ryzalitu (żeby front wystawał o 2 m przed y=0)
    m.box((0, cy_ryz, h_ryzalit / 2), (szer_ryzalit, gl_ryz, h_ryzalit), "tynk_kremowy")
    m.box((0, cy_ryz, 0.5), (szer_ryzalit + 0.15, gl_ryz + 0.15, 1.0), "piaskowiec")
    m.box((0, cy_ryz - gl_ryz / 2 + 0.05, h_parter + 0.05), (szer_ryzalit + 0.4, 0.3, 0.15), "piaskowiec")
    m.box((0, cy_ryz, h_ryzalit + 0.06), (szer_ryzalit + 0.4, gl_ryz + 0.4, 0.15), "piaskowiec")
    m.dach_czterospadowy((0, cy_ryz, h_ryzalit), szer_ryzalit, gl_ryz, 3.4, "dachowka_ciemna")

    y_front_ryz = wysun_ryzalit  # krawędź frontowa ryzalitu

    # 6 filarów wejścia (parter ryzalitu, szersze niż arkady zwykłe)
    n_fil = 6
    for i in range(n_fil):
        px = -szer_ryzalit / 2 + 1.5 + i * ((szer_ryzalit - 3.0) / (n_fil - 1))
        m.box((px, y_front_ryz, h_parter / 2), (1.1, 0.4, h_parter), "piaskowiec")

    # schody na całą szerokość ryzalitu
    for i in range(4):
        m.box((0, y_front_ryz + 0.5 + i * 0.5, 0.1 * (i + 1)), (szer_ryzalit - 1.0 - i * 0.4, 0.5, 0.2), "kamien")

    # drzwi 3 m szerokości, na osi
    m.box((0, y_front_ryz - 0.1, 1.8), (3.0, 0.3, 3.6), "drzwi_ciemne")
    # płyta szyldu nad drzwiami (bez napisu)
    m.box((0, y_front_ryz + 0.05, 4.1), (4.0, 0.12, 0.8), "szyld")
    # markiza nad wejściem
    m.box((0, y_front_ryz + 1.0, 4.6), (4.5, 2.0, 0.1), "markiza_kremowa")

    # piętra ryzalitu - okna (szersze, reprezentacyjne), na froncie
    for p in range(n_pieter + 1):
        z0 = h_parter + p * h_pietro + 0.6
        m.okna_rzad((-szer_ryzalit / 2 + 2.0, y_front_ryz + 0.05), (szer_ryzalit / 2 - 2.0, y_front_ryz + 0.05),
                    z0, 2.2, 1.3, 4.0, m_rama="rama")

    # lukarny w rzędzie na dachu ryzalitu (front)
    for i in range(3):
        lx = -5.0 + i * 5.0
        lukarna(m, lx, y_front_ryz - 1.0, h_ryzalit, m_dach="dachowka_ciemna")

    # lukarny na dachach skrzydeł (front, strona fasady)
    for znak in (-1, 1):
        cx = znak * (szer_ryzalit / 2 + dl_fasada_bok / 2)
        for i in range(2):
            lx = cx - dl_fasada_bok / 4 + i * (dl_fasada_bok / 2)
            lukarna(m, lx, 0.2, h_do_dachu, m_dach="dachowka_ciemna")

    return m.obiekt((0, 0, 0), kat=0.0)


def fontanna_fauny(x, y):
    m = Siatka("Fontanna_Fauny")
    r = 3.0  # niecka 6 m średnicy
    m.walec((0, 0, 0.0), r, 0.4, "kamien", n=16)
    m.walec((0, 0, 0.3), r - 0.15, 0.1, "woda", n=16)  # tafla wody niżej krawędzi niecki
    # cokół centralny ze stylizowanymi bryłami
    m.walec((0, 0, 0.4), 0.8, 0.5, "kamien", n=12)
    m.box((0, 0, 1.1), (0.7, 0.7, 0.6), "piaskowiec")
    m.stozek((0, 0, 1.7), 0.5, 0.8, "piaskowiec", n=12)
    for a in range(4):
        import math
        ang = math.radians(a * 90 + 45)
        bx, by = 0.55 * math.cos(ang), 0.55 * math.sin(ang)
        m.box((bx, by, 1.3), (0.2, 0.2, 0.5), "kamien")
    return m.obiekt((x, y, 0), kat=0.0)


def zbuduj():
    obiekty = []

    hotel = budynek_hotel()
    obiekty.append(hotel)

    # fontanna 18 m przed fasadą, na osi (front = +Y)
    fnt = fontanna_fauny(0, 18.0)
    obiekty.append(fnt)

    # 3 drzewa przy ulicy (po bokach, przed fasadą)
    drzewa_poz = [(-16, 10, 1), (16, 10, 2), (26, 6, 3)]
    for i, (dx, dy, seed) in enumerate(drzewa_poz):
        d = drzewo("Drzewo_Hotel_%d" % (i + 1), typ=1 if i % 2 == 0 else 2, ziarno=seed, skala=1.0)
        ob = d.obiekt((dx, dy, 0), kat=0.0)
        obiekty.append(ob)

    # --- PUNKTY GRY ---
    punkt("miasto_drzwi_hotel", (0, -1.0, 0.0), 180.0)
    punkt("miasto_auto_sd_trasa_1", (15, -12.0, 0.0), 0.0)

    # światła przy wejściu
    swiatlo((-3, 2, 4.2), kolor="#ffe3b0", zasieg=10, moc=2.5)
    swiatlo((3, 2, 4.2), kolor="#ffe3b0", zasieg=10, moc=2.5)

    return obiekty


if __name__ == "__main__":
    wyczysc_scene()
    obs = zbuduj()
    eksport_fbx(obs, os.path.join(WYJSCIE, "Hotel.fbx"))
    zapisz_meta(os.path.join(DANE_LOK, "meta_hotel.json"))
    ziemia_podgladu(90)
    render_podglad(os.path.join(PODGLADY_LOK, "hotel_1.png"), cel=(0, -5, 9), odl=100, azymut=200, wysokosc_kat=20)
    render_podglad(os.path.join(PODGLADY_LOK, "hotel_2.png"), cel=(0, -35, 8), odl=70, azymut=15, wysokosc_kat=20)
    render_podglad(os.path.join(PODGLADY_LOK, "hotel_3.png"), cel=(0, 3, 3), odl=22, azymut=195, wysokosc_kat=10)
