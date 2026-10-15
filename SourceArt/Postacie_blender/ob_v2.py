# Wersje 2 (lepsze): Vis z ruchomymi czesciami, Skrytka, Amunicja, Pulpit, Worki, Ciezarowka, Lokomotywa, Wagon.
# Wymaga ob_lib.py + ob_p2.py (rod, kolo, blotnik, flat_sheet). Przod = -Y, pivot na dole.
import numpy as np
from mathutils import Euler


def M(loc=(0, 0, 0), rot=(0, 0, 0)):
    return Matrix.Translation(Vector(loc)) @ Euler([math.radians(a) for a in rot], "XYZ").to_matrix().to_4x4()


def grupa(objs, name, mat):
    """Laczy czesci zbudowane w ukladzie lokalnym i przenosi je macierza `mat` (obrot + przesuniecie)."""
    o = join(objs, name)
    o.data.transform(mat)
    return o


def naboj(C, mat, skala=1.0):
    """Nabój 9 mm (luska + pocisk), os wzdluz +Z, spod w (0,0,0)."""
    s = skala
    P = [cyl("luska", 0.0046 * s, 0.019 * s, (0, 0, 0.0095 * s), "mosiadz", C, seg=10),
         cyl("rant", 0.0049 * s, 0.0012 * s, (0, 0, 0.0006 * s), "mosiadz", C, seg=10),
         cyl("pocisk", 0.0045 * s, 0.006 * s, (0, 0, 0.022 * s), "miedz", C, seg=10, r2=0.0032 * s),
         sphere("czubek", 0.0032 * s, (0, 0, 0.025 * s), "miedz", C, seg=10, rings=5)]
    return grupa(P, "naboj", mat)


# ====================================================================== VIS
def build_vis():
    C = new_collection("OB_Pistolet_Vis")
    os_ = Vector((0, -math.sin(math.radians(16)), math.cos(math.radians(16))))   # os chwytu (w gore)
    gc = Vector((0, 0.012, -0.012))
    R = [box("szkielet", (0.026, 0.16, 0.022), (0, -0.07, 0.034), "oksyda", C, bevel=0.002, seg=2),
         box("chwyt", (0.028, 0.05, 0.115), gc, "oksyda", C, rot=(16, 0, 0), bevel=0.003, seg=2)]
    for sx in (-1, 1):
        R.append(box("okladzina", (0.004, 0.042, 0.09), (sx * 0.016, gc.y, gc.z), "bakelit", C, rot=(16, 0, 0), bevel=0.0015, seg=2))
        for k in (-0.028, 0.028):
            p = gc + os_ * k
            R.append(cyl("srubka", 0.0035, 0.004, (sx * 0.0185, p.y, p.z), "stal", C, rot=(0, 90, 0), seg=8))
    R.append(box("kablak_d", (0.008, 0.05, 0.006), (0, -0.05, 0.003), "oksyda", C, bevel=0.0015))
    R.append(box("kablak_p", (0.008, 0.006, 0.03), (0, -0.073, 0.017), "oksyda", C, rot=(-10, 0, 0), bevel=0.0015))
    R.append(box("zwalniacz", (0.004, 0.022, 0.008), (0.0145, -0.03, 0.046), "stal", C, bevel=0.001))
    R.append(box("bezpiecznik", (0.004, 0.012, 0.01), (0.0145, 0.02, 0.03), "stal", C, bevel=0.001))
    join(R, "Vis")

    Z = [box("zamek", (0.028, 0.205, 0.032), (0, -0.065, 0.06), "oksyda", C, bevel=0.003, seg=2)]
    for i in range(7):
        for sx in (-1, 1):
            Z.append(box("nac", (0.002, 0.0025, 0.024), (sx * 0.0141, 0.012 + i * 0.0045, 0.06), "grafit", C))
    Z.append(cyl("wylot", 0.0085, 0.006, (0, -0.1685, 0.065), "stal", C, rot=(90, 0, 0), seg=12))
    Z.append(cyl("otwor", 0.0045, 0.004, (0, -0.172, 0.065), "guma", C, rot=(90, 0, 0), seg=10))
    Z.append(box("muszka", (0.003, 0.006, 0.006), (0, -0.16, 0.078), "oksyda", C))
    Z.append(box("szczerbinka", (0.016, 0.006, 0.006), (0, 0.03, 0.078), "oksyda", C))
    Z.append(box("okno_wyrzutowe", (0.002, 0.028, 0.012), (-0.0141, -0.025, 0.066), "guma", C))
    join(Z, "Zamek")

    piv = (0, 0.037, 0.062)
    K = [box("kurek", (0.008, 0.009, 0.022), (0, 0.037 + 0.005, 0.062 + 0.01), "stal", C, rot=(-28, 0, 0), bevel=0.001),
         box("kurek_g", (0.009, 0.006, 0.008), (0, 0.037 + 0.011, 0.062 + 0.02), "stal", C, rot=(-28, 0, 0))]
    join(K, "Kurek", origin=piv)
    join([box("spust", (0.006, 0.006, 0.02), (0, -0.04, 0.016), "stal", C, rot=(15, 0, 0), bevel=0.001)], "Spust")

    mc = gc + os_ * -0.005 + Vector((0, 0.0015, 0))
    Mg = [box("mag", (0.022, 0.032, 0.1), mc, "oksyda", C, rot=(16, 0, 0)),
          box("stopka", (0.03, 0.046, 0.008), gc + os_ * -0.0585, "oksyda", C, rot=(16, 0, 0), bevel=0.002)]
    top = mc + os_ * 0.05
    Mg.append(naboj(C, M((0, top.y + 0.012, top.z + 0.001), (90, 0, 0))))
    join(Mg, "Magazynek")

    empty("Lufa", (0, -0.172, 0.065), C, size=0.03)
    empty("Uchwyt", tuple(gc), C, size=0.03)
    empty("Wyrzut", (-0.016, -0.025, 0.066), C, size=0.02)
    report("OB_Pistolet_Vis")


