# -*- coding: utf-8 -*-
"""
_wnetrza_wspolne.py — pomocniki do wnętrz (hotel, radio, księgarnia, fotograf).
NIE jest modułem obiektu (nie ma zbuduj()). Korzysta tylko z lowpoly.py.

Ściany opisujemy osią i linią środkową:
    os_='x'  -> ściana biegnie wzdłuż X, leży na y=c, od x=s0 do x=s1
    os_='y'  -> ściana biegnie wzdłuż Y, leży na x=c, od y=s0 do y=s1
Otwory (drzwi, okna) rejestrujemy w Plan PRZED budową ścian i okładzin — wtedy
każda warstwa (ściana, boazeria, tapeta) sama je omija.
"""
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from lowpoly import *

GR = 0.2   # grubość ściany


class Plan:
    def __init__(self):
        self.otw = []   # (os, c, s0, s1, z0, z1, rodzaj)

    def otwor(self, os_, c, s, szer, z0, wys, rodzaj="drzwi"):
        self.otw.append((os_, c, s - szer / 2, s + szer / 2, z0, z0 + wys, rodzaj))

    def drzwi(self, os_, c, s, z0=0.0, szer=1.1, wys=2.2):
        self.otwor(os_, c, s, szer, z0, wys, "drzwi")

    def okno(self, os_, c, s, z0, szer, wys):
        self.otwor(os_, c, s, szer, z0, wys, "okno")

    def _box(self, m, os_, c, a, b, z0, z1, grub, mat):
        if os_ == "x":
            m.box_od_do(a, c - grub / 2, z0, b, c + grub / 2, z1, mat, bez_dolu=False)
        else:
            m.box_od_do(c - grub / 2, a, z0, c + grub / 2, b, z1, mat, bez_dolu=False)

    def pas(self, m, os_, c, s0, s1, z0, z1, mat, grub=GR, przes=0.0, tol=0.3):
        """Pas ściany (lub okładziny) z wyciętymi otworami leżącymi na linii c."""
        if s0 > s1:
            s0, s1 = s1, s0
        otw = [o for o in self.otw if o[0] == os_ and abs(o[1] - c) < tol
               and o[3] > s0 and o[2] < s1 and o[5] > z0 and o[4] < z1]
        cuts = {s0, s1}
        for o in otw:
            cuts.add(max(s0, min(s1, o[2]))); cuts.add(max(s0, min(s1, o[3])))
        cuts = sorted(cuts)
        for a, b in zip(cuts, cuts[1:]):
            if b - a < 1e-4:
                continue
            mid = (a + b) / 2
            dz = sorted((max(z0, o[4]), min(z1, o[5])) for o in otw if o[2] <= mid <= o[3])
            z = z0
            for za, zb in dz:
                if za > z + 1e-4:
                    self._box(m, os_, c + przes, a, b, z, za, grub, mat)
                z = max(z, zb)
            if z1 > z + 1e-4:
                self._box(m, os_, c + przes, a, b, z, z1, grub, mat)

    def sciana(self, m, os_, c, s0, s1, z0, z1, mat, grub=GR):
        """Ściana na linii środkowej, przedłużona o pół grubości na końcach (narożniki bez szpar)."""
        lo, hi = min(s0, s1) - grub / 2, max(s0, s1) + grub / 2
        self.pas(m, os_, c, lo, hi, z0, z1, mat, grub=grub)

    def okladzina(self, m, x0, x1, y0, y1, z0, z1, mat, grub=0.03, sciany="NESW", odsun=GR / 2):
        """Warstwa na wewnętrznych licach pokoju (osie ścian x0..x1, y0..y1): tapeta, boazeria, lamperia."""
        ix0, ix1, iy0, iy1 = x0 + odsun, x1 - odsun, y0 + odsun, y1 - odsun
        p = odsun + grub / 2
        if "S" in sciany:
            self.pas(m, "x", y0, ix0, ix1, z0, z1, mat, grub=grub, przes=+p)
        if "N" in sciany:
            self.pas(m, "x", y1, ix0, ix1, z0, z1, mat, grub=grub, przes=-p)
        if "W" in sciany:
            self.pas(m, "y", x0, iy0 + grub, iy1 - grub, z0, z1, mat, grub=grub, przes=+p)
        if "E" in sciany:
            self.pas(m, "y", x1, iy0 + grub, iy1 - grub, z0, z1, mat, grub=grub, przes=-p)

    def listwa(self, m, x0, x1, y0, y1, z, wys, wyst, mat, sciany="NESW"):
        """Listwa (gzyms, cokolik, poręcz lamperii) wystająca o wyst z lica."""
        self.okladzina(m, x0, x1, y0, y1, z, z + wys, mat, grub=wyst, sciany=sciany)

    # ------------------------------------------------------------------ wypełnienia otworów
    def wypelnij_okna(self, m_szyba, m_det, rama="rama_ciemna", parapet="kamien", szprosy=(1, 2),
                      strona_wew=None, gl_sciany=GR + 0.06, m_kol=None):
        """Szyba (obiekt z kolizją) + rama, szprosy, parapet wewnętrzny (detal).
        Okna 'okno_wew' (między pokojami) nie mają szyby `okno` – tylko niewidzialna kolizja w m_kol."""
        for (os_, c, s0, s1, z0, z1, r) in self.otw:
            if r not in ("okno", "okno_wew"):
                continue
            if r == "okno_wew" and m_kol is None:
                continue
            sz, wy = s1 - s0, z1 - z0
            sm = (s0 + s1) / 2
            B = lambda a, b, za, zb, g, mat, pr=0.0: self._box(m_det, os_, c + pr, a, b, za, zb, g, mat)
            # szyba w osi ściany
            if r == "okno":
                self._box(m_szyba, os_, c, s0, s1, z0, z1, 0.04, "okno")
            else:
                self._box(m_kol, os_, c, s0, s1, z0, z1, 0.04, "okno")
            # rama w ościeżu
            f = 0.08
            B(s0, s0 + f, z0, z1, 0.12, rama); B(s1 - f, s1, z0, z1, 0.12, rama)
            B(s0, s1, z0, z0 + f, 0.12, rama); B(s0, s1, z1 - f, z1, 0.12, rama)
            nv, nh = szprosy
            for i in range(1, nv + 1):
                s = s0 + sz * i / (nv + 1)
                B(s - 0.025, s + 0.025, z0, z1, 0.08, rama)
            for j in range(1, nh + 1):
                z = z0 + wy * j / (nh + 1)
                B(s0, s1, z - 0.025, z + 0.025, 0.08, rama)
            # parapety po obu stronach (lekko wystają)
            for sg in (+1, -1):
                B(s0 - 0.08, s1 + 0.08, z0 - 0.06, z0, 0.16, parapet, pr=sg * (GR / 2 + 0.02))

    def rama_drzwi(self, m_det, os_, c, s, z0=0.0, szer=1.1, wys=2.2, mat="drzwi_ciemne", strony=(+1, -1),
                   szer_opaski=0.12, wyst=0.04, glebokosc=GR + 0.08):
        """Opaska wokół otworu drzwiowego + okładzina ościeży (bez skrzydła, skrzydło to prefab)."""
        a, b = s - szer / 2, s + szer / 2
        B = lambda a_, b_, za, zb, g, pr=0.0: self._box(m_det, os_, c + pr, a_, b_, za, zb, g, mat)
        for sg in strony:
            pr = sg * (GR / 2 + 0.03 + wyst / 2)
            B(a - szer_opaski, a, z0, z0 + wys + szer_opaski, wyst, pr)
            B(b, b + szer_opaski, z0, z0 + wys + szer_opaski, wyst, pr)
            B(a, b, z0 + wys, z0 + wys + szer_opaski, wyst, pr)
        # ościeżnica (cienka okładzina w otworze)
        B(a, a + 0.03, z0, z0 + wys, glebokosc); B(b - 0.03, b, z0, z0 + wys, glebokosc)
        B(a, b, z0 + wys - 0.03, z0 + wys, glebokosc)


