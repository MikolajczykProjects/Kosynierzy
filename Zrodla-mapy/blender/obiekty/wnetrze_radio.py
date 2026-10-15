# -*- coding: utf-8 -*-
"""
Wnętrze: budynek nadajnika radiostacji Gleiwitz (lata 30., funkcjonalny), 31.08.1939.
Parter z=0 (wys. 3,2 m), piwnica z=-3,0 (wys. 2,7 m, beton).
Trasa: tylne drzwi (−Y) -> korytarz -> sterownia (pulpit pod szybą do studia) -> pokój techniczny
(ściana łącznicy) -> klatka -> schody w dół -> piwnica; korytarz prowadzi też do sieni z wyjściem (+Y).
Meble/aparatura ruchoma (pulpit, łącznica, krzesła, mikrofon) = prefaby zespołu -> tylko punkty.
"""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *
from _wnetrza_wspolne import *

HR = 3.2      # wysokość parteru
ZB = -3.0     # podłoga piwnicy
ZBS = -0.3    # sufit piwnicy (spód stropu)
LAMP = 1.5    # lamperia olejna


def zbuduj():
    P = Plan()
    podl = Siatka("Radio_Podloga")
    sc = Siatka("Radio_Sciany")
    lam = Siatka("Radio_Lamperia")
    suf = Siatka("Radio_Sufit_NOCOL")
    sch = Siatka("Radio_Schody")
    bal = Siatka("Radio_Balustrada_COL")
    okn = Siatka("Radio_Okna")
    det = Siatka("Radio_Detal_NOCOL")
    lmp = Siatka("Radio_Lampy_NOCOL")
    ram = Siatka("Radio_DrzwiWyjscie_Rama_NOCOL")
    stu = Siatka("Radio_Studio_Panele")
    roz = Siatka("Radio_Technika_Rozdzielnia")
    rak = Siatka("Radio_Sterownia_Szafa")
    ppod = Siatka("Radio_Piwnica_Podloga")
    psc = Siatka("Radio_Piwnica_Sciany")
    psuf = Siatka("Radio_Piwnica_Sufit_NOCOL")
    pkom = Siatka("Radio_Piwnica_Komorki")
    prur = Siatka("Radio_Piwnica_Rury_NOCOL")
    plmp = Siatka("Radio_Piwnica_Lampy_NOCOL")
    skol = Siatka("Radio_SzybaStudia_COL")
    tab = Siatka("Radio_Technika_TablicaLacznicy")

    # ------------------------------------------------------------------ OTWORY
    P.drzwi("x", -9.0, 0.0)                         # tylne drzwi (wejście ataku)
    P.drzwi("x", 9.0, 0.0)                          # wyjście (sień)
    P.otwor("x", 5.0, 0.0, 2.0, 0.0, HR, "przejscie")   # korytarz -> sień
    P.drzwi("y", -1.0, -6.5)                        # korytarz -> sterownia
    P.drzwi("y", -1.0, 2.0)                         # korytarz -> studio
    P.drzwi("y", 1.0, -6.5)                         # korytarz -> technika
    P.drzwi("x", -2.0, 2.3)                         # technika -> klatka
    P.drzwi("y", 3.0, 6.5, z0=ZB)                   # przedsionek piwnicy -> piwnica
    P.otwor("x", -3.0, -4.0, 2.6, 1.0, 1.15, "okno_wew")  # szyba sterownia/studio (przezierna, sama kolizja)
    P.okno("y", -7.0, -5.0, 1.0, 1.4, 1.7)          # okno sterowni (zachód)
    P.okno("x", -9.0, -4.6, 1.0, 1.4, 1.7)          # okno sterowni (płd.)
    P.okno("x", -9.0, 4.2, 1.6, 1.6, 1.1)           # okno techniki (wysokie)
    P.okno("y", 7.0, -5.5, 2.2, 1.6, 0.7)           # naświetle nad łącznicą
    for x in (-2.6, 2.6):
        P.okno("x", 9.0, x, 1.0, 1.0, 1.6)          # okna sieni
    P.okno("y", 7.0, 2.0, 1.4, 1.2, 1.4)            # okno klatki
    for x in (-3.5, -0.5):
        P.okno("x", 9.0, x, ZBS - 0.6, 0.9, 0.45)   # okienka piwniczne

    # ------------------------------------------------------------------ PODŁOGI PARTERU
    T = -0.004
    plyta(podl, -1.1, 1.1, -9.1, 5.0, T, 0.3, "kafle_biale")            # korytarz
    posadzka_krata(podl, -0.9, 0.9, -8.9, 5.0, 0.0, 4, 32, "kafle_biale", "cegla_jasna")
    plyta(podl, -4.1, 4.1, 4.9, 9.1, T, 0.3, "kafle_biale")             # sień
    posadzka_krata(podl, -3.9, 3.9, 5.0, 8.9, 0.0, 17, 9, "kafle_biale", "cegla_jasna")
    plyta(podl, -7.1, -0.9, -9.1, -2.9, T, 0.3, "parkiet")              # sterownia
    deski(podl, -6.9, -1.1, -8.9, -3.1, 0.0, 0.14, "parkiet", "modrzew", wzdluz="y")
    plyta(podl, -7.1, -0.9, -3.1, 5.1, T, 0.3, "parkiet")               # studio
    deski(podl, -6.9, -1.1, -2.9, 4.9, 0.0, 0.14, "modrzew", "parkiet", wzdluz="x")
    plyta(podl, 0.9, 7.1, -9.1, -1.9, T, 0.3, "tynk_szary")             # technika (lastryko)
    posadzka_krata(podl, 1.1, 6.9, -8.9, -2.1, 0.0, 10, 12, "tynk_szary", "beton")
    plyta(podl, 0.9, 5.0, -2.1, 5.1, T, 0.3, "kamien")                  # klatka (bez otworu schodów)
    plyta(podl, 5.0, 7.1, -2.1, -1.2, T, 0.3, "kamien")
    # progi
    for (x0, x1, y0, y1) in ((-1.25, -0.75, -7.05, -5.95), (-1.25, -0.75, 1.45, 2.55), (0.75, 1.25, -7.05, -5.95), (1.75, 2.85, -2.25, -1.75)):
        podl.box_od_do(x0, y0, -0.01, x1, y1, 0.012, "kamien_ciemny")

    # ------------------------------------------------------------------ ŚCIANY PARTERU
    W = "tynk_kremowy"
    P.sciana(sc, "x", -9.0, -7.0, 7.0, 0, HR, W)
    P.sciana(sc, "x", 9.0, -4.0, 4.0, 0, HR, W)
    P.sciana(sc, "y", -4.0, 5.0, 9.0, 0, HR, W)
    P.sciana(sc, "y", 4.0, 5.0, 9.0, 0, HR, W)
    P.sciana(sc, "x", 5.0, -7.0, 7.0, 0, HR, W)
    P.sciana(sc, "y", -7.0, -9.0, 5.0, 0, HR, W)
    P.sciana(sc, "y", 7.0, -9.0, -2.0, 0, HR, W)
    P.sciana(sc, "y", 7.0, -2.0, 5.0, ZB, HR, W)          # szyb klatki od piwnicy
    P.sciana(sc, "y", -1.0, -9.0, 5.0, 0, HR, W)
    P.sciana(sc, "y", 1.0, -9.0, 5.0, 0, HR, W)
    P.sciana(sc, "x", -3.0, -7.0, -1.0, 0, HR, W)
    P.sciana(sc, "x", -2.0, 1.0, 3.0, 0, HR, W)
    P.sciana(sc, "x", -2.0, 3.0, 7.0, ZB, HR, W)

    # lamperia olejna (zieleń) do 1,5 m + czarna linia; studio ma panele akustyczne
    pokoje = [(-1, 1, -9, 5, "ESW"), (-4, 4, 5, 9, "NESW"), (-7, -1, -9, -3, "NESW"), (1, 7, -9, -2, "NESW"), (1, 7, -2, 5, "NESW")]
    for (x0, x1, y0, y1, s_) in pokoje:
        P.okladzina(lam, x0, x1, y0, y1, 0, LAMP, "zielen_okienna", grub=0.02, sciany=s_)
        P.listwa(det, x0, x1, y0, y1, LAMP, 0.03, 0.035, "czarny", sciany=s_)
        P.listwa(det, x0, x1, y0, y1, 0.0, 0.1, 0.04, "kamien_ciemny", sciany=s_)
    P.okladzina(lam, -7, -1, -3, 5, 0, 0.9, "boazeria", grub=0.03)
    P.listwa(det, -7, -1, -3, 5, 0.9, 0.05, 0.05, "drewno_ciemne")

    # panele akustyczne w studiu (kratka tkanin, od 0,95 m do 3,0 m)
    def panele(os_, c, s0, s1, sg, z0=0.95, z1=3.0, k=0.62):
        ns = int((s1 - s0) // k)
        nz = int((z1 - z0) // k)
        off = (s1 - s0 - ns * k) / 2
        for i in range(ns):
            for j in range(nz):
                a, b = s0 + off + i * k + 0.03, s0 + off + (i + 1) * k - 0.03
                za, zb = z0 + j * k + 0.03, z0 + (j + 1) * k - 0.03
                if any(o[0] == os_ and abs(o[1] - c) < 0.3 and o[2] - 0.15 < b and o[3] + 0.15 > a and o[4] - 0.15 < zb and o[5] + 0.15 > za for o in P.otw):
                    continue
                g = 0.06 if (i + j) % 2 == 0 else 0.045
                P._box(stu, os_, c + sg * (GR / 2 + g / 2), a, b, za, zb, g, "tkanina_bez" if (i + j) % 2 == 0 else "tkanina_zielona")
    panele("y", -7.0, -2.9, 4.9, +1)
    panele("x", 5.0, -6.9, -1.1, -1)
    panele("y", -1.0, -2.9, 4.9, -1)
    panele("x", -3.0, -6.9, -1.1, +1)
    for k in range(6):                                                  # ciężka kotara w studiu (fałdy)
        x = -6.6 + k * 0.5
        det.box_od_do(x - 0.24, 4.62, 0.02, x + 0.24, 4.78 + 0.06 * (k % 2), 3.05, "tkanina_bordo")
    det.box_od_do(-6.9, 4.6, 3.05, -3.4, 4.88, 3.12, "metal")

    # ------------------------------------------------------------------ OKNA
    P.wypelnij_okna(okn, det, rama="metal_ciemny", parapet="kamien", szprosy=(2, 2), m_kol=skol)
    # rolety (płótno, ciepłe) w sterowni i technice, opuszczone do połowy
    det.box_od_do(-6.86, -5.75, 2.0, -6.83, -4.25, 2.72, "tkanina_bez")
    det.box_od_do(-5.35, -8.86, 2.0, -3.85, -8.83, 2.72, "tkanina_bez")
    det.box_od_do(3.4, -8.86, 2.15, 5.0, -8.83, 2.72, "tkanina_bez")
    for x in (-2.6, 2.6):                                               # firanki w sieni
        det.box_od_do(x - 0.55, 8.83, 1.95, x + 0.55, 8.86, 2.62, "tkanina_bez")

    # ------------------------------------------------------------------ DRZWI
    P.rama_drzwi(ram, "x", 9.0, 0.0, mat="drzwi_ciemne", strony=(-1,), szer_opaski=0.14, wyst=0.05)
    ram.box_od_do(-0.85, 8.86, 2.34, 0.85, 8.92, 2.5, "drzwi_ciemne")
    P.okno("x", 9.0, 0.0, 2.4, 1.1, 0.6)                                # naświetle nad wyjściem
    okn.box_od_do(-0.55, 8.98, 2.42, 0.55, 9.02, 2.98, "okno")
    P.rama_drzwi(det, "x", -9.0, 0.0, mat="drzwi_ciemne", strony=(+1,))
    for (os_, c, s, z0) in (("y", -1.0, -6.5, 0), ("y", -1.0, 2.0, 0), ("y", 1.0, -6.5, 0), ("x", -2.0, 2.3, 0)):
        P.rama_drzwi(det, os_, c, s, z0=z0, mat="drewno_ciemne")
    P.rama_drzwi(det, "y", 3.0, 6.5, z0=ZB, mat="deska_szara")
    # lampka "na antenie" nad drzwiami studia (czerwony klosz, bez napisu)
    det.box_od_do(-0.98, 1.7, 2.45, -0.86, 2.3, 2.75, "metal_ciemny")
    det.box_od_do(-0.86, 1.78, 2.5, -0.82, 2.22, 2.7, "czerwony")
    # wieszaki na płaszcze w sieni + skrzynka na listy (bez napisu)
    det.box_od_do(-3.88, 5.6, 1.7, -3.84, 7.6, 1.82, "drewno_ciemne")
    for k in range(6):
        det.box_od_do(-3.84, 5.75 + k * 0.35, 1.72, -3.7, 5.8 + k * 0.35, 1.78, "metal")
    det.box_od_do(3.85, 5.8, 1.1, 3.9, 6.6, 1.9, "zielen_okienna")
    # tablica ogłoszeń (pusta) i zegar elektryczny w korytarzu
    det.box_od_do(-0.88, -2.2, 1.2, -0.84, -0.8, 2.2, "drewno")
    det.box_od_do(-0.84, -2.1, 1.3, -0.82, -0.9, 2.1, "tkanina_bez")
    det.box_od_do(-0.4, 4.6, 2.55, 0.4, 4.85, 2.65, "metal")
    det.box((0.0, 4.72, 2.4), (0.36, 0.08, 0.36), "metal_ciemny")
    det.box((0.0, 4.68, 2.4), (0.3, 0.02, 0.3), "bialy")

    # ------------------------------------------------------------------ STEROWNIA: szafa wzmacniaczy (wbudowana)
    XR = -7.0 + GR / 2
    rak.box_od_do(XR, -8.8, 0, XR + 0.55, -6.6, 2.4, "metal_ciemny", bez_dolu=False)
    for j in range(5):
        z = 0.35 + j * 0.4
        rak.box_od_do(XR + 0.55, -8.7, z, XR + 0.58, -6.7, z + 0.34, "metal")
        for i in range(4):
            y = -8.45 + i * 0.5
            det.box_od_do(XR + 0.58, y - 0.08, z + 0.12, XR + 0.6, y + 0.08, z + 0.28, "bialy" if j < 3 else "czarny")
            det.box_od_do(XR + 0.58, y + 0.12, z + 0.05, XR + 0.62, y + 0.17, z + 0.1, "czarny")
    rak.box_od_do(XR, -8.85, 2.4, XR + 0.6, -6.55, 2.5, "metal")
    # zegar nad szybą do studia, kanał kablowy pod oknem
    det.box((-4.0, -3.15, 2.62), (0.42, 0.08, 0.42), "metal_ciemny")
    det.box((-4.0, -3.2, 2.62), (0.34, 0.02, 0.34), "bialy")
    det.box_od_do(-6.9, -3.2, 0.3, -1.1, -3.12, 0.42, "metal")
    # studio: zegar i drewniane listwy sufitowe
    det.box((-4.0, -2.85, 2.62), (0.42, 0.08, 0.42), "metal_ciemny")
    det.box((-4.0, -2.8, 2.62), (0.34, 0.02, 0.34), "bialy")

    # ------------------------------------------------------------------ TECHNIKA: rozdzielnia, korytka kablowe
    YR = -9.0 + GR / 2
    roz.box_od_do(1.4, YR, 0, 3.4, YR + 0.35, 2.3, "metal_ciemny", bez_dolu=False)
    for i in range(4):
        x = 1.65 + i * 0.5
        det.box((x, YR + 0.37, 1.85), (0.28, 0.03, 0.28), "bialy")           # mierniki
        det.box((x, YR + 0.39, 1.85), (0.05, 0.02, 0.18), "czarny")
        det.box((x, YR + 0.4, 1.05), (0.06, 0.12, 0.3), "metal")              # wyłączniki nożowe
        det.box((x, YR + 0.37, 0.55), (0.3, 0.03, 0.4), "metal")
    roz.box_od_do(1.35, YR, 2.3, 3.45, YR + 0.4, 2.4, "metal")
    for x in (1.7, 2.4, 3.1):                                                 # rury kablowe do sufitu
        det.box_od_do(x - 0.04, YR + 0.05, 2.4, x + 0.04, YR + 0.13, HR, "metal")
    # korytko kablowe pod sufitem: od korytarza do ściany łącznicy
    for (a, b) in (((1.3, -5.5, 2.75), (6.75, -5.5, 2.75)), ((1.3, -5.9, 2.75), (6.75, -5.9, 2.75))):
        det.belka(a, b, 0.05, "metal")
    for k in range(12):
        x = 1.4 + k * 0.45
        det.box_od_do(x, -5.95, 2.72, x + 0.04, -5.45, 2.76, "metal")
    for k in range(5):                                                        # wiązki kabli
        y = -5.82 + k * 0.08
        det.belka((1.3, y, 2.8), (6.75, y, 2.8), 0.035, "czarny")
        det.belka((6.75, y, 2.8), (6.83, y, 2.4), 0.035, "czarny")
    # listwa na ścianie łącznicy (tu stanie prefab łącznicy) + rury z podłogi
    XE = 7.0 - GR / 2
    tab.box_od_do(XE - 0.05, -8.4, 0.1, XE, -2.6, 2.45, "boazeria", bez_dolu=False)      # tablica (deski)
    for y in (-8.4, -6.5, -4.5, -2.6):                                                    # listwy pionowe
        tab.box_od_do(XE - 0.09, y - 0.05, 0.1, XE - 0.05, y + 0.05, 2.45, "drewno_ciemne")
    tab.box_od_do(XE - 0.1, -8.5, 2.45, XE, -2.5, 2.55, "drewno_ciemne")                # gzyms
    tab.box_od_do(XE - 0.1, -8.5, 0.0, XE, -2.5, 0.12, "drewno_ciemne")                  # cokół
    for k in range(10):                                                                    # rząd izolatorów porcelanowych
        y = -8.1 + k * 0.6
        det.box((XE - 0.1, y, 2.3), (0.08, 0.06, 0.08), "bialy")
    for k in range(5):                                                                     # wyprowadzenia kabli z korytka
        y = -5.82 + k * 0.08
        det.box_od_do(XE - 0.14, y - 0.017, 1.9, XE - 0.1, y + 0.017, 2.42, "czarny")
    det.box((XE - 0.08, -7.4, 1.95), (0.04, 0.5, 0.3), "metal")                          # puszki zaciskowe
    det.box((XE - 0.08, -3.6, 1.95), (0.04, 0.5, 0.3), "metal")
    # tablica na kartki (korek) na ścianie od korytarza
    det.box_od_do(1.13, -4.6, 1.1, 1.16, -3.4, 1.9, "drewno")
    det.box_od_do(1.16, -4.5, 1.2, 1.18, -3.5, 1.8, "tkanina_bez")

    # ------------------------------------------------------------------ SCHODY DO PIWNICY (klatka)
    n = 17
    y_dol = 3.56
    schody_proste(sch, 5.1, 6.9, y_dol, -1, ZB, 0.0, n, 0.28, "kamien", mat_nosek="metal")
    # balustrada: rury (detal) + niewidzialna kolizja
    det.belka((5.0, -1.2, 1.0), (5.0, 4.9, 1.0), 0.05, "metal_ciemny")
    det.belka((5.0, -1.2, 0.5), (5.0, 4.9, 0.5), 0.03, "metal_ciemny")
    for k in range(7):
        y = -1.15 + k * 1.0
        det.box_od_do(4.97, y - 0.025, 0.0, 5.03, y + 0.025, 1.0, "metal_ciemny")
    bal.box_od_do(4.95, -1.2, 0.0, 5.05, 4.9, 1.05, "metal_ciemny")
    # poręcz przyścienna wzdłuż biegu (ściana wsch.)
    det.belka((6.84, y_dol, ZB + 0.95), (6.84, -1.2, 0.95), 0.05, "drewno_ciemne")
    # lico stropu przy otworze
    det.box_od_do(4.9, -1.2, -0.3, 5.0, 4.9, 0.0, "beton")

    # ------------------------------------------------------------------ PIWNICA
    plyta(ppod, 2.9, 7.1, -2.1, 9.1, ZB, 0.2, "beton")
    plyta(ppod, -5.1, 3.1, 0.9, 9.1, ZB, 0.2, "beton")
    for (x0, x1, y0, y1) in ((3.1, 6.9, 3.6, 8.9), (-4.9, 2.9, 1.1, 8.9)):      # łączenia płyt betonu
        for k in range(1, int((y1 - y0) // 1.5) + 1):
            y = y0 + k * 1.5
            ppod.quad((x0, y - 0.02, ZB + 0.002), (x1, y - 0.02, ZB + 0.002), (x1, y + 0.02, ZB + 0.002), (x0, y + 0.02, ZB + 0.002), "kamien_ciemny")
    B = "beton"
    P.sciana(psc, "y", 3.0, -2.0, 9.0, ZB, ZBS, B)
    P.sciana(psc, "y", 7.0, 5.0, 9.0, ZB, ZBS, B)
    P.sciana(psc, "x", 9.0, -5.0, 7.0, ZB, ZBS, B)
    P.sciana(psc, "y", -5.0, 1.0, 9.0, ZB, ZBS, B)
    P.sciana(psc, "x", 1.0, -5.0, 3.0, ZB, ZBS, B)
    # brudna lamperia (szary tynk do 1,2 m) w obu pomieszczeniach
    P.okladzina(psc, 3, 7, -2, 9, ZB, ZB + 1.2, "tynk_brudny", grub=0.02)
    P.okladzina(psc, -5, 3, 1, 9, ZB, ZB + 1.2, "tynk_brudny", grub=0.02)
    plyta(psuf, 2.9, 7.1, -2.1, 9.1, ZBS + 0.05, 0.05, "beton")
    plyta(psuf, -5.1, 3.1, 0.9, 9.1, ZBS + 0.05, 0.05, "beton")
    # komórki z desek pod ścianą zachodnią (zabudowa, kolizja)
    for k in range(3):
        y0 = 1.25 + k * 1.6
        pkom.box_od_do(-4.9, y0 + 1.5, ZB, -3.2, y0 + 1.56, ZBS, "deska_szara")      # przegroda
        for j in range(5):                                                          # front z desek ze szparami
            yy = y0 + j * 0.3
            pkom.box_od_do(-3.26, yy, ZB, -3.2, yy + 0.24, ZB + 2.0, "deska_szara" if j % 2 else "drewno")
    pkom.box_od_do(-4.9, 1.1, ZB, -3.2, 1.25, ZBS, "deska_szara")
    # węglarka/zasypy przy przedsionku
    pkom.box_od_do(3.1, 7.6, ZB, 4.6, 8.9, ZB + 1.1, "deska_szara")
    pkom.box_od_do(3.15, 7.65, ZB + 1.0, 4.55, 8.85, ZB + 1.05, "czarny")
    # rury pod stropem (woda, c.o.) + zawory
    for (a, b, r) in ((( -4.8, 8.6, -0.55), (6.8, 8.6, -0.55), 0.08), ((-4.8, 8.35, -0.5), (2.8, 8.35, -0.5), 0.06),
                      ((3.4, -1.8, -0.6), (3.4, 8.6, -0.6), 0.1), ((6.6, 8.6, -0.55), (6.6, 8.6, ZB + 0.1), 0.08)):
        prur.belka(a, b, r * 2, "metal" if r < 0.1 else "metal_ciemny")
    for x in (-2.0, 1.5, 5.0):
        prur.walec((x, 8.6, -0.72), 0.06, 0.12, "czerwony", n=6, dol=True)

    # ------------------------------------------------------------------ SUFITY PARTERU
    sufit(suf, -7.1, 7.1, -9.1, 5.1, HR)
    sufit(suf, -4.1, 4.1, 4.9, 9.1, HR)
    for y in (-6.0, -1.0, 3.0):                                                 # podciągi (belki stropu)
        suf.box_od_do(-7.0, y - 0.15, HR - 0.25, 7.0, y + 0.15, HR, "sufit", bez_dolu=False)

    # ------------------------------------------------------------------ LAMPY I ŚWIATŁA
    def lp(x, y, z_suf, z_dol, rodzaj="emalia", kol="#ffcf8a", zas=7, moc=1.5):
        m_ = plmp if z_suf < 0 else lmp
        if rodzaj == "emalia":
            lampa_przemyslowa(lmp, x, y, z_suf, z_dol)
        elif rodzaj == "kula":
            lmp.box((x, y, (z_suf + z_dol) / 2 + 0.15), (0.02, 0.02, z_suf - z_dol - 0.3), "czarny")
            lmp.walec((x, y, z_dol), 0.2, 0.35, "lampa", n=8, r_gora=0.12, dol=True)
            lmp.walec((x, y, z_dol + 0.35), 0.1, 0.06, "metal", n=6)
        else:
            zarowka(m_, x, y, z_suf, z_dol)
        swiatlo((x, y, z_dol - 0.1), kolor=kol, zasieg=zas, moc=moc)
    for y in (-7.5, -4.0, -1.0 + 1.6, 3.6):
        lp(0.0, y, HR, 2.45, zas=5, moc=1.1)
    lp(0.0, 7.0, HR, 2.4, rodzaj="kula", zas=6, moc=1.3)
    lp(-4.0, -7.6, HR, 2.3, zas=6, moc=1.6)
    lp(-4.0, -4.3, HR, 2.35, zas=6, moc=1.7)                       # nad pulpitem przy szybie
    lp(-4.0, -0.2, HR, 2.4, rodzaj="kula", zas=6, moc=1.2)
    lp(-4.0, 3.2, HR, 2.4, rodzaj="kula", zas=6, moc=1.2)
    lp(3.0, -7.6, HR, 2.35, zas=6, moc=1.4)
    lp(5.0, -5.5, HR, 2.35, zas=6, moc=1.6)                        # nad łącznicą
    lp(3.0, 1.5, HR, 2.4, zas=6, moc=1.2)
    lp(5.0, 6.2, ZBS, ZB + 2.0, rodzaj="goła", kol="#ffb866", zas=6, moc=1.4)
    lp(-2.5, 4.0, ZBS, ZB + 2.0, rodzaj="goła", kol="#ffb866", zas=6, moc=1.3)
    lp(0.5, 7.2, ZBS, ZB + 2.0, rodzaj="goła", kol="#ffb866", zas=6, moc=1.3)
    lp(5.0, 0.8, ZBS, ZB + 2.1, rodzaj="goła", kol="#ffb866", zas=5, moc=1.0)

    # ------------------------------------------------------------------ PUNKTY
    punkt("spawn_radio_tyl", (0.0, -8.0, 0.0), 0)
    punkt("radio_drzwi_wyjscie", (0.0, 8.0, 0.0), 0)
    punkt("radio_sterownia_pulpit", (-4.0, -3.75, 0.0), 0)          # prefab pulpitu pod szybą, operator patrzy na +Y
    punkt("radio_sterownia_mikrofon", (-4.5, -3.55, 0.9), 0)        # na blacie pulpitu
    punkt("radio_technika_kable", (5.9, -5.5, 0.0), 90)             # przed ścianą łącznicy
    punkt("radio_technika_lacznica", (6.55, -5.5, 0.0), 270)        # prefab łącznicy pod ścianą wsch., front na zachód
    punkt("radio_kartka_grubera", (1.2, -4.0, 1.5), 90)             # przypięta do tablicy w technice
    punkt("radio_schody_gora", (6.0, -1.6, 0.0), 0)
    punkt("radio_schody_dol", (6.0, 4.4, ZB), 0)
    punkt("radio_piwnica_drzwi", (3.0, 6.5, ZB), 270)               # środek otworu, przejście na zachód
    punkt("radio_piwnica_pracownik_1", (-2.6, 8.2, ZB), 180)
    punkt("radio_piwnica_pracownik_2", (-1.2, 8.3, ZB), 180)
    punkt("radio_piwnica_pracownik_3", (0.3, 8.2, ZB), 200)
    punkt("radio_sd_1", (-2.2, -7.8, 0.0), 315)
    punkt("radio_sd_2", (-6.0, -4.2, 0.0), 120)
    punkt("radio_sd_3", (0.0, -3.0, 0.0), 180)
    punkt("radio_sd_4", (0.0, 3.2, 0.0), 180)
    punkt("radio_sd_5", (-2.0, -5.0, 0.0), 270)
    punkt("radio_cialo_honiok", (1.6, 7.6, 0.0), 90)
    punkt("radio_kartka_honiok", (2.5, 7.2, 0.0), 0)

    siatki = [podl, sc, lam, suf, sch, bal, okn, det, lmp, ram, stu, roz, rak, tab, skol, ppod, psc, psuf, pkom, prur, plmp]
    return [s.obiekt() for s in siatki if not s.pusta()]


if __name__ == "__main__":
    wyczysc_scene()
    obs = zbuduj()
    policz_trojkaty(obs)
    eksport_fbx(obs, os.path.join(UNITY_MODELE, "Wnetrze_Radio.fbx"))
    zapisz_meta(os.path.join(DANE, "meta_wnetrze_radio.json"))
    swiatla_podgladu()
    ukryj_sufity(obs, True, ("Sufit",))
    for o in obs:
        if o.name.endswith("_COL"):
            o.hide_render = True
    render_podglad(os.path.join(PODGLADY, "wnetrze_radio_1.png"), cel=(0.0, -1.0, 0.0), odl=30, azymut=200, wysokosc_kat=58)
    # piwnica z góry: ukryj parter
    for o in obs:
        if "Piwnica" not in o.name and o.name != "Radio_Schody":
            o.hide_render = True
    ukryj_sufity(obs, True, ("Sufit",))
    render_podglad(os.path.join(PODGLADY, "wnetrze_radio_3.png"), cel=(1.0, 4.0, ZB), odl=20, azymut=210, wysokosc_kat=60)
    for o in obs:
        o.hide_render = o.name.endswith("_COL")
    oczy(os.path.join(PODGLADY, "wnetrze_radio_2.png"), (-1.6, -8.3, 1.65), (-4.0, -3.0, 1.3), ogniskowa=17, noc=True)
    oczy(os.path.join(PODGLADY, "wnetrze_radio_4.png"), (2.2, -3.4, 1.65), (6.9, -6.2, 1.3), ogniskowa=17, noc=True)
    oczy(os.path.join(PODGLADY, "wnetrze_radio_5.png"), (5.6, 4.6, ZB + 1.65), (-3.0, 7.5, ZB + 0.8), ogniskowa=17, noc=True)