# ====================================================================== SKRYTKA
def build_skrytka():
    C = new_collection("OB_Skrytka")
    w, d, h, t = 0.64, 0.42, 0.32, 0.022
    P = [box("dno", (w, d, 0.02), (0, 0, 0.01), "drewno", C)]
    bh = (h - 0.02) / 3
    for k in range(3):
        z = 0.02 + bh * k + bh / 2
        for sy in (-1, 1):
            P.append(box("deska_y", (w, t, bh - 0.004), (0, sy * (d / 2 - t / 2), z), "drewno", C, bevel=0.003, seg=2))
        for sx in (-1, 1):
            P.append(box("deska_x", (t, d - 2 * t, bh - 0.004), (sx * (w / 2 - t / 2), 0, z), "drewno", C, bevel=0.003, seg=2))
    # okucia narozne (katowniki) + nity
    for sx in (-1, 1):
        for sy in (-1, 1):
            P.append(box("kat_y", (0.06, 0.003, h - 0.01), (sx * (w / 2 - 0.03), sy * (d / 2 + 0.0015), h / 2), "zeliwo", C))
            P.append(box("kat_x", (0.003, 0.06, h - 0.01), (sx * (w / 2 + 0.0015), sy * (d / 2 - 0.03), h / 2), "zeliwo", C))
            for z in (0.05, h / 2, h - 0.05):
                P.append(sphere("nit", 0.0045, (sx * (w / 2 - 0.03), sy * (d / 2 + 0.003), z), "stal", C, seg=6, rings=3))
    # uchwyty ze sznura po bokach
    for sx in (-1, 1):
        P.append(torus("sznur", 0.055, 0.008, (sx * (w / 2 + 0.012), 0, h * 0.72), "worek", C, rot=(-90, 0, 90), seg=12, rseg=6, arc=180))
        for sy in (-1, 1):
            P.append(sphere("wezel", 0.013, (sx * (w / 2 + 0.008), sy * 0.055, h * 0.72), "worek", C, seg=8, rings=5))
    # zawiasy z tylu
    for sx in (-1, 1):
        P.append(cyl("zawias", 0.008, 0.07, (sx * 0.18, d / 2 + 0.008, h), "zeliwo", C, rot=(0, 90, 0), seg=8))
        P.append(box("zawias_t", (0.05, 0.003, 0.08), (sx * 0.18, d / 2 + 0.003, h - 0.04), "zeliwo", C))
    # skobel + otwarta klodka
    P.append(torus("skobel", 0.012, 0.003, (0, -d / 2 - 0.006, h - 0.05), "stal", C, rot=(90, 0, 0), seg=10, rseg=4))
    P.append(box("klodka", (0.035, 0.014, 0.03), (0, -d / 2 - 0.012, h - 0.09), "mosiadz", C, bevel=0.003, seg=2))
    P.append(torus("palak", 0.011, 0.003, (0.006, -d / 2 - 0.012, h - 0.073), "stal", C, rot=(90, 0, 0), seg=10, rseg=4, arc=200))
    P.append(box("tabliczka", (0.2, 0.003, 0.07), (0, -d / 2 - 0.0015, h * 0.4), "papier_stary", C))
    P.append(box("wyscielka", (w - 0.06, d - 0.06, 0.02), (0, 0, 0.03), "worek", C, bevel=0.008))
    join(P, "Skrzynka")

    W = []
    for k in range(3):
        W.append(box("deska_w", (w + 0.02, (d + 0.02) / 3 - 0.004, 0.022), (0, -d / 2 - 0.01 + (d + 0.02) / 6 * (2 * k + 1), h + 0.011), "drewno", C, bevel=0.003, seg=2))
    for sx in (-1, 1):
        W.append(box("listwa", (0.05, d + 0.02, 0.012), (sx * (w / 2 - 0.08), 0, h + 0.028), "drewno_ciemne", C, bevel=0.002))
        W.append(box("okucie_w", (0.04, d + 0.03, 0.004), (sx * 0.18, 0, h + 0.036), "zeliwo", C))
    W.append(box("zaczep", (0.04, 0.004, 0.07), (0, -d / 2 - 0.012, h - 0.01), "zeliwo", C))
    join(W, "Wieko", origin=(0, d / 2 + 0.008, h))
    empty("Wnetrze", (0, 0, 0.04), C)
    report("OB_Skrytka")


# ====================================================================== AMUNICJA
def build_amunicja():
    C = new_collection("OB_Amunicja")
    bw, bd, bh, t = 0.11, 0.075, 0.03, 0.0015
    cx = -0.035
    P = [box("dno", (bw, bd, 0.002), (cx, 0, 0.001), "zolty_karton", C),
         box("wklad", (bw - 0.004, bd - 0.004, 0.006), (cx, 0, 0.005), "zolty_karton", C)]
    for sy in (-1, 1):
        P.append(box("sc_y", (bw, t, bh), (cx, sy * (bd / 2), bh / 2), "zolty_karton", C))
    for sx in (-1, 1):
        P.append(box("sc_x", (t, bd, bh), (cx + sx * bw / 2, 0, bh / 2), "zolty_karton", C))
    P.append(box("etykieta", (0.08, 0.0004, 0.018), (cx, -bd / 2 - 0.001, 0.016), "papier", C))
    P.append(grupa([box("klapa", (bw, bd, t), (0, -bd / 2, 0), "zolty_karton", C)], "klapa_g",
                   M((cx, bd / 2, bh), (-110, 0, 0))))
    for i in range(5):
        for j in range(4):
            if (i, j) in ((4, 0), (4, 1)):
                continue          # brakuje dwoch nabojow
            P.append(naboj(C, M((cx + (i - 2) * 0.0195, (j - 1.5) * 0.0168, 0.008))))
    # magazynek lezacy obok, z nabojem na gorze
    P.append(box("mag", (0.1, 0.032, 0.022), (0.075, 0.0, 0.011), "oksyda", C, bevel=0.002, seg=2))
    P.append(box("mag_stopka", (0.008, 0.046, 0.03), (0.128, 0.0, 0.015), "oksyda", C, bevel=0.002))
    P.append(naboj(C, M((0.03, 0.012, 0.016), (90, 0, 0))))
    # luzne naboje
    for k, (x, y, a) in enumerate(((0.02, -0.07, 20), (0.05, -0.085, 75), (-0.06, -0.08, 140))):
        P.append(naboj(C, M((x, y, 0.0046), (90, 0, a))))
    join(P, "Amunicja")
    report("OB_Amunicja")


# ====================================================================== WORKI
def worek(C, loc, rotz, L=0.5, Wd=0.27, H=0.13, rng=None):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=2.0)
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=2, use_grid_fill=True)
    for v in bm.verts:
        x, y, z = v.co
        end = abs(x) ** 3
        fx = x * (1 - 0.05 * z * z)
        fy = y * (1 - 0.45 * end) * (1 - 0.12 * z * z)
        fz = z * (1 - 0.55 * end) * (1 - 0.1 * y * y)
        if z < 0:
            fz *= 0.75                     # spod splaszczony od ciezaru
        v.co = Vector((fx * L / 2, fy * Wd / 2, fz * H / 2)) + Vector(rng.normal(0, 0.004, 3))
    bm.normal_update()
    _uv_box(bm, "worek")
    o = _finish("worek", bm, C, atlas_mat())
    for p in o.data.polygons:
        p.use_smooth = True
    ear = cyl("ucho", 0.022, 0.05, (L / 2 + 0.012, 0, 0), "worek", C, rot=(0, 90, 0), seg=8, r2=0.008)
    w = join([o, ear], "w")
    w.data.transform(M(loc, (rng.normal(0, 2), rng.normal(0, 2), rotz)))
    return w


def build_worki():
    C = new_collection("OB_Worki")
    rng = np.random.default_rng(5)
    P = []
    for r in range(7):
        z = 0.06 + r * 0.128
        if r % 2 == 0:
            for y in (-0.14, 0.14):
                for x in (-0.48, 0.0, 0.48):
                    P.append(worek(C, (x + rng.normal(0, 0.01), y, z), 180 * (rng.random() > 0.5) + rng.normal(0, 3), rng=rng))
        else:
            for y in (-0.14, 0.14):
                for x in (-0.24, 0.24):
                    P.append(worek(C, (x + rng.normal(0, 0.01), y, z), 180 * (rng.random() > 0.5) + rng.normal(0, 3), rng=rng))
            for x in (-0.62, 0.62):
                P.append(worek(C, (x, 0, z), 90 + rng.normal(0, 3), rng=rng))
    join(P, "Worki")
    report("OB_Worki")