# ---------------------------------------------------------------------- podłogi
def posadzka_krata(m, x0, x1, y0, y1, z, nx, ny, m1, m2):
    """Kratka (szachownica) z quadów na wysokości z (płytki, normalne w górę)."""
    dx, dy = (x1 - x0) / nx, (y1 - y0) / ny
    for i in range(nx):
        for j in range(ny):
            a, b = x0 + i * dx, y0 + j * dy
            m.quad((a, b, z), (a + dx, b, z), (a + dx, b + dy, z), (a, b + dy, z), m1 if (i + j) % 2 == 0 else m2)


def deski(m, x0, x1, y0, y1, z, szer, m1, m2, wzdluz="y"):
    """Pasy desek parkietu naprzemiennie w dwóch tonach."""
    if wzdluz == "y":
        n = max(1, int(round((x1 - x0) / szer)))
        d = (x1 - x0) / n
        for i in range(n):
            a = x0 + i * d
            m.quad((a, y0, z), (a + d, y0, z), (a + d, y1, z), (a, y1, z), m1 if i % 2 == 0 else m2)
    else:
        n = max(1, int(round((y1 - y0) / szer)))
        d = (y1 - y0) / n
        for i in range(n):
            b = y0 + i * d
            m.quad((x0, b, z), (x1, b, z), (x1, b + d, z), (x0, b + d, z), m1 if i % 2 == 0 else m2)


