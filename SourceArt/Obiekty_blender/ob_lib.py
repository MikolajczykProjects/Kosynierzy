# Biblioteka do budowania obiektow OB_* w Blenderze (rola D).
# Uzycie w Blenderze:  exec(open(r"...\ob_lib.py", encoding="utf-8").read())
# Konwencje: 1 j = 1 m, pivot na dole, PRZOD MODELU = -Y w Blenderze (= +Z w Unity po eksporcie).
import bpy, bmesh, math, os
import numpy as np
from mathutils import Vector, Matrix

ROOT = r"C:\Users\borys\Desktop\HACKATON\paczka\_Obiekty"
DIR_MODELE = os.path.join(ROOT, "Modele")
DIR_TEX = os.path.join(ROOT, "Materialy")
for _d in (DIR_MODELE, DIR_TEX):
    os.makedirs(_d, exist_ok=True)

# ---------------------------------------------------------------- atlas
N = 8          # 8x8 kafli
TS = 256       # px na kafel
ATLAS_PX = N * TS

# nazwa: (indeks, kolor bazowy sRGB, rodzaj)
TILES = {
    "drewno_ciemne": (0, (0.42, 0.27, 0.16), "wood"),
    "drewno":        (1, (0.62, 0.42, 0.25), "wood"),
    "drewno_jasne":  (2, (0.78, 0.60, 0.40), "wood"),
    "drewno_drzwi":  (3, (0.50, 0.31, 0.17), "wood"),
    "oksyda":        (4, (0.20, 0.20, 0.21), "metal"),
    "stal":          (5, (0.60, 0.61, 0.62), "metal"),
    "mosiadz":       (6, (0.80, 0.64, 0.34), "metal"),
    "lakier_czarny": (7, (0.08, 0.08, 0.09), "metal"),
    "papier_stary":  (8, (0.85, 0.76, 0.55), "paper"),
    "papier":        (9, (0.92, 0.90, 0.84), "paper"),
    "czerwony":      (10, (0.70, 0.12, 0.08), "plain"),
    "kabel_czarny":  (11, (0.09, 0.09, 0.09), "fabric"),
    "kabel_bialy":   (12, (0.86, 0.85, 0.80), "fabric"),
    "panel_szary":   (13, (0.55, 0.57, 0.55), "metal"),
    "panel_zielony": (14, (0.42, 0.48, 0.40), "metal"),
    "bakelit":       (15, (0.17, 0.10, 0.07), "plain"),
    "tarcza":        (16, (0.90, 0.86, 0.72), "dial"),
    "guma":          (17, (0.11, 0.11, 0.11), "plain"),
    "skora":         (18, (0.40, 0.24, 0.13), "fabric"),
    "plandeka":      (19, (0.46, 0.48, 0.33), "fabric"),
    "chrom":         (20, (0.86, 0.87, 0.88), "metal"),
    "szklo_ciemne":  (21, (0.22, 0.26, 0.29), "plain"),
    "miedz":         (22, (0.76, 0.46, 0.28), "metal"),
    "worek":         (23, (0.68, 0.58, 0.42), "fabric"),
    "rdza":          (24, (0.52, 0.28, 0.14), "metal"),
    "zielen_wojsk":  (25, (0.36, 0.40, 0.28), "metal"),
    "beton":         (26, (0.62, 0.61, 0.57), "plain"),
    "kabel_czerwony":(27, (0.70, 0.11, 0.07), "fabric"),
    "zeliwo":        (28, (0.26, 0.27, 0.26), "metal"),
    "tkanina_zielona":(29, (0.26, 0.42, 0.30), "fabric"),
    "film":          (30, (0.28, 0.19, 0.10), "plain"),
    "kremowy":       (31, (0.88, 0.82, 0.66), "plain"),
    "zolty_karton":  (32, (0.80, 0.62, 0.20), "plain"),
    "granat":        (33, (0.19, 0.25, 0.42), "fabric"),
    "szary_wojsk":   (34, (0.30, 0.32, 0.33), "metal"),
    "lakier_zielony":(35, (0.16, 0.30, 0.20), "metal"),
    "czerwien_kolej":(36, (0.55, 0.10, 0.07), "metal"),
    "stal_polerowana":(37, (0.72, 0.73, 0.75), "metal"),
    "dach_wagonu":   (38, (0.33, 0.33, 0.32), "plain"),
    "szklo_jasne":   (39, (0.30, 0.36, 0.40), "plain"),
    "wegiel":        (40, (0.06, 0.06, 0.06), "plain"),
    "grafit":        (41, (0.14, 0.14, 0.15), "metal"),
    "czerwien_hydrant":(42, (0.66, 0.11, 0.07), "metal"),
    "skora_cialo":   (43, (0.86, 0.66, 0.53), "plain"),
    "wlosy_ciemne":  (44, (0.13, 0.09, 0.06), "fabric"),
    "wlosy_jasne":   (45, (0.72, 0.58, 0.36), "fabric"),
    "wlosy_siwe":    (46, (0.66, 0.65, 0.63), "fabric"),
    "mundur_sd":     (47, (0.37, 0.40, 0.33), "fabric"),
    "mundur_policja":(48, (0.22, 0.33, 0.24), "fabric"),
    "plaszcz_czarny":(49, (0.10, 0.10, 0.11), "fabric"),
    "skora_czarna":  (50, (0.08, 0.08, 0.08), "fabric"),
    "garnitur":      (51, (0.30, 0.30, 0.33), "fabric"),
    "plaszcz_brazowy":(52, (0.40, 0.29, 0.19), "fabric"),
    "koszula":       (53, (0.90, 0.89, 0.86), "fabric"),
    "sukienka_czerwona":(54, (0.58, 0.16, 0.15), "fabric"),
    "sukienka_niebieska":(55, (0.26, 0.36, 0.56), "fabric"),
    "spodnie_szare": (56, (0.42, 0.42, 0.44), "fabric"),
    "oczy":          (57, (0.07, 0.06, 0.05), "plain"),
    "usta":          (58, (0.62, 0.32, 0.30), "plain"),
    "kitel":         (59, (0.78, 0.77, 0.72), "fabric"),
    "kamizelka":     (60, (0.30, 0.22, 0.16), "fabric"),
}