# ====================================================================== PULPIT
def build_pulpit():
    C = new_collection("OB_Pulpit")
    W, D = 1.7, 0.8
    P = [box("szafka", (W, D, 0.72), (0, 0, 0.36), "szary_wojsk", C, bevel=0.012, seg=2),
         box("cokol", (W - 0.05, D - 0.05, 0.08), (0, -0.01, 0.04), "lakier_czarny", C),
         box("blat", (W + 0.04, D + 0.04, 0.035), (0, 0, 0.7375), "grafit", C, bevel=0.006, seg=2),
         box("blat_wklad", (W - 0.1, 0.36, 0.004), (0, -0.2, 0.757), "skora", C)]
    for i, x in enumerate((-0.55, 0.0, 0.55)):
        P.append(box("drzwiczki", (0.5, 0.012, 0.5), (x, -D / 2 - 0.004, 0.4), "szary_wojsk", C, bevel=0.006, seg=2))
        P.append(box("uchwyt", (0.012, 0.02, 0.1), (x + 0.2, -D / 2 - 0.016, 0.45), "chrom", C, bevel=0.003))
        for k in range(5):
            P.append(box("went", (0.3, 0.006, 0.012), (x, -D / 2 - 0.011, 0.25 + k * 0.025), "grafit", C))
    # skosny panel (budowany pionowo, potem pochylony o 30 st. do tylu)
    L = [box("lico", (W - 0.04, 0.03, 0.6), (0, 0, 0.3), "panel_zielony", C, bevel=0.006, seg=2),
         box("obudowa", (W, 0.28, 0.62), (0, 0.155, 0.31), "szary_wojsk", C, bevel=0.012, seg=2),
         box("rama", (W, 0.035, 0.03), (0, -0.01, 0.605), "chrom", C)]
    Lamp = []
    for i, x in enumerate((-0.6, -0.2, 0.2, 0.6)):
        L.append(cyl("bezel", 0.075, 0.03, (x, -0.025, 0.44), "chrom", C, rot=(90, 0, 0), seg=24))
        L.append(cyl("tarcza", 0.064, 0.034, (x, -0.027, 0.44), "tarcza", C, rot=(90, 0, 0), seg=24))
        L.append(box("wskazowka", (0.004, 0.004, 0.055), (x + 0.008, -0.046, 0.455), "lakier_czarny", C, rot=(0, -20 + i * 15, 0)))
        L.append(box("opis", (0.12, 0.006, 0.024), (x, -0.018, 0.345), "kremowy", C))
    for i in range(6):
        x = -0.625 + i * 0.25
        L.append(cyl("kolnierz", 0.036, 0.012, (x, -0.02, 0.24), "chrom", C, rot=(90, 0, 0), seg=16))
        L.append(cyl("galka", 0.026, 0.04, (x, -0.04, 0.24), "bakelit", C, rot=(90, 0, 0), seg=16, r2=0.022))
        L.append(box("znacznik", (0.003, 0.004, 0.02), (x, -0.061, 0.25), "kremowy", C))
    for i in range(8):
        x = -0.63 + i * 0.12
        L.append(box("plytka", (0.03, 0.006, 0.042), (x, -0.018, 0.115), "chrom", C, bevel=0.002))
        L.append(rod("dzwignia", (x, -0.02, 0.115), (x, -0.06, 0.115 + (0.02 if i % 2 else -0.02)), 0.0035, "chrom", C))
    L.append(box("gniazda", (0.62, 0.006, 0.045), (0.32, -0.018, 0.045), "mosiadz", C, bevel=0.002))
    for i in range(10):
        L.append(cyl("gniazdo", 0.007, 0.01, (0.05 + i * 0.06, -0.022, 0.045), "guma", C, rot=(90, 0, 0), seg=8))
    for i, x in enumerate((-0.4, -0.2, 0.0, 0.2, 0.4)):
        L.append(cyl("oprawka", 0.016, 0.02, (x, -0.022, 0.555), "chrom", C, rot=(90, 0, 0), seg=12))
        kol = (1.0, 0.05, 0.02) if i == 4 else (1.0, 0.6, 0.15)
        nazwa = "M_Lampka_Czerwona" if i == 4 else "M_Lampa_Radio"
        Lamp.append(sphere("lampka", 0.012, (x, -0.034, 0.555), "czerwony", C, seg=10, rings=6, mat=mat_emis(nazwa, kol, 4)))
    pan = M((0, -0.06, 0.755), (-30, 0, 0))
    P.append(grupa(L, "panel", pan))
    # sluchawki, mikrofon biurkowy, notes
    for x in (-0.6, -0.42):
        P.append(cyl("muszla", 0.04, 0.025, (x, -0.24, 0.775), "bakelit", C, seg=16))
    P.append(torus("palak", 0.09, 0.007, (-0.51, -0.24, 0.79), "skora", C, seg=16, rseg=6, arc=180))
    P.append(rod("przewod", (-0.6, -0.22, 0.77), (-0.7, 0.05, 0.85), 0.004, "kabel_czarny", C))
    P.append(cyl("mik_podst", 0.06, 0.02, (0.45, -0.2, 0.765), "chrom", C, seg=16, r2=0.05))
    P.append(rod("mik_stojak", (0.45, -0.2, 0.77), (0.45, -0.2, 0.95), 0.006, "chrom", C))
    P.append(torus("mik_rama", 0.045, 0.006, (0.45, -0.2, 0.99), "chrom", C, rot=(90, 0, 0), seg=16, rseg=5))
    P.append(cyl("mik_glowica", 0.035, 0.03, (0.45, -0.2, 0.99), "stal", C, rot=(90, 0, 0), seg=16))
    P.append(box("notes", (0.21, 0.3, 0.012), (0.05, -0.24, 0.764), "papier", C, rot=(0, 0, 8)))
    P.append(cyl("olowek", 0.004, 0.16, (0.2, -0.25, 0.773), "czerwony", C, rot=(0, 90, 30), seg=6))
    join(P, "Pulpit")
    lam = grupa(Lamp, "Lampki", pan)
    empty("PunktInterakcji", (0, -0.8, 0), C)
    report("OB_Pulpit")


# ====================================================================== KOLA
def kolo_wojskowe(C, name, xs, y, r=0.46, w=0.2, kolor="szary_wojsk"):
    P = []
    for x in xs:
        P.append(cyl("opona", r, w, (x, y, r), "guma", C, rot=(0, 90, 0), seg=24))
        P.append(torus("bieznik", r - 0.01, 0.03, (x, y, r), "guma", C, rot=(0, 90, 0), seg=24, rseg=6))
        P.append(cyl("felga", r * 0.6, w + 0.01, (x, y, r), kolor, C, rot=(0, 90, 0), seg=20))
        P.append(cyl("piasta", r * 0.22, w + 0.05, (x, y, r), kolor, C, rot=(0, 90, 0), seg=12))
        for k in range(6):
            a = 2 * math.pi * k / 6
            P.append(cyl("sruba", 0.015, w + 0.07, (x, y + 0.075 * math.cos(a), r + 0.075 * math.sin(a)), "stal", C, rot=(0, 90, 0), seg=6))
        for k in range(6):
            a = 2 * math.pi * (k + 0.5) / 6
            P.append(cyl("otwor", 0.03, w + 0.02, (x, y + 0.17 * math.cos(a), r + 0.17 * math.sin(a)), "grafit", C, rot=(0, 90, 0), seg=8))
    return join(P, name, origin=(sum(xs) / len(xs), y, r))


