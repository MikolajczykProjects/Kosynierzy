# -*- coding: utf-8 -*-
"""
lowpoly.py — wspólna biblioteka Blendera (5.x) do mapy SENDER GLEIWITZ 1939.

UKŁAD (Blender):  X = wschód, Y = północ, Z = góra, 1 j. = 1 m.
Unity dostaje to jako (x, z, y) — skrypt w Unity sam koryguje osie (kompas w FBX).
FRONT obiektu = +Y (w Unity to będzie +Z).  PIVOT = środek podstawy (0,0,0).

Użycie w skrypcie obiektu:
    import sys, os; sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    from lowpoly import *
    def zbuduj():
        m = Siatka("Ratusz")
        m.box((0,0,5), (20,12,10), "tynk_ochra")
        ...
        return [m.obiekt()]          # lista obiektów bpy (każdy osobny obiekt, pivot na dole)

Konwencje nazw obiektów (czyta je Unity):
    *_COL      siatka tylko do kolizji (bez renderera)
    *_NOCOL    bez kolizji (np. drobne detale, sufity)
    Drzewo*    kolizja kapsułą na pniu, Krzak* bez kolizji
Puste punkty gry i światła NIE są obiektami — dodaj je przez:
    punkt("radio_technika_kable", (x,y,z), yaw_deg)       # yaw 0 = patrzy na +Y (północ), 90 = na +X (wschód)
    swiatlo((x,y,z), kolor="#ffcf8a", zasieg=8, moc=2.0)
(pozycje LOKALNE obiektu; przesuń/obróć je razem z obiektem przez przesun_meta()).
"""
import bpy, bmesh, math, json, os, random
from mathutils import Vector, Matrix, geometry

TU = os.path.dirname(os.path.abspath(__file__))
KORZEN = os.path.dirname(TU)                      # .../mapa
DANE = os.path.join(KORZEN, "dane")
PODGLADY = os.path.join(KORZEN, "podglady")
UNITY_MODELE = os.path.join(KORZEN, "unity", "Assets", "_Mapa", "Modele")