# (metalicznosc, gladkosc) kazdego kafla — trafia do T_Obiekty_Atlas_MS (R = metal, A = gladkosc, jak URP Lit)
MS = {
    "drewno_ciemne": (0, .18), "drewno": (0, .15), "drewno_jasne": (0, .12), "drewno_drzwi": (0, .35),
    "oksyda": (1, .68), "stal": (1, .74), "mosiadz": (1, .78), "lakier_czarny": (0, .82),
    "papier_stary": (0, .04), "papier": (0, .05), "czerwony": (0, .35), "kabel_czarny": (0, .25),
    "kabel_bialy": (0, .25), "panel_szary": (.85, .62), "panel_zielony": (.7, .58), "bakelit": (0, .65),
    "tarcza": (0, .3), "guma": (0, .12), "skora": (0, .35), "plandeka": (0, .06), "chrom": (1, .94),
    "szklo_ciemne": (0, .92), "miedz": (1, .72), "worek": (0, .04), "rdza": (.35, .12),
    "zielen_wojsk": (0, .42), "beton": (0, .05), "kabel_czerwony": (0, .25), "zeliwo": (.9, .48),
    "tkanina_zielona": (0, .06), "szary_wojsk": (0, .45), "lakier_zielony": (0, .72),
    "czerwien_kolej": (0, .62), "stal_polerowana": (1, .86), "dach_wagonu": (0, .2), "szklo_jasne": (0, .95),
    "wegiel": (0, .35), "grafit": (.8, .45), "czerwien_hydrant": (0, .55), "skora_cialo": (0, .3), "skora_czarna": (0, .55), "oczy": (0, .9), "usta": (0, .45), "film": (0, .55), "kremowy": (0, .3), "zolty_karton": (0, .1), "granat": (0, .06),
}
# lakierowana blacha: rysy odslaniaja stal pod farba
MALOWANE = {"lakier_czarny", "zielen_wojsk", "szary_wojsk", "lakier_zielony", "czerwien_kolej", "czerwien_hydrant"}
# metal z zaciekami rdzy
RDZEWIEJE = {"zeliwo", "rdza", "czerwien_hydrant"}
# sila wypuklosci normal mapy
SILA_N = {"wood": 4.0, "fabric": 3.0, "metal": 2.5, "paper": 1.2, "dial": 0.6, "plain": 1.2}