# ====================================================================== CIEZAROWKA (typu Opel Blitz)
def build_ciezarowka():
    C = new_collection("OB_Ciezarowka")
    K = "szary_wojsk"
    r, yF, yR = 0.46, -2.1, 1.5
    B = []
    for sx in (-1, 1):
        B.append(box("podluznica", (0.12, 6.3, 0.22), (sx * 0.45, 0.05, 0.78), "grafit", C))
    B.append(box("zderzak", (2.0, 0.12, 0.16), (0, -3.18, 0.72), K, C, bevel=0.03, seg=2))
    for sx in (-1, 1):
        B.append(torus("hak", 0.05, 0.015, (sx * 0.55, -3.27, 0.72), "grafit", C, rot=(0, 90, 0), seg=10, rseg=4))
    B.append(box("chlodnica", (0.8, 0.12, 0.8), (0, -3.0, 1.32), K, C, bevel=0.05, seg=3))
    for k in range(9):
        B.append(box("zebro", (0.025, 0.04, 0.64), (-0.32 + k * 0.08, -3.07, 1.3), "grafit", C))
    B.append(box("maska", (0.86, 1.32, 0.62), (0, -2.3, 1.38), K, C, bevel=0.07, seg=3))
    B.append(rod("zawias_maski", (0, -2.95, 1.7), (0, -1.65, 1.7), 0.012, K, C))
    for sx in (-1, 1):
        for k in range(7):
            B.append(box("zaluzja", (0.01, 0.05, 0.22), (sx * 0.435, -2.65 + k * 0.1, 1.38), "grafit", C))
    for sx in (-1, 1):
        x = sx * 0.82
        blotnik(C, B, x, yF, r + 0.14, 0.42, r, K)
        B.append(box("blot_przod", (0.42, 0.55, 0.06), (x, -2.78, 0.78), K, C, rot=(28, 0, 0), bevel=0.02, seg=2))
        B.append(box("blot_tyl", (0.42, 0.5, 0.06), (x, -1.45, 0.73), K, C, rot=(-22, 0, 0), bevel=0.02, seg=2))
        B.append(box("stopien", (0.4, 0.95, 0.05), (sx * 0.92, -1.0, 0.62), "grafit", C))
        B.append(cyl("reflektor", 0.125, 0.2, (sx * 0.6, -2.82, 1.2), K, C, rot=(90, 0, 0), seg=16, r2=0.1))
        B.append(rod("wspornik", (sx * 0.6, -2.75, 1.08), (sx * 0.7, -2.75, 0.93), 0.015, K, C))
        B.append(rod("lusterko_r", (sx * 0.98, -1.7, 1.95), (sx * 1.18, -1.75, 2.1), 0.01, "grafit", C))
        B.append(box("lusterko", (0.03, 0.12, 0.18), (sx * 1.2, -1.75, 2.15), "grafit", C, bevel=0.01))
    # kabina
    B.append(box("kabina_dol", (2.0, 1.5, 0.78), (0, -1.0, 1.26), K, C, bevel=0.04, seg=2))
    B.append(box("kabina_gora", (1.94, 1.42, 0.72), (0, -0.98, 2.0), K, C, bevel=0.08, seg=3))
    B.append(box("dach", (1.9, 1.38, 0.1), (0, -0.98, 2.36), K, C, bevel=0.05, seg=3))
    for sx in (-1, 1):
        B.append(box("szyba_p", (0.8, 0.04, 0.48), (sx * 0.44, -1.7, 2.02), "szklo_jasne", C, rot=(-6, 0, 0)))
    B.append(box("slupek_s", (0.06, 0.05, 0.5), (0, -1.71, 2.02), K, C))
    B.append(box("szyba_bok_P", (0.02, 0.75, 0.42), (-0.975, -0.86, 2.02), "szklo_jasne", C))
    B.append(box("szyba_tyl", (0.9, 0.02, 0.3), (0, -0.265, 2.08), "szklo_jasne", C))
    B.append(box("linia_drzwi", (0.01, 0.02, 0.75), (-1.0, -0.5, 1.4), "grafit", C))
    B.append(box("klamka_P", (0.03, 0.09, 0.02), (-1.01, -0.65, 1.5), "chrom", C))
    B.append(cyl("bak", 0.2, 0.7, (-0.78, -0.05, 0.75), K, C, rot=(90, 0, 0), seg=16))
    B.append(box("skrzynka_nar", (0.3, 0.6, 0.3), (0.8, 0.6, 0.85), K, C, bevel=0.02))
    B.append(rod("wydech", (-0.4, 2.5, 0.55), (-0.4, 3.3, 0.55), 0.035, "grafit", C))
    # skrzynia ladunkowa
    y0, y1 = -0.15, 3.25
    yc, Ld = (y0 + y1) / 2, y1 - y0
    B.append(box("podloga", (2.2, Ld, 0.08), (0, yc, 1.16), "drewno", C))
    B.append(box("belki", (2.0, Ld - 0.1, 0.12), (0, yc, 1.06), "grafit", C))
    for sx in (-1, 1):
        for k in range(3):
            B.append(box("burta", (0.04, Ld, 0.14), (sx * 1.08, yc, 1.27 + k * 0.15), "drewno", C, bevel=0.004, seg=2))
        for y in np.linspace(y0 + 0.1, y1 - 0.1, 5):
            B.append(box("klonica", (0.05, 0.05, 0.47), (sx * 1.105, float(y), 1.42), K, C))
        B.append(box("blotnik_t", (0.6, 0.95, 0.04), (sx * 0.83, yR, 1.08), K, C, bevel=0.01))
        B.append(box("chlapacz", (0.5, 0.01, 0.35), (sx * 0.83, yR + 0.5, 0.85), "guma", C))
        B.append(box("tylne_sw", (0.12, 0.04, 0.08), (sx * 0.9, y1 + 0.03, 1.0), "czerwony", C))
    B.append(box("sciana_p", (2.2, 0.04, 0.47), (0, y0 + 0.02, 1.42), "drewno", C, bevel=0.004))
    B.append(box("klapa", (2.2, 0.04, 0.47), (0, y1 - 0.02, 1.42), "drewno", C, bevel=0.004))
    B.append(box("tablica", (0.45, 0.01, 0.12), (0, y1 + 0.005, 0.95), "kremowy", C))
    # plandeka na palakach
    B.append(box("plandeka_bok", (2.24, Ld - 0.04, 0.55), (0, yc, 1.92), "plandeka", C, bevel=0.02, seg=2))
    dach = cyl("plandeka_dach", 1.12, Ld - 0.04, (0, 0, 0), "plandeka", C, rot=(90, 0, 0), seg=24)
    dach.data.transform(Matrix.Diagonal((1, 1, 0.42, 1)))
    dach.data.transform(Matrix.Translation((0, yc, 2.19)))
    B.append(dach)
    for y in np.linspace(y0 + 0.05, y1 - 0.05, 5):
        pk = torus("palak", 1.125, 0.018, (0, 0, 0), "plandeka", C, rot=(90, 0, 0), seg=24, rseg=4, arc=180)
        pk.data.transform(Matrix.Diagonal((1, 1, 0.43, 1)))
        pk.data.transform(Matrix.Translation((0, float(y), 2.19)))
        B.append(pk)
    for sx in (-1, 1):
        for y in np.linspace(y0 + 0.2, y1 - 0.2, 8):
            B.append(box("troczek", (0.012, 0.02, 0.12), (sx * 1.125, float(y), 1.62), "worek", C))
    B.append(cyl("zwiniety_tyl", 0.07, 2.1, (0, y1 - 0.02, 2.42), "plandeka", C, rot=(0, 90, 0), seg=10))
    B.append(box("otwor_tyl", (1.9, 0.01, 0.5), (0, y1 - 0.01, 1.98), "guma", C))
    join(B, "Karoseria")
    D = [box("drzwi", (0.04, 0.93, 0.72), (1.0, -0.98, 1.27), K, C, bevel=0.01, seg=2),
         box("rama_d", (0.04, 0.93, 0.08), (1.0, -0.98, 2.27), K, C),
         box("slupek_d", (0.04, 0.06, 0.6), (1.0, -0.54, 1.95), K, C),
         box("szyba_d", (0.02, 0.8, 0.42), (0.995, -1.0, 2.0), "szklo_jasne", C),
         box("klamka_d", (0.03, 0.09, 0.02), (1.02, -0.65, 1.5), "chrom", C)]
    join(D, "Drzwi_Kierowcy", origin=(1.0, -1.445, 1.0))
    R = [cyl("szklo", 0.105, 0.02, (sx * 0.6, -2.925, 1.2), "kremowy", C, rot=(90, 0, 0), seg=16,
             mat=mat_emis("M_Reflektor", (1.0, 0.92, 0.7), 3)) for sx in (-1, 1)]
    join(R, "Reflektory")
    kolo_wojskowe(C, "Kolo_PP", [-0.82], yF, r, 0.22)
    kolo_wojskowe(C, "Kolo_LP", [0.82], yF, r, 0.22)
    kolo_wojskowe(C, "Kolo_PT", [-0.72, -0.95], yR, r, 0.2)
    kolo_wojskowe(C, "Kolo_LT", [0.72, 0.95], yR, r, 0.2)
    empty("Swiatla_L", (0.6, -2.94, 1.2), C)
    empty("Swiatla_P", (-0.6, -2.94, 1.2), C)
    empty("Paka", (0, yc, 1.25), C)
    empty("Siedzenie_Kierowcy", (0.45, -0.9, 1.45), C)
    report("OB_Ciezarowka")