# ----------------------------------------------------------------------------- PALETA
# Jedna paleta dla całej mapy (sRGB hex). Unity tworzy z niej materiały M_<nazwa>.
# Dodajesz kolor? Dopisz go TUTAJ (nie w swoim pliku), żeby Unity go znało.
PALETA = {
    # tynki kamienic (1939, lekko przybrudzone, sepia)
    "tynk_bez": "#d9c7a3", "tynk_ochra": "#d2a85f", "tynk_piasek": "#c9b48a",
    "tynk_szarozielony": "#a3a98c", "tynk_losos": "#d39a7f", "tynk_kremowy": "#e6dcc0",
    "tynk_szary": "#a8a49a", "tynk_brudny": "#9a8f7b", "tynk_rozowy": "#c99a8f",
    "tynk_zolty": "#d8bd6a", "tynk_bialy": "#e8e2d2", "tynk_niebieski": "#93a3a6",
    "cokol": "#6e655a", "kamien": "#a39a8a", "kamien_ciemny": "#7b7469", "piaskowiec": "#b9a684",
    "cegla": "#9a4b33", "cegla_ciemna": "#743727", "cegla_jasna": "#b06a4c", "klinkier": "#5e3328",
    "dachowka": "#9b4a32", "dachowka_ciemna": "#6f3424", "dachowka_brazowa": "#5a3a2c",
    "blacha": "#6d7275", "blacha_miedz": "#5f8a78", "papa": "#3c3a38", "lupek": "#4a4d55",
    "okno": "#1d2633", "okno_swiatlo": "#2a2f38", "rama": "#e9e3d3", "rama_ciemna": "#4b3a2c",
    "drzwi": "#5a3b26", "drzwi_ciemne": "#3a2618", "markiza_czerwona": "#9c3a2e", "markiza_zielona": "#3f5e44",
    "markiza_kremowa": "#d8cfb0", "szyld": "#2b2620", "szyld_zloty": "#b8964a",
    "bruk": "#5d5a55", "bruk_jasny": "#77736b", "chodnik": "#9b968c", "kraweznik": "#b1aca1",
    "asfalt": "#4a4846", "ziemia": "#7a6648", "zwir": "#9a8f7a", "polna_droga": "#8c7a58",
    "trawa": "#6f8a45", "trawa_ciemna": "#55703a", "trawa_sucha": "#9a9a55", "zyto_scierniska": "#c9b06a",
    "pole_zaorane": "#6b5238", "pole_zielone": "#7d9448",
    "lisc_1": "#4f6e34", "lisc_2": "#64823c", "lisc_3": "#7b8f3f", "lisc_jesien": "#9a8a3a",
    "iglaste": "#344f33", "kora": "#4e3b2b", "kora_jasna": "#6b5640", "zywoplot": "#4a6a33",
    "woda": "#3f5a66", "dno": "#3b3a30",
    "drewno": "#7a5634", "drewno_ciemne": "#4f3622", "modrzew": "#8a6440", "modrzew_ciemny": "#6a4a2e",
    "deska_szara": "#7d7468", "slupek": "#5a4a3a",
    "metal": "#4f5356", "metal_ciemny": "#2f3234", "szyna": "#5b5550", "podklad": "#4a3a2c",
    "zielen_okienna": "#3f5a40", "czarny": "#1c1c1c", "bialy": "#e8e6e0", "zloto": "#b8964a",
    "czerwony": "#8e2b22", "tkanina_zielona": "#3e5a3c", "tkanina_bordo": "#6e2a2a", "tkanina_bez": "#cdbb98",
    # wnętrza
    "parkiet": "#8a5f3a", "posadzka_jasna": "#d8cfbd", "posadzka_ciemna": "#3e3a36", "dywan_czerwony": "#7a2f2a",
    "tapeta_zielona": "#7d8a6a", "tapeta_bordo": "#7a4a44", "tapeta_kremowa": "#d6c9a8", "tapeta_niebieska": "#7f8e96",
    "boazeria": "#5e3f26", "sufit": "#e4ddcc", "kafle_biale": "#dcdad2", "beton": "#8d8a84", "lampa": "#fff1c8",
    "ksiazka_1": "#7a2f2a", "ksiazka_2": "#3e5a3c", "ksiazka_3": "#2f3f5e", "ksiazka_4": "#b8964a", "ksiazka_5": "#5e3f26",
}
# materiały specjalne (Unity: przezroczystość / emisja / połysk)
SPECJALNE = {
    "woda": {"alfa": 0.82, "smooth": 0.85},
    "okno": {"smooth": 0.75, "metal": 0.2},
    "okno_swiatlo": {"smooth": 0.7, "emisja": "#ffbe6a", "emisja_noca": True},
    "lampa": {"emisja": "#ffe0a0"},
    "metal": {"metal": 0.6, "smooth": 0.35}, "metal_ciemny": {"metal": 0.6, "smooth": 0.3},
    "szyna": {"metal": 0.7, "smooth": 0.45}, "blacha_miedz": {"metal": 0.3, "smooth": 0.3},
    "zloto": {"metal": 0.8, "smooth": 0.5}, "szyld_zloty": {"metal": 0.7, "smooth": 0.45},
}

def _lin(c):
    c = c / 255.0
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4

def hex_rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))