def _vnoise(rng, cells, size=TS):
    g = rng.random((cells + 1, cells + 1))
    x = np.linspace(0, cells, size, endpoint=False)
    i = x.astype(int); f = x - i
    f = f * f * (3 - 2 * f)
    a = g[i][:, i]; b = g[i + 1][:, i]; c = g[i][:, i + 1]; d = g[i + 1][:, i + 1]
    fy = f[:, None]; fx = f[None, :]
    return (a * (1 - fy) + b * fy) * (1 - fx) + (c * (1 - fy) + d * fy) * fx


def _vnoise_xy(rng, cx, cy, size=TS):
    g = rng.random((cy + 1, cx + 1))
    def ax(c):
        x = np.linspace(0, c, size, endpoint=False); i = x.astype(int); f = x - i
        return i, f * f * (3 - 2 * f)
    ix, fx = ax(cx); iy, fy = ax(cy)
    a = g[iy][:, ix]; b = g[iy][:, ix + 1]; c = g[iy + 1][:, ix]; d = g[iy + 1][:, ix + 1]
    fx = fx[None, :]; fy = fy[:, None]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def _rysy(rng, n, dl=(0.04, 0.25), size=TS):
    """Maska cienkich rys (0/1)."""
    m = np.zeros((size, size))
    for _ in range(n):
        x0, y0 = rng.random(2) * size
        a = rng.random() * math.pi
        L = rng.uniform(*dl) * size
        t = np.linspace(0, 1, int(L) + 2)
        xs = ((x0 + math.cos(a) * L * t) % size).astype(int)
        ys = ((y0 + math.sin(a) * L * t) % size).astype(int)
        m[ys, xs] = 1
    return m


def _zacieki(rng, size=TS):
    """Pionowe zacieki splywajace od gory kafla (0..1)."""
    col = rng.random(size)
    k = np.ones(9) / 9
    col = np.convolve(np.concatenate([col[-4:], col, col[:4]]), k, mode="valid")
    col = np.clip((col - 0.55) * 4, 0, 1)
    yy = np.linspace(0, 1, size)[:, None]          # 0 = dol, 1 = gora (wiersz 0 = dol)
    dl = rng.uniform(0.3, 0.9, size)[None, :]
    return col[None, :] * np.clip((yy - (1 - dl)) / dl, 0, 1) ** 0.7