def plyta(m, x0, x1, y0, y1, z_gora, gr, mat, dol=True):
    m.box_od_do(x0, y0, z_gora - gr, x1, y1, z_gora, mat, bez_dolu=not dol)


def sufit(m, x0, x1, y0, y1, z, mat="sufit", gr=0.2):
    """Płyta sufitu z widocznym spodem."""
    m.box_od_do(x0, y0, z, x1, y1, z + gr, mat, bez_dolu=False)


def schody_proste(m, x0, x1, y_start, kierunek, z0, z1, n, glebokosc, mat, mat_nosek=None, pelne=True):
    """Biegi schodów wzdłuż Y (kierunek +1/-1). Każdy stopień to pełny blok od z0 (pelne=True)."""
    h = (z1 - z0) / n
    for i in range(n):
        ya = y_start + kierunek * i * glebokosc
        yb = ya + kierunek * glebokosc
        zb = z0 + (i + 1) * h
        za = z0 if pelne else zb - h - 0.15
        m.box_od_do(x0, min(ya, yb), za, x1, max(ya, yb), zb, mat, bez_dolu=False)
        if mat_nosek:
            ye = yb - kierunek * 0.04 if False else ya
            m.box_od_do(x0, min(ya, ya + kierunek * 0.04), zb - 0.04, x1, max(ya, ya + kierunek * 0.04), zb + 0.005, mat_nosek)
    return h


def skos(m, x0, x1, ya, za, yb, zb, gr_z, mat):
    """Skośna płyta (np. balustrada pełna) w płaszczyźnie YZ, grubość w X od x0 do x1, wysokość gr_z."""
    P = [(ya, za), (yb, zb), (yb, zb + gr_z), (ya, za + gr_z)]
    L = [(x0, y, z) for (y, z) in P]
    R = [(x1, y, z) for (y, z) in P]
    m.poly(list(reversed(L)) if (yb - ya) > 0 else L, mat)
    m.poly(R if (yb - ya) > 0 else list(reversed(R)), mat)
    for i in range(4):
        j = (i + 1) % 4
        q = [L[i], L[j], R[j], R[i]]
        m.poly(q if (yb - ya) > 0 else list(reversed(q)), mat)


# ---------------------------------------------------------------------- oprawy (detal)
def kinkiet(m, x, y, z, nx, ny, mosiadz="zloto"):
    """Kinkiet ścienny: płytka mosiężna + ramię + klosz. (nx, ny) = normalna ściany do wnętrza."""
    m.box((x + nx * 0.02, y + ny * 0.02, z), (0.16 if ny else 0.04, 0.04 if ny else 0.16, 0.24), mosiadz)
    m.box((x + nx * 0.12, y + ny * 0.12, z + 0.05), (0.2 if nx else 0.04, 0.2 if ny else 0.04, 0.03), mosiadz)
    m.walec((x + nx * 0.22, y + ny * 0.22, z + 0.06), 0.09, 0.16, "lampa", n=6, r_gora=0.05)