# ====================================================================== KOLA KOLEJOWE
def kolo_szprychowe(C, P, x, y, r, w=0.13, szprych=16, przeciwwaga=True, pin=None):
    sx = 1 if x > 0 else -1
    P.append(cyl("obrecz", r, w, (x, y, r), "stal_polerowana", C, rot=(0, 90, 0), seg=32))
    P.append(cyl("tarcza", r - 0.08, w + 0.004, (x, y, r), "grafit", C, rot=(0, 90, 0), seg=32))
    P.append(torus("wieniec", r - 0.09, 0.03, (x + sx * 0.05, y, r), "czerwien_kolej", C, rot=(0, 90, 0), seg=32, rseg=5))
    for k in range(szprych):
        a = 2 * math.pi * k / szprych
        rm = (r - 0.09 + 0.17) / 2
        P.append(box("szprycha", (0.04, 0.045, r - 0.27), (x + sx * 0.05, y + rm * math.cos(a), r + rm * math.sin(a)),
                     "czerwien_kolej", C, rot=(math.degrees(a) - 90, 0, 0)))
    P.append(cyl("piasta", 0.17, w + 0.1, (x, y, r), "czerwien_kolej", C, rot=(0, 90, 0), seg=16))
    P.append(cyl("os_kapsel", 0.07, w + 0.14, (x, y, r), "stal_polerowana", C, rot=(0, 90, 0), seg=12))
    if przeciwwaga:
        P.append(box("przeciwwaga", (0.06, 0.4, 0.22), (x + sx * 0.05, y + 0.28, r + 0.05), "czerwien_kolej", C, rot=(-30, 0, 0)))
    if pin is not None:
        P.append(cyl("czop", 0.05, 0.18, (x + sx * 0.12, pin[0], pin[1]), "stal_polerowana", C, rot=(0, 90, 0), seg=10))


def kolo_tarczowe(C, P, x, y, r, w=0.13):
    P.append(cyl("obrecz", r, w, (x, y, r), "stal_polerowana", C, rot=(0, 90, 0), seg=24))
    P.append(cyl("tarcza", r - 0.06, w + 0.02, (x, y, r), "grafit", C, rot=(0, 90, 0), seg=24))
    P.append(cyl("piasta", 0.12, w + 0.08, (x, y, r), "grafit", C, rot=(0, 90, 0), seg=12))


def bufor(C, P, x, y, z, kier):
    P.append(cyl("bufor_tuleja", 0.14, 0.4, (x, y + kier * 0.2, z), "grafit", C, rot=(90, 0, 0), seg=16))
    P.append(cyl("bufor_talerz", 0.22, 0.05, (x, y + kier * 0.42, z), "stal_polerowana", C, rot=(90, 0, 0), seg=24))


