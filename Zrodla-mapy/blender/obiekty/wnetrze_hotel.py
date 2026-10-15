# -*- coding: utf-8 -*-
"""
Wnętrze: hotel Haus Oberschlesien (1928, art déco + neoklasycyzm), Gliwice 1939.
Hol 20x14 m, wys. 5 m (podłoga z=0). Wyjście (drzwi główne) w ścianie +Y.
Schody przy ścianie wschodniej prowadzą na podest z=2,5 -> korytarz piętra -> pokój Naujocksa (5x6 m),
które stoją na wschód od holu (x>10) na wysokości piętra.
Meble ruchome (stoły, krzesła, fotele, biurko, łóżko, telefon) = prefaby zespołu -> tylko punkty.
"""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *
from _wnetrza_wspolne import *

H = 5.0            # wysokość holu
ZP = 2.5           # poziom piętra (podest, korytarz, pokój)
HP = 3.0           # wysokość piętra
LAMP = 1.5         # wysokość boazerii w holu


def zbuduj():
    P = Plan()
    podl = Siatka("Hotel_Podloga")
    dyw = Siatka("Hotel_Dywan_NOCOL")
    sc = Siatka("Hotel_Sciany")
    boa = Siatka("Hotel_Boazeria")
    suf = Siatka("Hotel_Sufit_NOCOL")
    kol = Siatka("Hotel_Kolumny")
    sch = Siatka("Hotel_Schody")
    rec = Siatka("Hotel_Recepcja")
    kom = Siatka("Hotel_Kominek")
    okn = Siatka("Hotel_Okna")
    det = Siatka("Hotel_Detal_NOCOL")
    zyr = Siatka("Hotel_Zyrandole_NOCOL")
    ram = Siatka("Hotel_DrzwiWyjscie_Rama_NOCOL")
    szf = Siatka("Hotel_Pokoj_Szafa")
    pie = Siatka("Hotel_Pokoj_Piec")
    win = Siatka("Hotel_Winda")

    # ------------------------------------------------------------------ OTWORY
    P.drzwi("x", 7.0, 0.0)                               # wyjście (drzwi główne)
    P.okno("x", 7.0, 0.0, 2.45, 1.1, 1.0)                # naświetle nad drzwiami
    for x in (-7.2, -3.6, 3.6, 7.2):
        P.okno("x", 7.0, x, 1.6, 1.5, 2.9)               # wysokie okna frontu
    P.otwor("y", -10.0, 0.0, 6.0, 0.0, 3.6, "wneka")      # wnęka recepcji
    P.drzwi("y", 10.0, -5.05, z0=ZP)                      # podest -> korytarz piętra
    P.drzwi("y", 12.2, 2.0, z0=ZP)                        # korytarz -> pokój Naujocksa
    P.okno("y", 17.2, 2.4, ZP + 0.85, 1.3, 1.7)           # okno pokoju
    P.okno("x", 5.0, 11.15, ZP + 0.9, 1.1, 1.6)           # okno na końcu korytarza

    # ------------------------------------------------------------------ PODŁOGI
    # hol: płyta + szachownica z ciemną bordiurą i mosiężną listwą
    plyta(podl, -10.1, 10.1, -7.1, 7.1, -0.004, 0.25, "posadzka_ciemna")
    posadzka_krata(podl, -9.2, 9.2, -6.2, 6.2, 0.0, 18, 12, "posadzka_jasna", "posadzka_ciemna")
    for (a, b, c, d) in ((-9.9, -9.2, -6.9, 6.9), (9.2, 9.9, -6.9, 6.9), (-9.2, 9.2, -6.9, -6.2), (-9.2, 9.2, 6.2, 6.9)):
        podl.quad((a, c, 0), (b, c, 0), (b, d, 0), (a, d, 0), "posadzka_ciemna")
    for (a, b, c, d) in ((-9.32, -9.24, -6.32, 6.32), (9.24, 9.32, -6.32, 6.32), (-9.32, 9.32, -6.32, -6.24), (-9.32, 9.32, 6.24, 6.32)):
        det.quad((a, c, 0.004), (b, c, 0.004), (b, d, 0.004), (a, d, 0.004), "zloto")
    # wnęka recepcji: parkiet
    plyta(podl, -13.1, -9.9, -3.1, 3.1, -0.004, 0.25, "parkiet")
    deski(podl, -12.9, -10.0, -2.9, 2.9, 0.0, 0.16, "parkiet", "modrzew", wzdluz="y")
    # korytarz piętra + pokój: strop nad pustką
    plyta(podl, 9.9, 12.3, -7.1, 5.1, ZP - 0.004, 0.3, "parkiet")
    deski(podl, 10.1, 12.1, -6.9, 4.9, ZP, 0.16, "parkiet", "modrzew", wzdluz="x")
    plyta(podl, 12.1, 17.3, -1.1, 5.1, ZP - 0.004, 0.3, "parkiet")
    deski(podl, 12.3, 17.1, -0.9, 4.9, ZP, 0.15, "parkiet", "modrzew", wzdluz="y")
    plyta(podl, 11.6, 12.8, 1.4, 2.6, ZP + 0.002, 0.01, "kamien")       # próg pokoju

    # ------------------------------------------------------------------ DYWANY (NOCOL)
    def dywan(x0, x1, y0, y1, z=0.012, bord=0.25):
        dyw.box_od_do(x0, y0, z - 0.012, x1, y1, z, "szyld_zloto" if False else "zloto")
        dyw.box_od_do(x0 + 0.06, y0 + 0.06, z, x1 - 0.06, y1 - 0.06, z + 0.006, "tkanina_bordo")
        dyw.box_od_do(x0 + bord, y0 + bord, z + 0.006, x1 - bord, y1 - bord, z + 0.01, "dywan_czerwony")
    dywan(-3.4, 3.4, -2.2, 2.2)                  # środek holu, między kolumnami
    dywan(-1.0, 1.0, 2.2, 6.75)                  # chodnik od drzwi
    dywan(-3.0, 3.0, -6.4, -3.6)                 # salonik przy kominku
    dywan(10.5, 11.7, -6.6, 4.6, z=ZP + 0.012)   # chodnik w korytarzu
    dywan(13.3, 16.0, 0.4, 3.6, z=ZP + 0.012)    # dywan w pokoju

    # ------------------------------------------------------------------ ŚCIANY HOLU
    P.sciana(sc, "x", 7.0, -10.0, 10.0, 0, H, "tynk_kremowy")
    P.sciana(sc, "x", -7.0, -10.0, 10.0, 0, H, "tynk_kremowy")
    P.sciana(sc, "y", -10.0, -7.0, 7.0, 0, H, "tynk_kremowy")
    P.sciana(sc, "y", 10.0, -7.0, 7.0, 0, ZP + HP, "tynk_kremowy")      # wschodnia też dla korytarza piętra
    # wnęka recepcji
    P.sciana(sc, "y", -13.0, -3.0, 3.0, 0, 3.6, "tynk_kremowy")
    P.sciana(sc, "x", -3.0, -13.0, -10.0, 0, 3.6, "tynk_kremowy")
    P.sciana(sc, "x", 3.0, -13.0, -10.0, 0, 3.6, "tynk_kremowy")
    # korytarz piętra i pokój
    ZK = ZP + HP
    P.sciana(sc, "y", 12.2, -7.0, 5.0, ZP, ZK, "tynk_kremowy")
    P.sciana(sc, "x", -7.0, 10.0, 12.2, ZP, ZK, "tynk_kremowy")
    P.sciana(sc, "x", 5.0, 10.0, 12.2, ZP, ZK, "tynk_kremowy")
    P.sciana(sc, "y", 17.2, -1.0, 5.0, ZP, ZK, "tynk_kremowy")
    P.sciana(sc, "x", -1.0, 12.2, 17.2, ZP, ZK, "tynk_kremowy")
    P.sciana(sc, "x", 5.0, 12.2, 17.2, ZP, ZK, "tynk_kremowy")

    # hol: boazeria do 1,5 m, nad nią ciepła tapeta, fryz i gzyms
    P.okladzina(boa, -10, 10, -7, 7, 0, LAMP, "boazeria", grub=0.05)
    P.okladzina(sc, -10, 10, -7, 7, LAMP, 4.3, "tapeta_kremowa", grub=0.03)
    P.okladzina(sc, -10, 10, -7, 7, 4.3, H, "tynk_bialy", grub=0.03)
    P.listwa(det, -10, 10, -7, 7, 0.0, 0.14, 0.08, "drewno_ciemne")            # cokół
    P.listwa(det, -10, 10, -7, 7, LAMP, 0.08, 0.1, "zloto")                    # mosiężna listwa boazerii
    P.listwa(det, -10, 10, -7, 7, 4.25, 0.1, 0.09, "zloto")                    # pas pod fryzem
    P.listwa(det, -10, 10, -7, 7, 4.75, 0.25, 0.22, "sufit")                   # gzyms
    P.listwa(det, -10, 10, -7, 7, 4.6, 0.15, 0.14, "tynk_bialy")
    # płyciny boazerii (ramki z listew)
    def plyciny(os_, c, s0, s1, sg, z0=0.3, z1=1.25, krok=1.2):
        n = max(1, int((s1 - s0) // krok))
        d = (s1 - s0) / n
        for i in range(n):
            a, b = s0 + i * d + 0.15, s0 + (i + 1) * d - 0.15
            mid = (a + b) / 2
            if any(o[0] == os_ and abs(o[1] - c) < 0.3 and o[2] - 0.1 < b and o[3] + 0.1 > a and o[4] < z1 for o in P.otw):
                continue
            pr = sg * (GR / 2 + 0.05 + 0.015)
            for (aa, bb, za, zb) in ((a, b, z1 - 0.05, z1), (a, b, z0, z0 + 0.05), (a, a + 0.05, z0, z1), (b - 0.05, b, z0, z1)):
                P._box(det, os_, c + pr, aa, bb, za, zb, 0.03, "drewno_ciemne")
    plyciny("x", 7.0, -9.9, 9.9, -1); plyciny("x", -7.0, -9.9, 9.9, +1)
    plyciny("y", -10.0, -6.9, 6.9, +1); plyciny("y", 10.0, 1.2, 6.9, -1)

    # pilastry holu (art déco: trzon z żłobkami + mosiężne kapitele)
    def pilaster(os_, c, s, sg):
        pr = lambda g: sg * (GR / 2 + 0.03 + g / 2)
        P._box(boa, os_, c + pr(0.16), s - 0.3, s + 0.3, 0, 4.25, 0.16, "tynk_bialy")
        P._box(boa, os_, c + pr(0.2), s - 0.36, s + 0.36, 0, 0.5, 0.2, "posadzka_ciemna")
        for k in (-0.15, 0.0, 0.15):
            P._box(det, os_, c + pr(0.2), s + k - 0.025, s + k + 0.025, 0.6, 3.75, 0.2, "sufit")
        P._box(det, os_, c + pr(0.22), s - 0.36, s + 0.36, 3.85, 4.0, 0.22, "zloto")
        P._box(det, os_, c + pr(0.26), s - 0.4, s + 0.4, 4.0, 4.25, 0.26, "tynk_bialy")
    for x in (-9.2, -5.4, -1.8, 1.8, 5.4, 9.2):
        pilaster("x", 7.0, x, -1)
    for x in (-9.2, -4.4, 4.4, 9.2):
        pilaster("x", -7.0, x, +1)
    for y in (-6.2, -3.7, 3.7, 6.2):
        pilaster("y", -10.0, y, +1)
    for y in (1.7, 4.2, 6.2):
        pilaster("y", 10.0, y, -1)

    # wnęka recepcji: boazeria pełna wysokość lady, tapeta zielona, łuk/gzyms nad wnęką
    P.okladzina(boa, -13, -10, -3, 3, 0, 1.2, "boazeria", grub=0.04, sciany="SWN")
    P.okladzina(sc, -13, -10, -3, 3, 1.2, 3.6, "tapeta_zielona", grub=0.03, sciany="SWN")
    for zz, g, mm in ((3.6, 0.3, "tynk_bialy"), (3.9, 0.12, "zloto")):
        sc.box_od_do(-10.32, -3.4, zz, -9.86, 3.4, zz + g, mm, bez_dolu=False)
    for y in (-3.2, 3.2):                                                    # ościeża wnęki = pilastry
        boa.box_od_do(-10.35, y - 0.25, 0, -9.82, y + 0.25, 3.6, "tynk_bialy")
        det.box_od_do(-10.37, y - 0.27, 3.3, -9.8, y + 0.27, 3.45, "zloto")
        boa.box_od_do(-10.37, y - 0.3, 0, -9.8, y + 0.3, 0.5, "posadzka_ciemna")

    # korytarz: boazeria 1 m + tapeta kremowa; pokój: boazeria 0,9 + tapeta bordo
    P.okladzina(boa, 10, 12.2, -7, 5, ZP, ZP + 1.0, "boazeria", grub=0.04)
    P.okladzina(sc, 10, 12.2, -7, 5, ZP + 1.0, ZK, "tapeta_kremowa", grub=0.03)
    P.listwa(det, 10, 12.2, -7, 5, ZP + 1.0, 0.06, 0.07, "zloto")
    P.listwa(det, 10, 12.2, -7, 5, ZK - 0.2, 0.2, 0.12, "sufit")
    P.okladzina(boa, 12.2, 17.2, -1, 5, ZP, ZP + 0.9, "boazeria", grub=0.04)
    P.okladzina(sc, 12.2, 17.2, -1, 5, ZP + 0.9, ZK, "tapeta_bordo", grub=0.03)
    P.listwa(det, 12.2, 17.2, -1, 5, ZP + 0.9, 0.06, 0.07, "drewno_ciemne")
    P.listwa(det, 12.2, 17.2, -1, 5, ZK - 0.22, 0.22, 0.12, "sufit")
    P.listwa(det, 12.2, 17.2, -1, 5, ZK - 0.6, 0.04, 0.04, "zloto")
    # ślepe drzwi pokoi w korytarzu (płyciny na ścianie, bez otworu)
    for y in (-5.6, -2.6):
        for g, za, zb, mm in ((0.05, 0, 2.3, "drzwi_ciemne"), (0.08, 0.0, 2.2, "drzwi")):
            s_ = 0.6 if mm == "drzwi" else 0.68
            det.box_od_do(12.2 - GR / 2 - 0.03 - g, y - s_, ZP + za, 12.2 - GR / 2 - 0.03, y + s_, ZP + zb, mm)
        det.box_od_do(12.2 - 0.25, y + 0.42, ZP + 1.0, 12.2 - 0.17, y + 0.5, ZP + 1.08, "zloto")   # klamka
        det.box_od_do(12.2 - 0.2, y - 0.12, ZP + 1.75, 12.2 - 0.17, y + 0.12, ZP + 1.9, "zloto")   # numer (pusta tabliczka)

    # ------------------------------------------------------------------ OKNA
    P.wypelnij_okna(okn, det, rama="rama_ciemna", parapet="posadzka_jasna", szprosy=(1, 3))
    # zasłony bordo (do podłogi) i lambrekiny przy oknach frontu
    for x in (-7.2, -3.6, 3.6, 7.2):
        for sg in (-1, 1):
            det.box_od_do(x + sg * 0.75 - 0.22, 6.68, 0.02, x + sg * 0.75 + 0.22, 6.82, 4.65, "tkanina_bordo")
        det.box_od_do(x - 1.05, 6.62, 4.45, x + 1.05, 6.84, 4.75, "tkanina_bordo")
        det.box_od_do(x - 1.05, 6.6, 4.42, x + 1.05, 6.84, 4.47, "zloto")
    for sg in (-1, 1):                                                       # pokój
        det.box_od_do(17.0, 2.4 + sg * 0.85 - 0.2, ZP + 0.02, 17.08, 2.4 + sg * 0.85 + 0.2, ZK - 0.25, "tkanina_zielona")
    for k in range(12):                                                     # kaloryfer żeberkowy
        y = 1.85 + k * 0.1
        det.box_od_do(16.84, y, ZP + 0.15, 17.04, y + 0.06, ZP + 0.75, "metal")
    # obramienia drzwi wewnętrznych (podest -> korytarz, korytarz -> pokój)
    P.rama_drzwi(det, "y", 10.0, -5.05, z0=ZP, mat="drzwi_ciemne")
    P.rama_drzwi(det, "y", 12.2, 2.0, z0=ZP, mat="drzwi_ciemne")
    # obrazy w złotych ramach (pejzaże z plam koloru, bez napisów)
    def obraz(os_, c, s, z, w, h, sg, k1, k2):
        pr = lambda g: sg * (GR / 2 + 0.03 + g / 2)
        P._box(det, os_, c + pr(0.05), s - w / 2, s + w / 2, z, z + h, 0.05, "zloto")
        P._box(det, os_, c + pr(0.06), s - w / 2 + 0.07, s + w / 2 - 0.07, z + 0.07, z + h * 0.55, 0.06, k1)
        P._box(det, os_, c + pr(0.06), s - w / 2 + 0.07, s + w / 2 - 0.07, z + h * 0.55, z + h - 0.07, 0.06, k2)
    obraz("x", 5.0, 14.7, ZP + 1.35, 1.2, 0.85, -1, "pole_zielone", "tynk_niebieski")
    obraz("y", 12.2, 0.0, ZP + 1.4, 0.9, 0.7, +1, "trawa_ciemna", "tynk_piasek")
    obraz("y", 10.0, 0.0, ZP + 1.4, 0.9, 0.7, +1, "zyto_scierniska", "tynk_niebieski")
    obraz("y", 12.2, -4.1, ZP + 1.4, 0.7, 0.9, -1, "woda", "tynk_kremowy")
    obraz("x", -7.0, -3.3, 2.2, 1.4, 1.0, +1, "trawa_ciemna", "tynk_niebieski")
    obraz("x", -7.0, 3.3, 2.2, 1.4, 1.0, +1, "pole_zielone", "tynk_kremowy")
    # rolety (płócienne, do połowy) dają ciepły kolor w oknie, za którym nic nie ma
    for x in (-7.2, -3.6, 3.6, 7.2):
        det.box_od_do(x - 0.7, 6.9, 3.7, x + 0.7, 6.94, 4.45, "tkanina_bez")
    det.box_od_do(17.12, 1.8, ZP + 1.85, 17.16, 3.0, ZP + 2.5, "tkanina_bez")

    # ------------------------------------------------------------------ DRZWI GŁÓWNE (rama)
    P.rama_drzwi(ram, "x", 7.0, 0.0, mat="drzwi_ciemne", strony=(-1,), szer_opaski=0.16, wyst=0.06)
    ram.box_od_do(-1.15, 6.74, 0, -0.8, 6.86, 3.55, "tynk_bialy")           # portal: pilastry
    ram.box_od_do(0.8, 6.74, 0, 1.15, 6.86, 3.55, "tynk_bialy")
    ram.box_od_do(-1.3, 6.7, 3.55, 1.3, 6.86, 3.8, "tynk_bialy")
    ram.box_od_do(-1.4, 6.66, 3.8, 1.4, 6.86, 3.92, "zloto")
    ram.box_od_do(-0.9, 6.68, 3.92, 0.9, 6.86, 4.15, "tynk_bialy")          # stopniowana attyka
    ram.box_od_do(-0.45, 6.66, 4.15, 0.45, 6.86, 4.3, "tynk_bialy")
    for x in (-1.0, 0.95):
        ram.box_od_do(x - 0.02, 6.72, 0.3, x + 0.07, 6.74, 3.3, "zloto")
    ram.box_od_do(-0.55, 6.86, 2.2, 0.55, 6.96, 2.45, "drzwi_ciemne")       # nadproże między drzwiami a naświetlem
    for x in (-0.28, 0.0, 0.28):                                              # promienie naświetla (art déco)
        det.belka((0, 6.93, 2.47), (x * 2.0, 6.93, 3.42), 0.04, "zloto")
    for x in (-8.9, 8.9):                                                     # kinkiety przy wejściu
        kinkiet(det, x, 6.86, 2.6, 0, -1)

    # ------------------------------------------------------------------ KOLUMNY (osłony)
    for (x, y) in ((-4.5, -2.8), (4.5, -2.8), (-4.5, 2.8), (4.5, 2.8)):
        kol.box((x, y, 0.2), (1.0, 1.0, 0.4), "posadzka_ciemna")
        kol.box((x, y, 0.47), (0.86, 0.86, 0.14), "posadzka_jasna")
        kol.walec((x, y, 0.54), 0.38, 3.5, "tynk_bialy", n=12)
        for zz in (0.62, 3.75):
            kol.walec((x, y, zz), 0.41, 0.08, "zloto", n=12)
        kol.walec((x, y, 4.04), 0.38, 0.22, "tynk_bialy", n=12, r_gora=0.5)
        kol.box((x, y, 4.36), (1.0, 1.0, 0.12), "zloto")
        kol.box((x, y, 4.6), (1.2, 1.2, 0.36), "tynk_bialy")
        kol.box((x, y, 4.89), (1.0, 1.0, 0.22), "sufit")
        for k in range(12):                                                  # kanelury
            a = 2 * math.pi * (k + 0.5) / 12
            det.box((x + math.cos(a) * 0.38, y + math.sin(a) * 0.38, 2.3), (0.05, 0.05, 2.9), "sufit", rot_z=math.degrees(a))
        # kinkiet na kolumnie (od strony środka holu)
        sx = -1 if x > 0 else 1
        kinkiet(det, x + sx * 0.38, y, 2.6, sx, 0)

    # ------------------------------------------------------------------ SCHODY NA PIĘTRO (ściana wsch.)
    XS0, XS1 = 8.1, 9.9
    n = 15
    gl = 0.3
    y_dol = 1.2
    hst = schody_proste(sch, XS0, XS1, y_dol, -1, 0.0, ZP, n, gl, "posadzka_jasna", mat_nosek="posadzka_ciemna")
    y_gora = y_dol - n * gl                                                   # = -3.3
    sch.box_od_do(XS0, -6.9, 0.0, XS1, y_gora, ZP, "posadzka_jasna", bez_dolu=False)   # podest na masywie
    # chodnik na stopniach i podeście
    for i in range(n):
        ya = y_dol - i * gl
        zb = (i + 1) * hst
        dyw.box_od_do(8.5, ya - gl, zb, 9.5, ya, zb + 0.012, "dywan_czerwony")
        det.box_od_do(8.48, ya - gl + 0.01, zb, 9.52, ya - gl + 0.04, zb + 0.03, "zloto")   # pręty mosiężne
    dyw.box_od_do(8.5, -6.7, ZP, 9.5, y_gora, ZP + 0.012, "dywan_czerwony")
    # pełna balustrada (kamień) + mosiężna poręcz, skośna i na podeście
    skos(sch, XS0 - 0.16, XS0, y_dol + 0.05, 0.0, y_gora, ZP, 1.05, "piaskowiec")
    sch.box_od_do(XS0 - 0.16, -6.9, ZP, XS0, y_gora, ZP + 1.05, "piaskowiec")
    det.belka((XS0 - 0.08, y_dol + 0.05, 1.12), (XS0 - 0.08, y_gora, ZP + 1.12), 0.08, "zloto")
    det.box_od_do(XS0 - 0.13, -6.9, ZP + 1.05, XS0 - 0.03, y_gora, ZP + 1.15, "zloto")
    # słupek startowy z mosiężną lampą-pochodnią
    sch.box_od_do(XS0 - 0.3, y_dol - 0.05, 0, XS0 + 0.1, y_dol + 0.35, 1.25, "posadzka_ciemna")
    det.box_od_do(XS0 - 0.34, y_dol - 0.09, 1.25, XS0 + 0.14, y_dol + 0.39, 1.33, "zloto")
    det.walec((XS0 - 0.1, y_dol + 0.15, 1.33), 0.06, 0.45, "zloto", n=6)
    det.walec((XS0 - 0.1, y_dol + 0.15, 1.78), 0.16, 0.32, "lampa", n=8, r_gora=0.1)
    # płyciny pod biegiem schodów (bok masywu od strony holu)
    for i in range(4):
        ya, yb = y_dol - 0.3 - i * 1.0, y_dol - 1.1 - i * 1.0
        zt = (y_dol - ya) / gl * hst
        if zt > 0.6:
            det.box_od_do(XS0 - 0.19, yb, 0.25, XS0 - 0.16, ya, zt - 0.2, "boazeria")
    for i in range(3):
        ya, yb = y_gora - 0.25 - i * 1.15, y_gora - 1.15 - i * 1.15
        det.box_od_do(XS0 - 0.19, yb, 0.3, XS0 - 0.16, ya, 2.0, "boazeria")

    # ------------------------------------------------------------------ RECEPCJA (lada wbudowana + przegródki na klucze)
    LX0, LX1 = -11.1, -10.45
    rec.box_od_do(LX0, -2.7, 0, LX1, 1.9, 1.05, "boazeria", bez_dolu=False)
    rec.box_od_do(LX0 - 0.1, -2.8, 1.05, LX1 + 0.12, 2.0, 1.12, "posadzka_ciemna")  # blat z czarnego marmuru
    rec.box_od_do(LX1, -2.7, 0, LX1 + 0.06, 1.9, 0.12, "posadzka_ciemna")             # cokół
    for k in range(9):                                                                   # pionowe żłobki art déco
        y = -2.45 + k * 0.53
        det.box_od_do(LX1, y - 0.04, 0.2, LX1 + 0.04, y + 0.04, 0.95, "zloto" if k % 4 == 0 else "drewno_ciemne")
    det.box_od_do(LX1, -2.7, 0.95, LX1 + 0.04, 1.9, 1.0, "zloto")
    rec.box_od_do(LX0, -2.7, 1.12, LX0 + 0.12, 1.9, 1.3, "boazeria")                     # nadstawka od tyłu
    # ściana z przegródkami (regał wbudowany w tylną ścianę wnęki)
    XB = -13.0 + GR / 2
    rec.box_od_do(XB, -2.2, 0, XB + 0.45, 2.2, 0.95, "boazeria", bez_dolu=False)        # szafka dolna
    rec.box_od_do(XB, -2.25, 0.95, XB + 0.5, 2.25, 1.0, "posadzka_ciemna")
    rec.box_od_do(XB, -2.0, 1.25, XB + 0.04, 2.0, 2.85, "drewno_ciemne")                 # tło przegródek
    for j in range(6):
        z = 1.25 + j * 0.32
        rec.box_od_do(XB, -2.0, z, XB + 0.3, 2.0, z + 0.03, "boazeria")
    rec.box_od_do(XB, -2.0, 2.82, XB + 0.32, 2.0, 2.9, "boazeria")
    for i in range(11):
        y = -2.0 + i * 0.4
        rec.box_od_do(XB, y - 0.015, 1.25, XB + 0.3, y + 0.015, 2.85, "boazeria")
    for i in range(10):                                                                 # klucze z mosiężnymi gruszkami
        for j in range(5):
            if (i * 7 + j * 3) % 5 == 0:
                continue
            y, z = -1.8 + i * 0.4, 1.33 + j * 0.32
            det.box_od_do(XB + 0.08, y - 0.03, z, XB + 0.14, y + 0.03, z + 0.12, "zloto")
    rec.box_od_do(XB, -2.4, 2.9, XB + 0.35, 2.4, 3.15, "boazeria")                     # gzyms regału
    det.box_od_do(XB + 0.32, -2.4, 3.0, XB + 0.37, 2.4, 3.06, "zloto")
    det.box((XB + 0.03, 0, 3.45), (0.06, 0.52, 0.52), "zloto")                           # zegar ścienny
    det.box((XB + 0.07, 0, 3.45), (0.03, 0.4, 0.4), "bialy")
    # telefon na ścianie obok wnęki (prefab): drewniana płyta + półeczka
    det.box_od_do(-9.88, 4.25, 1.05, -9.84, 4.95, 1.95, "drewno_ciemne")
    det.box_od_do(-9.88, 4.3, 1.1, -9.62, 4.9, 1.14, "drewno_ciemne")
    kinkiet(det, -9.85, 5.35, 2.1, 1, 0)

    # winda (art déco, ściana zachodnia, mosiężne drzwi nożycowe; bez otworu)
    win.box_od_do(-9.9, -6.25, 0, -9.7, -4.25, 3.1, "tynk_bialy")
    win.box_od_do(-9.9, -6.0, 0, -9.62, -4.5, 2.3, "metal_ciemny")
    for k in range(10):
        y = -5.95 + k * 0.155
        det.box_od_do(-9.62, y, 0.05, -9.58, y + 0.03, 2.25, "zloto")
    for k in range(2):
        det.belka((-9.6, -5.95, 0.1 + k * 1.1), (-9.6, -4.55, 1.15 + k * 1.1), 0.03, "zloto")
        det.belka((-9.6, -4.55, 0.1 + k * 1.1), (-9.6, -5.95, 1.15 + k * 1.1), 0.03, "zloto")
    win.box_od_do(-9.9, -6.35, 3.1, -9.6, -4.15, 3.25, "zloto")
    det.box_od_do(-9.7, -5.5, 2.45, -9.64, -5.0, 2.95, "zloto")                         # tarcza wskazówki piętra
    win.box_od_do(-9.9, -6.15, 2.35, -9.66, -4.35, 2.4, "zloto")

    # ------------------------------------------------------------------ KOMINEK (ściana tylna, oś holu)
    KY = -7.0 + GR / 2
    kom.box_od_do(-1.6, KY, 0, 1.6, KY + 0.55, 1.35, "posadzka_jasna", bez_dolu=False)
    kom.box_od_do(-0.65, KY + 0.5, 0.15, 0.65, KY + 0.57, 1.0, "czarny")                 # palenisko
    kom.box_od_do(-0.75, KY + 0.47, 0.1, 0.75, KY + 0.6, 1.1, "posadzka_ciemna")
    kom.box_od_do(-0.65, KY + 0.6, 0.1, 0.65, KY + 0.62, 1.0, "czarny")
    kom.box_od_do(-1.8, KY, 1.35, 1.8, KY + 0.7, 1.47, "posadzka_ciemna")               # gzyms
    kom.box_od_do(-1.7, KY + 0.55, 0, 1.7, KY + 1.3, 0.08, "posadzka_ciemna")           # płyta przed kominkiem
    for sg in (-1, 1):
        kom.box_od_do(sg * 1.25 - 0.18, KY + 0.55, 0.08, sg * 1.25 + 0.18, KY + 0.62, 1.35, "posadzka_ciemna")
    kom.box_od_do(-1.2, KY, 1.47, 1.2, KY + 0.3, 3.55, "posadzka_jasna")                # nadstawa
    kom.box_od_do(-0.95, KY + 0.3, 1.75, 0.95, KY + 0.33, 3.3, "zloto")                 # rama lustra
    kom.box_od_do(-0.85, KY + 0.33, 1.85, 0.85, KY + 0.35, 3.2, "okno")                 # lustro
    for k, w in enumerate((1.3, 1.0, 0.7)):                                              # stopniowany szczyt
        kom.box_od_do(-w, KY, 3.55 + k * 0.15, w, KY + 0.32, 3.7 + k * 0.15, "posadzka_jasna")
    det.box_od_do(-0.5, KY + 0.6, 1.0, 0.5, KY + 0.64, 1.04, "zloto")
    for sg in (-1, 1):
        det.walec((sg * 1.4, KY + 0.35, 1.47), 0.05, 0.3, "zloto", n=6)                 # mosiężne lichtarze
        det.walec((sg * 1.4, KY + 0.35, 1.77), 0.03, 0.12, "lampa", n=6)
    # ślepe drzwi dwuskrzydłowe do restauracji po bokach kominka
    for x in (-6.4, 6.4):
        det.box_od_do(x - 0.95, KY, 0, x + 0.95, KY + 0.07, 2.9, "drzwi_ciemne")
        det.box_od_do(x - 0.85, KY + 0.07, 0.05, x + 0.85, KY + 0.11, 2.75, "drzwi")
        det.box_od_do(x - 0.02, KY + 0.11, 0.05, x + 0.02, KY + 0.13, 2.75, "drzwi_ciemne")
        for sg in (-1, 1):
            det.box_od_do(x + sg * 0.42 - 0.3, KY + 0.11, 1.2, x + sg * 0.42 + 0.3, KY + 0.12, 2.6, "okno")
        det.box_od_do(x - 1.05, KY, 2.9, x + 1.05, KY + 0.14, 3.05, "zloto")

    # ------------------------------------------------------------------ SUFITY (NOCOL), kasetony
    sufit(suf, -10.1, 10.1, -7.1, 7.1, H)
    for x in (-6.6, -2.2, 2.2, 6.6):
        suf.box_od_do(x - 0.18, -7.0, H - 0.35, x + 0.18, 7.0, H, "sufit", bez_dolu=False)
    for y in (-3.5, 0.0, 3.5):
        suf.box_od_do(-10.0, y - 0.18, H - 0.35, 10.0, y + 0.18, H, "sufit", bez_dolu=False)
    for (x, y) in ((-4.4, -1.75), (0.0, -1.75), (4.4, -1.75), (-4.4, 1.75), (0.0, 1.75), (4.4, 1.75)):
        suf.box_od_do(x - 1.9, y - 1.5, H - 0.04, x + 1.9, y + 1.5, H, "tynk_kremowy", bez_dolu=False)
        suf.box_od_do(x - 1.6, y - 1.2, H - 0.06, x + 1.6, y + 1.2, H, "zloto", bez_dolu=False)
        suf.box_od_do(x - 1.5, y - 1.1, H - 0.08, x + 1.5, y + 1.1, H, "sufit", bez_dolu=False)
    sufit(suf, -13.1, -9.9, -3.1, 3.1, 3.6)
    sufit(suf, 9.9, 12.3, -7.1, 5.1, ZK)
    sufit(suf, 12.1, 17.3, -1.1, 5.1, ZK)
    suf.walec((14.7, 2.0, ZK - 0.04), 0.6, 0.04, "sufit", n=12, dol=True)               # rozeta

    # ------------------------------------------------------------------ ŚWIATŁA I ŻYRANDOLE
    for i, x in enumerate((-6.6, 0.0, 6.6)):
        zyrandol(zyr, x, 0.0, H - 0.35, 3.55, r=0.9 if x else 1.1)
        punkt("hotel_latarnia_%d" % (i + 1), (x, 0.0, 3.6), 0)
        swiatlo((x, 0.0, 3.5), kolor="#ffcf8a", zasieg=11, moc=2.6)
    swiatlo((-11.6, 0.0, 3.0), kolor="#ffc77a", zasieg=6, moc=1.6)                      # recepcja
    zyrandol(zyr, -11.6, 0.0, 3.6, 2.95, r=0.45)
    swiatlo((0.0, -5.6, 2.2), kolor="#ffb35c", zasieg=5, moc=1.2)                       # poświata kominka
    swiatlo((-0.1 + 0, 6.2, 2.7), kolor="#ffcf8a", zasieg=6, moc=1.0)                   # wejście
    swiatlo((8.9, -2.0, 3.8), kolor="#ffcf8a", zasieg=7, moc=1.4)                       # schody
    for y in (-5.0, -1.5, 2.5):                                                          # korytarz: kinkiety
        kinkiet(det, 10.0 + GR / 2 + 0.03, y, ZP + 1.85, 1, 0)
        swiatlo((10.6, y, ZP + 2.0), kolor="#ffc77a", zasieg=5, moc=1.0)
    zyrandol(zyr, 14.7, 2.0, ZK, ZK - 0.9, r=0.5)
    swiatlo((14.7, 2.0, ZK - 0.95), kolor="#ffc77a", zasieg=7, moc=1.4)
    kinkiet(det, 17.2 - GR / 2 - 0.03, 4.3, ZP + 1.7, -1, 0)

    # ------------------------------------------------------------------ POKÓJ NAUJOCKSA: wbudowane
    # szafa wnękowa w ścianie południowej
    szf.box_od_do(12.4, -0.9, ZP, 14.4, -0.32, ZP + 2.45, "boazeria", bez_dolu=False)
    szf.box_od_do(12.35, -0.9, ZP + 2.45, 14.45, -0.28, ZP + 2.6, "drewno_ciemne")
    for x in (12.9, 13.9):
        det.box_od_do(x - 0.42, -0.32, ZP + 0.15, x + 0.42, -0.29, ZP + 2.3, "drewno_ciemne")
        det.box_od_do(x - 0.32, -0.29, ZP + 1.3, x + 0.32, -0.28, ZP + 2.15, "okno")      # lustro w drzwiach
    det.box_od_do(13.38, -0.29, ZP + 1.0, 13.42, -0.26, ZP + 1.25, "zloto")
    # piec kaflowy w narożniku
    pie.box_od_do(16.35, -0.9, ZP, 17.1, -0.15, ZP + 0.35, "drewno_ciemne")
    pie.box_od_do(16.4, -0.85, ZP + 0.35, 17.1, -0.2, ZP + 2.2, "kafle_biale")
    pie.box_od_do(16.32, -0.92, ZP + 2.2, 17.1, -0.12, ZP + 2.4, "kafle_biale")
    pie.box_od_do(16.45, -0.8, ZP + 2.4, 17.1, -0.25, ZP + 2.55, "kafle_biale")
    for j in range(5):
        det.box_od_do(16.38, -0.87, ZP + 0.6 + j * 0.32, 17.1, -0.18, ZP + 0.62 + j * 0.32, "tynk_szary")
    det.box_od_do(16.58, -0.16, ZP + 0.6, 16.92, -0.13, ZP + 0.9, "zloto")              # drzwiczki
    # półka wnękowa (wbudowana) na ścianie północnej
    szf.box_od_do(12.6, 4.6, ZP + 1.15, 13.6, 4.9, ZP + 1.2, "boazeria")
    szf.box_od_do(12.6, 4.6, ZP + 1.55, 13.6, 4.9, ZP + 1.6, "boazeria")

    # ------------------------------------------------------------------ PUNKTY
    punkt("spawn_hotel_wejscie", (0.0, 5.3, 0.0), 180)
    punkt("hotel_drzwi_wyjscie", (0.0, 6.0, 0.0), 0)
    punkt("hotel_recepcja", (-11.8, 0.0, 0.0), 90)
    punkt("hotel_telefon", (-9.82, 4.6, 1.45), 90)            # na ścianie (prefab telefonu), front w hol
    punkt("hotel_kolumna", (-4.5, 2.05, 0.0), 0)               # za kolumną, od strony kominka
    punkt("hotel_patrol_1", (-6.5, 4.8, 0.0), 90)
    punkt("hotel_patrol_2", (6.0, -4.6, 0.0), 270)
    punkt("hotel_schody_dol", (9.0, 2.0, 0.0), 180)
    punkt("hotel_schody_gora", (9.0, -5.0, ZP), 90)
    punkt("hotel_pokoj_drzwi", (12.2, 2.0, ZP), 90)           # środek otworu drzwi pokoju
    punkt("hotel_pokoj_biurko", (16.5, 3.9, ZP), 270)          # prefab biurka pod ścianą okienną
    punkt("hotel_pokoj_mapa", (16.5, 3.9, ZP + 0.78), 270)     # na blacie biurka
    punkt("hotel_pokoj_lozko", (13.4, 2.8, ZP), 90)
    punkt("hotel_pokoj_lista", (13.1, 4.75, ZP + 1.2), 180)    # na półce wnękowej
    # meble holu (prefaby): fotele przy kominku, kanapy między kolumnami
    punkt("hotel_mebel_kominek_1", (-1.5, -4.6, 0.0), 160)
    punkt("hotel_mebel_kominek_2", (1.5, -4.6, 0.0), 200)
    punkt("hotel_mebel_salon_1", (-7.0, 1.5, 0.0), 90)
    punkt("hotel_mebel_salon_2", (6.5, 4.6, 0.0), 270)

    siatki = [podl, dyw, sc, boa, suf, kol, sch, rec, kom, okn, det, zyr, ram, szf, pie, win]
    return [s.obiekt() for s in siatki if not s.pusta()]


if __name__ == "__main__":
    wyczysc_scene()
    obs = zbuduj()
    policz_trojkaty(obs)
    eksport_fbx(obs, os.path.join(UNITY_MODELE, "Wnetrze_Hotel.fbx"))
    zapisz_meta(os.path.join(DANE, "meta_wnetrze_hotel.json"))
    swiatla_podgladu()
    ukryj_sufity(obs, True, ("Sufit",))
    render_podglad(os.path.join(PODGLADY, "wnetrze_hotel_1.png"), cel=(1.5, -0.5, 1.0), odl=34, azymut=200, wysokosc_kat=52)
    render_podglad(os.path.join(PODGLADY, "wnetrze_hotel_3.png"), cel=(13.5, -1.0, 2.5), odl=16, azymut=230, wysokosc_kat=55)
    ukryj_sufity(obs, False, ("Sufit",))
    oczy(os.path.join(PODGLADY, "wnetrze_hotel_2.png"), (2.5, 5.6, 1.65), (-3.0, -3.0, 1.9), ogniskowa=17, noc=True)
    oczy(os.path.join(PODGLADY, "wnetrze_hotel_4.png"), (12.7, 0.0, ZP + 1.65), (16.8, 3.5, ZP + 1.2), ogniskowa=16, noc=True)
