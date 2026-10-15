# Priorytet 1: OB_Karta_Historii, OB_Pistolet_Vis, OB_Lacznica
import numpy as np

# ============================================================ OB_Karta_Historii
def pocztowka_tex():
    W, H = 512, 256          # lewa polowa = przod (zdjecie), prawa = tyl (pismo)
    rng = np.random.default_rng(7)
    yy, xx = np.mgrid[0:H, 0:W]
    img = np.zeros((H, W, 3))
    paper = np.array((0.86, 0.77, 0.56))
    img[:] = paper * (0.95 + 0.05 * rng.random((H, W, 1)))
    # --- przod: sepiowe zdjecie z ramka
    x0, x1, y0, y1 = 18, 238, 18, 238
    ph = img[y0:y1, x0:x1]
    h, w = ph.shape[:2]
    v = np.linspace(0, 1, h)[:, None]
    u = np.linspace(0, 1, w)[None, :]
    sky = 0.75 - 0.25 * v
    ground = 0.35 + 0.08 * np.sin(u * 30) * 0.2
    horizon = 0.72 + 0.03 * np.sin(u * 9)
    lum = np.where(v < horizon, sky, ground + 0.05 * rng.random((h, w)))
    # wieza radiostacji (smukly trojkat + kratownica)
    cx = 0.62
    half = 0.07 * (v - 0.08) / (horizon - 0.08)
    tower = (v > 0.08) & (v < horizon) & (np.abs(u - cx) < half + 0.004)
    lat = (np.abs(((v * 40) % 1) - 0.5) < 0.12) | (np.abs(u - cx) < 0.004)
    edge = tower & ((np.abs(np.abs(u - cx) - half) < 0.006) | lat)
    lum = np.where(edge, 0.12, lum)
    # domy na horyzoncie
    for hx, hw, hh in ((0.1, 0.12, 0.08), (0.25, 0.09, 0.12), (0.38, 0.1, 0.06), (0.8, 0.14, 0.1)):
        m = (u > hx) & (u < hx + hw) & (v > horizon - hh) & (v < horizon + 0.01)
        lum = np.where(m, 0.22, lum)
    vign = 1 - 0.5 * ((u - .5) ** 2 + (v - .5) ** 2)
    lum = lum * vign
    sepia = np.stack([lum * 1.07, lum * 0.86, lum * 0.62], -1)
    img[y0:y1, x0:x1] = np.clip(sepia, 0, 1)
    # --- tyl: linia podzialu, znaczek, "pismo"
    img[20:236, 383:385] = (0.45, 0.35, 0.25)
    img[24:70, 440:490] = (0.55, 0.20, 0.15)            # znaczek (bez symboli)
    img[30:64, 446:484] = (0.80, 0.70, 0.50)
    for row in range(6):
        y = 60 + row * 30
        x = 270
        while x < 375:
            L = rng.integers(8, 26)
            img[y:y + 3, x:min(x + L, 375)] = (0.18, 0.15, 0.28)
            x += L + rng.integers(4, 9)
    for row in range(4):
        y = 120 + row * 26
        img[y:y + 2, 395:495] = (0.45, 0.38, 0.30)
    return save_image("T_Pocztowka", img)


def build_karta():
    C = new_collection("OB_Karta_Historii")
    mat = mat_image("M_Pocztowka", pocztowka_tex(), rough=0.9)
    w, t, h = 0.2, 0.004, 0.125
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1)
    bmesh.ops.scale(bm, vec=(w, t, h), verts=bm.verts)
    bm.normal_update()
    uv = bm.loops.layers.uv.verify()
    for f in bm.faces:
        for l in f.loops:
            p = l.vert.co
            u = p.x / w + 0.5; v = p.z / h + 0.5
            if f.normal.y < -0.5:      # przod -> lewa polowa
                l[uv].uv = (u * 0.5, v)
            elif f.normal.y > 0.5:     # tyl -> prawa polowa (lustro)
                l[uv].uv = (0.5 + (1 - u) * 0.5, v)
            else:
                l[uv].uv = (0.01, 0.01)
    bmesh.ops.translate(bm, vec=(0, 0, 1.0), verts=bm.verts)
    me = bpy.data.meshes.new("Karta"); bm.to_mesh(me); bm.free()
    me.materials.append(mat)
    o = bpy.data.objects.new("Karta", me); C.objects.link(o)
    # pivot karty w jej srodku (obrot w miejscu)
    o.data.transform(Matrix.Translation((0, 0, -1.0))); o.location = (0, 0, 1.0)
    empty("PunktInterakcji", (0, 0, 1.0), C)
    report("OB_Karta_Historii")