# ====================================================================== LOKOMOTYWA (typu pruska P8 z tendrem)
def build_lokomotywa():
    C = new_collection("OB_Lokomotywa")
    P = []
    Kc, Kr = "lakier_czarny", "czerwien_kolej"
    # rama, czolownica, zderzaki, latarnie
    for sx in (-1, 1):
        P.append(box("rama", (0.08, 10.4, 0.75), (sx * 0.62, -3.9, 1.1), Kr, C))
    P.append(box("czolownica", (2.9, 0.22, 0.55), (0, -9.0, 1.2), Kr, C, bevel=0.02, seg=2))
    for sx in (-1, 1):
        bufor(C, P, sx * 0.88, -9.11, 1.2, -1)
        P.append(cyl("latarnia", 0.12, 0.2, (sx * 0.75, -9.05, 1.78), Kc, C, rot=(90, 0, 0), seg=14))
        P.append(cyl("latarnia_s", 0.09, 0.02, (sx * 0.75, -9.16, 1.78), "kremowy", C, rot=(90, 0, 0), seg=14))
    P.append(box("hak", (0.08, 0.3, 0.1), (0, -9.25, 1.2), "grafit", C))
    P.append(cyl("latarnia_g", 0.11, 0.18, (0, -8.95, 3.62), Kc, C, rot=(90, 0, 0), seg=14))
    # pomosty z fartuchami
    for sx in (-1, 1):
        P.append(box("pomost", (0.5, 9.3, 0.05), (sx * 1.2, -4.15, 1.72), Kc, C))
        P.append(box("fartuch", (0.03, 9.3, 0.2), (sx * 1.46, -4.15, 1.62), Kr, C))
    # kociol + obrecze
    P.append(cyl("kociol", 0.82, 6.6, (0, -4.0, 2.75), Kc, C, rot=(90, 0, 0), seg=32))
    for y in (-6.9, -5.7, -4.5, -3.3, -2.1, -0.9):
        P.append(torus("obrecz", 0.826, 0.018, (0, y, 2.75), "oksyda", C, rot=(90, 0, 0), seg=32, rseg=4))
    # dymnica + drzwi dymnicy
    P.append(cyl("dymnica", 0.87, 1.55, (0, -8.05, 2.75), "grafit", C, rot=(90, 0, 0), seg=32))
    P.append(cyl("dymnica_pierscien", 0.89, 0.08, (0, -8.85, 2.75), "grafit", C, rot=(90, 0, 0), seg=32))
    P.append(cyl("drzwi_dymnicy", 0.76, 0.1, (0, -8.92, 2.75), "grafit", C, rot=(90, 0, 0), seg=32, r2=0.7))
    P.append(cyl("rygiel", 0.05, 0.18, (0, -9.02, 2.75), "stal_polerowana", C, rot=(90, 0, 0), seg=10))
    P.append(box("rygiel_t", (0.3, 0.03, 0.04), (0, -9.1, 2.75), "stal_polerowana", C))
    for dz in (-0.38, 0.38):
        P.append(box("zawias_dym", (0.95, 0.025, 0.06), (-0.3, -8.97, 2.75 + dz), "grafit", C))
    P.append(torus("porecz_przod", 0.84, 0.013, (0, -8.95, 2.75), "stal_polerowana", C, rot=(90, 0, 0), seg=28, rseg=4, arc=180))
    P.append(box("tabliczka", (0.6, 0.02, 0.16), (0, -8.98, 3.3), Kc, C))
    # komin, kopuly, zawory, gwizdek
    P.append(cyl("komin_siodlo", 0.33, 0.22, (0, -8.0, 3.62), "grafit", C, seg=20))
    P.append(cyl("komin", 0.24, 0.62, (0, -8.0, 4.0), "grafit", C, seg=20, r2=0.28))
    P.append(cyl("komin_krawedz", 0.31, 0.07, (0, -8.0, 4.33), "grafit", C, seg=20))
    for y, rr, hh in ((-4.6, 0.4, 0.42), (-2.6, 0.34, 0.3)):
        P.append(cyl("kopula", rr, hh, (0, y, 3.55 + hh / 2), Kc, C, seg=24))
        P.append(sphere("kopula_g", rr, (0, y, 3.55 + hh), Kc, C, seg=24, rings=8, scale=(1, 1, 0.45)))
    for sx in (-1, 1):
        P.append(cyl("zawor", 0.05, 0.2, (sx * 0.12, -1.2, 3.65), "mosiadz", C, seg=12))
    P.append(cyl("gwizdek", 0.035, 0.3, (0.3, -0.75, 3.7), "mosiadz", C, seg=12))
    P.append(sphere("gwizdek_g", 0.045, (0.3, -0.75, 3.87), "mosiadz", C, seg=10, rings=5))
    # owiewki (Wagnera) + wsporniki
    for sx in (-1, 1):
        P.append(box("owiewka", (0.025, 2.1, 1.55), (sx * 1.32, -7.65, 2.55), Kc, C, bevel=0.01, seg=2))
        P.append(rod("wspornik_ow", (sx * 0.85, -7.3, 2.9), (sx * 1.31, -7.3, 2.9), 0.02, Kc, C))
        P.append(rod("wspornik_ow2", (sx * 0.85, -8.2, 2.5), (sx * 1.31, -8.2, 2.5), 0.02, Kc, C))
    # poreczki, rury, pompa
    for sx in (-1, 1):
        P.append(rod("porecz", (sx * 0.95, -7.2, 3.0), (sx * 0.95, -0.8, 3.0), 0.013, "stal_polerowana", C))
        for y in (-7.0, -5.2, -3.4, -1.6):
            P.append(rod("slupek_p", (sx * 0.8, y, 2.95), (sx * 0.95, y, 3.0), 0.01, "stal_polerowana", C))
        P.append(rod("rura_zasil", (sx * 0.86, -6.6, 2.25), (sx * 0.86, -0.6, 2.25), 0.035, "grafit", C))
    P.append(cyl("pompa", 0.17, 0.95, (-1.12, -4.9, 2.25), "grafit", C, seg=16))
    P.append(cyl("pompa_g", 0.2, 0.08, (-1.12, -4.9, 2.76), "stal_polerowana", C, seg=16))
    # cylindry, prowadnice, wodzik, korbowody
    yk = (-4.9, -3.0, -1.1)
    rk = 0.875
    pins = [(y - 0.3, rk) for y in yk]
    for sx in (-1, 1):
        P.append(cyl("cylinder", 0.36, 1.0, (sx * 1.05, -7.3, 1.3), "grafit", C, rot=(90, 0, 0), seg=24))
        for dy in (-0.52, 0.52):
            P.append(cyl("pokrywa", 0.39, 0.06, (sx * 1.05, -7.3 + dy, 1.3), "stal_polerowana", C, rot=(90, 0, 0), seg=24))
        P.append(box("skrzynia_suw", (0.32, 0.9, 0.3), (sx * 1.05, -7.3, 1.75), "grafit", C, bevel=0.02))
        for dz in (-0.1, 0.1):
            P.append(box("prowadnica", (0.04, 1.3, 0.04), (sx * 1.05, -6.15, 1.3 + dz), "stal_polerowana", C))
        P.append(box("wodzik", (0.12, 0.22, 0.18), (sx * 1.05, -6.0, 1.3), "stal_polerowana", C, bevel=0.01))
        P.append(rod("korbowod", (sx * 1.05, -6.0, 1.3), (sx * 1.0, pins[1][0], pins[1][1]), 0.045, "stal_polerowana", C))
        P.append(box("wiazar", (0.04, pins[2][0] - pins[0][0] + 0.2, 0.1), (sx * 0.95, (pins[0][0] + pins[2][0]) / 2, rk), "stal_polerowana", C, bevel=0.01))
        P.append(rod("drazek_rozrz", (sx * 1.1, -6.8, 1.9), (sx * 1.0, -3.6, 1.25), 0.025, "stal_polerowana", C))
        for y, pin in zip(yk, pins):
            kolo_szprychowe(C, P, sx * 0.76, y, rk, pin=pin)
        for y in (-8.35, -7.35):
            kolo_tarczowe(C, P, sx * 0.76, y, 0.5)
        P.append(box("wozek_rama", (0.08, 1.6, 0.25), (sx * 0.9, -7.85, 0.55), "grafit", C))
    # budka maszynisty
    P.append(box("budka_czolo", (2.9, 0.06, 2.3), (0, -0.3, 2.9), Kc, C))
    for sx in (-1, 1):
        P.append(cyl("okno_okragle", 0.2, 0.08, (sx * 0.9, -0.33, 3.35), "szklo_jasne", C, rot=(90, 0, 0), seg=20))
        P.append(torus("okno_rama", 0.2, 0.02, (sx * 0.9, -0.36, 3.35), "mosiadz", C, rot=(90, 0, 0), seg=20, rseg=4))
        P.append(box("bok_dol", (0.05, 2.3, 1.0), (sx * 1.45, 0.85, 2.25), Kc, C, bevel=0.01))
        for y in (-0.17, 0.85, 1.92):
            P.append(box("slupek", (0.05, 0.16, 0.95), (sx * 1.45, y, 3.22), Kc, C))
        P.append(box("szyba_bok", (0.02, 0.8, 0.6), (sx * 1.44, 0.34, 3.22), "szklo_jasne", C))
        P.append(rod("uchwyt_wej", (sx * 1.5, 2.0, 1.8), (sx * 1.5, 2.0, 2.9), 0.015, "stal_polerowana", C))
        for z in (1.0, 1.4):
            P.append(box("stopien", (0.3, 0.3, 0.04), (sx * 1.35, 2.05, z), "grafit", C))
    P.append(box("dach_budki", (3.1, 2.7, 0.1), (0, 0.85, 3.75), Kc, C, bevel=0.05, seg=3))
    P.append(box("rynna", (3.12, 2.72, 0.03), (0, 0.85, 3.69), "grafit", C))
    P.append(box("podloga_b", (2.9, 2.3, 0.08), (0, 0.85, 1.76), "grafit", C))
    # tender
    for sx in (-1, 1):
        P.append(box("rama_t", (0.08, 6.9, 0.6), (sx * 0.62, 5.85, 1.1), Kr, C))
    P.append(box("tender", (2.9, 6.6, 1.75), (0, 5.95, 2.5), Kc, C, bevel=0.04, seg=2))
    for z in (1.85, 3.2):
        P.append(box("listwa_t", (2.94, 6.62, 0.04), (0, 5.95, z), "grafit", C))
    for sx in (-1, 1):
        P.append(box("burta_wegla", (0.04, 4.2, 0.4), (sx * 1.5, 4.9, 3.55), Kc, C, rot=(0, sx * 15, 0)))
    wg = bmesh.new()
    bmesh.ops.create_grid(wg, x_segments=10, y_segments=14, size=1.0)
    rngw = np.random.default_rng(9)
    for v in wg.verts:
        v.co.x *= 1.4; v.co.y *= 2.0
        v.co.z = 0.35 * (1 - (v.co.x / 1.4) ** 2) * (1 - (v.co.y / 2.0) ** 4) + rngw.normal(0, 0.03)
    bmesh.ops.translate(wg, vec=(0, 4.9, 3.38), verts=wg.verts)
    wg.normal_update()
    P.append(_finish("wegiel", wg, C, atlas_mat(), "wegiel"))
    P.append(box("skrzynia_nar_t", (2.6, 0.5, 0.45), (0, 2.75, 3.55), Kc, C, bevel=0.02))
    P.append(cyl("wlew", 0.3, 0.12, (0, 8.4, 3.43), "grafit", C, seg=20))
    for sx in (-1, 1):
        bufor(C, P, sx * 0.88, 9.3, 1.2, 1)
        P.append(rod("drabinka", (sx * 0.5, 9.27, 1.7), (sx * 0.5, 9.27, 3.35), 0.015, "stal_polerowana", C))
    for z in np.linspace(1.85, 3.25, 6):
        P.append(rod("szczebel", (-0.5, 9.27, float(z)), (0.5, 9.27, float(z)), 0.012, "stal_polerowana", C))
    P.append(box("czolownica_t", (2.9, 0.2, 0.5), (0, 9.2, 1.2), Kr, C))
    for yb in (4.0, 7.9):
        for sx in (-1, 1):
            P.append(box("wozek_t", (0.08, 2.3, 0.35), (sx * 0.88, yb, 0.75), "grafit", C))
            for k in range(4):
                P.append(box("resor", (0.1, 0.95 - k * 0.18, 0.03), (sx * 0.88, yb, 0.98 + k * 0.035), "grafit", C))
            for dy in (-0.75, 0.75):
                kolo_tarczowe(C, P, sx * 0.76, yb + dy, 0.5)
                P.append(box("maznica", (0.18, 0.26, 0.26), (sx * 0.97, yb + dy, 0.5), "grafit", C, bevel=0.01))
    join(P, "Lokomotywa")
    empty("Komin", (0, -8.0, 4.37), C)
    report("OB_Lokomotywa")