def _tile(name, kind, base, seed):
    """Zwraca (rgb sRGB, wysokosc 0..1, metal, gladkosc) dla jednego kafla."""
    rng = np.random.default_rng(seed)
    base = np.array(base)
    n1 = _vnoise(rng, 4); n2 = _vnoise(rng, 16); n3 = _vnoise(rng, 48); fine = rng.random((TS, TS))
    yy, xx = np.mgrid[0:TS, 0:TS] / TS
    m0, s0 = MS.get(name, (0, .2))
    metal = np.full((TS, TS), float(m0)); glad = np.full((TS, TS), float(s0))
    if kind == "wood":
        # deski wzdluz v (pionowo), kazda z wlasnym odcieniem, wlokna, lukowe sloje (flader) i seki
        desek = 4
        u = xx * desek; nr = np.floor(u).astype(int); ul = u - nr
        odc = rng.uniform(0.88, 1.1, desek + 1)[nr]
        przes = rng.uniform(0, 10, desek + 1)[nr]
        srod = rng.uniform(0.3, 0.7, desek + 1)[nr]
        wl = _vnoise_xy(rng, 96, 3)                       # dlugie wlokna
        wl2 = _vnoise_xy(rng, 40, 6)
        d = np.abs(ul - srod) * 7.0 + 0.9 * np.sin((yy * 1.3 + przes) * math.pi) + yy * 1.5 + 0.25 * wl2
        sloje = 0.5 + 0.5 * np.sin(d * math.pi * 2.2)
        sloje = sloje ** 3                                 # waskie ciemne linie slojow
        jas = odc * (0.9 + 0.1 * (wl - .5) * 2.0 - 0.13 * sloje + 0.04 * (n2 - .5) + 0.02 * (fine - .5))
        h = 0.55 + 0.12 * (wl - .5) - 0.12 * sloje
        # seki
        for _ in range(int(rng.integers(1, 3))):
            kx, ky = rng.random(2); kr = rng.uniform(0.025, 0.05)
            dx = (xx - kx) / kr; dy = (yy - ky) / (kr * 1.8)
            r = np.sqrt(dx * dx + dy * dy)
            sek = np.clip(1.2 - r, 0, 1)
            pierscien = np.clip(1 - np.abs(np.sin(r * 5)) , 0, 1) * np.clip(2.5 - r, 0, 1) * 0.5
            jas = jas * (1 - 0.45 * sek - 0.1 * pierscien)
            h = h - 0.15 * sek
        szpara = (ul < 0.012) | (ul > 0.988)
        jas = np.where(szpara, jas * 0.45, jas)
        h = np.where(szpara, 0.0, h)
        glad = glad * (0.85 + 0.3 * (wl - .5)) - np.where(szpara, s0, 0)
        rgb = base[None, None, :] * jas[..., None]
    elif kind == "metal":
        brush = np.convolve(rng.random(TS), np.ones(3) / 3, mode="same")[None, :].repeat(TS, 0)
        jas = 0.94 + 0.07 * (n1 - .5) + 0.025 * (brush - .5) + 0.02 * (fine - .5)
        h = 0.5 + 0.03 * (n2 - .5) + 0.02 * (n3 - .5)
        rgb = base[None, None, :] * jas[..., None]
        glad = glad + 0.12 * (n1 - .5) - 0.1 * (n2 > 0.7)
        # realistycznie: lekki brud zbierajacy sie przy dole + plamy matowosci (odciski, kurz)
        brud = np.clip(0.35 - yy, 0, 1) * (0.5 + 0.5 * n2)
        rgb = rgb * (1 - 0.18 * brud[..., None])
        kurz = np.clip((n1 - 0.55) * 3, 0, 1) * 0.6 + 0.4 * np.clip((n3 - 0.6) * 3, 0, 1) * 0.3
        if name in MALOWANE:
            kurz = kurz * 0.3
        glad = glad * (1 - 0.35 * kurz) - 0.1 * brud
        if name in RDZEWIEJE:
            z = _zacieki(rng) * (0.6 + 0.4 * n2)
            rdza = np.array((0.42, 0.2, 0.08)) * (0.8 + 0.4 * n3)[..., None]
            w = np.clip(z * (0.9 if name == "rdza" else 0.35), 0, 1)[..., None]
            rgb = rgb * (1 - w) + rdza * w
            metal = metal * (1 - w[..., 0]); glad = glad * (1 - w[..., 0]) + 0.08 * w[..., 0]
            h = h + 0.15 * w[..., 0] * n3
    elif kind == "paper":
        wl = _vnoise(rng, 64)
        jas = 0.95 + 0.07 * (n1 - .5) + 0.04 * (n2 - .5) + 0.03 * (wl - .5)
        plama = _vnoise(rng, 5) > 0.78
        jas = np.where(plama, jas * 0.9, jas)
        h = 0.5 + 0.15 * (wl - .5) + 0.05 * (n2 - .5)
        rgb = base[None, None, :] * jas[..., None]
    elif kind == "fabric":
        okres = {"plandeka": 2.5, "worek": 3.0, "tkanina_zielona": 3.0}.get(name, 5.0)
        u = xx * TS / okres; v = yy * TS / okres
        wx = np.abs(np.sin(u * math.pi)); wy = np.abs(np.sin(v * math.pi))
        splot = np.where((np.floor(u) + np.floor(v)) % 2 == 0, wx, wy)
        jas = 0.9 + 0.07 * (splot - .5) + 0.08 * (n1 - .5) + 0.04 * (fine - .5)
        h = 0.4 + 0.35 * splot + 0.08 * (n2 - .5)
        if name in ("skora", "skora_czarna"):
            pory = _vnoise(rng, 40)
            jas = 0.9 + 0.1 * (pory - .5) + 0.06 * (n1 - .5)
            h = 0.5 + 0.3 * (pory - .5)
            glad = glad + 0.15 * (n1 - .5)
        rgb = base[None, None, :] * jas[..., None]
    elif kind == "dial":
        jas = 0.97 + 0.04 * (n1 - .5) + 0.02 * (fine - .5)
        h = np.full((TS, TS), 0.5)
        rgb = base[None, None, :] * jas[..., None]
        cx, cy = 0.5, 0.35
        ang = np.arctan2(yy - cy, xx - cx); r = np.hypot(xx - cx, yy - cy)
        ticks = (np.abs(((ang + math.pi) * 12 / math.pi) % 1 - .5) < 0.08) & (r > .3) & (r < .4) & (yy > cy)
        rgb[ticks] = (0.1, 0.08, 0.06)
    else:
        jas = 0.95 + 0.07 * (n1 - .5) + 0.04 * (n2 - .5) + 0.02 * (fine - .5)
        h = 0.5 + 0.06 * (n2 - .5) + 0.03 * (fine - .5)
        if name == "guma":
            h = 0.5 + 0.2 * (n3 - .5)
        if name in ("bakelit", "szklo_ciemne", "czerwony", "kremowy"):
            glad = glad * (1 - 0.25 * np.clip((n1 - 0.5) * 3, 0, 1))
        rgb = base[None, None, :] * jas[..., None]
    return np.clip(rgb, 0, 1), np.clip(h, 0, 1), np.clip(metal, 0, 1), np.clip(glad, 0, 1)


