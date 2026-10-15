# -*- coding: utf-8 -*-
"""
Wnętrze: zakład fotograficzny (Gliwice, 1939), ok. 8 x 12 m, wys. 3,8 m.
Sklep (front +Y, witryna i wyjście): y 1.5..6, lada w poprzek, gabloty przy ścianach bocznych.
Studio (atelier): x -4..1.4, y -6..1.5 — tło z płótna z zawinięciem na podłogę, podest, duże okno atelierowe.
Zaplecze: x 1.4..4, y -3.4..1.5 (drzwi ze sklepu, przejście do studia).
Ciemnia: x 1.4..4, y -6..-3.4, drzwi z zaplecza za czarną kotarą, czerwone światło.
Meble ruchome (aparat na statywie, lampy studyjne, krzesła, stół do retuszu, powiększalnik) = prefaby -> punkty.
"""
import sys, os, random, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *
from _wnetrza_wspolne import *

H = 3.8
X0, X1 = -4.0, 4.0
YF, YS, YC, YT = 6.0, 1.5, -3.4, -6.0     # front, sklep|tył, zaplecze|ciemnia, tył
XZ = 1.4                                  # studio | zaplecze+ciemnia


def zbuduj():
    rng = random.Random(1939 + 8)
    P = Plan()
    podl = Siatka("Fotograf_Podloga")
    sc = Siatka("Fotograf_Sciany")
    okl = Siatka("Fotograf_Okladziny")
    suf = Siatka("Fotograf_Sufit_NOCOL")
    lad = Siatka("Fotograf_Lada")
    gab = Siatka("Fotograf_Gabloty")
    tlo = Siatka("Fotograf_Studio_Tlo")
    pod = Siatka("Fotograf_Studio_Podest")
    zlw = Siatka("Fotograf_Ciemnia_Zlew")
    reg = Siatka("Fotograf_Zaplecze_Regal")
    pud = Siatka("Fotograf_Zaplecze_Pudla_NOCOL")
    kot = Siatka("Fotograf_Kotary_NOCOL")
    okn = Siatka("Fotograf_Okna")
    det = Siatka("Fotograf_Detal_NOCOL")
    lmp = Siatka("Fotograf_Lampy_NOCOL")
    ram = Siatka("Fotograf_DrzwiWyjscie_Rama_NOCOL")

    # ------------------------------------------------------------------ OTWORY
    P.drzwi("x", YF, 0.0)                                   # wyjście na ulicę
    P.okno("x", YF, 0.0, 2.45, 1.1, 0.6)                    # naświetle
    for x in (-2.35, 2.35):
        P.okno("x", YF, x, 0.75, 2.3, 2.25)                 # witryny
    P.drzwi("x", YS, 2.7)                                   # sklep -> zaplecze
    P.otwor("y", XZ, -0.9, 1.8, 0.0, 2.5, "przejscie")      # zaplecze -> studio
    P.drzwi("x", YC, 2.7, szer=0.9, wys=2.1)                # zaplecze -> ciemnia
    P.okno("y", X0, -2.0, 0.9, 3.6, 2.6)                    # okno atelierowe (północne)
    P.okno("y", X1, -1.0, 1.3, 1.0, 1.4)                    # okno zaplecza

    # ------------------------------------------------------------------ PODŁOGI
    T = -0.004
    plyta(podl, X0 - 0.1, X1 + 0.1, YS, YF + 0.1, T, 0.3, "parkiet")
    deski(podl, X0 + 0.1, X1 - 0.1, YS + 0.1, YF - 0.1, 0.0, 0.12, "parkiet", "modrzew", wzdluz="y")
    plyta(podl, X0 - 0.1, XZ, YT - 0.1, YS, T, 0.3, "drewno")
    deski(podl, X0 + 0.1, XZ - 0.1, YT + 0.1, YS - 0.1, 0.0, 0.16, "drewno", "modrzew", wzdluz="x")
    plyta(podl, XZ, X1 + 0.1, YC, YS, T, 0.3, "deska_szara")
    deski(podl, XZ + 0.1, X1 - 0.1, YC + 0.1, YS - 0.1, 0.0, 0.2, "deska_szara", "tynk_brudny", wzdluz="y")
    plyta(podl, XZ, X1 + 0.1, YT - 0.1, YC, T, 0.3, "beton")
    posadzka_krata(podl, XZ + 0.1, X1 - 0.1, YT + 0.1, YC - 0.1, 0.0, 8, 8, "kafle_biale", "tynk_szary")
    podl.box_od_do(-0.55, YF - 0.1, -0.01, 0.55, YF + 0.1, 0.015, "kamien_ciemny")
    det.box_od_do(-0.7, YF - 1.1, 0.0, 0.7, YF - 0.15, 0.012, "dywan_czerwony")
    det.box_od_do(-3.0, 3.4, 0.0, -1.0, 5.3, 0.012, "dywan_czerwony")                    # chodnik przy gablocie
    det.box_od_do(-2.9, 3.5, 0.012, -1.1, 5.2, 0.016, "tkanina_bordo")

    # ------------------------------------------------------------------ ŚCIANY
    W = "tynk_kremowy"
    P.sciana(sc, "x", YF, X0, X1, 0, H, W)
    P.sciana(sc, "x", YT, X0, X1, 0, H, W)
    P.sciana(sc, "x", YS, X0, X1, 0, H, W)
    P.sciana(sc, "x", YC, XZ, X1, 0, H, W)
    P.sciana(sc, "y", X0, YT, YF, 0, H, W)
    P.sciana(sc, "y", X1, YT, YF, 0, H, W)
    P.sciana(sc, "y", XZ, YT, YS, 0, H, W)
    # sklep: tapeta bordo, boazeria do 1,2 m, gzyms z mosiądzem
    P.okladzina(okl, X0, X1, YS, YF, 1.2, H, "tapeta_bordo", grub=0.02)
    P.okladzina(okl, X0, X1, YS, YF, 0.0, 1.2, "boazeria", grub=0.035)
    P.listwa(det, X0, X1, YS, YF, 1.2, 0.06, 0.06, "drewno_ciemne")
    P.listwa(det, X0, X1, YS, YF, H - 0.22, 0.22, 0.09, "drewno_ciemne")
    P.listwa(det, X0, X1, YS, YF, H - 0.28, 0.04, 0.05, "zloto")
    # studio: jasny tynk, niska boazeria
    P.okladzina(okl, X0, XZ, YT, YS, 0.0, 0.9, "boazeria", grub=0.03)
    P.listwa(det, X0, XZ, YT, YS, 0.9, 0.05, 0.05, "drewno_ciemne")
    P.listwa(det, X0, XZ, YT, YS, H - 0.15, 0.15, 0.06, "sufit")
    # zaplecze: lamperia olejna
    P.okladzina(okl, XZ, X1, YC, YS, 0.0, 1.5, "zielen_okienna", grub=0.02)
    P.listwa(det, XZ, X1, YC, YS, 1.5, 0.03, 0.03, "czarny")
    # ciemnia: czarna lamperia, szary tynk
    P.okladzina(okl, XZ, X1, YT, YC, 0.0, 1.8, "czarny", grub=0.02)
    P.okladzina(okl, XZ, X1, YT, YC, 1.8, H, "tynk_szary", grub=0.02)

    # ------------------------------------------------------------------ PORTRETY (ramy, sepia, sylwetka bez twarzy)
    def portret(os_, c, sg, s, z, w, h, rama="zloto"):
        w0 = c + sg * (GR / 2 + 0.035)
        B = lambda a, b, za, zb, d0, d1, mat: P._box(det, os_, w0 + sg * (d0 + d1) / 2, a, b, za, zb, d1 - d0, mat)
        B(s - w / 2, s + w / 2, z - h / 2, z + h / 2, 0.0, 0.03, rama)
        B(s - w / 2 + 0.05, s + w / 2 - 0.05, z - h / 2 + 0.05, z + h / 2 - 0.05, 0.03, 0.035, "tapeta_kremowa")
        B(s - w * 0.33, s + w * 0.33, z - h / 2 + 0.05, z - h * 0.2, 0.035, 0.04, "ksiazka_5")          # ramiona
        B(s - w * 0.24, s + w * 0.24, z - h * 0.2, z - h * 0.15, 0.035, 0.04, "ksiazka_5")
        B(s - w * 0.05, s + w * 0.05, z - h * 0.15, z - h * 0.09, 0.035, 0.04, "ksiazka_5")             # szyja
        B(s - w * 0.12, s + w * 0.12, z - h * 0.09, z + h * 0.2, 0.035, 0.04, "ksiazka_5")              # głowa
        B(s - w * 0.15, s + w * 0.15, z - h * 0.02, z + h * 0.12, 0.035, 0.04, "ksiazka_5")
    # nad gablotami i za ladą
    for (s, z, w, h) in ((4.4, 2.75, 0.6, 0.8), (3.3, 2.55, 0.45, 0.6), (5.3, 2.55, 0.45, 0.6)):
        portret("y", X0, +1, s, z, w, h)
        portret("y", X1, -1, s, z, w, h, rama="drewno_ciemne")
    for (s, z, w, h) in ((-2.6, 2.3, 0.9, 1.15), (-1.3, 2.5, 0.55, 0.75), (-0.2, 2.3, 0.9, 1.15), (0.95, 2.5, 0.55, 0.75)):
        portret("x", YS, +1, s, z, w, h, rama="zloto" if w > 0.6 else "drewno_ciemne")
    # w studiu: próbki na ścianie wsch. (przy przejściu)
    for (s, z) in ((0.6, 1.8), (-2.8, 1.8)):
        portret("y", XZ, -1, s, z, 0.5, 0.65, rama="drewno_ciemne")

    # ------------------------------------------------------------------ GABLOTY (wbudowane, ściany boczne sklepu)
    def gablota(os_, c, sg, s0, s1):
        w0 = c + sg * (GR / 2 + 0.035)
        B = lambda m, a, b, za, zb, d0, d1, mat: P._box(m, os_, w0 + sg * (d0 + d1) / 2, a, b, za, zb, d1 - d0, mat)
        B(gab, s0, s1, 0.0, 0.1, 0.0, 0.42, "drewno_ciemne")
        B(gab, s0, s1, 0.1, 0.92, 0.0, 0.45, "boazeria")
        B(gab, s0 - 0.03, s1 + 0.03, 0.92, 0.96, 0.0, 0.5, "drewno_ciemne")
        n = 3
        d = (s1 - s0) / n
        for i in range(n):                                                     # płyciny szafki
            a = s0 + i * d
            B(det, a + 0.08, a + d - 0.08, 0.2, 0.82, 0.45, 0.465, "drewno_ciemne")
        B(gab, s0, s1, 0.96, 2.15, 0.0, 0.02, "tkanina_bordo")                 # plecy (aksamit)
        for s in (s0, s1):
            B(gab, s - 0.03, s + 0.03, 0.96, 2.15, 0.0, 0.36, "drewno_ciemne")
        B(gab, s0 - 0.04, s1 + 0.04, 2.15, 2.27, 0.0, 0.4, "drewno_ciemne")
        for z in (1.38, 1.78):                                                  # półki szklane (krawędź mosiężna)
            B(gab, s0, s1, z - 0.012, z, 0.02, 0.34, "rama")
            B(det, s0, s1, z - 0.02, z, 0.34, 0.36, "zloto")
        for s in [s0 + k * (s1 - s0) / 4 for k in range(1, 4)]:                 # szprosy frontu (mosiądz)
            B(det, s - 0.012, s + 0.012, 0.96, 2.15, 0.35, 0.37, "zloto")
        B(det, s0, s1, 0.96, 0.99, 0.35, 0.37, "zloto")
        B(det, s0, s1, 2.12, 2.15, 0.35, 0.37, "zloto")
        # eksponaty: aparaty (korpus + obiektyw + mieszek), ramki
        for z in (0.96, 1.38, 1.78):
            s = s0 + 0.18
            while s < s1 - 0.2:
                r = rng.random()
                if r < 0.55:
                    B(det, s - 0.09, s + 0.09, z, z + 0.12, 0.1, 0.2, "czarny")
                    B(det, s - 0.035, s + 0.035, z + 0.025, z + 0.095, 0.2, 0.26, "metal")
                    s += 0.32
                elif r < 0.8:
                    hh = rng.uniform(0.14, 0.22)
                    B(det, s - hh * 0.4, s + hh * 0.4, z, z + hh, 0.06, 0.08, "zloto")
                    B(det, s - hh * 0.32, s + hh * 0.32, z + 0.03, z + hh - 0.03, 0.08, 0.085, "tapeta_kremowa")
                    s += 0.28
                else:
                    B(det, s - 0.1, s + 0.1, z, z + 0.16, 0.08, 0.28, "drewno")            # aparat mieszkowy
                    B(det, s - 0.07, s + 0.07, z + 0.02, z + 0.14, 0.28, 0.36, "czarny")
                    s += 0.34
    gablota("y", X0, +1, 2.4, 5.4)
    gablota("y", X1, -1, 3.6, 5.4)

    # ------------------------------------------------------------------ LADA (w poprzek sklepu)
    LX0, LX1, LY0, LY1, LZ = -3.0, 1.75, 2.5, 3.1, 0.95
    lad.box_od_do(LX0, LY0, 0.0, LX1, LY1 - 0.05, 0.1, "drewno_ciemne", bez_dolu=False)
    lad.box_od_do(LX0, LY0, 0.1, LX1, LY1, LZ - 0.05, "boazeria", bez_dolu=False)
    lad.box_od_do(LX0 - 0.04, LY0 - 0.04, LZ - 0.05, LX1 + 0.04, LY1 + 0.06, LZ, "drewno_ciemne", bez_dolu=False)
    n = 6
    dx = (LX1 - LX0) / n
    for i in range(n):                                                          # płyciny od strony klienta (+Y)
        a = LX0 + i * dx
        lad.box_od_do(a + 0.1, LY1, 0.22, a + dx - 0.1, LY1 + 0.02, LZ - 0.17, "drewno_ciemne")
        lad.box_od_do(a + 0.16, LY1 + 0.02, 0.28, a + dx - 0.16, LY1 + 0.035, LZ - 0.23, "boazeria")
    for x in [LX0 + i * dx for i in range(n + 1)]:
        lad.box_od_do(x - 0.035, LY1, 0.1, x + 0.035, LY1 + 0.04, LZ - 0.05, "drewno_ciemne")
    det.box_od_do(LX0 - 0.05, LY1 + 0.055, LZ - 0.05, LX1 + 0.05, LY1 + 0.075, LZ - 0.02, "zloto")
    det.box_od_do(LX0 - 0.05, LY1 + 0.04, 0.1, LX1 + 0.05, LY1 + 0.06, 0.13, "zloto")
    # część lady z gablotką na blacie (ramki pod szkłem): skrzynka z mosiężnym okuciem
    lad.box_od_do(-2.7, 2.6, LZ, -1.4, 3.05, LZ + 0.22, "drewno_ciemne", bez_dolu=False)
    det.box_od_do(-2.66, 2.64, LZ + 0.22, -1.44, 3.01, LZ + 0.225, "rama")
    for k in range(3):
        x = -2.5 + k * 0.4
        det.box_od_do(x - 0.12, 2.7, LZ + 0.225, x + 0.12, 2.95, LZ + 0.23, "tapeta_kremowa")
    # półki za ladą (na ścianie przy drzwiach zaplecza): albumy
    regal_wbudowany(P, gab, det, "x", YS, -3.85, -2.0, +1, 0.0, 1.55, 2, rng, gleb=0.32, segment=0.95,
                    kolory=("czarny", "ksiazka_5", "ksiazka_1", "tkanina_bez"), jasne=(), gesto=0.75, szer=(0.08, 0.3))

    # ------------------------------------------------------------------ WITRYNA (podest z eksponatami)
    for x0_, x1_ in ((-3.5, -1.2), (1.2, 3.5)):
        lad.box_od_do(x0_, YF - 0.85, 0.0, x1_, YF - 0.12, 0.6, "boazeria", bez_dolu=False)
        lad.box_od_do(x0_ - 0.02, YF - 0.87, 0.6, x1_ + 0.02, YF - 0.1, 0.65, "drewno_ciemne", bez_dolu=False)
        det.box_od_do(x0_, YF - 0.87, 0.65, x1_, YF - 0.1, 0.655, "tkanina_bordo")
        for k in range(3):                                                     # sztalugi z portretami (frontem na ulicę)
            x = x0_ + 0.4 + k * (x1_ - x0_ - 0.8) / 2
            hh = 0.55 if k == 1 else 0.42
            det.box_od_do(x - hh * 0.4, YF - 0.42, 0.68, x + hh * 0.4, YF - 0.38, 0.68 + hh, "zloto")
            det.box_od_do(x - 0.02, YF - 0.55, 0.655, x + 0.02, YF - 0.42, 0.68 + hh * 0.85, "drewno_ciemne")
        det.box_od_do(x0_ + 0.95, YF - 0.75, 0.655, x0_ + 1.15, YF - 0.62, 0.78, "czarny")    # aparat
        det.box_od_do(x0_ + 1.02, YF - 0.62, 0.68, x0_ + 1.08, YF - 0.55, 0.75, "metal")
        det.box_od_do(x0_ - 0.1, YF - 0.2, 2.75, x1_ + 0.1, YF - 0.13, 3.0, "tkanina_bordo")    # lambrekin
        det.box_od_do(x0_ - 0.1, YF - 0.21, 2.74, x1_ + 0.1, YF - 0.13, 2.76, "zloto")
    for x in (-2.35, 2.35):                                                    # szprosy witryny (pas górny)
        P._box(det, "x", YF, x - 1.15, x + 1.15, 2.4, 2.46, 0.1, "rama_ciemna")

    # ------------------------------------------------------------------ STUDIO: tło z płótna + podest
    XA, XB = -3.6, 0.6
    yb = YT + GR / 2 + 0.08
    det.belka((XA - 0.25, yb, 3.45), (XB + 0.25, yb, 3.45), 0.14, "tkanina_bez")         # zwinięte drugie tło
    det.belka((XA - 0.25, yb + 0.12, 3.25), (XB + 0.25, yb + 0.12, 3.25), 0.16, "metal_ciemny")   # wałek
    for x in (XA - 0.25, XB + 0.25):
        det.box_od_do(x - 0.03, YT + GR / 2, 3.15, x + 0.03, yb + 0.25, 3.55, "metal_ciemny")
    zs = 0.35
    tlo.quad((XA, yb + 0.12, 3.25), (XA, yb + 0.12, zs), (XB, yb + 0.12, zs), (XB, yb + 0.12, 3.25), "tynk_szary")
    tlo.quad((XB, yb + 0.12, 3.25), (XB, yb + 0.12, zs), (XA, yb + 0.12, zs), (XA, yb + 0.12, 3.25), "tynk_szary")
    luk = [(yb + 0.12, zs), (yb + 0.22, 0.14), (yb + 0.42, 0.04), (yb + 0.75, 0.006), (yb + 1.6, 0.006)]
    for (ya, za), (yc, zc) in zip(luk, luk[1:]):                             # zawinięcie na podłogę
        tlo.quad((XA, ya, za), (XB, ya, za), (XB, yc, zc), (XA, yc, zc), "tynk_szary")
    # malowane chmury/plamy (płasko na płótnie)
    for (x, z, w, h, m) in ((-2.9, 2.55, 1.0, 0.55, "kamien"), (-1.3, 2.9, 1.3, 0.4, "tynk_brudny"), (-0.1, 2.0, 0.9, 0.7, "kamien"),
                            (-2.5, 1.5, 1.2, 0.5, "tynk_brudny"), (-1.1, 1.0, 0.9, 0.4, "kamien"), (-1.6, 2.1, 0.6, 0.35, "piaskowiec")):
        n_ = 9
        pts = []
        for i in range(n_):
            a = -2 * math.pi * i / n_                                           # malejący kąt -> normalna +Y
            r_ = 1.0 + 0.18 * math.sin(3 * a + x)
            pts.append((x + math.cos(a) * w / 2 * r_, yb + 0.13, z + math.sin(a) * h / 2 * r_))
        tlo.poly(pts, m)
    pod.box_od_do(-2.6, YT + 1.15, 0.0, -0.4, YT + 2.55, 0.28, "drewno", bez_dolu=False)
    pod.box_od_do(-2.0, YT + 2.55, 0.0, -1.0, YT + 2.85, 0.14, "drewno", bez_dolu=False)            # stopień
    det.box_od_do(-2.5, YT + 1.25, 0.28, -0.5, YT + 2.45, 0.29, "dywan_czerwony")
    det.box_od_do(-2.6, YT + 2.53, 0.24, -0.4, YT + 2.56, 0.28, "zloto")
    # kotara boczna przy oknie atelierowym (zsunięta) + rolety
    for k in range(5):
        y = 0.25 + k * 0.1
        kot.box_od_do(X0 + GR / 2 + 0.05, y, 0.02, X0 + GR / 2 + 0.16 + 0.03 * (k % 2), y + 0.1, 3.55, "tkanina_bordo")
    det.box_od_do(X0 + GR / 2 + 0.03, -3.8, 3.56, X0 + GR / 2 + 0.07, 0.8, 3.6, "zloto")
    det.box_od_do(X0 + GR / 2 + 0.03, -3.75, 2.5, X0 + GR / 2 + 0.05, -0.25, 3.45, "tkanina_bez")   # roleta (do połowy)

    # ------------------------------------------------------------------ ZAPLECZE: regał na klisze i pudła
    regal_wbudowany(P, reg, pud, "y", X1, -0.35, 1.3, -1, 0.0, 2.3, 4, rng, gleb=0.45, segment=0.85,
                    mat_kor="deska_szara", mat_polka="deska_szara", mat_tyl="drewno",
                    kolory=("tkanina_bez", "tynk_kremowy", "czarny", "ksiazka_5"), jasne=(), gesto=0.8, szer=(0.18, 0.4))
    for (x, y, w, d_, h) in ((1.85, -2.95, 0.4, 0.5, 0.35), (1.85, -2.95, 0.36, 0.42, 0.25)):            # skrzynie
        zz = 0.0 if h > 0.3 else 0.35
        det.box_od_do(x - w / 2, y - d_ / 2, zz, x + w / 2, y + d_ / 2, zz + h, "drewno")
    det.box_od_do(XZ + GR / 2 + 0.01, -2.7, 1.3, XZ + GR / 2 + 0.05, -1.9, 1.9, "drewno")          # tablica z odbitkami
    for k in range(4):
        det.box_od_do(XZ + GR / 2 + 0.05, -2.62 + k * 0.18, 1.45 + 0.12 * (k % 2), XZ + GR / 2 + 0.06, -2.48 + k * 0.18, 1.65 + 0.12 * (k % 2), "tapeta_kremowa")

    # ------------------------------------------------------------------ CIEMNIA: zlew z kuwetami, półki, linka
    YB = YT + GR / 2
    zlw.box_od_do(XZ + GR / 2, YB, 0.0, X1 - GR / 2, YB + 0.7, 0.82, "beton", bez_dolu=False)
    zlw.box_od_do(XZ + GR / 2, YB, 0.82, X1 - GR / 2, YB + 0.72, 0.9, "kamien_ciemny", bez_dolu=False)
    for k in range(3):                                                         # kuwety
        x = XZ + 0.45 + k * 0.72
        det.box_od_do(x - 0.25, YB + 0.18, 0.9, x + 0.25, YB + 0.58, 0.95, "bialy")
        det.box_od_do(x - 0.21, YB + 0.22, 0.95, x + 0.21, YB + 0.54, 0.951, "czarny")
    det.box_od_do(XZ + 1.2, YB + 0.02, 0.9, XZ + 1.26, YB + 0.08, 1.35, "metal")                  # kran
    det.box_od_do(XZ + 1.2, YB + 0.02, 1.3, XZ + 1.26, YB + 0.3, 1.35, "metal")
    zlw.box_od_do(XZ + GR / 2, YB, 1.5, X1 - GR / 2, YB + 0.28, 1.54, "deska_szara")              # półka
    zlw.box_od_do(XZ + GR / 2, YB, 1.95, X1 - GR / 2, YB + 0.28, 1.99, "deska_szara")
    for zz in (1.54, 1.99):                                                    # butelki z odczynnikami
        x = XZ + 0.3
        while x < X1 - 0.3:
            hh = rng.uniform(0.18, 0.3)
            det.walec((x, YB + 0.14, zz), 0.05, hh, "ksiazka_5" if rng.random() < 0.7 else "czarny", n=6, r_gora=0.03)
            x += rng.uniform(0.13, 0.22)
    # blat boczny pod powiększalnik (wbudowany), ściana wsch.
    zlw.box_od_do(X1 - GR / 2 - 0.65, YT + 1.0, 0.0, X1 - GR / 2, YC - 0.25, 0.85, "deska_szara", bez_dolu=False)
    # linka z odbitkami w poprzek ciemni
    det.belka((XZ + 0.15, -4.5, 2.05), (X1 - 0.15, -4.5, 2.05), 0.015, "czarny")
    for k in range(6):
        x = XZ + 0.45 + k * 0.38
        det.box_od_do(x - 0.1, -4.505, 1.82, x + 0.1, -4.495, 2.04, "bialy" if k % 3 else "tapeta_kremowa")
        det.box_od_do(x - 0.012, -4.52, 2.0, x + 0.012, -4.48, 2.08, "drewno")
    # czarna kotara (śluza światła) za drzwiami od strony ciemni
    yk = YC - GR / 2 - 0.12
    det.box_od_do(2.1, yk - 0.02, 2.2, 3.3, yk + 0.02, 2.24, "metal_ciemny")
    x = 2.12
    k = 0
    while x < 2.5:
        kot.box_od_do(x, yk - 0.05 - 0.03 * (k % 2), 0.02, x + 0.09, yk + 0.04, 2.2, "czarny")
        x += 0.085
        k += 1
    # czerwona lampa ciemniowa (skrzynka z czerwoną szybką) nad zlewem
    det.box_od_do(2.4, YB, 2.25, 3.0, YB + 0.22, 2.55, "metal_ciemny")
    det.box_od_do(2.46, YB + 0.22, 2.29, 2.94, YB + 0.235, 2.51, "czerwony")

    # ------------------------------------------------------------------ DRZWI, OKNA
    P.wypelnij_okna(okn, det, rama="rama_ciemna", parapet="drewno_ciemne", szprosy=(0, 0))
    for k in (-1, 1):                                                          # szprosy okna atelierowego
        P._box(det, "y", X0, -2.0 + k * 0.6 - 0.025, -2.0 + k * 0.6 + 0.025, 0.9, 3.5, 0.08, "rama_ciemna")
    for z in (1.77, 2.63):
        P._box(det, "y", X0, -3.8, -0.2, z - 0.025, z + 0.025, 0.08, "rama_ciemna")
    P.rama_drzwi(ram, "x", YF, 0.0, mat="drzwi_ciemne", strony=(-1,), szer_opaski=0.14, wyst=0.05)
    det.box_od_do(-0.6, YF - 0.2, 3.05, 0.6, YF - 0.13, 3.17, "drzwi_ciemne")
    det.box_od_do(0.4, YF - 0.25, 2.8, 0.46, YF - 0.13, 2.84, "zloto")                         # dzwonek
    det.walec((0.43, YF - 0.3, 2.68), 0.04, 0.1, "zloto", n=6, r_gora=0.015, dol=True)
    P.rama_drzwi(det, "x", YS, 2.7, mat="drzwi_ciemne")
    P.rama_drzwi(det, "y", XZ, -0.9, szer=1.8, wys=2.5, mat="drewno_ciemne")
    P.rama_drzwi(det, "x", YC, 2.7, szer=0.9, wys=2.1, mat="czarny")

    # ------------------------------------------------------------------ SUFIT, ŚWIETLIK, LAMPY
    sufit(suf, X0 - 0.1, X1 + 0.1, YT - 0.1, YF + 0.1, H)
    # świetlik atelier w suficie studia (szyba + szprosy, tuż pod stropem)
    suf.box_od_do(-3.4, -4.6, H - 0.03, 0.4, -0.4, H, "okno", bez_dolu=False)
    for k in range(5):
        x = -3.4 + k * 0.95
        suf.box_od_do(x - 0.04, -4.6, H - 0.08, x + 0.04, -0.4, H, "rama_ciemna", bez_dolu=False)
    for y in (-4.6, -2.5, -0.4):
        suf.box_od_do(-3.4, y - 0.04, H - 0.08, 0.4, y + 0.04, H, "rama_ciemna", bez_dolu=False)
    # sklep: mleczne kule na mosiądzu + kinkiety przy portretach
    for (x, y) in ((-1.0, 4.4), (1.6, 4.4)):
        lmp.box((x, y, H - 0.45), (0.03, 0.03, 0.9), "zloto")
        lmp.walec((x, y, H - 0.05), 0.12, 0.05, "zloto", n=8, dol=True)
        lmp.walec((x, y, H - 1.35), 0.15, 0.08, "zloto", n=8, r_gora=0.2, dol=True)
        lmp.walec((x, y, H - 1.27), 0.24, 0.32, "lampa", n=8, r_gora=0.12, dol=False)
        swiatlo((x, y, H - 1.35), kolor="#ffcf8a", zasieg=7, moc=1.4)
    for x in (-1.95, 0.4):
        kinkiet(det, x, YS + GR / 2 + 0.035, 2.1, 0, 1)
        swiatlo((x, YS + 0.5, 2.2), kolor="#ffc77a", zasieg=3.5, moc=0.6)
    # studio: dwie emaliowane lampy (białe klosze)
    for (x, y) in ((-1.5, -1.0), (-1.5, -4.0)):
        lampa_przemyslowa(lmp, x, y, H, 2.9, klosz="bialy")
        swiatlo((x, y, 2.8), kolor="#ffd59a", zasieg=6, moc=1.2)
    # zaplecze: zielona emalia
    lampa_przemyslowa(lmp, 2.7, -1.0, H, 2.5, klosz="zielen_okienna")
    swiatlo((2.7, -1.0, 2.4), kolor="#ffcf8a", zasieg=5, moc=1.0)
    # ciemnia: czerwone światło
    swiatlo((2.7, YB + 0.4, 2.3), kolor="#ff2a1a", zasieg=4, moc=1.0)

    # ------------------------------------------------------------------ PUNKTY
    punkt("spawn_fotograf", (0.0, 4.4, 0.0), 180)
    punkt("fotograf_drzwi_wyjscie", (0.0, 5.0, 0.0), 0)
    punkt("fotograf_wlasciciel", (-0.6, 2.0, 0.0), 0)                # za ladą, patrzy na klienta (+Y)
    punkt("fotograf_gestapo", (2.9, -0.4, 0.0), 0)                   # zaplecze, twarzą do drzwi sklepu
    punkt("fotograf_ciemnia", (2.7, -4.8, 0.0), 180)                 # przed zlewem z kuwetami
    punkt("fotograf_ciemnia_drzwi", (2.7, YC, 0.0), 180)
    punkt("fotograf_zaplecze_drzwi", (2.7, YS, 0.0), 180)
    punkt("fotograf_studio_aparat", (-1.5, -1.2, 0.0), 180)          # prefab aparatu na statywie, obiektyw na podest
    punkt("fotograf_studio_podest", (-1.5, -4.2, 0.28), 0)           # osoba pozuje, patrzy na aparat
    punkt("fotograf_studio_lampa_1", (-3.2, -2.6, 0.0), 135)
    punkt("fotograf_studio_lampa_2", (0.6, -2.4, 0.0), 225)
    punkt("fotograf_zaplecze_biurko", (3.3, -2.6, 0.0), 90)          # stół do retuszu (prefab) pod ścianą wsch.
    punkt("fotograf_ciemnia_powiekszalnik", (3.45, -4.4, 0.85), 270)

    siatki = [podl, sc, okl, suf, lad, gab, tlo, pod, zlw, reg, pud, kot, okn, det, lmp, ram]
    return [s.obiekt() for s in siatki if not s.pusta()]


