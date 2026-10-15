# Priorytet 2. Przod = -Y (Unity +Z), pivot na dole.
import numpy as np


def _paper(h, w, base, seed):
    rng = np.random.default_rng(seed)
    img = np.zeros((h, w, 3)) + np.array(base)
    n = rng.random((h // 8 + 2, w // 8 + 2))
    n = np.kron(n, np.ones((8, 8)))[:h, :w]
    img *= (0.94 + 0.06 * n)[..., None]
    return img, rng


def _disk(img, cx, cy, r, col, ring=None):
    h, w, _ = img.shape
    yy, xx = np.mgrid[0:h, 0:w]
    d = np.hypot(xx - cx, yy - cy)
    m = (d < r) if ring is None else ((d < r) & (d > r - ring))
    img[m] = col


def _line(img, x0, y0, x1, y1, th, col):
    n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
    for t in np.linspace(0, 1, n):
        x = int(x0 + (x1 - x0) * t); y = int(y0 + (y1 - y0) * t)
        img[max(0, y - th):y + th + 1, max(0, x - th):x + th + 1] = col


def tex_mapa():
    H = W = 512
    img, rng = _paper(H, W, (0.86, 0.82, 0.68), 3)
    green = (0.62, 0.70, 0.50)
    for _ in range(7):
        x, y = rng.integers(20, 480, 2)
        img[y:y + rng.integers(30, 70), x:x + rng.integers(30, 80)] = green
    # rzeka Klodnica
    for x in range(W):
        y = int(330 + 25 * np.sin(x / 70) + 10 * np.sin(x / 23))
        img[y - 5:y + 5, x] = (0.45, 0.6, 0.75)
    # ulice: siatka + przekatne
    st = (0.35, 0.25, 0.18)
    for x in range(40, W, 55):
        img[:, x:x + 3] = st
    for y in range(30, H, 60):
        img[y:y + 3, :] = st
    _line(img, 0, 500, 300, 250, 2, st)
    _line(img, 250, 260, 280, 30, 2, st)            # droga na polnoc (Tarnowitzer Landstr.)
    # kolej
    for x in range(0, W, 12):
        img[420:423, x:x + 7] = (0.1, 0.1, 0.1)
    # kwartaly zabudowy
    for _ in range(40):
        x, y = rng.integers(0, 500, 2)
        img[y:y + 10, x:x + 14] = (0.72, 0.62, 0.52)
    # CZERWONE KOLKO na polnocy (radiostacja) + strzalka
    _disk(img, 280, 60, 34, (0.78, 0.08, 0.06), ring=6)
    _disk(img, 280, 60, 5, (0.78, 0.08, 0.06))
    _line(img, 260, 300, 278, 100, 2, (0.78, 0.08, 0.06))
    # rama i legenda
    img[:6, :] = img[-6:, :] = st; img[:, :6] = img[:, -6:] = st
    img[460:500, 380:500] = (0.9, 0.87, 0.75)
    for k in range(3):
        img[470 + k * 10:473 + k * 10, 390:480] = (0.3, 0.3, 0.3)
    return save_image("T_Mapa", img)


def tex_lista():
    H, W = 362, 256
    img, rng = _paper(H, W, (0.93, 0.91, 0.85), 4)
    img[20:34, 30:226] = (0.15, 0.15, 0.15)            # naglowek
    for r in range(14):
        y = 52 + r * 20
        img[y:y + 4, 30:42] = (0.2, 0.2, 0.25)          # numer
        x = 52
        while x < 220:
            L = int(rng.integers(10, 40))
            img[y:y + 4, x:min(x + L, 220)] = (0.2, 0.2, 0.25)
            x += L + 6
        if r % 3 == 1:
            img[y + 1:y + 3, 30:220] = (0.55, 0.12, 0.1)   # podkreslenie czerwonym olowkiem
    _disk(img, 196, 330, 20, (0.55, 0.12, 0.12), ring=3)     # okragla pieczatka (bez symboli)
    return save_image("T_Lista", img)


def tex_kartka():
    H, W = 340, 256
    img, rng = _paper(H, W, (0.90, 0.87, 0.78), 5)
    ink = (0.12, 0.14, 0.3)
    for r in range(3):
        y0 = 60 + r * 45
        x = 30
        while x < 200 - r * 40:
            L = int(rng.integers(14, 34))
            for i in range(L):
                y = int(y0 + 3 * np.sin((x + i) / 3.0))
                img[y:y + 3, x + i] = ink
            x += L + 9
    # zagiecie
    img[:, 127:129] *= 0.85
    img[169:171, :] *= 0.85
    return save_image("T_Kartka", img)


def flat_sheet(name, w, d, coll, mat, z=0.0, bend=0.0):
    bm = bmesh.new()
    nx, ny = 4, 4
    vs = [[bm.verts.new(((i / nx - .5) * w, (j / ny - .5) * d, z + bend * math.sin(i / nx * math.pi) * (1 if j % 2 else 0.6)))
           for i in range(nx + 1)] for j in range(ny + 1)]
    uv = bm.loops.layers.uv.verify()
    for j in range(ny):
        for i in range(nx):
            f = bm.faces.new((vs[j][i], vs[j][i + 1], vs[j + 1][i + 1], vs[j + 1][i]))
            for l in f.loops:
                p = l.vert.co
                l[uv].uv = (p.x / w + .5, p.y / d + .5)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    me.materials.append(mat)
    o = bpy.data.objects.new(name, me); coll.objects.link(o)
    return o


def rod(name, p0, p1, r, tile, C, seg=6):
    p0 = Vector(p0); p1 = Vector(p1); d = p1 - p0
    o = cyl(name, r, d.length, (0, 0, 0), tile, C, seg=seg)
    q = Vector((0, 0, 1)).rotation_difference(d.normalized())
    o.data.transform(q.to_matrix().to_4x4())
    o.data.transform(Matrix.Translation((p0 + p1) / 2))
    return o


def solidify(o, t):
    md = o.modifiers.new("S", "SOLIDIFY"); md.thickness = t; md.offset = 1
    apply_mods(o)
    return o


# ---------------------------------------------------------------- obiekty
def build_aparat():
    C = new_collection("OB_Aparat")
    P = []
    P.append(box("korpus", (0.11, 0.03, 0.062), (0, 0, 0.031), "lakier_czarny", C, bevel=0.003))
    for sx in (-1, 1):
        P.append(cyl("bok", 0.015, 0.062, (sx * 0.055, 0, 0.031), "lakier_czarny", C, seg=10))
    P.append(box("skora", (0.112, 0.032, 0.04), (0, 0, 0.026), "skora", C))
    P.append(box("gora", (0.13, 0.031, 0.012), (0, 0, 0.068), "chrom", C, bevel=0.002))
    P.append(box("wizjer", (0.05, 0.022, 0.012), (-0.02, 0, 0.079), "chrom", C, bevel=0.002))
    for x in (-0.04, 0.0):
        P.append(box("okienko", (0.012, 0.024, 0.008), (x, 0, 0.079), "szklo_ciemne", C))
    P.append(cyl("spust", 0.006, 0.01, (0.04, 0, 0.078), "chrom", C, seg=8))
    P.append(cyl("pokretlo", 0.01, 0.008, (-0.05, 0, 0.078), "chrom", C, seg=10))
    P.append(cyl("obiektyw1", 0.02, 0.02, (0.005, -0.025, 0.035), "chrom", C, rot=(90, 0, 0), seg=14))
    P.append(cyl("obiektyw2", 0.016, 0.025, (0.005, -0.045, 0.035), "lakier_czarny", C, rot=(90, 0, 0), seg=14))
    P.append(cyl("soczewka", 0.012, 0.004, (0.005, -0.058, 0.035), "szklo_ciemne", C, rot=(90, 0, 0), seg=12))
    join(P, "Aparat")
    empty("Obiektyw", (0.005, -0.06, 0.035), C, size=0.03)
    report("OB_Aparat")


def stol(C, w, d, h, tile="drewno"):
    P = [box("blat", (w, d, 0.04), (0, 0, h - 0.02), tile, C, bevel=0.006),
         box("oskrzynie", (w - 0.1, d - 0.1, 0.1), (0, 0, h - 0.09), tile, C)]
    for sx in (-1, 1):
        for sy in (-1, 1):
            P.append(box("noga", (0.06, 0.06, h - 0.04), (sx * (w / 2 - 0.06), sy * (d / 2 - 0.06), (h - 0.04) / 2), tile, C, bevel=0.005))
    return P


def build_mapa_stol():
    C = new_collection("OB_Mapa_Stol")
    H = 0.78
    P = stol(C, 1.4, 0.9, H, "drewno_ciemne")
    P.append(cyl("olowek", 0.004, 0.17, (0.45, -0.25, H + 0.006), "czerwony", C, rot=(0, 90, 25), seg=6))
    P.append(cyl("kubek", 0.04, 0.09, (-0.55, 0.28, H + 0.045), "kremowy", C, seg=12))
    P.append(box("ksiazka", (0.16, 0.23, 0.04), (0.52, 0.22, H + 0.02), "granat", C, rot=(0, 0, 12), bevel=0.004))
    join(P, "Stol")
    m = flat_sheet("Mapa", 0.9, 0.62, C, mat_image("M_Mapa", tex_mapa(), 0.9), z=H + 0.003, bend=0.004)
    m.rotation_euler = (0, 0, math.radians(-6))
    empty("CelZdjecia", (0, 0, H + 0.01), C)
    report("OB_Mapa_Stol")


def build_lista():
    C = new_collection("OB_Lista")
    s = flat_sheet("Lista", 0.21, 0.297, C, mat_image("M_Lista", tex_lista(), 0.9), z=0.002, bend=0.002)
    s.rotation_euler = (0, 0, math.radians(8))
    P = [box("spinacz", (0.008, 0.035, 0.002), (-0.07, 0.15, 0.004), "stal", C)]
    join(P, "Spinacz")
    empty("CelZdjecia", (0, 0, 0.01), C)
    report("OB_Lista")


def build_kartka():
    C = new_collection("OB_Kartka")
    s = flat_sheet("Kartka", 0.15, 0.2, C, mat_image("M_Kartka", tex_kartka(), 0.9), z=0.002, bend=0.01)
    empty("CelZdjecia", (0, 0, 0.01), C)
    report("OB_Kartka")


def build_telefon():
    C = new_collection("OB_Telefon")
    P = []
    P.append(box("podst", (0.2, 0.16, 0.05), (0, 0, 0.025), "bakelit", C, bevel=0.01))
    P.append(box("korp", (0.16, 0.13, 0.07), (0, 0.005, 0.08), "bakelit", C, rot=(-12, 0, 0), bevel=0.012))
    P.append(cyl("tarcza_t", 0.045, 0.01, (0, -0.06, 0.09), "chrom", C, rot=(78, 0, 0), seg=16))
    P.append(cyl("tarcza_s", 0.035, 0.012, (0, -0.064, 0.09), "tarcza", C, rot=(78, 0, 0), seg=16))
    for sx in (-1, 1):
        P.append(box("widelki", (0.012, 0.02, 0.05), (sx * 0.05, 0.0, 0.135), "bakelit", C))
    # korbka z boku
    P.append(cyl("os", 0.006, 0.03, (0.115, 0.0, 0.06), "chrom", C, rot=(0, 90, 0), seg=6))
    P.append(box("ramie", (0.006, 0.012, 0.05), (0.13, 0.0, 0.045), "chrom", C))
    P.append(cyl("raczka", 0.007, 0.03, (0.145, 0.0, 0.025), "drewno", C, rot=(0, 90, 0), seg=6))
    join(P, "Telefon")
    S = []
    S.append(cyl("rura", 0.012, 0.15, (0, 0, 0.17), "bakelit", C, rot=(0, 90, 0), seg=8))
    for sx in (-1, 1):
        S.append(cyl("muszla", 0.028, 0.035, (sx * 0.085, 0, 0.155), "bakelit", C, seg=12, r2=0.02))
    join(S, "Sluchawka", origin=(0, 0, 0.15))
    report("OB_Telefon")


def skrzynka(C, w, d, h, name, tile="drewno", wieko=False):
    P = [box(name + "_k", (w, d, h), (0, 0, h / 2), tile, C, bevel=0.008)]
    # okucia i listwy
    for sx in (-1, 1):
        P.append(box("list", (0.04, d + 0.01, h + 0.005), (sx * (w / 2 - 0.05), 0, h / 2), "drewno_ciemne", C))
    P.append(box("pas", (w + 0.01, d + 0.01, 0.03), (0, 0, h * 0.5), "zeliwo", C))
    return P


def build_skrytka():
    C = new_collection("OB_Skrytka")
    w, d, h = 0.6, 0.4, 0.3
    P = [box("dno", (w, d, 0.03), (0, 0, 0.015), "drewno", C)]
    for sx in (-1, 1):
        P.append(box("sciana_x", (0.03, d, h), (sx * (w / 2 - 0.015), 0, h / 2), "drewno", C, bevel=0.004))
    for sy in (-1, 1):
        P.append(box("sciana_y", (w, 0.03, h), (0, sy * (d / 2 - 0.015), h / 2), "drewno", C, bevel=0.004))
    for sx in (-1, 1):
        P.append(box("okucie", (0.05, d + 0.01, 0.04), (sx * 0.2, 0, 0.05), "rdza", C))
    P.append(box("uchwyt", (0.1, 0.02, 0.02), (0, -d / 2 - 0.01, h * 0.6), "zeliwo", C))
    join(P, "Skrzynka")
    W = [box("wieko", (w + 0.02, d + 0.02, 0.035), (0, 0, h + 0.0175), "drewno_ciemne", C, bevel=0.004)]
    for sx in (-1, 1):
        W.append(box("okucie_w", (0.05, d + 0.03, 0.01), (sx * 0.2, 0, h + 0.038), "rdza", C))
    W.append(box("skobel", (0.04, 0.012, 0.06), (0, -d / 2 - 0.016, h - 0.01), "zeliwo", C))
    join(W, "Wieko", origin=(0, d / 2 + 0.01, h))
    empty("Wnetrze", (0, 0, 0.03), C)
    report("OB_Skrytka")


def build_amunicja():
    C = new_collection("OB_Amunicja")
    P = [box("pudelko", (0.1, 0.065, 0.04), (0, 0, 0.02), "zolty_karton", C, bevel=0.002),
         box("etykieta", (0.07, 0.066, 0.02), (0, 0, 0.022), "papier", C)]
    # magazynek obok
    P.append(box("magazynek", (0.022, 0.032, 0.11), (0.08, 0.0, 0.016), "oksyda", C, rot=(0, 90, 0), bevel=0.002))
    P.append(box("stopka", (0.024, 0.036, 0.008), (0.135, 0.0, 0.016), "stal", C, rot=(0, 90, 0)))
    for i in range(3):
        P.append(cyl("naboj", 0.0045, 0.03, (-0.03 + i * 0.012, -0.05, 0.0045), "mosiadz", C, rot=(0, 90, 70 + i * 20), seg=6))
    join(P, "Amunicja")
    report("OB_Amunicja")


def build_mikrofon():
    C = new_collection("OB_Mikrofon_Burzowy")
    P = []
    for i in range(3):   # statyw trojnog
        a = math.radians(90 + i * 120)
        P.append(rod("noga", (0.34 * math.cos(a), 0.34 * math.sin(a), 0.0), (0.02 * math.cos(a), 0.02 * math.sin(a), 0.9), 0.012, "zeliwo", C))
    P.append(cyl("kolumna", 0.016, 1.05, (0, 0, 0.98), "chrom", C, seg=8))
    P.append(cyl("zlacze", 0.025, 0.06, (0, 0, 0.88), "zeliwo", C, seg=8))
    # glowica: pierscien w ramie (mikrofon wegiel. na sprezynach)
    P.append(torus("rama", 0.13, 0.012, (0, 0, 1.62), "chrom", C, rot=(90, 0, 0), seg=20, rseg=6))
    P.append(cyl("widelki", 0.01, 0.12, (0, 0, 1.48), "chrom", C, seg=6))
    P.append(cyl("kapsula", 0.085, 0.06, (0, 0, 1.62), "zeliwo", C, rot=(90, 0, 0), seg=16))
    P.append(cyl("siatka", 0.075, 0.064, (0, 0, 1.62), "stal", C, rot=(90, 0, 0), seg=16))
    for k in range(4):   # sprezyny
        a = math.radians(45 + k * 90)
        P.append(cyl("sprezyna", 0.005, 0.05, (0.105 * math.cos(a), 0, 1.62 + 0.105 * math.sin(a)), "mosiadz", C,
                     rot=(0, 90 - math.degrees(a), 0), seg=6))
    # skrzynka sterujaca + kabel
    P.append(box("puszka", (0.12, 0.08, 0.1), (0, 0.03, 1.2), "zielen_wojsk", C, bevel=0.006))
    P.append(cyl("kabel", 0.008, 0.6, (0.0, 0.08, 0.9), "kabel_czarny", C, rot=(10, 0, 0), seg=6))
    P.append(torus("zwoj", 0.15, 0.01, (0.25, 0.25, 0.012), "kabel_czarny", C, seg=16, rseg=5))
    P.append(torus("zwoj2", 0.12, 0.01, (0.27, 0.23, 0.03), "kabel_czarny", C, seg=16, rseg=5))
    join(P, "Mikrofon")
    lamp = sphere("Lampka", 0.015, (0, 0, 0), "czerwony", C, seg=10, rings=6,
                  mat=mat_emis("M_Lampka_Czerwona", (1.0, 0.05, 0.02), 6))
    lamp.location = (0, -0.012, 1.23)
    empty("CelZdjecia", (0, 0, 1.62), C)
    report("OB_Mikrofon_Burzowy")


def zegar(P, C, x, y, z, r, rot=(90, 0, 0)):
    P.append(cyl("zeg_o", r, 0.02, (x, y, z), "mosiadz", C, rot=rot, seg=16))
    P.append(cyl("zeg_t", r * 0.85, 0.022, (x, y, z), "tarcza", C, rot=rot, seg=16))


def build_pulpit():
    C = new_collection("OB_Pulpit")
    W, D = 1.6, 0.75
    P = [box("szafka", (W, D, 0.75), (0, 0, 0.375), "panel_szary", C, bevel=0.01),
         box("cokol", (W - 0.06, D - 0.06, 0.08), (0, 0, 0.04), "oksyda", C)]
    # skosny panel
    P.append(box("skos", (W, 0.5, 0.05), (0, 0.05, 0.86), "panel_zielony", C, rot=(25, 0, 0), bevel=0.006))
    P.append(box("wypelnienie", (W - 0.01, 0.23, 0.12), (0, 0.165, 0.8), "panel_szary", C))
    P.append(box("nadstawka", (W, 0.2, 0.35), (0, 0.3, 0.95), "panel_szary", C, bevel=0.008))
    ang = math.radians(25)
    def on_slope(x, t, h=0.03):
        # t: 0..1 wzdluz skosu (od przodu)
        yy = 0.05 + (t - 0.5) * 0.5 * math.cos(ang)
        zz = 0.86 + (t - 0.5) * 0.5 * math.sin(ang) + h * math.cos(ang)
        yy -= h * math.sin(ang)
        return (x, yy, zz)
    for i in range(5):
        x = -0.6 + i * 0.3
        p = on_slope(x, 0.7, 0.02)
        zegar(P, C, p[0], p[1], p[2], 0.06, rot=(25, 0, 0))
        q = on_slope(x, 0.25, 0.03)
        P.append(cyl("pokretlo", 0.025, 0.035, q, "bakelit", C, rot=(25, 0, 0), seg=10))
    for i in range(8):
        p = on_slope(-0.7 + i * 0.2, 0.05, 0.02)
        P.append(box("przel", (0.02, 0.04, 0.02), p, "chrom", C, rot=(25 + 30 * (i % 2), 0, 0)))
    for i in range(3):   # zegary na nadstawce
        zegar(P, C, -0.5 + i * 0.5, 0.195, 1.0, 0.08, rot=(90, 0, 0))
    join(P, "Pulpit")
    lam = []
    for i in range(4):
        lam.append(sphere("lam", 0.014, (0.25 + i * 0.1, 0.2, 1.08), "czerwony", C, seg=8, rings=5,
                          mat=mat_emis("M_Lampa_Radio", (1.0, 0.7, 0.3), 3)))
    join(lam, "Lampki")
    empty("PunktInterakcji", (0, -0.8, 0), C)
    report("OB_Pulpit")


def build_szafa_aparatura():
    C = new_collection("OB_Szafa_Aparatura")
    W, D, H = 0.8, 0.6, 2.0
    P = [box("szafa", (W, D, H), (0, 0, H / 2), "panel_szary", C, bevel=0.012),
         box("cokol", (W - 0.04, D - 0.04, 0.08), (0, 0, 0.04), "oksyda", C)]
    F = -D / 2
    # okno z lampami
    P.append(box("rama_okna", (0.62, 0.02, 0.6), (0, F - 0.005, 1.5), "oksyda", C))
    P.append(box("tlo", (0.56, 0.01, 0.54), (0, F - 0.012, 1.5), "szklo_ciemne", C))
    for i in range(4):
        x = -0.2 + i * 0.133
        P.append(cyl("cokol_l", 0.03, 0.04, (x, F - 0.08, 1.27), "bakelit", C, seg=8))
    zegar(P, C, -0.18, F - 0.01, 1.0, 0.07)
    zegar(P, C, 0.18, F - 0.01, 1.0, 0.07)
    for i in range(3):
        P.append(cyl("pokr", 0.03, 0.035, (-0.2 + i * 0.2, F - 0.015, 0.82), "bakelit", C, rot=(90, 0, 0), seg=10))
    for j in range(8):
        P.append(box("went", (0.5, 0.012, 0.015), (0, F - 0.006, 0.2 + j * 0.05), "oksyda", C))
    P.append(box("tabl", (0.25, 0.006, 0.05), (0, F - 0.006, 1.86), "mosiadz", C))
    join(P, "Szafa")
    L = []
    for i in range(4):
        x = -0.2 + i * 0.133
        L.append(cyl("banka", 0.028, 0.16, (x, F - 0.08, 1.37), "kremowy", C, seg=10, r2=0.022,
                     mat=mat_emis("M_Lampa_Radio", (1.0, 0.7, 0.3), 3)))
        L.append(sphere("szczyt", 0.022, (x, F - 0.08, 1.45), "kremowy", C, seg=10, rings=6,
                        mat=mat_emis("M_Lampa_Radio", (1.0, 0.7, 0.3), 3)))
    join(L, "Lampy")
    report("OB_Szafa_Aparatura")


def build_latarnia():
    C = new_collection("OB_Latarnia")
    P = [cyl("podstawa", 0.2, 0.35, (0, 0, 0.175), "zeliwo", C, seg=8, r2=0.14),
         cyl("pierscien", 0.16, 0.06, (0, 0, 0.38), "zeliwo", C, seg=8),
         cyl("slup", 0.075, 3.05, (0, 0, 1.9), "zeliwo", C, seg=8, r2=0.05),
         torus("obr", 0.07, 0.015, (0, 0, 0.9), "zeliwo", C, seg=8, rseg=4),
         cyl("glowica", 0.07, 0.12, (0, 0, 3.47), "zeliwo", C, seg=8, r2=0.1),
         box("daszek_p", (0.42, 0.42, 0.04), (0, 0, 3.98), "zeliwo", C, bevel=0.008),
         cyl("daszek", 0.3, 0.14, (0, 0, 4.07), "zeliwo", C, seg=4, r2=0.05, rot=(0, 0, 45)),
         sphere("kula", 0.04, (0, 0, 4.18), "zeliwo", C, seg=8, rings=5),
         cyl("dno_lat", 0.13, 0.05, (0, 0, 3.55), "zeliwo", C, seg=4, r2=0.2, rot=(0, 0, 45))]
    for sx in (-1, 1):
        for sy in (-1, 1):
            P.append(box("pret", (0.02, 0.02, 0.4), (sx * 0.16, sy * 0.16, 3.77), "zeliwo", C))
    join(P, "Slup")
    k = cyl("Klosz", 0.2, 0.38, (0, 0, 3.77), "kremowy", C, seg=4, r2=0.24, rot=(0, 0, 45),
            mat=mat_emis("M_Klosz", (1.0, 0.8, 0.45), 3), smooth=False)
    report("OB_Latarnia")


def kolo(C, name, x, y, r=0.36, w=0.18):
    P = [cyl("opona", r, w, (x, y, r), "guma", C, rot=(0, 90, 0), seg=16),
         cyl("felga", r * 0.62, w + 0.012, (x, y, r), "lakier_czarny", C, rot=(0, 90, 0), seg=16),
         cyl("kolpak", r * 0.3, w + 0.03, (x, y, r), "chrom", C, rot=(0, 90, 0), seg=12)]
    # biala opaska opony + 5 okraglych srub (widac obrot, ksztalt neutralny)
    P.append(cyl("opaska", r * 0.8, w + 0.004, (x, y, r), "kabel_bialy", C, rot=(0, 90, 0), seg=16))
    P.append(cyl("felga2", r * 0.68, w + 0.008, (x, y, r), "lakier_czarny", C, rot=(0, 90, 0), seg=16))
    for k in range(5):
        a = 2 * math.pi * k / 5
        P.append(cyl("sruba", 0.018, w + 0.035, (x, y + 0.13 * math.cos(a), r + 0.13 * math.sin(a)), "chrom", C, rot=(0, 90, 0), seg=6))
    return join(P, name, origin=(x, y, r))


def blotnik(C, P, x, y, r, w, zc, tile="lakier_czarny"):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=18, radius1=r, radius2=r, depth=w)
    bmesh.ops.rotate(bm, cent=(0, 0, 0), matrix=Matrix(_rot((0, 90, 0))), verts=bm.verts)
    bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=(0, 0, 0),
                           plane_no=(0, 0, 1), clear_inner=True)
    bm.normal_update()
    bmesh.ops.translate(bm, vec=(x, y, zc), verts=bm.verts)
    P.append(_finish("blotnik", bm, C, atlas_mat(), tile))


