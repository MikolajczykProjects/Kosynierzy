# Priorytet 3. Wymaga ob_lib.py + ob_p2.py (helpery: rod, flat_sheet, stol, kolo, blotnik, zegar, _paper, _disk).
import numpy as np


# ---------------------------------------------------------------- tekstury plakatow (bez symboli: teatr/kino/mydlo/kolej)
def _plakat(img, x0, y0, w, h, bg, ink, akcent, rng, styl):
    img[y0:y0 + h, x0:x0 + w] = bg
    img[y0 + 6:y0 + h - 6, x0 + 6:x0 + 8] = ink
    if styl == 0:     # duzy tytul + kolo (slonce/reflektor)
        img[y0 + 16:y0 + 40, x0 + 18:x0 + w - 18] = ink
        _disk(img, x0 + w // 2, y0 + h // 2, w // 4, akcent)
    elif styl == 1:   # pasy ukosne (kolej) + tytul
        for k in range(0, w, 18):
            for t in range(h // 3):
                x = x0 + k + t // 2
                if x < x0 + w - 4:
                    img[y0 + h // 3 + t, x:x + 6] = akcent
        img[y0 + 14:y0 + 34, x0 + 14:x0 + w - 14] = ink
    else:             # sylwetka butelki/mydla + tekst
        img[y0 + h // 4:y0 + h * 3 // 4, x0 + w // 2 - 18:x0 + w // 2 + 18] = akcent
        img[y0 + 16:y0 + 32, x0 + 16:x0 + w - 16] = ink
    for r in range(3):   # linie tekstu na dole
        y = y0 + h - 40 + r * 10
        img[y:y + 4, x0 + 18:x0 + w - 18 - int(rng.integers(0, 40))] = ink


def tex_plakaty():
    H, W = 512, 512
    img, rng = _paper(H, W, (0.5, 0.47, 0.4), 11)
    kol = [((0.88, 0.80, 0.55), (0.15, 0.1, 0.1), (0.75, 0.25, 0.1)),
           ((0.2, 0.3, 0.45), (0.95, 0.9, 0.8), (0.9, 0.75, 0.3)),
           ((0.85, 0.85, 0.8), (0.1, 0.25, 0.15), (0.2, 0.45, 0.3)),
           ((0.7, 0.2, 0.15), (0.95, 0.9, 0.8), (0.95, 0.85, 0.5)),
           ((0.9, 0.85, 0.6), (0.25, 0.15, 0.3), (0.45, 0.3, 0.55)),
           ((0.3, 0.25, 0.2), (0.9, 0.8, 0.6), (0.8, 0.5, 0.2))]
    i = 0
    for y in (8, 262):
        for x in (8, 178, 348):
            bg, ink, ak = kol[i % len(kol)]
            _plakat(img, x, y, 156, 242, bg, ink, ak, rng, i % 3)
            i += 1
    return save_image("T_Plakaty", img)


def tex_plakat():
    img, rng = _paper(362, 256, (0.88, 0.80, 0.55), 12)
    _plakat(img, 0, 0, 256, 362, (0.88, 0.80, 0.55), (0.15, 0.1, 0.1), (0.75, 0.25, 0.1), rng, 0)
    return save_image("T_Plakat", img)


def cyl_uv_full(name, r, h, loc, coll, mat, seg=16):
    """Walec z tekstura owinieta dookola (u = kat, v = wysokosc)."""
    bm = bmesh.new()
    uv = bm.loops.layers.uv.verify()
    ring0 = [bm.verts.new((r * math.cos(2 * math.pi * i / seg), r * math.sin(2 * math.pi * i / seg), 0)) for i in range(seg)]
    ring1 = [bm.verts.new((v.co.x, v.co.y, h)) for v in ring0]
    for i in range(seg):
        j = (i + 1) % seg
        f = bm.faces.new((ring0[i], ring0[j], ring1[j], ring1[i]))
        u0, u1 = i / seg, (i + 1) / seg
        for l, (u, v) in zip(f.loops, ((u0, 0), (u1, 0), (u1, 1), (u0, 1))):
            l[uv].uv = (u * 2, v)
    bmesh.ops.translate(bm, vec=Vector(loc), verts=bm.verts)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    me.materials.append(mat)
    o = bpy.data.objects.new(name, me); coll.objects.link(o)
    for p in me.polygons:
        p.use_smooth = True
    return o


# ---------------------------------------------------------------- pojazdy
def build_ciezarowka():
    C = new_collection("OB_Ciezarowka")
    r, yF, yR, xw = 0.45, -2.0, 1.5, 0.82
    B = []
    B.append(box("rama", (1.0, 5.6, 0.22), (0, -0.2, 0.62), "oksyda", C))
    B.append(box("maska", (1.05, 1.3, 0.75), (0, -2.45, 1.22), "zielen_wojsk", C, bevel=0.04))
    B.append(box("atrapa", (0.95, 0.08, 0.8), (0, -3.12, 1.2), "zeliwo", C, bevel=0.02))
    for k in range(8):
        B.append(box("zebro", (0.03, 0.09, 0.7), (-0.35 + k * 0.1, -3.14, 1.2), "oksyda", C))
    B.append(box("kabina", (2.0, 1.4, 1.35), (0, -1.1, 1.62), "zielen_wojsk", C, bevel=0.06))
    B.append(box("szyba_p", (1.7, 0.05, 0.5), (0, -1.81, 1.95), "szklo_ciemne", C, rot=(-8, 0, 0)))
    B.append(box("szyby_b", (2.02, 0.7, 0.45), (0, -1.15, 1.95), "szklo_ciemne", C))
    B.append(box("dach_k", (2.02, 1.42, 0.06), (0, -1.1, 2.3), "zielen_wojsk", C, bevel=0.02))
    for sx in (-1, 1):
        blotnik(C, B, sx * xw, yF, r + 0.1, 0.36, r, "zielen_wojsk")
        B.append(box("blot_p", (0.36, 0.6, 0.12), (sx * xw, yF - 0.6, 0.72), "zielen_wojsk", C, rot=(22, 0, 0)))
        B.append(box("stopien", (0.3, 0.6, 0.05), (sx * 0.95, -1.2, 0.55), "oksyda", C))
        B.append(cyl("reflektor", 0.13, 0.18, (sx * 0.62, -2.95, 1.45), "zeliwo", C, rot=(90, 0, 0), seg=12))
        B.append(box("lusterko", (0.05, 0.03, 0.18), (sx * 1.1, -1.6, 1.9), "oksyda", C))
        B.append(box("tylne_sw", (0.1, 0.03, 0.08), (sx * 0.95, 3.0, 1.0), "czerwony", C))
    # skrzynia ladunkowa + plandeka
    B.append(box("podloga", (2.2, 3.7, 0.15), (0, 1.12, 1.05), "drewno", C))
    for sx in (-1, 1):
        B.append(box("burta", (0.06, 3.7, 0.45), (sx * 1.1, 1.12, 1.3), "drewno_ciemne", C))
    B.append(box("burta_t", (2.2, 0.06, 0.45), (0, 2.97, 1.3), "drewno_ciemne", C))
    B.append(box("plandeka", (2.24, 3.6, 0.95), (0, 1.12, 1.95), "plandeka", C, bevel=0.03))
    roof = cyl("dach_pl", 1.12, 3.6, (0, 0, 0), "plandeka", C, rot=(90, 0, 0), seg=16)
    roof.data.transform(Matrix.Diagonal((1, 1, 0.35, 1)))
    roof.location = (0, 1.12, 2.42)
    B.append(roof)
    for k in range(5):   # zebra plandeki
        B.append(box("listwa", (2.28, 0.05, 0.06), (0, -0.6 + k * 0.85, 2.0), "oksyda", C))
    B.append(box("zderzak", (2.0, 0.12, 0.14), (0, -3.25, 0.75), "oksyda", C))
    join(B, "Karoseria")
    R = []
    for sx in (-1, 1):
        R.append(cyl("szklo", 0.11, 0.02, (sx * 0.62, -3.05, 1.45), "kremowy", C, rot=(90, 0, 0), seg=12,
                     mat=mat_emis("M_Reflektor", (1.0, 0.92, 0.7), 3)))
    join(R, "Reflektory")
    for nm, sx, y in (("Kolo_PP", -1, yF), ("Kolo_LP", 1, yF), ("Kolo_PT", -1, yR), ("Kolo_LT", 1, yR)):
        kolo(C, nm, sx * (xw if y == yF else 0.78), y, r, 0.3 if y == yR else 0.22)
    empty("Swiatla_L", (0.62, -3.07, 1.45), C)
    empty("Swiatla_P", (-0.62, -3.07, 1.45), C)
    empty("Paka", (0, 1.12, 1.15), C)
    report("OB_Ciezarowka")


def build_rower():
    C = new_collection("OB_Rower")
    r = 0.34
    yP, yT = -0.52, 0.53
    def wheel(name, y):
        P = [torus("obr", r - 0.02, 0.02, (0, 0, 0), "guma", C, rot=(0, 90, 0), seg=20, rseg=5),
             torus("felga", r - 0.05, 0.008, (0, 0, 0), "chrom", C, rot=(0, 90, 0), seg=20, rseg=4),
             cyl("piasta", 0.025, 0.08, (0, 0, 0), "chrom", C, rot=(0, 90, 0), seg=8)]
        for k in range(10):
            a = 2 * math.pi * k / 10
            P.append(rod("szprycha", (0, 0, 0), (0, (r - 0.05) * math.cos(a), (r - 0.05) * math.sin(a)), 0.002, "chrom", C, seg=4))
        o = join(P, name, origin=(0, 0, 0))
        o.location = (0, y, r)
        return o
    wheel("Kolo_P", yP); wheel("Kolo_T", yT)
    BB = (0, 0.05, 0.3); ST = (0, 0.2, 0.85); HT = (0, -0.42, 0.88); HB = (0, -0.46, 0.68)
    F = [rod("rura_s", BB, ST, 0.016, "lakier_czarny", C), rod("rura_d", BB, HB, 0.018, "lakier_czarny", C),
         rod("rura_g", ST, HT, 0.016, "lakier_czarny", C), rod("glowka", HB, HT, 0.02, "lakier_czarny", C),
         rod("dol_t", BB, (0, yT, r), 0.01, "lakier_czarny", C), rod("gora_t", ST, (0, yT, r), 0.01, "lakier_czarny", C),
         rod("widelec", HB, (0, yP, r), 0.013, "lakier_czarny", C),
         rod("mostek", HT, (0, -0.36, 1.0), 0.012, "chrom", C),
         rod("kierownica", (-0.28, -0.3, 1.0), (0.28, -0.3, 1.0), 0.011, "chrom", C),
         rod("kier_l", (0.28, -0.3, 1.0), (0.3, -0.38, 1.0), 0.011, "chrom", C),
         rod("kier_p", (-0.28, -0.3, 1.0), (-0.3, -0.38, 1.0), 0.011, "chrom", C),
         rod("sztyca", ST, (0, 0.22, 0.92), 0.012, "chrom", C),
         box("siodlo", (0.14, 0.24, 0.05), (0, 0.24, 0.94), "skora", C, bevel=0.015),
         box("blotnik_t", (0.06, 0.5, 0.01), (0, yT + 0.05, r + 0.38), "lakier_czarny", C, rot=(-15, 0, 0)),
         cyl("lampa", 0.045, 0.07, (0, -0.47, 0.92), "chrom", C, rot=(90, 0, 0), seg=10),
         cyl("zebatka", 0.09, 0.012, (-0.04, BB[1], BB[2]), "chrom", C, rot=(0, 90, 0), seg=14),
         rod("lancuch_g", (-0.04, BB[1], BB[2] + 0.09), (-0.04, yT, r + 0.04), 0.005, "oksyda", C),
         rod("lancuch_d", (-0.04, BB[1], BB[2] - 0.09), (-0.04, yT, r - 0.04), 0.005, "oksyda", C)]
    join(F, "Rama")
    Pd = []
    for s in (-1, 1):
        x = s * 0.07
        Pd.append(rod("korba", (x, BB[1], BB[2]), (x, BB[1] + 0.0, BB[2] + s * 0.17), 0.01, "chrom", C))
        Pd.append(box("pedal", (0.1, 0.05, 0.02), (s * 0.12, BB[1], BB[2] + s * 0.17), "guma", C))
    join(Pd, "Pedaly", origin=BB)
    empty("Siodelko", (0, 0.24, 0.97), C)
    report("OB_Rower")


# ---------------------------------------------------------------- male rzeczy
def build_film():
    C = new_collection("OB_Film")
    P = [cyl("puszka", 0.0125, 0.04, (0, 0, 0.02), "stal", C, seg=12),
         cyl("szpula", 0.006, 0.012, (0, 0, 0.046), "oksyda", C, seg=8),
         box("jezyk", (0.022, 0.0012, 0.026), (0.012, -0.008, 0.02), "film", C, rot=(0, 0, 30))]
    join(P, "Film")
    report("OB_Film")


def build_radio():
    C = new_collection("OB_Radio_Odbiornik")
    w, d, h = 0.45, 0.25, 0.36
    P = [box("obudowa", (w, d, h), (0, 0, h / 2), "drewno", C, bevel=0.03),
         box("gora_luk", (w - 0.04, d, 0.06), (0, 0, h + 0.02), "drewno", C, bevel=0.03),
         box("glosnik", (0.3, 0.012, 0.16), (0, -d / 2, 0.24), "worek", C, bevel=0.01)]
    for k in range(3):
        P.append(box("listwa", (0.012, 0.016, 0.16), (-0.07 + k * 0.07, -d / 2 - 0.003, 0.24), "drewno_ciemne", C))
    for x in (-0.15, 0.15):
        P.append(cyl("galka", 0.025, 0.03, (x, -d / 2 - 0.01, 0.07), "bakelit", C, rot=(90, 0, 0), seg=10))
    join(P, "Radio")
    s = box("Skala", (0.2, 0.012, 0.04), (0, -d / 2 - 0.002, 0.08), "kremowy", C, mat=mat_emis("M_Lampa_Radio", (1.0, 0.7, 0.3), 3))
    report("OB_Radio_Odbiornik")


# ---------------------------------------------------------------- wnetrza
def build_wnetrza():
    C = new_collection("OB_Stol")
    join(stol(C, 1.2, 0.8, 0.76, "drewno"), "Stol")

    C = new_collection("OB_Krzeslo")
    P = [box("siedzisko", (0.44, 0.42, 0.04), (0, 0, 0.45), "drewno", C, bevel=0.006)]
    for sx in (-1, 1):
        for sy in (-1, 1):
            P.append(box("noga", (0.035, 0.035, 0.43), (sx * 0.19, sy * 0.18, 0.215), "drewno", C))
        P.append(box("oparcie_s", (0.035, 0.035, 0.5), (sx * 0.19, 0.19, 0.7), "drewno", C, rot=(-6, 0, 0)))
    for z in (0.75, 0.88):
        P.append(box("szczebel", (0.4, 0.025, 0.06), (0, 0.205, z), "drewno", C, rot=(-6, 0, 0)))
    P.append(box("tapicerka", (0.4, 0.38, 0.03), (0, 0, 0.485), "tkanina_zielona", C, bevel=0.01))
    join(P, "Krzeslo")

    C = new_collection("OB_Lozko")
    P = [box("rama", (0.95, 2.0, 0.25), (0, 0, 0.3), "drewno_ciemne", C, bevel=0.01),
         box("materac", (0.88, 1.92, 0.18), (0, 0, 0.5), "papier", C, bevel=0.04),
         box("koc", (0.92, 1.3, 0.05), (0, -0.3, 0.6), "granat", C, bevel=0.02),
         box("poduszka", (0.6, 0.35, 0.12), (0, 0.72, 0.62), "papier", C, bevel=0.05),
         box("wezglowie", (0.95, 0.06, 1.0), (0, 1.0, 0.5), "drewno_ciemne", C, bevel=0.01),
         box("nogi_p", (0.95, 0.06, 0.6), (0, -1.0, 0.3), "drewno_ciemne", C, bevel=0.01)]
    join(P, "Lozko")

    C = new_collection("OB_Szafa")
    P = [box("korpus", (1.0, 0.55, 2.0), (0, 0, 1.0), "drewno_ciemne", C, bevel=0.01),
         box("gzyms", (1.08, 0.6, 0.08), (0, 0, 2.02), "drewno_ciemne", C, bevel=0.01),
         box("cokol", (1.04, 0.58, 0.1), (0, 0, 0.05), "drewno_ciemne", C)]
    for sx in (-1, 1):
        P.append(box("drzwi", (0.46, 0.02, 1.7), (sx * 0.245, -0.28, 1.05), "drewno", C, bevel=0.006))
        P.append(box("plycina", (0.34, 0.012, 1.3), (sx * 0.245, -0.295, 1.1), "drewno_ciemne", C))
        P.append(cyl("galka", 0.015, 0.03, (sx * 0.04, -0.3, 1.05), "mosiadz", C, rot=(90, 0, 0), seg=8))
    join(P, "Szafa")

    C = new_collection("OB_Biurko")
    P = [box("blat", (1.3, 0.7, 0.04), (0, 0, 0.74), "drewno_ciemne", C, bevel=0.006),
         box("szuflady", (0.42, 0.66, 0.7), (0.42, 0, 0.36), "drewno", C, bevel=0.006),
         box("bok", (0.04, 0.66, 0.72), (-0.61, 0, 0.36), "drewno", C),
         box("plecy", (1.2, 0.03, 0.4), (0, 0.32, 0.52), "drewno", C)]
    for k in range(3):
        P.append(box("front_sz", (0.38, 0.015, 0.2), (0.42, -0.335, 0.12 + k * 0.23), "drewno_ciemne", C))
        P.append(cyl("uchwyt", 0.012, 0.02, (0.42, -0.35, 0.12 + k * 0.23), "mosiadz", C, rot=(90, 0, 0), seg=8))
    P.append(box("bibula", (0.5, 0.35, 0.006), (-0.1, -0.05, 0.763), "tkanina_zielona", C))
    join(P, "Biurko")

    C = new_collection("OB_Lada_Recepcja")
    P = [box("korpus", (2.0, 0.55, 1.05), (0, 0, 0.525), "drewno_ciemne", C, bevel=0.01),
         box("blat", (2.1, 0.65, 0.05), (0, 0, 1.075), "drewno", C, bevel=0.01),
         box("cokol", (2.02, 0.57, 0.08), (0, 0, 0.04), "oksyda", C)]
    for k in range(4):
        P.append(box("plycina", (0.4, 0.015, 0.7), (-0.72 + k * 0.48, -0.28, 0.55), "drewno", C, bevel=0.005))
    P.append(cyl("dzwonek", 0.04, 0.03, (0.6, -0.1, 1.115), "mosiadz", C, seg=12, r2=0.01))
    P.append(box("ksiega", (0.35, 0.25, 0.03), (-0.3, 0.0, 1.115), "skora", C, rot=(0, 0, 8)))
    join(P, "Lada")

    C = new_collection("OB_Regal_Ksiazki")
    rng = np.random.default_rng(3)
    P = [box("plecy", (1.0, 0.02, 2.0), (0, 0.165, 1.0), "drewno_ciemne", C)]
    for sx in (-1, 1):
        P.append(box("bok", (0.03, 0.35, 2.0), (sx * 0.485, 0, 1.0), "drewno_ciemne", C))
    kolory = ["czerwony", "granat", "tkanina_zielona", "skora", "zolty_karton", "drewno_jasne", "kremowy", "panel_zielony"]
    for k in range(5):
        z = 0.02 + k * 0.42
        P.append(box("polka", (0.94, 0.33, 0.025), (0, 0, z), "drewno", C))
        if k == 4:
            break
        x = -0.45
        while x < 0.42:
            bw = float(rng.uniform(0.025, 0.05)); bh = float(rng.uniform(0.24, 0.33))
            tilt = 0 if rng.random() > 0.08 else 12
            P.append(box("ksiazka", (bw, 0.22, bh), (x + bw / 2, -0.02, z + 0.0125 + bh / 2), kolory[int(rng.integers(0, len(kolory)))], C, rot=(0, tilt, 0)))
            x += bw + 0.003
    join(P, "Regal")

    C = new_collection("OB_Lampa_Biurkowa")
    P = [cyl("podstawa", 0.08, 0.025, (0, 0, 0.0125), "mosiadz", C, seg=12, r2=0.07),
         rod("ramie", (0, 0.03, 0.02), (0, 0.0, 0.36), 0.008, "mosiadz", C),
         rod("ramie2", (0, 0.0, 0.36), (0, -0.1, 0.38), 0.008, "mosiadz", C),
         cyl("klosz", 0.11, 0.08, (0, -0.15, 0.36), "tkanina_zielona", C, seg=12, r2=0.04)]
    join(P, "Lampa")
    sphere("Zarowka", 0.03, (0, -0.15, 0.33), "kremowy", C, mat=mat_emis("M_Klosz", (1.0, 0.8, 0.45), 3))

    C = new_collection("OB_Kanapa")
    P = [box("siedzisko", (1.9, 0.8, 0.42), (0, 0, 0.25), "skora", C, bevel=0.04),
         box("oparcie", (1.9, 0.22, 0.5), (0, 0.32, 0.65), "skora", C, bevel=0.06)]
    for sx in (-1, 1):
        P.append(box("porecz", (0.2, 0.8, 0.62), (sx * 0.86, 0, 0.36), "skora", C, bevel=0.07))
    for sx in (-0.45, 0.45):
        P.append(box("poduszka", (0.75, 0.6, 0.12), (sx, -0.06, 0.52), "skora", C, bevel=0.04))
    for sx in (-1, 1):
        for sy in (-1, 1):
            P.append(cyl("noga", 0.025, 0.06, (sx * 0.85, sy * 0.33, 0.03), "drewno_ciemne", C, seg=6))
    join(P, "Kanapa")

    C = new_collection("OB_Lustro")
    P = [box("rama", (0.62, 0.04, 1.02), (0, 0, 0.51), "mosiadz", C, bevel=0.01),
         box("szklo", (0.54, 0.042, 0.94), (0, -0.002, 0.51), "chrom", C)]
    join(P, "Lustro")
    for n in ("OB_Stol", "OB_Krzeslo", "OB_Lozko", "OB_Szafa", "OB_Biurko", "OB_Lada_Recepcja",
              "OB_Regal_Ksiazki", "OB_Lampa_Biurkowa", "OB_Kanapa", "OB_Lustro"):
        report(n)


# ---------------------------------------------------------------- fotograf
def build_fotograf():
    C = new_collection("OB_Aparat_Studyjny")
    P = []
    for i in range(3):
        a = math.radians(90 + i * 120)
        P.append(rod("noga", (0.35 * math.cos(a), 0.35 * math.sin(a), 0), (0.05 * math.cos(a), 0.05 * math.sin(a), 1.2), 0.018, "drewno", C))
    P.append(box("glowica", (0.2, 0.2, 0.05), (0, 0, 1.22), "drewno_ciemne", C))
    P.append(box("korpus", (0.32, 0.3, 0.32), (0, 0.1, 1.42), "drewno", C, bevel=0.01))
    P.append(box("mieszek", (0.26, 0.25, 0.26), (0, -0.15, 1.42), "skora", C, bevel=0.03))
    P.append(box("przod", (0.24, 0.04, 0.24), (0, -0.29, 1.42), "drewno", C, bevel=0.008))
    P.append(cyl("obiektyw", 0.05, 0.1, (0, -0.35, 1.42), "mosiadz", C, rot=(90, 0, 0), seg=12))
    P.append(box("plachta", (0.42, 0.45, 0.4), (0, 0.38, 1.4), "guma", C, rot=(-10, 0, 0), bevel=0.05))
    P.append(box("plachta_d", (0.44, 0.06, 0.3), (0, 0.6, 1.12), "guma", C, rot=(-5, 0, 0)))
    join(P, "Aparat")
    empty("Obiektyw", (0, -0.41, 1.42), C)

    C = new_collection("OB_Lampa_Ciemnia")
    P = [box("podstawa", (0.12, 0.12, 0.02), (0, 0, 0.01), "zeliwo", C),
         cyl("oprawa", 0.06, 0.06, (0, 0, 0.05), "zeliwo", C, seg=10)]
    join(P, "Lampa")
    cyl("Klosz", 0.055, 0.12, (0, 0, 0.14), "czerwony", C, seg=10, r2=0.04, mat=mat_emis("M_Lampa_Ciemnia", (1.0, 0.06, 0.03), 3))
    report("OB_Aparat_Studyjny"); report("OB_Lampa_Ciemnia")


# ---------------------------------------------------------------- ulica
def build_ulica():
    plak = mat_image("M_Plakaty", tex_plakaty(), 0.9)
    C = new_collection("OB_Lawka")
    P = []
    for sx in (-1, 1):
        x = sx * 0.75
        P.append(box("noga_p", (0.05, 0.05, 0.45), (x, -0.18, 0.225), "zeliwo", C))
        P.append(box("noga_t", (0.05, 0.05, 0.85), (x, 0.2, 0.425), "zeliwo", C, rot=(-10, 0, 0)))
        P.append(box("wspornik", (0.05, 0.45, 0.04), (x, 0, 0.44), "zeliwo", C))
    for k in range(4):
        P.append(box("deska", (1.7, 0.09, 0.03), (0, -0.18 + k * 0.105, 0.47), "drewno", C, bevel=0.004))
    for k in range(3):
        P.append(box("oparcie", (1.7, 0.03, 0.09), (0, 0.22 + k * 0.02, 0.62 + k * 0.12), "drewno", C, rot=(-10, 0, 0), bevel=0.004))
    join(P, "Lawka")

    C = new_collection("OB_Kiosk")
    P = [box("budka", (1.8, 1.6, 2.3), (0, 0, 1.15), "panel_zielony", C, bevel=0.02),
         box("dach", (2.2, 2.0, 0.12), (0, 0, 2.36), "zeliwo", C, bevel=0.02),
         cyl("daszek", 1.4, 0.4, (0, 0, 2.62), "zeliwo", C, seg=4, r2=0.2, rot=(0, 0, 45)),
         box("okno", (1.2, 0.04, 0.8), (0, -0.81, 1.45), "szklo_ciemne", C),
         box("lada", (1.4, 0.3, 0.05), (0, -0.92, 1.0), "drewno", C),
         box("gazety", (1.2, 0.2, 0.04), (0, -0.92, 1.04), "papier", C)]
    join(P, "Kiosk")
    for sx, rot in ((-0.905, -90), (0.905, 90)):
        s = flat_sheet("plakat", 1.2, 1.4, C, plak)
        s.rotation_euler = (math.radians(90), 0, math.radians(rot)); s.location = (sx, 0.0, 1.2)
    bpy.context.view_layer.update()
    pl = [o for o in C.objects if o.name.startswith("plakat")]
    join(pl, "Plakaty")

    C = new_collection("OB_Slup_Ogloszeniowy")
    P = [cyl("cokol", 0.62, 0.3, (0, 0, 0.15), "zeliwo", C, seg=16),
         cyl("gzyms", 0.66, 0.1, (0, 0, 2.75), "zeliwo", C, seg=16),
         cyl("kopula", 0.6, 0.45, (0, 0, 3.02), "panel_zielony", C, seg=16, r2=0.12),
         sphere("kula", 0.08, (0, 0, 3.3), "zeliwo", C, seg=8, rings=5)]
    join(P, "Slup")
    cyl_uv_full("Plakaty", 0.6, 2.4, (0, 0, 0.3), C, plak, seg=16)

    C = new_collection("OB_Hydrant")
    P = [cyl("podst", 0.14, 0.08, (0, 0, 0.04), "zeliwo", C, seg=10),
         cyl("korpus", 0.1, 0.65, (0, 0, 0.4), "czerwony", C, seg=10, r2=0.085),
         cyl("glowa", 0.11, 0.08, (0, 0, 0.75), "czerwony", C, seg=10),
         cyl("czapka", 0.1, 0.12, (0, 0, 0.85), "czerwony", C, seg=10, r2=0.03),
         cyl("nakretka", 0.025, 0.04, (0, 0, 0.92), "zeliwo", C, seg=5)]
    for sx in (-1, 1):
        P.append(cyl("wylot", 0.04, 0.08, (sx * 0.12, 0, 0.55), "zeliwo", C, rot=(0, 90, 0), seg=8))
    join(P, "Hydrant")

    C = new_collection("OB_Kosz")
    P = [cyl("slupek", 0.03, 1.0, (0, 0.12, 0.5), "zeliwo", C, seg=8),
         cyl("kosz", 0.17, 0.45, (0, 0, 0.75), "panel_zielony", C, seg=12, r2=0.19),
         torus("obr", 0.19, 0.012, (0, 0, 0.975), "zeliwo", C, seg=12, rseg=4)]
    join(P, "Kosz")

    C = new_collection("OB_Wozek_Reczny")
    P = [box("skrzynia", (0.9, 1.2, 0.08), (0, 0, 0.55), "drewno", C)]
    for sx in (-1, 1):
        P.append(box("burta", (0.04, 1.2, 0.25), (sx * 0.45, 0, 0.7), "drewno_ciemne", C))
        P.append(rod("dyszel", (sx * 0.4, 0.55, 0.55), (sx * 0.3, 1.5, 0.8), 0.02, "drewno", C))
        P.append(torus("kolo", 0.4, 0.03, (sx * 0.52, 0.05, 0.42), "drewno_ciemne", C, rot=(0, 90, 0), seg=14, rseg=4))
        P.append(cyl("piasta", 0.05, 0.1, (sx * 0.52, 0.05, 0.42), "zeliwo", C, rot=(0, 90, 0), seg=8))
        for k in range(6):
            a = math.pi * k / 3
            P.append(rod("szpr", (sx * 0.52, 0.05, 0.42), (sx * 0.52, 0.05 + 0.38 * math.cos(a), 0.42 + 0.38 * math.sin(a)), 0.012, "drewno", C))
        P.append(box("podporka", (0.04, 0.04, 0.5), (sx * 0.35, -0.5, 0.27), "drewno", C))
    P.append(rod("os", (-0.55, 0.05, 0.42), (0.55, 0.05, 0.42), 0.02, "zeliwo", C))
    P.append(rod("raczka", (-0.3, 1.5, 0.8), (0.3, 1.5, 0.8), 0.02, "drewno", C))
    P.append(box("worek", (0.5, 0.6, 0.25), (0.1, -0.1, 0.72), "worek", C, bevel=0.08))
    join(P, "Wozek")
    empty("Uchwyt", (0, 1.5, 0.8), C)

    C = new_collection("OB_Plakat")
    s = flat_sheet("Plakat", 0.6, 0.85, C, mat_image("M_Plakat", tex_plakat(), 0.9))
    s.rotation_euler = (math.radians(90), 0, 0); s.location = (0, -0.002, 1.6)
    for n in ("OB_Lawka", "OB_Kiosk", "OB_Slup_Ogloszeniowy", "OB_Hydrant", "OB_Kosz", "OB_Wozek_Reczny", "OB_Plakat"):
        report(n)


# ---------------------------------------------------------------- dworzec
def build_dworzec():
    C = new_collection("OB_Lokomotywa")
    P = [box("rama", (2.3, 11.0, 0.35), (0, 0, 1.15), "oksyda", C),
         cyl("kociol", 0.85, 6.2, (0, -1.6, 2.35), "lakier_czarny", C, rot=(90, 0, 0), seg=16),
         cyl("dymnica", 0.9, 1.0, (0, -5.0, 2.35), "zeliwo", C, rot=(90, 0, 0), seg=16),
         cyl("drzwi_dym", 0.75, 0.1, (0, -5.52, 2.35), "zeliwo", C, rot=(90, 0, 0), seg=16),
         cyl("komin", 0.25, 0.8, (0, -4.9, 3.5), "zeliwo", C, seg=12, r2=0.3),
         cyl("dzwon", 0.3, 0.45, (0, -2.4, 3.3), "lakier_czarny", C, seg=12),
         sphere("dzwon_g", 0.3, (0, -2.4, 3.52), "lakier_czarny", C, seg=12, rings=6, scale=(1, 1, 0.5)),
         cyl("piasecznica", 0.25, 0.35, (0, -0.6, 3.25), "lakier_czarny", C, seg=12),
         box("budka", (2.6, 2.4, 2.4), (0, 2.6, 2.6), "lakier_czarny", C, bevel=0.04),
         box("dach_b", (2.8, 2.6, 0.1), (0, 2.6, 3.85), "zeliwo", C, bevel=0.03),
         box("okno_b", (2.62, 0.8, 0.6), (0, 2.6, 3.0), "szklo_ciemne", C),
         box("okno_p", (0.5, 0.05, 0.5), (0.8, 1.38, 3.1), "szklo_ciemne", C),
         box("okno_p2", (0.5, 0.05, 0.5), (-0.8, 1.38, 3.1), "szklo_ciemne", C),
         box("tender", (2.6, 2.2, 1.6), (0, 4.7, 2.0), "lakier_czarny", C, bevel=0.04),
         box("wegiel", (2.3, 1.9, 0.3), (0, 4.7, 2.85), "oksyda", C, bevel=0.1)]
    for sx in (-1, 1):
        P.append(box("pomost", (0.45, 7.0, 0.05), (sx * 1.15, -1.9, 1.55), "oksyda", C))
        for y in (-3.0, -1.4, 0.2):
            P.append(cyl("kolo_n", 0.75, 0.14, (sx * 0.85, y, 0.75), "czerwony", C, rot=(0, 90, 0), seg=18))
            P.append(cyl("piasta", 0.15, 0.18, (sx * 0.85, y, 0.75), "lakier_czarny", C, rot=(0, 90, 0), seg=10))
            P.append(torus("obrecz", 0.73, 0.04, (sx * 0.85, y, 0.75), "lakier_czarny", C, rot=(0, 90, 0), seg=18, rseg=4))
        P.append(box("korbowod", (0.05, 3.3, 0.1), (sx * 0.95, -1.4, 0.55), "stal", C))
        P.append(box("cylinder", (0.4, 0.9, 0.5), (sx * 0.95, -4.4, 1.3), "lakier_czarny", C, bevel=0.04))
        for y in (-4.6, 3.6, 5.0):
            P.append(cyl("kolo_m", 0.45, 0.12, (sx * 0.85, y, 0.45), "czerwony", C, rot=(0, 90, 0), seg=14))
        P.append(cyl("bufor", 0.18, 0.4, (sx * 0.8, -5.7, 1.25), "zeliwo", C, rot=(90, 0, 0), seg=10))
        P.append(cyl("lampa", 0.12, 0.2, (sx * 0.7, -5.6, 1.8), "zeliwo", C, rot=(90, 0, 0), seg=10))
    P.append(box("belka_czol", (2.6, 0.2, 0.5), (0, -5.5, 1.25), "czerwony", C))
    join(P, "Lokomotywa")
    empty("Komin", (0, -4.9, 3.95), C)

    C = new_collection("OB_Wagon")
    L = 14.0
    P = [box("pudlo", (2.9, L, 2.5), (0, 0, 2.45), "panel_zielony", C, bevel=0.04),
         box("pas_dol", (2.92, L - 0.2, 0.3), (0, 0, 1.4), "lakier_czarny", C),
         box("rama", (2.6, L + 0.4, 0.3), (0, 0, 1.05), "oksyda", C)]
    roof = cyl("dach", 1.5, L, (0, 0, 0), "zeliwo", C, rot=(90, 0, 0), seg=16)
    roof.data.transform(Matrix.Diagonal((1, 1, 0.28, 1)))
    roof.location = (0, 0, 3.7)
    P.append(roof)
    for sx in (-1, 1):
        for k in range(9):
            y = -5.6 + k * 1.4
            P.append(box("okno", (2.92, 0.9, 0.75), (0, y, 2.85), "szklo_ciemne", C))
        P.append(box("drzwi_k", (2.92, 0.7, 1.9), (0, sx * (L / 2 - 0.5), 2.2), "lakier_czarny", C))
        for y in (-5.0, 5.0):
            P.append(box("wozek", (2.2, 2.4, 0.4), (0, y, 0.75), "oksyda", C))
            for yy in (y - 0.8, y + 0.8):
                for xx in (-1, 1):
                    P.append(cyl("kolo", 0.45, 0.12, (xx * 0.8, yy, 0.45), "zeliwo", C, rot=(0, 90, 0), seg=14))
        for xx in (-1, 1):
            P.append(cyl("bufor", 0.17, 0.4, (xx * 0.8, sx * (L / 2 + 0.3), 1.1), "zeliwo", C, rot=(90, 0, 0), seg=10))
    join(P, "Wagon")
    report("OB_Lokomotywa"); report("OB_Wagon")


build_ciezarowka(); build_rower(); build_film(); build_radio()
build_wnetrza(); build_fotograf(); build_ulica(); build_dworzec()