def zyrandol(m, x, y, z_sufit, z_dol, r=0.8, mosiadz="zloto"):
    """Żyrandol art déco: pręt, stopniowany mosiężny talerz, mleczne klosze."""
    m.box((x, y, (z_sufit + z_dol + 0.4) / 2), (0.05, 0.05, z_sufit - z_dol - 0.4), mosiadz)
    m.walec((x, y, z_sufit - 0.06), 0.22, 0.06, mosiadz, n=8, dol=True)
    m.walec((x, y, z_dol + 0.25), r * 0.55, 0.15, mosiadz, n=8, r_gora=r * 0.3, dol=True)
    m.walec((x, y, z_dol), r * 0.35, 0.25, "lampa", n=8, r_gora=r * 0.5, dol=True)
    for k in range(6):
        a = 2 * math.pi * k / 6
        px, py = x + math.cos(a) * r, y + math.sin(a) * r
        m.belka((x, y, z_dol + 0.35), (px, py, z_dol + 0.3), 0.04, mosiadz)
        m.walec((px, py, z_dol + 0.3), 0.08, 0.2, "lampa", n=6, r_gora=0.04, dol=True)


def lampa_przemyslowa(m, x, y, z_sufit, z_dol, klosz="zielen_okienna"):
    """Emaliowana lampa na kablu: kabel, stożkowy klosz, żarówka."""
    m.box((x, y, (z_sufit + z_dol + 0.25) / 2), (0.02, 0.02, z_sufit - z_dol - 0.25), "czarny")
    m.walec((x, y, z_dol + 0.12), 0.05, 0.14, klosz, n=6)
    m.walec((x, y, z_dol), 0.28, 0.13, klosz, n=8, r_gora=0.06, dol=False)
    m.walec((x, y, z_dol + 0.01), 0.07, 0.1, "lampa", n=6, dol=True)


def zarowka(m, x, y, z_sufit, z_dol):
    m.box((x, y, (z_sufit + z_dol) / 2 + 0.05), (0.015, 0.015, z_sufit - z_dol - 0.1), "czarny")
    m.walec((x, y, z_dol), 0.06, 0.12, "lampa", n=6, r_gora=0.03, dol=True)


# ---------------------------------------------------------------------- podgląd
def swiatla_podgladu(mnoznik=1.0):
    """Zamienia META['swiatla'] na prawdziwe lampy Blendera (tylko do renderu podglądu, nie trafiają do FBX)."""
    obs = []
    for i, s in enumerate(META["swiatla"]):
        r, g, b = hex_rgb(s["kolor"])
        ld = bpy.data.lights.new("_Lp%d" % i, "POINT")
        ld.color = (r / 255, g / 255, b / 255)
        ld.energy = s["moc"] * 90.0 * mnoznik * (s["zasieg"] / 8.0) ** 2
        ld.shadow_soft_size = 0.15
        try:
            ld.use_custom_distance = True
            ld.cutoff_distance = s["zasieg"] * 1.6
        except Exception:
            pass
        o = bpy.data.objects.new("_Lp%d" % i, ld)
        o.location = s["p"]
        bpy.context.scene.collection.objects.link(o)
        obs.append(o)
    return obs


def ukryj_sufity(obs, ukryj=True, frag=("Sufit",)):
    for o in obs:
        if any(f in o.name for f in frag):
            o.hide_render = ukryj


def oczy(plik, oko, cel, ogniskowa=18, szer=1600, wys=900, noc=False):
    """Render z punktu 'oko' na 'cel' (np. z wysokości oczu gracza 1,65 m)."""
    o, c = Vector(oko), Vector(cel)
    d = o - c
    odl = d.length
    e = math.degrees(math.asin(d.z / odl))
    a = math.degrees(math.atan2(d.x, -d.y))
    render_podglad(plik, cel=tuple(c), odl=odl, azymut=a, wysokosc_kat=e, ogniskowa=ogniskowa, szer=szer, wys=wys, noc=noc)