def _normal(h, sila):
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5
    dy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * 0.5   # wiersz rosnie = v w gore (OpenGL, jak w Unity)
    k = sila * TS / 128.0
    n = np.stack([-dx * k, -dy * k, np.ones_like(h)], -1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return n * 0.5 + 0.5


def _zapisz_px(name, px, kolor):
    img = bpy.data.images.get(name)
    if img:
        bpy.data.images.remove(img)
    img = bpy.data.images.new(name, px.shape[1], px.shape[0], alpha=True)
    img.colorspace_settings.name = "sRGB" if kolor else "Non-Color"
    img.alpha_mode = "STRAIGHT"
    img.pixels.foreach_set(px.astype(np.float32).ravel())
    img.filepath_raw = os.path.join(DIR_TEX, name + ".png")
    img.file_format = "PNG"
    img.save()
    return img


def make_atlas(force=False):
    """Tworzy 3 obrazy: T_Obiekty_Atlas (kolor), _MS (R metal, A gladkosc), _N (normal OpenGL)."""
    img = bpy.data.images.get("T_Obiekty_Atlas")
    if img and not force and bpy.data.images.get("T_Obiekty_Atlas_N"):
        return img
    kol = np.zeros((ATLAS_PX, ATLAS_PX, 4)); kol[..., 3] = 1
    ms = np.zeros((ATLAS_PX, ATLAS_PX, 4)); ms[..., 3] = 0.2
    nm = np.zeros((ATLAS_PX, ATLAS_PX, 4)); nm[..., :] = (0.5, 0.5, 1, 1)
    for name, (idx, col, kind) in TILES.items():
        tx, ty = idx % N, idx // N
        rgb, h, met, gl = _tile(name, kind, col, 1000 + idx)
        r0, r1, c0, c1 = ty * TS, (ty + 1) * TS, tx * TS, (tx + 1) * TS
        kol[r0:r1, c0:c1, :3] = rgb
        ms[r0:r1, c0:c1, 0] = met
        ms[r0:r1, c0:c1, 3] = gl
        nm[r0:r1, c0:c1, :3] = _normal(h, SILA_N.get(kind, 1.0))
    _zapisz_px("T_Obiekty_Atlas_MS", ms, False)
    _zapisz_px("T_Obiekty_Atlas_N", nm, False)
    return _zapisz_px("T_Obiekty_Atlas", kol, True)


def save_image(name, rgb, path_name=None):
    """rgb: HxWx3 sRGB float, wiersz 0 = GORA obrazka."""
    h, w, _ = rgb.shape
    px = np.ones((h, w, 4), dtype=np.float32)
    px[..., :3] = rgb[::-1]
    img = bpy.data.images.get(name)
    if img:
        bpy.data.images.remove(img)
    img = bpy.data.images.new(name, w, h, alpha=False)
    img.pixels.foreach_set(px.ravel())
    img.filepath_raw = os.path.join(DIR_TEX, (path_name or name) + ".png")
    img.file_format = "PNG"
    img.save()
    return img


# ---------------------------------------------------------------- materialy
def mat_image(name, img, rough=0.85, metal=0.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    tex = next((n for n in nt.nodes if n.type == "TEX_IMAGE"), None) or nt.nodes.new("ShaderNodeTexImage")
    tex.image = img
    tex.interpolation = "Closest" if img.size[0] <= 256 else "Linear"
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal
    return m


def atlas_mat():
    m = bpy.data.materials.get("M_Obiekty_Atlas")
    if m and m.get("pbr") and bpy.data.images.get("T_Obiekty_Atlas_N"):
        return m
    kol = make_atlas()
    ms = bpy.data.images["T_Obiekty_Atlas_MS"]; nm = bpy.data.images["T_Obiekty_Atlas_N"]
    m = m or bpy.data.materials.new("M_Obiekty_Atlas")
    m.use_nodes = True
    nt = m.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    out = nt.nodes.new("ShaderNodeOutputMaterial"); out.location = (600, 0)
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled"); bsdf.location = (300, 0)
    nt.links.new(bsdf.outputs[0], out.inputs["Surface"])
    t_kol = nt.nodes.new("ShaderNodeTexImage"); t_kol.image = kol; t_kol.location = (-400, 300)
    t_ms = nt.nodes.new("ShaderNodeTexImage"); t_ms.image = ms; t_ms.location = (-400, 0)
    t_n = nt.nodes.new("ShaderNodeTexImage"); t_n.image = nm; t_n.location = (-400, -300)
    sep = nt.nodes.new("ShaderNodeSeparateColor"); sep.location = (-100, 50)
    inv = nt.nodes.new("ShaderNodeMath"); inv.operation = "SUBTRACT"; inv.inputs[0].default_value = 1.0
    inv.location = (-100, -100)
    nmap = nt.nodes.new("ShaderNodeNormalMap"); nmap.location = (-100, -300)
    nt.links.new(t_kol.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(t_ms.outputs["Color"], sep.inputs[0])
    nt.links.new(sep.outputs[0], bsdf.inputs["Metallic"])
    nt.links.new(t_ms.outputs["Alpha"], inv.inputs[1])
    nt.links.new(inv.outputs[0], bsdf.inputs["Roughness"])
    nt.links.new(t_n.outputs["Color"], nmap.inputs["Color"])
    nt.links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])
    m["pbr"] = True
    return m


def mat_emis(name, rgb, strength=4.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Base Color"].default_value = (*rgb, 1)
    bsdf.inputs["Emission Color"].default_value = (*rgb, 1)
    bsdf.inputs["Emission Strength"].default_value = strength
    bsdf.inputs["Roughness"].default_value = 0.3
    return m


# ---------------------------------------------------------------- kolekcje
def new_collection(name):
    old = bpy.data.collections.get(name)
    if old:
        for o in list(old.all_objects):
            bpy.data.objects.remove(o, do_unlink=True)
        bpy.data.collections.remove(old)
    c = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(c)
    return c


def solo(name):
    """Pokaz tylko kolekcje `name` (do ogladania)."""
    for lc in bpy.context.view_layer.layer_collection.children:
        lc.exclude = (lc.name != name)


# ---------------------------------------------------------------- UV
def _uv_box(bm, tile, margin=0.08):
    idx = TILES[tile][0]
    tx, ty = idx % N, idx // N
    uv = bm.loops.layers.uv.verify()
    vs = [v.co for v in bm.verts]
    mn = Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs)))
    mx = Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs)))
    rng = mx - mn
    rng = Vector((max(rng.x, 1e-4), max(rng.y, 1e-4), max(rng.z, 1e-4)))
    for f in bm.faces:
        n = f.normal
        ax = max(range(3), key=lambda k: abs(n[k]))
        a, b = [(1, 2), (0, 2), (0, 1)][ax]
        for l in f.loops:
            p = l.vert.co
            u = (p[a] - mn[a]) / rng[a]
            v = (p[b] - mn[b]) / rng[b]
            u = margin + u * (1 - 2 * margin); v = margin + v * (1 - 2 * margin)
            l[uv].uv = ((tx + u) / N, (ty + v) / N)