if __name__ == "__main__":
    wyczysc_scene()
    obs = zbuduj()
    policz_trojkaty(obs)
    eksport_fbx(obs, os.path.join(UNITY_MODELE, "Wnetrze_Fotograf.fbx"))
    zapisz_meta(os.path.join(DANE, "meta_wnetrze_fotograf.json"))
    swiatla_podgladu()
    ukryj_sufity(obs, True, ("Sufit",))
    render_podglad(os.path.join(PODGLADY, "wnetrze_fotograf_1.png"), cel=(0.0, 0.0, 0.5), odl=22, azymut=200, wysokosc_kat=58)
    ukryj_sufity(obs, False, ("Sufit",))
    oczy(os.path.join(PODGLADY, "wnetrze_fotograf_2.png"), (2.6, 5.0, 1.65), (-2.0, 1.6, 1.4), ogniskowa=16, noc=True)
    oczy(os.path.join(PODGLADY, "wnetrze_fotograf_3.png"), (-1.5, 0.8, 1.65), (-1.5, -6.0, 1.2), ogniskowa=16, noc=True)
    oczy(os.path.join(PODGLADY, "wnetrze_fotograf_4.png"), (2.7, -3.0, 1.65), (2.7, -6.0, 1.0), ogniskowa=15, noc=True)