# ============================================================ OB_Pistolet_Vis
def build_vis():
    C = new_collection("OB_Pistolet_Vis")
    P = []
    P.append(box("zamek", (0.028, 0.205, 0.032), (0, -0.065, 0.06), "oksyda", C, bevel=0.003))
    for i in range(6):   # nacięcia zamka
        for sx in (-1, 1):
            P.append(box("nac", (0.002, 0.003, 0.024), (sx * 0.0141, 0.012 + i * 0.005, 0.06), "zeliwo", C))
    P.append(box("szkielet", (0.026, 0.16, 0.022), (0, -0.07, 0.034), "oksyda", C, bevel=0.002))
    P.append(box("chwyt", (0.028, 0.05, 0.115), (0, 0.012, -0.012), "oksyda", C, rot=(16, 0, 0), bevel=0.003))
    for sx in (-1, 1):
        P.append(box("okladzina", (0.004, 0.042, 0.09), (sx * 0.016, 0.012, -0.012), "bakelit", C, rot=(16, 0, 0), bevel=0.0015))
    P.append(box("stopka", (0.03, 0.048, 0.008), (0, 0.027, -0.068), "oksyda", C, rot=(16, 0, 0)))
    P.append(box("kablak_d", (0.008, 0.05, 0.006), (0, -0.05, 0.003), "oksyda", C))
    P.append(box("kablak_p", (0.008, 0.006, 0.03), (0, -0.073, 0.017), "oksyda", C, rot=(-10, 0, 0)))
    P.append(box("spust", (0.006, 0.006, 0.02), (0, -0.04, 0.016), "stal", C, rot=(15, 0, 0)))
    P.append(cyl("wylot", 0.0085, 0.006, (0, -0.1685, 0.065), "stal", C, rot=(90, 0, 0), seg=10))
    P.append(cyl("otwor", 0.0045, 0.004, (0, -0.172, 0.065), "guma", C, rot=(90, 0, 0), seg=8))
    P.append(box("muszka", (0.003, 0.006, 0.006), (0, -0.16, 0.078), "oksyda", C))
    P.append(box("szczerbinka", (0.016, 0.006, 0.006), (0, 0.03, 0.078), "oksyda", C))
    P.append(box("kurek", (0.008, 0.014, 0.018), (0, 0.044, 0.07), "stal", C, rot=(-30, 0, 0)))
    P.append(box("zwalniacz", (0.004, 0.02, 0.008), (0.015, -0.03, 0.045), "stal", C))
    join(P, "Vis", origin=(0, 0, 0))
    empty("Lufa", (0, -0.172, 0.065), C, size=0.03)
    empty("Uchwyt", (0, 0.012, -0.012), C, size=0.03)
    report("OB_Pistolet_Vis")