def policz_trojkaty(obs):
    t = 0
    wyn = []
    for o in obs:
        if o.type != "MESH":
            continue
        n = sum(len(p.vertices) - 2 for p in o.data.polygons)
        wyn.append((o.name, n)); t += n
    for n, k in wyn:
        print("TRIS %-34s %6d" % (n, k))
    print("TRIS RAZEM", t)
    return t


# ---------------------------------------------------------------------- regały wbudowane
KSIAZKI = ("ksiazka_1", "ksiazka_2", "ksiazka_3", "ksiazka_4", "ksiazka_5")


def regal_wbudowany(P, m_kor, m_ks, os_, c, s0, s1, sg, z0, z1, n_polek, rng, gleb=0.36, segment=0.9,
                    mat_kor="drewno_ciemne", mat_polka="drewno", mat_tyl="boazeria", kolory=KSIAZKI,
                    jasne=("tkanina_bez",), gesto=0.9, szer=(0.07, 0.32), jasne_p=0.12):
    """Regał przy ścianie (linia c, oś os_), sg = kierunek do wnętrza pokoju.
    Korpus (cokół, gzyms, boki, półki, plecy) -> m_kor; rzędy książek bryłą -> m_ks."""
    w0 = c + sg * GR / 2
    srod = lambda a, b: (w0 + sg * a + w0 + sg * b) / 2
    B = lambda m, a, b, za, zb, d0, d1, mat: P._box(m, os_, srod(d0, d1), a, b, za, zb, abs(d1 - d0), mat)
    zc, zg = z0 + 0.12, z1 - 0.12
    B(m_kor, s0, s1, z0, zc, 0.0, gleb - 0.03, mat_kor)                       # cokół (cofnięty)
    B(m_kor, s0 - 0.04, s1 + 0.04, zg, z1, 0.0, gleb + 0.04, mat_kor)         # gzyms
    B(m_kor, s0, s1, zc, zg, 0.0, 0.02, mat_tyl)                              # plecy
    ns = max(1, int(round((s1 - s0) / segment)))
    d = (s1 - s0) / ns
    for i in range(ns + 1):                                                   # boki/przegrody
        s = s0 + i * d
        B(m_kor, s - 0.025, s + 0.025, zc, zg, 0.0, gleb, mat_kor)
    h = (zg - zc) / n_polek
    for j in range(1, n_polek):                                               # półki
        z = zc + j * h
        B(m_kor, s0, s1, z - 0.03, z, 0.02, gleb - 0.01, mat_polka)
    for i in range(ns):
        for j in range(n_polek):
            a, b = s0 + i * d + 0.03, s0 + (i + 1) * d - 0.03
            zb_ = zc + j * h
            hh = h - 0.03
            s = a + rng.uniform(0.0, 0.04)
            while s < b - 0.05:
                r = rng.random()
                if r > gesto:                                                 # luka
                    s += rng.uniform(0.08, 0.2)
                    continue
                if r < 0.1:                                                   # stos leżących
                    w = min(rng.uniform(0.2, 0.3), b - s)
                    hz = rng.uniform(0.07, 0.18)
                    fr = rng.uniform(0.04, 0.08)
                    B(m_ks, s, s + w, zb_, zb_ + hz, 0.05, gleb - fr, rng.choice(kolory))
                    s += w + 0.01
                    continue
                w = min(rng.uniform(*szer), b - s)
                hz = hh * rng.uniform(0.62, 0.9)
                fr = rng.uniform(0.02, 0.05)
                mat = rng.choice(jasne) if (jasne and rng.random() < jasne_p) else rng.choice(kolory)
                B(m_ks, s, s + w, zb_, zb_ + hz, 0.05, gleb - fr, mat)
                s += w + rng.uniform(0.004, 0.012)