def build_auto():
    C = new_collection("OB_Auto_SD")
    yF, yR, r = -1.45, 1.35, 0.36
    xw = 0.74
    B = []
    B.append(box("podwozie", (1.25, 3.9, 0.2), (0, -0.05, 0.42), "oksyda", C))
    for sx in (-1, 1):
        B.append(box("stopien", (0.22, 1.8, 0.04), (sx * 0.84, -0.05, 0.42), "guma", C))
    B.append(box("dol", (1.48, 2.7, 0.56), (0, 0.6, 0.78), "lakier_czarny", C, bevel=0.03))
    B.append(box("maska", (0.96, 1.35, 0.5), (0, -1.28, 0.84), "lakier_czarny", C, bevel=0.04))
    B.append(box("maska_g", (0.8, 1.35, 0.1), (0, -1.28, 1.12), "lakier_czarny", C, bevel=0.04))
    for k in range(6):
        for sx in (-1, 1):
            B.append(box("zaluzja", (0.01, 0.02, 0.22), (sx * 0.485, -1.55 + k * 0.08, 0.86), "chrom", C))
    B.append(box("atrapa", (0.7, 0.06, 0.62), (0, -1.96, 0.83), "chrom", C, bevel=0.02))
    for k in range(7):
        B.append(box("zebro", (0.025, 0.07, 0.54), (-0.27 + k * 0.09, -1.97, 0.83), "oksyda", C))
    B.append(box("kabina", (1.38, 1.85, 0.56), (0, 0.55, 1.33), "lakier_czarny", C, bevel=0.07))
    B.append(box("dach", (1.3, 1.7, 0.08), (0, 0.6, 1.62), "lakier_czarny", C, bevel=0.04))
    # szyby
    B.append(box("szyba_przod", (1.22, 0.04, 0.36), (0, -0.37, 1.34), "szklo_ciemne", C, rot=(-12, 0, 0)))
    B.append(box("szyby_bok_p", (1.395, 1.55, 0.32), (0, 0.65, 1.36), "szklo_ciemne", C))
    B.append(box("szyba_tyl", (0.9, 0.04, 0.25), (0, 1.48, 1.36), "szklo_ciemne", C, rot=(15, 0, 0)))
    B.append(box("slupek", (1.40, 0.06, 0.34), (0, 0.6, 1.36), "lakier_czarny", C))
    B.append(box("bagaznik", (1.36, 0.7, 0.6), (0, 1.8, 0.86), "lakier_czarny", C, rot=(22, 0, 0), bevel=0.06))
    # linie drzwi (prawa strona + tylne lewe)
    for (sx, y0) in ((-1, -0.35), (-1, 0.62), (1, 0.62)):
        B.append(box("linia", (0.01, 0.02, 0.85), (sx * 0.745, y0, 1.08), "oksyda", C))
    for sx in (-1, 1):
        B.append(box("klamka", (0.03, 0.08, 0.02), (sx * 0.755, 1.0, 1.05), "chrom", C))
    # blotniki
    for sx in (-1, 1):
        blotnik(C, B, sx * xw, yF, r + 0.1, 0.26, r)
        blotnik(C, B, sx * xw, yR, r + 0.1, 0.26, r)
        B.append(box("blot_p", (0.26, 0.5, 0.12), (sx * xw, yF - 0.5, 0.58), "lakier_czarny", C, rot=(25, 0, 0), bevel=0.03))
        B.append(box("blot_t", (0.26, 0.4, 0.12), (sx * xw, yR + 0.45, 0.6), "lakier_czarny", C, rot=(-25, 0, 0), bevel=0.03))
        # reflektory: obudowa na wsporniku
        B.append(cyl("wspornik", 0.015, 0.25, (sx * 0.5, -1.82, 0.95), "chrom", C, seg=6))
        B.append(cyl("obudowa", 0.11, 0.18, (sx * 0.5, -1.86, 1.1), "chrom", C, rot=(90, 0, 0), seg=14, r2=0.08))
        B.append(box("tylne_sw", (0.08, 0.03, 0.06), (sx * 0.6, 2.2, 0.95), "czerwony", C))
    for yb in (-2.1, 2.2):
        B.append(box("zderzak", (1.75, 0.08, 0.1), (0, yb, 0.5), "chrom", C, bevel=0.02))
    B.append(cyl("zapas", 0.34, 0.16, (0, 2.18, 0.98), "guma", C, rot=(70, 0, 0), seg=16))
    B.append(cyl("zapas_kol", 0.2, 0.18, (0, 2.18, 0.98), "lakier_czarny", C, rot=(70, 0, 0), seg=16))
    join(B, "Karoseria")
    # drzwi kierowcy: lewa strona (Blender +X = Unity -X), zawias z przodu
    D = [box("drzwi", (0.04, 0.95, 0.56), (0.75, 0.13, 0.82), "lakier_czarny", C, bevel=0.01),
         box("rama", (0.04, 0.95, 0.06), (0.75, 0.13, 1.53), "lakier_czarny", C),
         box("szyba", (0.035, 0.85, 0.32), (0.75, 0.13, 1.36), "szklo_ciemne", C),
         box("klamka_k", (0.04, 0.08, 0.02), (0.775, 0.5, 1.05), "chrom", C)]
    join(D, "Drzwi_Kierowcy", origin=(0.75, -0.34, 0.5))
    R = []
    for sx in (-1, 1):
        R.append(cyl("szklo", 0.1, 0.02, (sx * 0.5, -1.955, 1.1), "kremowy", C, rot=(90, 0, 0), seg=14,
                     mat=mat_emis("M_Reflektor", (1.0, 0.92, 0.7), 3)))
    join(R, "Reflektory")
    for nm, sx, y in (("Kolo_PP", -1, yF), ("Kolo_LP", 1, yF), ("Kolo_PT", -1, yR), ("Kolo_LT", 1, yR)):
        kolo(C, nm, sx * xw, y, r)
    empty("Swiatla_L", (0.5, -1.97, 1.1), C)
    empty("Swiatla_P", (-0.5, -1.97, 1.1), C)
    empty("Siedzenie_Kierowcy", (0.38, 0.2, 0.75), C)
    report("OB_Auto_SD")