def zapisz_palete():
    os.makedirs(DANE, exist_ok=True)
    out = {}
    for k, v in PALETA.items():
        e = {"kolor": v, "smooth": 0.12, "metal": 0.0}
        e.update(SPECJALNE.get(k, {}))
        out[k] = e
    with open(os.path.join(DANE, "paleta.json"), "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False, indent=1)

TEKSTURY_JSON = os.path.join(DANE, "tekstury.json")
TEKSTURY_DIR = os.path.join(KORZEN, "unity", "Assets", "_Mapa", "Tekstury")
_TEKSTURY = None
def _tekstury():
    global _TEKSTURY
    if _TEKSTURY is None:
        try:
            with open(TEKSTURY_JSON, "r", encoding="utf-8") as f:
                _TEKSTURY = json.load(f)
        except OSError:
            _TEKSTURY = {}
    return _TEKSTURY

_MAT = {}
def mat(nazwa):
    """Materiał Blendera M_<nazwa> (kolor z PALETA). Nieznana nazwa = błąd, żeby nikt nie wymyślał kolorów obok palety.
    Jeśli dane/tekstury.json przypisuje tej nazwie teksturę, dokleja węzły Image Texture (A*kolor -> Base Color,
    N -> Normal Map -> Normal), skalowane przez Mapping 1/kafel (UV w metrach z uv_boks)."""
    if nazwa not in PALETA:
        raise KeyError("Brak koloru '%s' w PALETA (lowpoly.py)" % nazwa)
    if nazwa in _MAT:
        return _MAT[nazwa]
    mn = "M_" + nazwa
    m = bpy.data.materials.get(mn) or bpy.data.materials.new(mn)
    r, g, b = hex_rgb(PALETA[nazwa])
    m.diffuse_color = (_lin(r), _lin(g), _lin(b), 1.0)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (_lin(r), _lin(g), _lin(b), 1.0)
        sp = SPECJALNE.get(nazwa, {})
        bsdf.inputs["Roughness"].default_value = 1.0 - sp.get("smooth", 0.12)
        bsdf.inputs["Metallic"].default_value = sp.get("metal", 0.0)
        if "emisja" in sp and "Emission Color" in bsdf.inputs:
            er, eg, eb = hex_rgb(sp["emisja"])
            bsdf.inputs["Emission Color"].default_value = (_lin(er), _lin(eg), _lin(eb), 1.0)
            bsdf.inputs["Emission Strength"].default_value = 0.0 if sp.get("emisja_noca") else 2.0

        info = _tekstury().get(nazwa)
        if info:
            plik_a = os.path.join(TEKSTURY_DIR, "T_%s_A.png" % info["tex"])
            plik_n = os.path.join(TEKSTURY_DIR, "T_%s_N.png" % info["tex"])
            if os.path.isfile(plik_a) and os.path.isfile(plik_n):
                nt = m.node_tree
                kafel = float(info.get("kafel", 1.0))
                tex_coord = nt.nodes.new("ShaderNodeTexCoord")
                mapping = nt.nodes.new("ShaderNodeMapping")
                mapping.inputs["Scale"].default_value = (1.0 / kafel, 1.0 / kafel, 1.0)
                nt.links.new(tex_coord.outputs["UV"], mapping.inputs["Vector"])

                img_a = bpy.data.images.get("T_%s_A" % info["tex"]) or bpy.data.images.load(plik_a)
                tex_a = nt.nodes.new("ShaderNodeTexImage")
                tex_a.image = img_a
                nt.links.new(mapping.outputs["Vector"], tex_a.inputs["Vector"])

                mix = nt.nodes.new("ShaderNodeMixRGB")
                mix.blend_type = "MULTIPLY"
                mix.inputs["Fac"].default_value = 1.0
                mix.inputs["Color2"].default_value = (_lin(r), _lin(g), _lin(b), 1.0)
                nt.links.new(tex_a.outputs["Color"], mix.inputs["Color1"])
                nt.links.new(mix.outputs["Color"], bsdf.inputs["Base Color"])

                img_n = bpy.data.images.get("T_%s_N" % info["tex"]) or bpy.data.images.load(plik_n)
                img_n.colorspace_settings.name = "Non-Color"
                tex_n = nt.nodes.new("ShaderNodeTexImage")
                tex_n.image = img_n
                nt.links.new(mapping.outputs["Vector"], tex_n.inputs["Vector"])

                normal_map = nt.nodes.new("ShaderNodeNormalMap")
                nt.links.new(tex_n.outputs["Color"], normal_map.inputs["Color"])
                nt.links.new(normal_map.outputs["Normal"], bsdf.inputs["Normal"])
    _MAT[nazwa] = m
    return m

# ----------------------------------------------------------------------------- META (punkty, światła)
META = {"punkty": [], "swiatla": []}

def punkt(nazwa, poz, yaw=0.0):
    META["punkty"].append({"n": nazwa, "p": list(map(float, poz)), "yaw": float(yaw)})

def swiatlo(poz, kolor="#ffcf8a", zasieg=8.0, moc=2.0, typ="point"):
    META["swiatla"].append({"p": list(map(float, poz)), "kolor": kolor, "zasieg": float(zasieg), "moc": float(moc), "typ": typ})

def przesun_meta(od_indeksu_p, od_indeksu_s, przes, kat_deg):
    """Przesuń/obróć punkty i światła dodane od podanych indeksów (gdy wstawiasz obiekt w miasto)."""
    c, s = math.cos(math.radians(kat_deg)), math.sin(math.radians(kat_deg))
    def tr(p):
        x, y, z = p
        return [przes[0] + x * c - y * s, przes[1] + x * s + y * c, przes[2] + z]
    for e in META["punkty"][od_indeksu_p:]:
        e["p"] = tr(e["p"]); e["yaw"] = e["yaw"] - kat_deg
    for e in META["swiatla"][od_indeksu_s:]:
        e["p"] = tr(e["p"])

def zapisz_meta(plik):
    os.makedirs(os.path.dirname(plik), exist_ok=True)
    with open(plik, "w", encoding="utf-8") as f:
        json.dump(META, f, ensure_ascii=False, indent=0)

# ----------------------------------------------------------------------------- SIATKA
class Siatka:
    """Zbiera bryły w JEDEN obiekt (wiele materiałów). Wszystkie współrzędne lokalne, pivot = (0,0,0)."""
    def __init__(self, nazwa):
        self.nazwa = nazwa
        self.v = []          # wierzchołki
        self.f = []          # ściany (listy indeksów, CCW patrząc z zewnątrz)
        self.fm = []         # nazwa materiału każdej ściany

    # --- prymitywy niskiego poziomu
    def poly(self, pts, m):
        i0 = len(self.v)
        self.v.extend([tuple(map(float, p)) for p in pts])
        self.f.append(list(range(i0, i0 + len(pts))))
        self.fm.append(m)

    def quad(self, a, b, c, d, m):
        self.poly([a, b, c, d], m)

    def tri(self, a, b, c, m):
        self.poly([a, b, c], m)

    # --- bryły
    def box(self, srodek, wym, m, rot_z=0.0, m_gora=None, m_dol=None, bez_dolu=True):
        """Prostopadłościan: srodek (x,y,z) = środek bryły, wym = (sx,sy,sz), obrót wokół Z w stopniach."""
        cx, cy, cz = srodek; hx, hy, hz = wym[0] / 2, wym[1] / 2, wym[2] / 2
        c, s = math.cos(math.radians(rot_z)), math.sin(math.radians(rot_z))
        def P(x, y, z):
            return (cx + x * c - y * s, cy + x * s + y * c, cz + z)
        p = [P(-hx, -hy, -hz), P(hx, -hy, -hz), P(hx, hy, -hz), P(-hx, hy, -hz),
             P(-hx, -hy, hz), P(hx, -hy, hz), P(hx, hy, hz), P(-hx, hy, hz)]
        self.quad(p[0], p[1], p[5], p[4], m)   # -Y
        self.quad(p[1], p[2], p[6], p[5], m)   # +X
        self.quad(p[2], p[3], p[7], p[6], m)   # +Y
        self.quad(p[3], p[0], p[4], p[7], m)   # -X
        self.quad(p[4], p[5], p[6], p[7], m_gora or m)  # góra
        if not bez_dolu:
            self.quad(p[3], p[2], p[1], p[0], m_dol or m)

    def box_od_do(self, x0, y0, z0, x1, y1, z1, m, **kw):
        self.box(((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2), (abs(x1 - x0), abs(y1 - y0), abs(z1 - z0)), m, **kw)

    def belka(self, a, b, grub, m, grub2=None):
        """Belka o przekroju kwadratowym między punktami a i b (np. kratownica wieży)."""
        a = Vector(a); b = Vector(b); d = b - a
        L = d.length
        if L < 1e-6:
            return
        os_ = d.normalized()
        pom = Vector((0, 0, 1)) if abs(os_.z) < 0.95 else Vector((1, 0, 0))
        u = os_.cross(pom).normalized() * (grub / 2)
        w = os_.cross(u).normalized() * ((grub2 or grub) / 2)
        k = [a - u - w, a + u - w, a + u + w, a - u + w]
        g = [b - u - w, b + u - w, b + u + w, b - u + w]
        for i in range(4):
            j = (i + 1) % 4
            self.quad(k[i], k[j], g[j], g[i], m)
        self.poly(list(reversed(k)), m)
        self.poly(g, m)

    def walec(self, srodek_dol, r, h, m, n=8, m_gora=None, r_gora=None, dol=False):
        cx, cy, cz = srodek_dol; rg = r if r_gora is None else r_gora
        d = [(cx + r * math.cos(2 * math.pi * i / n), cy + r * math.sin(2 * math.pi * i / n), cz) for i in range(n)]
        g = [(cx + rg * math.cos(2 * math.pi * i / n), cy + rg * math.sin(2 * math.pi * i / n), cz + h) for i in range(n)]
        for i in range(n):
            j = (i + 1) % n
            self.quad(d[i], d[j], g[j], g[i], m)
        if rg > 1e-4:
            self.poly(g, m_gora or m)
        if dol:
            self.poly(list(reversed(d)), m)

    def stozek(self, srodek_dol, r, h, m, n=8):
        cx, cy, cz = srodek_dol
        d = [(cx + r * math.cos(2 * math.pi * i / n), cy + r * math.sin(2 * math.pi * i / n), cz) for i in range(n)]
        t = (cx, cy, cz + h)
        for i in range(n):
            self.tri(d[i], d[(i + 1) % n], t, m)

    def ostroslup(self, srodek_dol, sx, sy, h, m, rot_z=0.0):
        """Dach namiotowy / hełm wieży na prostokącie."""
        cx, cy, cz = srodek_dol
        c, s = math.cos(math.radians(rot_z)), math.sin(math.radians(rot_z))
        P = lambda x, y: (cx + x * c - y * s, cy + x * s + y * c, cz)
        d = [P(-sx / 2, -sy / 2), P(sx / 2, -sy / 2), P(sx / 2, sy / 2), P(-sx / 2, sy / 2)]
        t = (cx, cy, cz + h)
        for i in range(4):
            self.tri(d[i], d[(i + 1) % 4], t, m)

    def dach_dwuspadowy(self, srodek_dol, dl, szer, h, m, m_szczyt=None, rot_z=0.0, okap=0.4):
        """Kalenica wzdłuż osi X (dł.), spadki na ±Y. m_szczyt = kolor trójkątów szczytowych (ściana)."""
        cx, cy, cz = srodek_dol
        c, s = math.cos(math.radians(rot_z)), math.sin(math.radians(rot_z))
        P = lambda x, y, z: (cx + x * c - y * s, cy + x * s + y * c, cz + z)
        L, W = dl / 2 + okap, szer / 2 + okap
        a, b = P(-L, -W, -0.25), P(L, -W, -0.25)
        cc, d = P(L, W, -0.25), P(-L, W, -0.25)
        r0, r1 = P(-L, 0, h), P(L, 0, h)
        self.quad(a, b, r1, r0, m)
        self.quad(cc, d, r0, r1, m)
        # szczyty (ściana, bez okapu)
        L2, W2 = dl / 2, szer / 2
        sa, sb, st = P(-L2, -W2, 0), P(-L2, W2, 0), P(-L2, 0, h - 0.25)
        self.tri(sb, sa, st, m_szczyt or m)
        sa, sb, st = P(L2, -W2, 0), P(L2, W2, 0), P(L2, 0, h - 0.25)
        self.tri(sa, sb, st, m_szczyt or m)
        # spód okapu
        self.quad(P(-L, -W, -0.25), P(-L, W, -0.25), P(L, W, -0.25), P(L, -W, -0.25), m_szczyt or m)

    def dach_czterospadowy(self, srodek_dol, dl, szer, h, m, rot_z=0.0, okap=0.4):
        cx, cy, cz = srodek_dol
        c, s = math.cos(math.radians(rot_z)), math.sin(math.radians(rot_z))
        P = lambda x, y, z: (cx + x * c - y * s, cy + x * s + y * c, cz + z)
        L, W = dl / 2 + okap, szer / 2 + okap
        k = max(0.0, L - W)
        a, b, cc, d = P(-L, -W, -0.25), P(L, -W, -0.25), P(L, W, -0.25), P(-L, W, -0.25)
        r0, r1 = P(-k, 0, h), P(k, 0, h)
        self.quad(a, b, r1, r0, m)
        self.quad(cc, d, r0, r1, m)
        self.tri(b, cc, r1, m)
        self.tri(d, a, r0, m)
        self.quad(a, d, cc, b, m)

    def okna_rzad(self, a, b, z0, wys, szer_okna, rozstaw, m_okno="okno", m_rama="rama", gl=0.06, parapet=True, m_parapet="kamien"):
        """Rząd okien na ścianie od a do b (punkty 2D na obrysie, ściana CCW, okna wystają na zewnątrz)."""
        a = Vector((a[0], a[1], 0)); b = Vector((b[0], b[1], 0))
        d = b - a; L = d.length
        if L < szer_okna + 0.6:
            return 0
        t = d.normalized(); n = Vector((t.y, -t.x, 0))   # na zewnątrz dla obrysu CCW
        ile = max(1, int((L - 0.8) // rozstaw))
        krok = L / ile
        kat = math.degrees(math.atan2(t.y, t.x))
        for i in range(ile):
            srod = a + t * (krok * (i + 0.5))
            p = srod + n * (gl / 2)
            self.box((p.x, p.y, z0 + wys / 2), (szer_okna + 0.16, gl + 0.02, wys + 0.16), m_rama, rot_z=kat)
            p2 = srod + n * (gl + 0.01)
            self.box((p2.x, p2.y, z0 + wys / 2), (szer_okna, 0.02, wys), m_okno, rot_z=kat)
            if parapet:
                p3 = srod + n * 0.12
                self.box((p3.x, p3.y, z0 - 0.06), (szer_okna + 0.3, 0.24, 0.1), m_parapet, rot_z=kat)
        return ile

    def obrys(self, pts, z0, z1, m, gora=True, m_gora=None):
        """Ściany z obrysu 2D (lista (x,y), dowolny kierunek) od z0 do z1 + dach płaski."""
        pts = [tuple(p[:2]) for p in pts]
        if pole(pts) < 0:
            pts = pts[::-1]
        n = len(pts)
        for i in range(n):
            a, b = pts[i], pts[(i + 1) % n]
            self.quad((a[0], a[1], z0), (b[0], b[1], z0), (b[0], b[1], z1), (a[0], a[1], z1), m)
        if gora:
            self.wielokat(pts, z1, m_gora or m)

    def wielokat(self, pts, z, m, w_dol=False, dziury=None):
        """Płaski wielokąt (może być wklęsły, z dziurami) na wysokości z, triangulowany."""
        pts = [Vector((p[0], p[1], z)) for p in pts]
        if pole([(p.x, p.y) for p in pts]) < 0:
            pts = pts[::-1]
        petle = [pts] + [[Vector((p[0], p[1], z)) for p in d] for d in (dziury or [])]
        tris = geometry.tessellate_polygon(petle)
        wsz = [p for petla in petle for p in petla]
        for t in tris:
            a, b, c = wsz[t[0]], wsz[t[1]], wsz[t[2]]
            # wymuś normalną w górę (lub w dół)
            if ((b - a).cross(c - a)).z < 0:
                b, c = c, b
            if w_dol:
                b, c = c, b
            self.tri(a, b, c, m)

    def dolacz(self, inna, przes=(0, 0, 0), kat=0.0):
        c, s = math.cos(math.radians(kat)), math.sin(math.radians(kat))
        i0 = len(self.v)
        for (x, y, z) in inna.v:
            self.v.append((przes[0] + x * c - y * s, przes[1] + x * s + y * c, przes[2] + z))
        for f, m in zip(inna.f, inna.fm):
            self.f.append([i + i0 for i in f]); self.fm.append(m)

    def pusta(self):
        return len(self.f) == 0

    def obiekt(self, poz=(0, 0, 0), kat=0.0, kolekcja=None):
        """Tworzy obiekt bpy. Geometria lokalna, obiekt ustawiony w poz i obrócony o kat (stopnie, wokół Z)."""
        me = bpy.data.meshes.new(self.nazwa)
        nazwy = sorted(set(self.fm))
        idx = {n: i for i, n in enumerate(nazwy)}
        me.from_pydata(self.v, [], self.f)
        for n in nazwy:
            me.materials.append(mat(n))
        for i, p in enumerate(me.polygons):
            p.material_index = idx[self.fm[i]]
            p.use_smooth = False
        me.validate(clean_customdata=False)
        me.update()
        ob = bpy.data.objects.new(self.nazwa, me)
        ob.location = poz
        ob.rotation_euler = (0, 0, math.radians(kat))
        (kolekcja or bpy.context.scene.collection).objects.link(ob)
        return ob

# ----------------------------------------------------------------------------- GEOMETRIA 2D
def pole(pts):
    a = 0.0
    for i in range(len(pts)):
        x0, y0 = pts[i][0], pts[i][1]
        x1, y1 = pts[(i + 1) % len(pts)][0], pts[(i + 1) % len(pts)][1]
        a += x0 * y1 - x1 * y0
    return a / 2

# ----------------------------------------------------------------------------- ROŚLINNOŚĆ (wspólne)
def korona(m, srodek, r, h, mat_lisc, rnd, seg=7):
    """Low-poly korona: dwa ścięte stożki z losowym drganiem."""
    cx, cy, cz = srodek
    pier = []
    for k, (zz, rr) in enumerate([(0.0, 0.55), (0.35, 1.0), (0.75, 0.8), (1.0, 0.0)]):
        ring = []
        for i in range(seg):
            a = 2 * math.pi * (i + 0.5 * k) / seg
            j = 1.0 + rnd.uniform(-0.15, 0.15)
            ring.append((cx + math.cos(a) * r * rr * j, cy + math.sin(a) * r * rr * j, cz + zz * h + rnd.uniform(-0.15, 0.15) * h * (0 < k < 3)))
        pier.append(ring)
    m.poly(list(reversed(pier[0])), mat_lisc)
    for k in range(3):
        A, B = pier[k], pier[k + 1]
        for i in range(seg):
            j = (i + 1) % seg
            if k == 2:
                m.tri(A[i], A[j], B[0], mat_lisc) if False else m.tri(A[i], A[j], (cx, cy, cz + h), mat_lisc)
            else:
                m.quad(A[i], A[j], B[j], B[i], mat_lisc)

def drzewo(nazwa, typ=0, ziarno=0, skala=1.0):
    """Zwraca Siatka drzewa (pivot u podstawy pnia). typ: 0 lipa/klon, 1 topola, 2 sosna, 3 kasztanowiec, 4 brzoza."""
    rnd = random.Random(ziarno)
    m = Siatka(nazwa)
    s = skala * rnd.uniform(0.85, 1.15)
    lis = rnd.choice(["lisc_1", "lisc_2", "lisc_3", "lisc_1", "lisc_2"])
    if typ == 1:      # topola
        m.walec((0, 0, 0), 0.22 * s, 3 * s, "kora", n=6, r_gora=0.16 * s)
        korona(m, (0, 0, 2.2 * s), 1.6 * s, 12 * s, lis, rnd)
    elif typ == 2:    # sosna
        m.walec((0, 0, 0), 0.25 * s, 7 * s, "kora_jasna", n=6, r_gora=0.12 * s)
        for k in range(3):
            m.stozek((0, 0, (5 + k * 2.2) * s), (2.6 - k * 0.7) * s, 3.2 * s, "iglaste", n=7)
    elif typ == 4:    # brzoza
        m.walec((0, 0, 0), 0.16 * s, 4 * s, "bialy", n=6, r_gora=0.1 * s)
        korona(m, (0, 0, 3.0 * s), 2.0 * s, 5.5 * s, "lisc_3", rnd)
    else:             # lipa / klon / kasztan
        h_pnia = (2.6 if typ == 0 else 2.2) * s
        m.walec((0, 0, 0), 0.3 * s, h_pnia + 0.6, "kora", n=6, r_gora=0.2 * s)
        korona(m, (0, 0, h_pnia), (3.2 if typ == 0 else 3.6) * s, 6.5 * s, lis, rnd)
        if typ == 3:
            korona(m, (rnd.uniform(-1, 1) * s, rnd.uniform(-1, 1) * s, h_pnia + 2.5 * s), 2.4 * s, 4.5 * s, lis, rnd)
    return m

def krzak(nazwa, ziarno=0, skala=1.0):
    rnd = random.Random(ziarno)
    m = Siatka(nazwa)
    korona(m, (0, 0, -0.1), 1.0 * skala, 1.3 * skala, rnd.choice(["zywoplot", "lisc_1", "lisc_2"]), rnd, seg=6)
    return m

# ----------------------------------------------------------------------------- SCENA, EKSPORT, RENDER
def wyczysc_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    _MAT.clear()
    META["punkty"].clear(); META["swiatla"].clear()

def kompas(kolekcja=None):
    """Puste obiekty, po których Unity rozpozna osie FBX. NIE USUWAJ przy eksporcie."""
    obs = []
    for n, p in (("Kompas_E", (100, 0, 0)), ("Kompas_N", (0, 100, 0)), ("Kompas_U", (0, 0, 100)), ("Kompas_0", (0, 0, 0))):
        e = bpy.data.objects.new(n, None)
        e.location = p
        (kolekcja or bpy.context.scene.collection).objects.link(e)
        obs.append(e)
    return obs

def uv_boks(obiekty, metr=1.0):
    """UV z rzutu pudełkowego w metrach (1 j. UV = `metr` m), liczone w przestrzeni świata obiektu
    (skala/obrót obiektu uwzględnione), żeby tekstury kafelkowały się równo na wszystkim. Siatki współdzielone robi raz."""
    zrobione = set()
    for o in obiekty:
        if o.type != "MESH" or o.data.name in zrobione:
            continue
        zrobione.add(o.data.name)
        me = o.data
        bm = bmesh.new(); bm.from_mesh(me)
        uvl = bm.loops.layers.uv.get("UVMap") or bm.loops.layers.uv.new("UVMap")
        M = o.matrix_world.copy(); R = M.to_3x3()
        for f in bm.faces:
            n = (R @ f.normal)
            ax, ay, az = abs(n.x), abs(n.y), abs(n.z)
            for l in f.loops:
                p = M @ l.vert.co
                if az >= ax and az >= ay:
                    u, v = p.x, p.y
                elif ax >= ay:
                    u, v = (p.y if n.x > 0 else -p.y), p.z
                else:
                    u, v = (-p.x if n.y > 0 else p.x), p.z
                l[uvl].uv = (u / metr, v / metr)
        bm.to_mesh(me); bm.free()


def eksport_fbx(obiekty, plik):
    """Eksport podanych obiektów (+ kompas) do FBX dla Unity. Dokleja UV (rzut pudełkowy w metrach) pod tekstury."""
    os.makedirs(os.path.dirname(plik), exist_ok=True)
    try:
        uv_boks(obiekty)
    except Exception as e:
        print("UV_BOKS blad:", e)
    for o in bpy.context.scene.objects:
        o.select_set(False)
    k = kompas()
    for o in list(obiekty) + k:
        o.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=plik, use_selection=True, object_types={"MESH", "EMPTY"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", global_scale=1.0,
        axis_forward="-Z", axis_up="Y", bake_space_transform=False,
        mesh_smooth_type="FACE", use_mesh_modifiers=True, add_leaf_bones=False,
        bake_anim=False, path_mode="AUTO", embed_textures=False, use_custom_props=False)
    for o in k:
        bpy.data.objects.remove(o, do_unlink=True)

def render_podglad(plik, cel=(0, 0, 0), odl=60.0, azymut=215.0, wysokosc_kat=28.0, ogniskowa=35, szer=1600, wys=900, noc=False):
    """Render podglądu (Eevee, fallback Workbench). azymut 0 = kamera na południu patrzy na północ."""
    os.makedirs(os.path.dirname(plik), exist_ok=True)
    sc = bpy.context.scene
    cel = Vector(cel)
    a, e = math.radians(azymut), math.radians(wysokosc_kat)
    poz = cel + Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e))) * odl
    cam = bpy.data.objects.get("_Kamera")
    if not cam:
        cam = bpy.data.objects.new("_Kamera", bpy.data.cameras.new("_Kamera"))
        sc.collection.objects.link(cam)
    cam.location = poz
    cam.rotation_euler = (cel - poz).to_track_quat("-Z", "Y").to_euler()
    cam.data.lens = ogniskowa
    cam.data.clip_end = 6000
    sc.camera = cam
    sun = bpy.data.objects.get("_Slonce")
    if not sun:
        sun = bpy.data.objects.new("_Slonce", bpy.data.lights.new("_Slonce", "SUN"))
        sc.collection.objects.link(sun)
    sun.data.energy = 0.6 if noc else 4.2
    sun.data.color = (0.55, 0.62, 0.9) if noc else (1.0, 0.88, 0.70)
    sun.data.angle = math.radians(3)
    sun.rotation_euler = (math.radians(52), 0, math.radians(-140))
    w = sc.world or bpy.data.worlds.new("Swiat")
    sc.world = w
    w.use_nodes = True
    bg = w.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.05, 0.07, 0.14, 1) if noc else (0.62, 0.70, 0.80, 1)
        bg.inputs[1].default_value = 0.35 if noc else 0.9
    sc.render.resolution_x, sc.render.resolution_y = szer, wys
    sc.render.film_transparent = False
    sc.render.filepath = plik
    sc.render.image_settings.file_format = "PNG"
    ok = False
    for eng in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE"):
        try:
            sc.render.engine = eng
            ok = True
            break
        except Exception:
            pass
    try:
        sc.eevee.taa_render_samples = 16
        sc.eevee.use_shadows = True
    except Exception:
        pass
    try:
        sc.view_settings.view_transform = "AgX"
        sc.view_settings.look = "AgX - Medium High Contrast"
    except Exception:
        try:
            sc.view_settings.view_transform = "Filmic"
        except Exception:
            pass
    try:
        bpy.ops.render.render(write_still=True)
    except Exception as ex:
        print("Eevee padło (%s), Workbench" % ex)
        sc.render.engine = "BLENDER_WORKBENCH"
        sc.display.shading.light = "STUDIO"
        sc.display.shading.color_type = "MATERIAL"
        sc.display.shading.show_shadows = True
        sc.display.shading.show_cavity = True
        bpy.ops.render.render(write_still=True)
    print("PODGLAD:", plik)

def ziemia_podgladu(r=80, m="trawa"):
    g = Siatka("_ZiemiaPodgladu")
    g.wielokat([(-r, -r), (r, -r), (r, r), (-r, r)], -0.01, m)
    return g.obiekt()