def _uv_full(bm, flip_u=False):
    """Calosc 0..1 rzutem z przodu (-Y) — do kartek/map z wlasna tekstura (patrz z gory: plaszczyzna XY)."""
    uv = bm.loops.layers.uv.verify()
    vs = [v.co for v in bm.verts]
    mnx, mxx = min(v.x for v in vs), max(v.x for v in vs)
    mny, mxy = min(v.y for v in vs), max(v.y for v in vs)
    mnz, mxz = min(v.z for v in vs), max(v.z for v in vs)
    flat_z = (mxz - mnz) < min(mxx - mnx, mxy - mny)
    for f in bm.faces:
        for l in f.loops:
            p = l.vert.co
            u = (p.x - mnx) / max(mxx - mnx, 1e-5)
            v = ((p.y - mny) / max(mxy - mny, 1e-5)) if flat_z else ((p.z - mnz) / max(mxz - mnz, 1e-5))
            if flip_u:
                u = 1 - u
            l[uv].uv = (u, v)


# ---------------------------------------------------------------- prymitywy
def _finish(name, bm, coll, mat, tile=None, full_uv=False, bevel=0.0, smooth=False, seg=1):
    if full_uv:
        _uv_full(bm)
    elif tile:
        _uv_box(bm, tile)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    me.materials.append(mat)
    o = bpy.data.objects.new(name, me)
    coll.objects.link(o)
    if smooth:
        for p in me.polygons:
            p.use_smooth = True
    if bevel > 0:
        md = o.modifiers.new("Bevel", "BEVEL")
        md.width = bevel; md.segments = seg; md.limit_method = "ANGLE"
        md.harden_normals = seg > 1
        md.angle_limit = math.radians(40)
        apply_mods(o)
    return o