# ====================================================================== WAGON (typu DRG, zielony)
def build_wagon():
    C = new_collection("OB_Wagon")
    P = []
    Lc = 16.0
    G = "lakier_zielony"
    for sx in (-1, 1):
        P.append(box("ostojnica", (0.1, Lc, 0.3), (sx * 1.2, 0, 1.12), "grafit", C))
        P.append(rod("ciegno1", (sx * 0.8, -5.6, 1.0), (sx * 0.8, -2.2, 0.6), 0.025, "grafit", C))
        P.append(rod("ciegno2", (sx * 0.8, -2.2, 0.6), (sx * 0.8, 2.2, 0.6), 0.025, "grafit", C))
        P.append(rod("ciegno3", (sx * 0.8, 2.2, 0.6), (sx * 0.8, 5.6, 1.0), 0.025, "grafit", C))
        for y in (-2.2, 2.2):
            P.append(rod("wieszak", (sx * 0.8, y, 0.6), (sx * 0.8, y, 1.0), 0.02, "grafit", C))
    P.append(cyl("zbiornik_gaz", 0.25, 1.5, (0.5, 0, 0.78), "grafit", C, rot=(90, 0, 0), seg=16))
    P.append(cyl("zbiornik_gaz2", 0.25, 1.5, (-0.5, 0, 0.78), "grafit", C, rot=(90, 0, 0), seg=16))
    P.append(box("akumulator", (0.9, 0.6, 0.4), (0, 3.2, 0.85), "grafit", C, bevel=0.02))
    # pudlo
    P.append(box("pudlo", (2.9, Lc, 2.35), (0, 0, 2.47), G, C, bevel=0.03, seg=2))
    for sx in (-1, 1):
        x = sx * 1.452
        for z in (2.25, 3.32):
            P.append(box("listwa", (0.025, Lc - 0.1, 0.045), (x, 0, z), "lakier_czarny", C))
        for k in range(10):
            y = -6.75 + k * 1.5
            P.append(box("szyba", (0.02, 1.0, 0.75), (x, y, 2.78), "szklo_jasne", C))
            for z in (2.38, 3.18):
                P.append(box("rama_o", (0.03, 1.12, 0.05), (x + sx * 0.005, y, z), "lakier_czarny", C))
            for dy in (-0.53, 0.53):
                P.append(box("rama_o2", (0.03, 0.05, 0.85), (x + sx * 0.005, y + dy, 2.78), "lakier_czarny", C))
            P.append(box("listwa_dol", (0.02, 0.03, 0.85), (x, y + 0.75, 1.76), "lakier_czarny", C))
        for sy in (-1, 1):
            y = sy * 7.35
            P.append(box("drzwi", (0.03, 0.75, 1.95), (x + sx * 0.01, y, 2.33), G, C, bevel=0.01))
            P.append(box("szyba_d", (0.02, 0.5, 0.6), (x + sx * 0.02, y, 2.92), "szklo_jasne", C))
            P.append(box("klamka", (0.04, 0.1, 0.025), (x + sx * 0.03, y - sy * 0.25, 2.3), "mosiadz", C))
            for dy in (-0.45, 0.45):
                P.append(rod("poreczka", (x + sx * 0.06, y + dy, 1.5), (x + sx * 0.06, y + dy, 2.6), 0.014, "mosiadz", C))
            for z, dx in ((1.0, 0.12), (0.68, 0.2)):
                P.append(box("stopien", (0.35, 0.7, 0.04), (sx * (1.45 + dx), y, z), "grafit", C))
    # dach + wywietrzniki
    dach = cyl("dach", 1.6, Lc + 0.1, (0, 0, 0), "dach_wagonu", C, rot=(90, 0, 0), seg=32)
    dach.data.transform(Matrix.Diagonal((1, 1, 0.3, 1)))
    dach.data.transform(Matrix.Translation((0, 0, 3.65)))
    P.append(dach)
    for sx in (-1, 1):
        P.append(rod("rynna", (sx * 1.56, -8.0, 3.6), (sx * 1.56, 8.0, 3.6), 0.02, "grafit", C))
    for y in np.linspace(-6.75, 6.75, 8):
        P.append(cyl("wywietrznik", 0.11, 0.16, (0, float(y), 4.18), "grafit", C, seg=12))
        P.append(sphere("wyw_g", 0.13, (0, float(y), 4.27), "grafit", C, seg=12, rings=5, scale=(1, 1, 0.5)))
    # czola: harmonijki, okna, czolownice, zderzaki
    for sy in (-1, 1):
        y = sy * 8.0
        P.append(box("harmonijka", (1.1, 0.35, 2.0), (0, y + sy * 0.17, 2.4), "guma", C))
        for k in range(5):
            P.append(box("zebro_h", (1.16, 0.03, 2.06), (0, y + sy * (0.04 + k * 0.07), 2.4), "grafit", C))
        for sx in (-1, 1):
            P.append(box("okno_czolo", (0.5, 0.02, 0.6), (sx * 0.95, y + sy * 0.01, 2.9), "szklo_jasne", C))
            bufor(C, P, sx * 0.88, y + sy * 0.12, 1.08, sy)
        P.append(box("czolownica", (2.9, 0.22, 0.35), (0, y + sy * 0.05, 1.1), "grafit", C))
        P.append(box("hak", (0.08, 0.3, 0.1), (0, y + sy * 0.3, 1.08), "grafit", C))
    # wozki z resorami
    for yb in (-5.6, 5.6):
        P.append(box("belka_wozka", (2.2, 0.4, 0.25), (0, yb, 0.92), "grafit", C))
        for sx in (-1, 1):
            P.append(box("rama_wozka", (0.1, 2.7, 0.32), (sx * 1.05, yb, 0.72), "grafit", C, bevel=0.01))
            for k in range(4):
                P.append(box("resor", (0.12, 1.0 - k * 0.18, 0.025), (sx * 1.05, yb, 0.52 + k * 0.03), "grafit", C))
            for dy in (-1.25, 1.25):
                kolo_tarczowe(C, P, sx * 0.76, yb + dy, 0.47)
                P.append(box("maznica", (0.2, 0.28, 0.28), (sx * 1.08, yb + dy, 0.47), "grafit", C, bevel=0.01))
    join(P, "Wagon")
    report("OB_Wagon")