def build_beczka():
    C = new_collection("OB_Beczka")
    P = [cyl("dol", 0.27, 0.48, (0, 0, 0.24), "drewno", C, seg=14, r2=0.32),
         cyl("gora", 0.32, 0.48, (0, 0, 0.72), "drewno", C, seg=14, r2=0.27),
         cyl("denko", 0.26, 0.02, (0, 0, 0.955), "drewno_ciemne", C, seg=14)]
    for z, rr in ((0.08, 0.28), (0.3, 0.315), (0.66, 0.315), (0.88, 0.28)):
        P.append(torus("obrecz", rr, 0.012, (0, 0, z), "zeliwo", C, seg=14, rseg=4))
    join(P, "Beczka")
    report("OB_Beczka")


def build_skrzynia():
    C = new_collection("OB_Skrzynia")
    w, d, h = 1.0, 0.75, 0.95
    P = [box("korpus", (w - 0.02, d - 0.02, h - 0.02), (0, 0, h / 2), "drewno_jasne", C)]
    # ramy na krawedziach kazdej sciany
    for sx in (-1, 1):
        for sy in (-1, 1):
            P.append(box("kant", (0.08, 0.08, h), (sx * (w / 2 - 0.04), sy * (d / 2 - 0.04), h / 2), "drewno", C, bevel=0.006))
    for z in (0.04, h - 0.04):
        for sy in (-1, 1):
            P.append(box("pozioma", (w, 0.08, 0.08), (0, sy * (d / 2 - 0.04), z), "drewno", C, bevel=0.006))
        for sx in (-1, 1):
            P.append(box("pozioma2", (0.08, d, 0.08), (sx * (w / 2 - 0.04), 0, z), "drewno", C, bevel=0.006))
    for sy in (-1, 1):   # krzyzulec
        a = math.degrees(math.atan2(h - 0.16, w - 0.16))
        P.append(box("skos", (math.hypot(w - 0.16, h - 0.16), 0.06, 0.08), (0, sy * (d / 2 - 0.02), h / 2), "drewno", C, rot=(0, -a, 0)))
    join(P, "Skrzynia")
    report("OB_Skrzynia")


def build_worki():
    C = new_collection("OB_Worki")
    P = []
    rows = 6
    for rz in range(rows):
        n = 3 if rz % 2 == 0 else 2
        L = 0.48
        for i in range(n):
            x = (i - (n - 1) / 2) * (L - 0.02)
            P.append(sphere("worek", 1.0, (x, 0, 0.09 + rz * 0.165), "worek", C, seg=10, rings=6,
                            scale=(L / 2, 0.24, 0.1)))
    join(P, "Worki")
    report("OB_Worki")


build_aparat(); build_mapa_stol(); build_lista(); build_kartka(); build_telefon()
build_skrytka(); build_amunicja(); build_mikrofon(); build_pulpit(); build_szafa_aparatura()
build_latarnia(); build_auto(); build_beczka(); build_skrzynia(); build_worki()