def apply_mods(o):
    dg = bpy.context.evaluated_depsgraph_get()
    ev = o.evaluated_get(dg)
    me = bpy.data.meshes.new_from_object(ev)
    old = o.data
    o.modifiers.clear()
    o.data = me
    bpy.data.meshes.remove(old)


def box(name, size, loc, tile, coll, rot=(0, 0, 0), bevel=0.0, mat=None, seg=1):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=Vector(size), verts=bm.verts)
    bm.normal_update()
    _uv_box(bm, tile) if mat is None else _uv_full(bm)
    bmesh.ops.rotate(bm, cent=(0, 0, 0), matrix=Matrix(_rot(rot)), verts=bm.verts)
    bmesh.ops.translate(bm, vec=Vector(loc), verts=bm.verts)
    o = _finish(name, bm, coll, mat or atlas_mat(), None, False, bevel, seg=seg)
    if seg > 1:
        for p_ in o.data.polygons:
            p_.use_smooth = True
    return o


def cyl(name, r, depth, loc, tile, coll, rot=(0, 0, 0), seg=12, r2=None, bevel=0.0, smooth=True, mat=None):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=seg,
                          radius1=r, radius2=(r if r2 is None else r2), depth=depth)
    bm.normal_update()
    _uv_box(bm, tile) if mat is None else _uv_full(bm)
    bmesh.ops.rotate(bm, cent=(0, 0, 0), matrix=Matrix(_rot(rot)), verts=bm.verts)
    bmesh.ops.translate(bm, vec=Vector(loc), verts=bm.verts)
    o = _finish(name, bm, coll, mat or atlas_mat(), None, False, bevel)
    if smooth:
        _smooth_sides(o)
    return o


def sphere(name, r, loc, tile, coll, seg=10, rings=6, scale=(1, 1, 1), mat=None):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=r)
    bmesh.ops.scale(bm, vec=Vector(scale), verts=bm.verts)
    bm.normal_update()
    _uv_box(bm, tile) if mat is None else _uv_full(bm)
    bmesh.ops.translate(bm, vec=Vector(loc), verts=bm.verts)
    o = _finish(name, bm, coll, mat or atlas_mat())
    for p in o.data.polygons:
        p.use_smooth = True
    return o


def torus(name, R, r, loc, tile, coll, rot=(0, 0, 0), seg=16, rseg=6, arc=360):
    bm = bmesh.new()
    verts_rings = []
    steps = seg if arc >= 360 else seg + 1
    for i in range(steps):
        a = math.radians(arc) * i / seg
        ring = []
        for j in range(rseg):
            b = 2 * math.pi * j / rseg
            x = (R + r * math.cos(b)) * math.cos(a)
            y = (R + r * math.cos(b)) * math.sin(a)
            z = r * math.sin(b)
            ring.append(bm.verts.new((x, y, z)))
        verts_rings.append(ring)
    nr = len(verts_rings) if arc >= 360 else len(verts_rings) - 1
    for i in range(nr):
        A = verts_rings[i]; B = verts_rings[(i + 1) % len(verts_rings)]
        for j in range(rseg):
            bm.faces.new((A[j], B[j], B[(j + 1) % rseg], A[(j + 1) % rseg]))
    bm.normal_update()
    _uv_box(bm, tile)
    bmesh.ops.rotate(bm, cent=(0, 0, 0), matrix=Matrix(_rot(rot)), verts=bm.verts)
    bmesh.ops.translate(bm, vec=Vector(loc), verts=bm.verts)
    o = _finish(name, bm, coll, atlas_mat())
    for p in o.data.polygons:
        p.use_smooth = True
    return o