# ====================================================================== KIOSK
def build_kiosk():
    C = new_collection("OB_Kiosk")
    W, D, H = 1.9, 1.6, 2.3
    G = "lakier_zielony"
    P = [box("cokol", (W + 0.06, D + 0.06, 0.25), (0, 0, 0.125), "beton", C, bevel=0.01),
         box("sciany", (W, D, H - 0.25), (0, 0, 0.25 + (H - 0.25) / 2), "drewno_drzwi", C)]
    # listwy narozne i poziome w kolorze zielonym
    for sx in (-1, 1):
        for sy in (-1, 1):
            P.append(box("narozn", (0.08, 0.08, H - 0.25), (sx * W / 2, sy * D / 2, 0.25 + (H - 0.25) / 2), G, C, bevel=0.01, seg=2))
    for z in (0.3, 1.0, H - 0.04):
        P.append(box("pas", (W + 0.03, D + 0.03, 0.07), (0, 0, z), G, C, bevel=0.01, seg=2))
    # okno sprzedazy ze szprosami, lada, markiza
    F = -D / 2
    P.append(box("okno", (1.3, 0.03, 0.85), (0, F - 0.005, 1.5), "szklo_jasne", C))
    P.append(box("rama_g", (1.42, 0.06, 0.07), (0, F - 0.02, 1.94), G, C))
    P.append(box("rama_d", (1.42, 0.06, 0.07), (0, F - 0.02, 1.06), G, C))
    for x in (-0.68, -0.23, 0.23, 0.68):
        P.append(box("szpros", (0.05, 0.05, 0.85), (x, F - 0.02, 1.5), G, C))
    P.append(box("szpros_poz", (1.36, 0.04, 0.04), (0, F - 0.02, 1.72), G, C))
    P.append(box("lada", (1.5, 0.35, 0.05), (0, F - 0.17, 1.03), "drewno", C, bevel=0.008, seg=2))
    for x in (-0.6, 0.6):
        P.append(box("wspornik", (0.04, 0.3, 0.12), (x, F - 0.15, 0.95), G, C, rot=(-30, 0, 0)))
    for k in range(6):
        P.append(box("gazeta", (0.3, 0.22, 0.01), (-0.55 + k * 0.22, F - 0.17, 1.06 + k * 0.002), "papier", C, rot=(0, 0, (k % 3 - 1) * 6)))
    P.append(box("markiza", (1.7, 0.6, 0.03), (0, F - 0.28, 2.05), "tkanina_zielona", C, rot=(-18, 0, 0)))
    for k in range(9):
        P.append(box("falbanka", (0.18, 0.01, 0.1), (-0.76 + k * 0.19, F - 0.57, 1.92), "kremowy" if k % 2 else "tkanina_zielona", C))
    P.append(box("szyld", (1.5, 0.04, 0.22), (0, F - 0.03, 2.2), "kremowy", C, bevel=0.01))
    P.append(box("szyld_rama", (1.56, 0.03, 0.28), (0, F - 0.015, 2.2), G, C))
    # drzwi z tylu
    P.append(box("drzwi", (0.7, 0.03, 1.8), (0, D / 2 + 0.01, 1.15), "drewno_drzwi", C, bevel=0.01))
    P.append(cyl("klamka", 0.015, 0.06, (0.25, D / 2 + 0.04, 1.15), "mosiadz", C, rot=(90, 0, 0), seg=8))
    # dach z blachy cynkowej z rynna
    P.append(box("okap", (W + 0.4, D + 0.4, 0.08), (0, 0, H + 0.04), "stal", C, bevel=0.02, seg=2))
    P.append(cyl("dach", 1.5, 0.45, (0, 0, H + 0.3), "stal", C, seg=4, r2=0.12, rot=(0, 0, 45)))
    P.append(sphere("kula", 0.08, (0, 0, H + 0.58), "mosiadz", C, seg=12, rings=6))
    for sy in (-1, 1):
        P.append(rod("rynna", (-(W / 2 + 0.2), sy * (D / 2 + 0.2), H), ((W / 2 + 0.2), sy * (D / 2 + 0.2), H), 0.03, "stal", C))
    join(P, "Kiosk")
    plak = mat_image("M_Plakaty", tex_plakaty(), 0.9)
    for sx, rot in ((-W / 2 - 0.006, -90), (W / 2 + 0.006, 90)):
        s = flat_sheet("plakat", 1.2, 1.15, C, plak)
        s.rotation_euler = (math.radians(90), 0, math.radians(rot)); s.location = (sx, 0.0, 1.55)
    bpy.context.view_layer.update()
    join([o for o in C.objects if o.name.startswith("plakat")], "Plakaty")
    report("OB_Kiosk")


# ====================================================================== HYDRANT (zeliwny, nadziemny, lata 30.)
def build_hydrant():
    C = new_collection("OB_Hydrant")
    R, Z = "czerwien_hydrant", "zeliwo"
    P = [cyl("kolnierz", 0.17, 0.05, (0, 0, 0.025), R, C, seg=24, bevel=0.006),
         cyl("podstawa", 0.125, 0.12, (0, 0, 0.11), R, C, seg=24, r2=0.105)]
    for k in range(8):
        a = 2 * math.pi * k / 8
        P.append(cyl("sruba", 0.012, 0.03, (0.145 * math.cos(a), 0.145 * math.sin(a), 0.06), Z, C, seg=6))
    # kolumna z zebrami
    P.append(cyl("kolumna", 0.095, 0.5, (0, 0, 0.42), R, C, seg=24))
    for k in range(12):
        a = 2 * math.pi * k / 12
        P.append(box("zebro", (0.018, 0.012, 0.42), (0.096 * math.cos(a), 0.096 * math.sin(a), 0.42), R, C,
                     rot=(0, 0, math.degrees(a)), bevel=0.004, seg=2))
    for z in (0.18, 0.66):
        P.append(torus("obrecz", 0.104, 0.016, (0, 0, z), R, C, seg=24, rseg=6))
    # glowica z wylotami
    P.append(cyl("glowica", 0.118, 0.14, (0, 0, 0.74), R, C, seg=24, bevel=0.008))
    for sx in (-1, 1):
        P.append(cyl("wylot", 0.045, 0.1, (sx * 0.15, 0, 0.74), R, C, rot=(0, 90, 0), seg=16))
        P.append(torus("wylot_krawedz", 0.048, 0.008, (sx * 0.2, 0, 0.74), R, C, rot=(0, 90, 0), seg=16, rseg=4))
        P.append(cyl("zaslepka", 0.055, 0.035, (sx * 0.215, 0, 0.74), Z, C, rot=(0, 90, 0), seg=16, bevel=0.004))
        for k in range(3):
            a = 2 * math.pi * k / 3 + 0.5
            P.append(box("ucho", (0.03, 0.02, 0.025), (sx * 0.215, 0.06 * math.cos(a), 0.74 + 0.06 * math.sin(a)), Z, C,
                         rot=(math.degrees(a), 0, 0)))
        for k in range(4):      # lancuszek zaslepki zwisajacy do glowicy
            P.append(torus("ogniwo", 0.008, 0.002, (sx * (0.2 - k * 0.018), -0.05, 0.69 - k * 0.012), "stal", C,
                           rot=(90 if k % 2 else 0, 0, 0), seg=8, rseg=4))
    # kopula z nakretka pieciokatna
    P.append(torus("rant_kopuly", 0.12, 0.012, (0, 0, 0.815), R, C, seg=24, rseg=6))
    P.append(sphere("kopula", 0.12, (0, 0, 0.815), R, C, seg=24, rings=8, scale=(1, 1, 0.55)))
    P.append(cyl("nakretka", 0.028, 0.045, (0, 0, 0.9), Z, C, seg=5))
    P.append(cyl("nakretka_p", 0.02, 0.012, (0, 0, 0.93), Z, C, seg=5))
    join(P, "Hydrant")
    report("OB_Hydrant")


build_vis(); build_skrytka(); build_amunicja(); build_worki(); build_pulpit(); build_kiosk(); build_hydrant()
build_ciezarowka(); build_lokomotywa(); build_wagon()