# ============================================================ OB_Lacznica
def build_lacznica():
    C = new_collection("OB_Lacznica")
    W, D, H = 1.2, 0.25, 1.5
    F = -D  # front y
    K = []
    K.append(box("pudlo", (W, D, H), (0, -D / 2, H / 2), "stal", C, bevel=0.015, seg=3))
    K.append(box("lico", (W - 0.1, 0.01, H - 0.12), (0, F - 0.004, H / 2), "panel_zielony", C, bevel=0.004, seg=2))
    for (x, z) in ((-0.55, 0.06), (0.55, 0.06), (-0.55, 1.44), (0.55, 1.44)):
        K.append(cyl("sruba", 0.012, 0.01, (x, F - 0.01, z), "mosiadz", C, rot=(90, 0, 0), seg=8))
    # zegary
    for i, x in enumerate((-0.35, 0.0, 0.35)):
        K.append(cyl("zeg_obr", 0.095, 0.035, (x, F - 0.02, 1.22), "mosiadz", C, rot=(90, 0, 0), seg=20))
        K.append(cyl("zeg_tarcza", 0.08, 0.01, (x, F - 0.036, 1.22), "tarcza", C, rot=(90, 0, 0), seg=20))
        K.append(box("wskazowka", (0.006, 0.003, 0.07), (x - 0.012 + i * 0.012, F - 0.043, 1.24), "oksyda", C, rot=(0, -25 + i * 25, 0)))
        K.append(box("tabliczka", (0.12, 0.006, 0.03), (x, F - 0.012, 1.06), "kremowy", C))
    # przelaczniki dzwigniowe
    for i in range(5):
        x = -0.4 + i * 0.2
        K.append(cyl("prz_baza", 0.025, 0.02, (x, F - 0.014, 0.95), "mosiadz", C, rot=(90, 0, 0), seg=10))
        K.append(cyl("prz_dzw", 0.006, 0.07, (x, F - 0.05, 0.97), "chrom", C, rot=(60, 0, 0), seg=6))
        K.append(sphere("prz_galka", 0.011, (x, F - 0.075, 0.99), "bakelit", C, seg=8, rings=5))
    # listwy zaciskowe
    for z in (0.83, 0.24):
        K.append(box("listwa", (0.62, 0.05, 0.07), (0, F - 0.025, z), "bakelit", C, bevel=0.005))
        for x in (-0.15, 0.0, 0.15):
            K.append(cyl("zacisk", 0.02, 0.03, (x, F - 0.055, z), "mosiadz", C, rot=(90, 0, 0), seg=8))
    # wylacznik nozowy z boku
    K.append(box("noz_baza", (0.14, 0.03, 0.22), (0.45, F - 0.015, 0.55), "bakelit", C, bevel=0.005))
    K.append(box("noz", (0.05, 0.015, 0.16), (0.45, F - 0.06, 0.6), "miedz", C, rot=(-30, 0, 0)))
    K.append(cyl("noz_raczka", 0.012, 0.06, (0.45, F - 0.11, 0.67), "bakelit", C, rot=(0, 90, 0), seg=8))
    # kratka wentylacyjna z lewej
    for j in range(6):
        K.append(box("szczelina", (0.16, 0.012, 0.012), (-0.45, F - 0.012, 0.35 + j * 0.06), "oksyda", C))
    # oprawka lampki
    K.append(cyl("lampka_opr", 0.04, 0.03, (0.47, F - 0.015, 1.4), "chrom", C, rot=(90, 0, 0), seg=12))
    join(K, "Korpus")

    lamp = sphere("Lampka", 0.028, (0, 0, 0), "czerwony", C, seg=12, rings=8,
                  mat=mat_emis("M_Lampka_Czerwona", (1.0, 0.05, 0.02), 6))
    lamp.location = (0.47, F - 0.035, 1.4)

    # kable
    cols = ("kabel_czerwony", "kabel_czarny", "kabel_bialy")
    ztop, zbot, r = 0.80, 0.27, 0.018
    yk = F - 0.075
    for i, x in enumerate((-0.15, 0.0, 0.15)):
        zm = (ztop + zbot) / 2
        root = empty(f"Kabel_{i+1}", (x, yk, zm), C, size=0.06)
        # gorna polowa: pivot u gory
        g = []
        g.append(cyl("k", r, ztop - zm - 0.006, (x, yk, (ztop + zm + 0.006) / 2), cols[i], C, seg=10))
        g.append(cyl("cu", r * 0.55, 0.02, (x, yk, zm + 0.004), "miedz", C, seg=8))
        g.append(cyl("koncowka", r * 1.4, 0.04, (x, yk, ztop), "mosiadz", C, seg=10))
        top = join(g, f"Kabel_{i+1}_Gora", origin=(x, yk, ztop))
        d = []
        d.append(cyl("k", r, zm - zbot - 0.006, (x, yk, (zm - 0.006 + zbot) / 2), cols[i], C, seg=10))
        d.append(cyl("cu", r * 0.55, 0.02, (x, yk, zm - 0.004), "miedz", C, seg=8))
        d.append(cyl("koncowka", r * 1.4, 0.04, (x, yk, zbot), "mosiadz", C, seg=10))
        bot = join(d, f"Kabel_{i+1}_Dol", origin=(x, yk, zbot))
        set_parent(top, root); set_parent(bot, root)
        empty(f"FX_Iskra_{i+1}", (x, yk - 0.01, zm), C, size=0.04)
    empty("PunktInterakcji", (0, -0.9, 0), C)
    report("OB_Lacznica")


build_karta(); build_vis(); build_lacznica()