def _smooth_sides(o):
    # boki (quady) gladkie, czapki (n-gony) plaskie
    for p in o.data.polygons:
        p.use_smooth = len(p.vertices) == 4
    o.data.update()


def _rot(rot):
    from mathutils import Euler
    return Euler([math.radians(a) for a in rot], "XYZ").to_matrix()


def join(objs, name, origin=(0, 0, 0)):
    """Laczy obiekty w jeden mesh z pivotem w `origin` (wsp. swiata)."""
    objs = [o for o in objs if o]
    bpy.context.view_layer.update()
    base = objs[0]
    bm = bmesh.new()
    mats = []
    for o in objs:
        tmp = o.data.copy()
        tmp.transform(o.matrix_world)
        # mapowanie materialow
        remap = []
        for m in tmp.materials:
            if m not in mats:
                mats.append(m)
            remap.append(mats.index(m))
        for p in tmp.polygons:
            p.material_index = remap[p.material_index] if remap else 0
        bm.from_mesh(tmp)
        bpy.data.meshes.remove(tmp)
    bmesh.ops.translate(bm, vec=-Vector(origin), verts=bm.verts)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    for m in mats:
        me.materials.append(m)
    coll = base.users_collection[0]
    for o in objs:
        bpy.data.objects.remove(o, do_unlink=True)
    ob = bpy.data.objects.new(name, me)
    ob.location = origin
    coll.objects.link(ob)
    bpy.context.view_layer.update()
    return ob


def empty(name, loc, coll, parent=None, size=0.1, rot=(0, 0, 0)):
    e = bpy.data.objects.new(name, None)
    e.empty_display_type = "ARROWS"
    e.empty_display_size = size
    coll.objects.link(e)
    e.location = loc
    e.rotation_euler = [math.radians(a) for a in rot]
    if parent:
        set_parent(e, parent)
    return e


def set_parent(child, parent):
    bpy.context.view_layer.update()
    mw = child.matrix_world.copy()
    child.parent = parent
    child.matrix_parent_inverse = Matrix.Identity(4)
    child.matrix_world = mw


def reparent_keep_mesh(child, parent):
    """Rodzic bez parent-inverse: lokalna pozycja dziecka = wzgledem rodzica."""
    set_parent(child, parent)


# ---------------------------------------------------------------- eksport
def export_fbx(coll_name, file_name=None):
    coll = bpy.data.collections[coll_name]
    for lc in bpy.context.view_layer.layer_collection.children:
        if lc.name == coll_name:
            lc.exclude = False
    bpy.context.view_layer.update()
    # nazwy w Blenderze sa globalne (PunktInterakcji.001) -> na czas eksportu czyste nazwy
    renamed = []
    for o in coll.all_objects:
        base = o.name.split(".")[0]
        if base != o.name:
            other = bpy.data.objects.get(base)
            if other:
                renamed.append((other, base)); other.name = base + "__tmp"
            renamed.append((o, o.name)); o.name = base
    bpy.ops.object.select_all(action="DESELECT")
    for o in coll.all_objects:
        o.hide_set(False)
        o.select_set(True)
    path = os.path.join(DIR_MODELE, (file_name or coll_name) + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"MESH", "EMPTY"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        use_mesh_modifiers=True, mesh_smooth_type="FACE", add_leaf_bones=False,
        bake_anim=False, path_mode="STRIP", use_custom_props=False)
    for o, n in reversed(renamed):
        o.name = n
    return path


def report(coll_name):
    bpy.context.view_layer.update()
    coll = bpy.data.collections[coll_name]
    tris = 0
    for o in coll.all_objects:
        if o.type == "MESH":
            tris += sum(len(p.vertices) - 2 for p in o.data.polygons)
    from mathutils import Vector as V
    pts = [o.matrix_world @ V(c) for o in coll.all_objects if o.type == "MESH" for c in o.bound_box]
    mn = V((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = V((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    print(f"{coll_name}: tris={tris} size=({mx.x-mn.x:.3f},{mx.y-mn.y:.3f},{mx.z-mn.z:.3f}) zmin={mn.z:.3f}",
          [o.name for o in coll.all_objects])
