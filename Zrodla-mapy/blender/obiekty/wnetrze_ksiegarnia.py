# -*- coding: utf-8 -*-
"""
Wnętrze: polska księgarnia na Rynku w Gliwicach, 1939.
Sklep x -4..4, y -2.6..5 (witryna i drzwi od frontu +Y), zaplecze za kotarą y -5..-2.6.
Wysokość 3,6 m. Regały wbudowane pod ścianami (książki bryłą), lada wzdłuż ściany wsch.
Meble ruchome (stół z nowościami, kasa, drabina, krzesło) = prefaby zespołu -> tylko punkty.
"""
import sys, os, random
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *
from _wnetrza_wspolne import *

H = 3.6
YF = 5.0      # ściana frontowa (witryna)
YZ = -2.6     # ściana zaplecza
YT = -5.0     # tylna ściana
X0, X1 = -4.0, 4.0
RH = 2.75     # wysokość regałów


def zbuduj():
    rng = random.Random(1939)
    P = Plan()
    podl = Siatka("Ksiegarnia_Podloga")
    sc = Siatka("Ksiegarnia_Sciany")
    tap = Siatka("Ksiegarnia_Tapeta")
    boa = Siatka("Ksiegarnia_Boazeria")
    suf = Siatka("Ksiegarnia_Sufit_NOCOL")
    reg = Siatka("Ksiegarnia_Regaly")
    ks = Siatka("Ksiegarnia_Ksiazki_NOCOL")
    lad = Siatka("Ksiegarnia_Lada")
    wit = Siatka("Ksiegarnia_Witryna")
    kot = Siatka("Ksiegarnia_Kotara_NOCOL")
    okn = Siatka("Ksiegarnia_Okna")
    det = Siatka("Ksiegarnia_Detal_NOCOL")
    lmp = Siatka("Ksiegarnia_Lampy_NOCOL")
    ram = Siatka("Ksiegarnia_DrzwiWyjscie_Rama_NOCOL")

    # ------------------------------------------------------------------ OTWORY
    P.drzwi("x", YF, 0.0)                                   # wyjście na Rynek
    P.okno("x", YF, 0.0, 2.45, 1.1, 0.55)                   # naświetle nad drzwiami
    for x in (-2.25, 2.25):
        P.okno("x", YF, x, 0.6, 2.5, 2.3)                   # witryny
    P.otwor("x", YZ, 2.65, 1.6, 0.0, 2.35, "przejscie")     # przejście na zaplecze (kotara)
    P.okno("y", X0, -3.8, 1.5, 0.9, 1.0)                    # okienko zaplecza (podwórze)

    # ------------------------------------------------------------------ PODŁOGI
    plyta(podl, X0 - 0.1, X1 + 0.1, YZ, YF + 0.1, -0.004, 0.3, "parkiet")
    deski(podl, X0 + 0.1, X1 - 0.1, YZ + 0.1, YF - 0.1, 0.0, 0.12, "parkiet", "modrzew", wzdluz="y")
    plyta(podl, X0 - 0.1, X1 + 0.1, YT - 0.1, YZ, -0.004, 0.3, "deska_szara")
    deski(podl, X0 + 0.1, X1 - 0.1, YT + 0.1, YZ - 0.1, 0.0, 0.2, "deska_szara", "drewno", wzdluz="x")
    podl.box_od_do(-0.55, YF - 0.1, -0.01, 0.55, YF + 0.1, 0.015, "kamien_ciemny")       # próg
    podl.box_od_do(1.85, YZ - 0.1, -0.01, 3.45, YZ + 0.1, 0.012, "drewno_ciemne")
    det.box_od_do(-0.7, YF - 1.1, 0.0, 0.7, YF - 0.15, 0.012, "dywan_czerwony")          # wycieraczka

    # ------------------------------------------------------------------ ŚCIANY
    W = "tynk_kremowy"
    P.sciana(sc, "x", YF, X0, X1, 0, H, W)
    P.sciana(sc, "x", YT, X0, X1, 0, H, W)
    P.sciana(sc, "x", YZ, X0, X1, 0, H, W)
    P.sciana(sc, "y", X0, YT, YF, 0, H, W)
    P.sciana(sc, "y", X1, YT, YF, 0, H, W)
    # sklep: tapeta zielona w paski nad regałami i na froncie, boazeria pod witrynami
    P.okladzina(tap, X0, X1, YZ, YF, 0.9, H, "tapeta_zielona", grub=0.02)
    P.okladzina(boa, X0, X1, YZ, YF, 0.0, 0.9, "boazeria", grub=0.03, sciany="N")
    P.listwa(det, X0, X1, YZ, YF, 0.9, 0.05, 0.05, "drewno_ciemne", sciany="N")
    for k in range(int((X1 - X0) / 0.3)):                     # pasy tapety (ciemniejsze prążki)
        x = X0 + 0.25 + k * 0.3
        if x > X1 - 0.2:
            break
        det.box_od_do(x, YF - 0.125, 2.95, x + 0.07, YF - 0.115, H - 0.2, "trawa_ciemna")
    for (os_, c, a, b, sg) in (("y", X0, YZ + 0.15, YF - 0.15, +1), ("y", X1, YZ + 0.15, YF - 0.15, -1), ("x", YZ, X0 + 0.15, X1 - 0.15, +1)):
        k = 0
        s = a + 0.2
        while s < b - 0.1:
            if os_ == "x" and 1.75 < s < 3.6:
                s += 0.3
                continue
            P._box(det, os_, c + sg * (GR / 2 + 0.025), s, s + 0.07, RH, H - 0.2, 0.01, "trawa_ciemna")
            s += 0.3
    P.listwa(det, X0, X1, YZ, YF, H - 0.2, 0.2, 0.08, "drewno_ciemne")                     # gzyms
    P.listwa(det, X0, X1, YZ, YF, H - 0.26, 0.04, 0.05, "zloto")
    # zaplecze: lamperia olejna
    P.okladzina(boa, X0, X1, YT, YZ, 0.0, 1.4, "zielen_okienna", grub=0.02)
    P.listwa(det, X0, X1, YT, YZ, 1.4, 0.03, 0.03, "czarny")

    # ------------------------------------------------------------------ REGAŁY WBUDOWANE
    KS = ("ksiazka_1",) * 3 + ("ksiazka_2",) * 3 + ("ksiazka_3",) * 3 + ("ksiazka_5",) * 4 + ("ksiazka_4", "drewno_ciemne")
    kw = dict(kolory=KS, szer=(0.05, 0.19), jasne_p=0.05)
    regal_wbudowany(P, reg, ks, "y", X0, YZ + 0.15, YF - 0.95, +1, 0.0, RH, 6, rng, **kw)    # zachodni
    regal_wbudowany(P, reg, ks, "y", X1, YZ + 0.15, YF - 0.95, -1, 0.0, RH, 6, rng, **kw)    # wschodni (za ladą)
    regal_wbudowany(P, reg, ks, "x", YZ, X0 + 0.52, 1.8, +1, 0.0, RH, 6, rng, **kw)          # od zaplecza
    # mosiężna szyna drabiny wzdłuż regału zachodniego
    xs = X0 + GR / 2 + 0.36 + 0.05
    det.belka((xs, YZ + 0.2, RH - 0.25), (xs, YF - 1.0, RH - 0.25), 0.035, "zloto")
    for y in (YZ + 0.3, 0.6, YF - 1.1):
        det.box_od_do(xs - 0.06, y - 0.02, RH - 0.27, xs, y + 0.02, RH - 0.23, "zloto")
    # półki-ekspozytory pod witrynami (książki frontem do szyby)
    for x0_, x1_ in ((-3.5, -1.0), (1.0, 3.5)):
        wit.box_od_do(x0_, YF - 0.95, 0.0, x1_, YF - 0.12, 0.45, "boazeria", bez_dolu=False)
        wit.box_od_do(x0_ - 0.02, YF - 0.97, 0.45, x1_ + 0.02, YF - 0.1, 0.5, "drewno_ciemne", bez_dolu=False)
        wit.box_od_do(x0_ + 0.05, YF - 0.55, 0.5, x1_ - 0.05, YF - 0.12, 0.72, "drewno_ciemne", bez_dolu=False)
        det.box_od_do(x0_, YF - 0.97, 0.5, x1_, YF - 0.55, 0.505, "tkanina_bordo")
        det.box_od_do(x0_ + 0.05, YF - 0.55, 0.72, x1_ - 0.05, YF - 0.12, 0.725, "tkanina_bordo")
        for k in range(5):                                       # książki wystawione (oparte, grzbietem do sklepu)
            x = x0_ + 0.2 + k * (x1_ - x0_ - 0.4) / 4
            z = 0.505 if k % 2 else 0.725
            y0 = YF - 0.9 if k % 2 else YF - 0.5
            hb = rng.uniform(0.24, 0.32)
            det.box_od_do(x - 0.12, y0, z, x + 0.12, y0 + 0.05, z + hb, rng.choice(KSIAZKI))
            det.box_od_do(x - 0.02, y0 + 0.05, z, x + 0.02, y0 + 0.16, z + hb * 0.6, "drewno_ciemne")   # podpórka
        for k in range(2):                                       # stosiki
            x = x0_ + 0.65 + k * 1.2
            det.box_od_do(x - 0.15, YF - 0.95, 0.505, x + 0.15, YF - 0.72, 0.505 + 0.06, rng.choice(KSIAZKI))
            det.box_od_do(x - 0.13, YF - 0.94, 0.565, x + 0.13, YF - 0.73, 0.62, rng.choice(KSIAZKI))

    # ------------------------------------------------------------------ LADA (wbudowana, wzdłuż ściany wsch.)
    LX0, LX1, LY0, LY1, LZ = 1.5, 2.15, -1.3, 2.9, 0.95
    lad.box_od_do(LX0 + 0.05, LY0, 0.0, LX1, LY1, 0.1, "drewno_ciemne", bez_dolu=False)        # cokół
    lad.box_od_do(LX0, LY0, 0.1, LX1, LY1, LZ - 0.05, "boazeria", bez_dolu=False)               # korpus
    lad.box_od_do(LX0 - 0.06, LY0 - 0.04, LZ - 0.05, LX1 + 0.04, LY1 + 0.04, LZ, "drewno_ciemne", bez_dolu=False)   # blat
    n = 5
    dy = (LY1 - LY0) / n
    for i in range(n):                                          # płyciny frontu (od strony klienta, -X)
        y0 = LY0 + i * dy
        lad.box_od_do(LX0 - 0.02, y0 + 0.1, 0.22, LX0, y0 + dy - 0.1, LZ - 0.17, "drewno_ciemne")
        lad.box_od_do(LX0 - 0.035, y0 + 0.16, 0.28, LX0 - 0.02, y0 + dy - 0.16, LZ - 0.23, "boazeria")
    for y in [LY0 + i * dy for i in range(n + 1)]:              # pilastry
        lad.box_od_do(LX0 - 0.04, y - 0.035, 0.1, LX0, y + 0.035, LZ - 0.05, "drewno_ciemne")
    det.box_od_do(LX0 - 0.075, LY0 - 0.05, LZ - 0.05, LX0 - 0.055, LY1 + 0.05, LZ - 0.02, "zloto")   # mosiężna listwa blatu
    det.box_od_do(LX0 - 0.06, LY0 - 0.05, 0.1, LX0 - 0.04, LY1 + 0.05, 0.13, "zloto")               # listwa przy cokole
    # półka pod blatem od strony sprzedawcy + papier do pakowania (rolka)
    lad.box_od_do(LX1, LY0 + 0.1, 0.45, LX1 + 0.02, LY1 - 0.1, 0.48, "drewno")
    det.belka((LX1 + 0.12, 2.2, 0.82), (LX1 + 0.12, 2.75, 0.82), 0.14, "tkanina_bez")
    det.box_od_do(LX1 + 0.02, 2.15, 0.7, LX1 + 0.2, 2.2, 0.9, "metal_ciemny")
    det.box_od_do(LX1 + 0.02, 2.75, 0.7, LX1 + 0.2, 2.8, 0.9, "metal_ciemny")

    # ------------------------------------------------------------------ KOTARA (przejście na zaplecze)
    yk = YZ + GR / 2 + 0.08
    det.box_od_do(1.75, yk - 0.02, 2.4, 3.55, yk + 0.02, 2.44, "zloto")                          # drążek
    for x in (1.75, 3.55):
        det.box((x, yk, 2.42), (0.07, 0.07, 0.07), "zloto")
    for (a, b) in ((1.85, 2.2), (3.1, 3.45)):                                                    # fałdy zsunięte na boki
        k = 0
        x = a
        while x < b - 0.01:
            kot.box_od_do(x, yk - 0.06 - 0.03 * (k % 2), 0.02, x + 0.09, yk + 0.06, 2.4, "tkanina_bordo")
            x += 0.085
            k += 1
    det.box_od_do(1.85, yk - 0.1, 1.05, 2.22, yk + 0.08, 1.1, "zloto")                          # opaska kotary
    det.box_od_do(3.08, yk - 0.1, 1.05, 3.45, yk + 0.08, 1.1, "zloto")
    P.rama_drzwi(det, "x", YZ, 2.65, szer=1.6, wys=2.35, mat="drewno_ciemne", strony=(-1,))

    # ------------------------------------------------------------------ OKNA I WYJŚCIE
    P.wypelnij_okna(okn, det, rama="rama_ciemna", parapet="drewno_ciemne", szprosy=(0, 0))
    for x in (-2.25, 2.25):                                      # szprosy witryny: pas górny (naświetle)
        P._box(det, "x", YF, x - 1.25, x + 1.25, 2.3, 2.36, 0.1, "rama_ciemna")
        for k in (-1, 1):
            P._box(det, "x", YF, x + k * 0.42 - 0.025, x + k * 0.42 + 0.025, 2.36, 2.9, 0.08, "rama_ciemna")
        # lambrekin nad witryną
        det.box_od_do(x - 1.35, YF - 0.2, 2.7, x + 1.35, YF - 0.13, 2.98, "tkanina_bordo")
        det.box_od_do(x - 1.35, YF - 0.21, 2.69, x + 1.35, YF - 0.13, 2.71, "zloto")
    P.rama_drzwi(ram, "x", YF, 0.0, mat="drzwi_ciemne", strony=(-1,), szer_opaski=0.14, wyst=0.05)
    det.box_od_do(-0.6, YF - 0.2, 2.9, 0.6, YF - 0.13, 3.02, "drzwi_ciemne")
    # dzwonek nad drzwiami (mosiądz)
    det.box_od_do(0.4, YF - 0.25, 2.7, 0.46, YF - 0.13, 2.74, "zloto")
    det.walec((0.43, YF - 0.3, 2.58), 0.04, 0.1, "zloto", n=6, r_gora=0.015, dol=True)

    # ------------------------------------------------------------------ ZAPLECZE
    regal_wbudowany(P, reg, ks, "x", YT, X0 + 0.15, 1.6, +1, 0.0, 2.4, 4, rng, gleb=0.5, segment=1.15,
                    mat_kor="deska_szara", mat_polka="deska_szara", mat_tyl="drewno",
                    kolory=("tkanina_bez", "tkanina_bez", "tynk_kremowy", "ksiazka_5", "ksiazka_3"), jasne=(), gesto=0.8)
    regal_wbudowany(P, reg, ks, "y", X1, YT + 0.15, YZ - 0.15, -1, 0.0, 2.4, 4, rng, gleb=0.5, segment=1.1,
                    mat_kor="deska_szara", mat_polka="deska_szara", mat_tyl="drewno",
                    kolory=("tkanina_bez", "tynk_kremowy", "ksiazka_1", "ksiazka_2"), jasne=(), gesto=0.75)
    # paczki z księgami (owinięte papierem, sznurek) na podłodze przy regale
    for (x, y, w, d_, h) in ((2.6, -4.55, 0.5, 0.35, 0.3), (2.6, -4.55, 0.45, 0.32, 0.22), (3.2, -3.4, 0.4, 0.5, 0.28)):
        zz = 0.0 if h != 0.22 else 0.3
        det.box_od_do(x - w / 2, y - d_ / 2, zz, x + w / 2, y + d_ / 2, zz + h, "tkanina_bez")
        det.box_od_do(x - 0.01, y - d_ / 2 - 0.005, zz, x + 0.01, y + d_ / 2 + 0.005, zz + h + 0.005, "drewno")
    det.box_od_do(X0 + 0.13, -4.25, 2.05, X0 + 0.15, -3.35, 2.5, "tkanina_bez")                 # roleta

    # ------------------------------------------------------------------ SUFIT I LAMPY
    sufit(suf, X0 - 0.1, X1 + 0.1, YT - 0.1, YF + 0.1, H)
    suf.box((-0.4, 1.2, H - 0.02), (1.4, 1.4, 0.04), "sufit")                                   # płycina sufitu
    suf.box((-0.4, 1.2, H - 0.05), (0.5, 0.5, 0.03), "tynk_bialy")                              # rozeta
    for (x, y) in ((-0.4, 3.0), (-0.4, -0.6)):                                                  # mleczne kule na mosiądzu
        lmp.box((x, y, H - 0.4), (0.03, 0.03, 0.8), "zloto")
        lmp.walec((x, y, H - 0.05), 0.12, 0.05, "zloto", n=8, dol=True)
        lmp.walec((x, y, H - 1.25), 0.15, 0.08, "zloto", n=8, r_gora=0.2, dol=True)
        lmp.walec((x, y, H - 1.17), 0.24, 0.32, "lampa", n=8, r_gora=0.12, dol=False)
        swiatlo((x, y, H - 1.25), kolor="#ffcf8a", zasieg=7, moc=1.4)
    lampa_przemyslowa(lmp, 2.6, 0.8, H, 2.35, klosz="zielen_okienna")                          # nad ladą
    swiatlo((2.6, 0.8, 2.25), kolor="#ffd59a", zasieg=4, moc=1.0)
    for y in (-1.4, 1.3):                                                                       # kinkiety nad regałem zach.
        kinkiet(det, X0 + GR / 2 + 0.03, y, RH + 0.35, 1, 0)
        swiatlo((X0 + 0.5, y, RH + 0.4), kolor="#ffc77a", zasieg=3.5, moc=0.6)
    zarowka(lmp, 0.0, -3.8, H, 2.5)                                                             # zaplecze: goła żarówka
    swiatlo((0.0, -3.8, 2.4), kolor="#ffb866", zasieg=5, moc=0.9)

    # ------------------------------------------------------------------ PUNKTY
    punkt("spawn_ksiegarnia", (0.0, 3.4, 0.0), 180)
    punkt("ksiegarnia_drzwi_wyjscie", (0.0, 4.0, 0.0), 0)
    punkt("ksiegarnia_zofia", (2.9, 0.8, 0.0), 270)                # za ladą, patrzy na sklep (-X)
    punkt("ksiegarnia_kasa", (1.85, 2.3, LZ), 270)                 # prefab kasy na ladzie
    punkt("ksiegarnia_mebel_stol", (-1.3, 0.8, 0.0), 0)            # stół z nowościami (prefab)
    punkt("ksiegarnia_mebel_drabina", (-3.2, 0.2, 0.0), 270)       # drabina przy szynie regału zach.
    punkt("ksiegarnia_zaplecze", (0.0, -3.8, 0.0), 0)              # środek zaplecza (stół pakowy = prefab)
    punkt("ksiegarnia_zaplecze_wejscie", (2.65, -2.0, 0.0), 180)   # przed kotarą od strony sklepu

    siatki = [podl, sc, tap, boa, suf, reg, ks, lad, wit, kot, okn, det, lmp, ram]
    return [s.obiekt() for s in siatki if not s.pusta()]


if __name__ == "__main__":
    wyczysc_scene()
    obs = zbuduj()
    policz_trojkaty(obs)
    eksport_fbx(obs, os.path.join(UNITY_MODELE, "Wnetrze_Ksiegarnia.fbx"))
    zapisz_meta(os.path.join(DANE, "meta_wnetrze_ksiegarnia.json"))
    swiatla_podgladu()
    ukryj_sufity(obs, True, ("Sufit",))
    render_podglad(os.path.join(PODGLADY, "wnetrze_ksiegarnia_1.png"), cel=(0.0, 0.0, 0.5), odl=17, azymut=200, wysokosc_kat=55)
    ukryj_sufity(obs, False, ("Sufit",))
    oczy(os.path.join(PODGLADY, "wnetrze_ksiegarnia_2.png"), (-0.6, 4.1, 1.65), (1.0, -2.0, 1.2), ogniskowa=16, noc=True)
    oczy(os.path.join(PODGLADY, "wnetrze_ksiegarnia_3.png"), (2.9, -1.8, 1.65), (-2.0, 4.0, 1.3), ogniskowa=16, noc=True)
