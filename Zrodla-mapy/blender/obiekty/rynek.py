# -*- coding: utf-8 -*-
"""
rynek.py — Rynek Gleiwitz 1939: Ratusz (z wieżą i podcieniami) + Fontanna Neptuna  -> Rynek.fbx
           oraz kościół Wszystkich Świętych (osobny origin)                         -> Kosciol.fbx
Ratusz: origin = środek ratusza, wejście na +Y, wieża na wschodzie (+X).
Fontanna: obiekt z pivotem w swoim środku, ustawiony 26 m na zachód (-X).
Kościół: origin = środek bryły, wieża na -Y, prezbiterium na +Y.
"""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from lowpoly import *

DACH_RATUSZA = "dachowka"        # zdjęcia współczesne: zielona miedź ("blacha_miedz"); brief: czerwona dachówka
HELM_RATUSZA = "blacha_miedz"


# ----------------------------------------------------------------------------- pomocniki
def poly_n(m, pts, mt, nrm):
    """Wielokąt z wymuszoną normalną (Unity robi culling)."""
    pts = [Vector(p) for p in pts]
    n = Vector((0, 0, 0))
    for i in range(len(pts)):
        a, b = pts[i], pts[(i + 1) % len(pts)]
        n += Vector(((a.y - b.y) * (a.z + b.z), (a.z - b.z) * (a.x + b.x), (a.x - b.x) * (a.y + b.y)))
    if n.dot(Vector(nrm)) < 0:
        pts = pts[::-1]
    m.poly([tuple(p) for p in pts], mt)


def W(c, t, n, u, v, off=0.0):
    """Punkt na ścianie: c = punkt bazowy, t = kierunek wzdłuż ściany, n = normalna na zewnątrz."""
    return (c[0] + t[0] * u + n[0] * off, c[1] + t[1] * u + n[1] * off, c[2] + v)


def ostrolok(w, h, k=5):
    hs = h - w * math.sqrt(3) / 2
    pts = [(-w / 2, 0), (w / 2, 0), (w / 2, hs)]
    for i in range(1, k):
        a = math.radians(60 * i / k); pts.append((-w / 2 + w * math.cos(a), hs + w * math.sin(a)))
    pts.append((0, h))
    for i in range(k - 1, 0, -1):
        a = math.radians(60 * i / k); pts.append((w / 2 - w * math.cos(a), hs + w * math.sin(a)))
    pts.append((-w / 2, hs))
    return pts


def polkolisty(w, h, k=6):
    hs = h - w / 2
    pts = [(-w / 2, 0), (w / 2, 0)]
    for i in range(k + 1):
        a = math.pi * i / k; pts.append((w / 2 * math.cos(a), hs + w / 2 * math.sin(a)))
    return pts


def ksztalt(m, ksz, c, t, n, off, mt):
    poly_n(m, [W(c, t, n, u, v, off) for (u, v) in ksz], mt, n)


def okno_gotyk(m, md, c, t, n, w, h, ram="cegla_ciemna", maswerk=True):
    ksztalt(m, ostrolok(w + 0.6, h + 0.4), W(c, t, n, 0, -0.2), t, n, 0.03, ram)
    ksztalt(m, ostrolok(w, h), c, t, n, 0.06, "okno")
    if maswerk and w > 1.2:
        a = W(c, t, n, 0, 0, 0.09); b = W(c, t, n, 0, h - w * 0.55, 0.09)
        md.belka(a, b, 0.12, "kamien")
        for s in (-1, 1):
            p = W(c, t, n, s * w / 4, h - w * 0.5, 0.09)
            md.belka(b, (p[0], p[1], p[2] + 0.35), 0.1, "kamien")


def pryzmat(m, cx, cy, z0, r, h, mt, n=8, rot=22.5, r_gora=None, gora=True, dol=False, do_srodka=False, m_gora=None):
    rg = r if r_gora is None else r_gora
    A = [math.radians(rot + 360.0 * i / n) for i in range(n)]
    d = [(cx + r * math.cos(a), cy + r * math.sin(a), z0) for a in A]
    g = [(cx + rg * math.cos(a), cy + rg * math.sin(a), z0 + h) for a in A]
    for i in range(n):
        j = (i + 1) % n
        am = (A[i] + A[j]) / 2
        nr = Vector((math.cos(am), math.sin(am), 0.3 if rg < r else 0))
        if do_srodka:
            nr = -nr
        poly_n(m, [d[i], d[j], g[j], g[i]] if rg > 1e-4 else [d[i], d[j], g[i]], mt, nr)
    if gora and rg > 1e-4:
        poly_n(m, g, m_gora or mt, (0, 0, -1 if do_srodka else 1))
    if dol:
        poly_n(m, d, mt, (0, 0, -1))
    return d, g


def pierscien(m, cx, cy, z, r0, r1, mt, n=8, rot=22.5, w_dol=False):
    for i in range(n):
        a, b = math.radians(rot + 360.0 * i / n), math.radians(rot + 360.0 * (i + 1) / n)
        poly_n(m, [(cx + r0 * math.cos(a), cy + r0 * math.sin(a), z), (cx + r1 * math.cos(a), cy + r1 * math.sin(a), z),
                   (cx + r1 * math.cos(b), cy + r1 * math.sin(b), z), (cx + r0 * math.cos(b), cy + r0 * math.sin(b), z)],
               mt, (0, 0, -1 if w_dol else 1))


def tarcza(md, c, t, n, r):
    """Tarcza zegara na ścianie (bez cyfr): złoty pierścień, biała tarcza, czarne wskazówki."""
    for rr, off, mt in ((r + 0.18, 0.03, "zloto"), (r, 0.06, "bialy")):
        poly_n(md, [W(c, t, n, rr * math.cos(2 * math.pi * i / 16), rr * math.sin(2 * math.pi * i / 16), off) for i in range(16)], mt, n)
    for i in range(12):
        a = 2 * math.pi * i / 12
        poly_n(md, [W(c, t, n, r * 0.82 * math.cos(a) + dx, r * 0.82 * math.sin(a) + dy, 0.07)
                    for dx, dy in ((-0.05, -0.05), (0.05, -0.05), (0.05, 0.05), (-0.05, 0.05))], "czarny", n)
    for (a, L, g) in ((math.radians(60), 0.55, 0.07), (math.radians(160), 0.8, 0.05)):
        ux, vy = math.cos(a), math.sin(a)
        px, py = -vy * g, ux * g
        poly_n(md, [W(c, t, n, -px, -py, 0.08), W(c, t, n, px, py, 0.08),
                    W(c, t, n, ux * r * L + px, vy * r * L + py, 0.08), W(c, t, n, ux * r * L - px, vy * r * L - py, 0.08)], "czarny", n)


def arkada(m, a, b, n_przesel, filar, z_luk, gora, grub, mt, mt_w):
    """Ściana z łukami półkolistymi od a do b (2D); na zewnątrz = (t.y, -t.x)."""
    a = Vector((a[0], a[1], 0)); b = Vector((b[0], b[1], 0))
    L = (b - a).length; t = (b - a).normalized(); n = Vector((t.y, -t.x, 0))
    kat = math.degrees(math.atan2(t.y, t.x))
    p = L / n_przesel
    for i in range(n_przesel + 1):
        u = min(max(i * p, filar / 2), L - filar / 2)
        s = a + t * u - n * (grub / 2)
        m.box((s.x, s.y, gora / 2), (filar, grub, gora), mt, rot_z=kat)
    for i in range(n_przesel):
        u0, u1 = i * p + filar / 2, (i + 1) * p - filar / 2
        if i == 0: u0 = filar
        if i == n_przesel - 1: u1 = L - filar
        r = (u1 - u0) / 2; uc = (u0 + u1) / 2
        K = 8
        arc = [(uc - r * math.cos(math.pi * k / K), z_luk + r * math.sin(math.pi * k / K)) for k in range(K + 1)]
        for k in range(K):
            (ua, va), (ub, vb) = arc[k], arc[k + 1]
            for off, nn, mm in ((0.0, n, mt), (-grub, -n, mt_w)):
                q = lambda u, v: tuple(a + t * u + n * off + Vector((0, 0, v)))
                poly_n(m, [q(ua, va), q(ub, vb), q(ub, gora), q(ua, gora)], mm, nn)
            q0 = lambda u, v: tuple(a + t * u + Vector((0, 0, v)))
            q1 = lambda u, v: tuple(a + t * u - n * grub + Vector((0, 0, v)))
            um, vm = (ua + ub) / 2, (va + vb) / 2
            nr = t * (uc - um) + Vector((0, 0, z_luk - vm))
            poly_n(m, [q0(ua, va), q0(ub, vb), q1(ub, vb), q1(ua, va)], mt_w, nr)
        # pionowe wnętrze filarów pod łukiem
        for uu, sg in ((u0, 1), (u1, -1)):
            poly_n(m, [tuple(a + t * uu), tuple(a + t * uu - n * grub), tuple(a + t * uu - n * grub + Vector((0, 0, z_luk))),
                       tuple(a + t * uu + Vector((0, 0, z_luk)))], mt_w, t * sg)


def opaska(md, x0, y0, x1, y1, z, h, wys, mt):
    """Gzyms / pas dookoła prostokąta (wysunięty o wys)."""
    md.box_od_do(x0 - wys, y0 - wys, z, x1 + wys, y0, z + h, mt)
    md.box_od_do(x0 - wys, y1, z, x1 + wys, y1 + wys, z + h, mt)
    md.box_od_do(x0 - wys, y0, z, x0, y1, z + h, mt)
    md.box_od_do(x1, y0, z, x1 + wys, y1, z + h, mt)


def okna_z_naczolkiem(m, md, a, b, z0, h, w, roz, naczolek=True, ram="piaskowiec"):
    ile = m.okna_rzad(a, b, z0, h, w, roz, m_rama=ram)
    if not ile or not naczolek:
        return
    A = Vector((a[0], a[1], 0)); B = Vector((b[0], b[1], 0))
    L = (B - A).length; t = (B - A).normalized(); n = Vector((t.y, -t.x, 0))
    kat = math.degrees(math.atan2(t.y, t.x)); krok = L / ile
    for i in range(ile):
        s = A + t * (krok * (i + 0.5)) + n * 0.1
        md.box((s.x, s.y, z0 + h + 0.25), (w + 0.6, 0.2, 0.16), "piaskowiec", rot_z=kat)
        md.box((s.x, s.y, z0 + h + 0.12), (w + 0.3, 0.12, 0.1), "piaskowiec", rot_z=kat)


def dach_naczolkowy(m, x0, x1, y0, y1, z0, h, f, mt, mt_szczyt, o=0.5, ox=0.3):
    ym = (y0 + y1) / 2; Wp = (y1 - y0) / 2 + o
    zg = z0 + f * h; wg = Wp * (1 - f)
    ins = (1 - f) * h * 0.9
    XA, XB = x0 - ox, x1 + ox
    for s in (-1, 1):
        ye = ym + s * Wp
        poly_n(m, [(XA, ye, z0), (XB, ye, z0), (XB, ym + s * wg, zg), (x1 - ins, ym, z0 + h), (x0 + ins, ym, z0 + h), (XA, ym + s * wg, zg)],
               mt, (0, s, 1))
        poly_n(m, [(XA, ye, z0 - 0.01), (XB, ye, z0 - 0.01), (XB, ym, z0 - 0.01), (XA, ym, z0 - 0.01)], mt_szczyt, (0, 0, -1))
    for xg, xi, s in ((XA, x0 + ins, -1), (XB, x1 - ins, 1)):
        poly_n(m, [(xg, ym - wg, zg), (xg, ym + wg, zg), (xi, ym, z0 + h)], mt, (s, 0, 1))
        xs = x0 if s < 0 else x1
        w0 = (y1 - y0) / 2; wz = wg * (w0 / Wp)
        poly_n(m, [(xg, ym - w0 - 0.0, z0), (xg, ym + w0, z0), (xg, ym + wg, zg), (xg, ym - wg, zg)], mt_szczyt, (s, 0, 0))
    return lambda y: z0 + (Wp - abs(y - ym)) * h / Wp


# ----------------------------------------------------------------------------- RATUSZ
def ratusz():
    m = Siatka("Ratusz"); md = Siatka("Ratusz_Detal_NOCOL")
    X0, X1, Y0, Y1 = -15.0, 15.0, -8.0, 8.0
    ZP, ZG = 5.0, 10.4                     # parter / okap
    m.box_od_do(X0, Y0, 0, X1, Y1, ZG, "tynk_kremowy")
    m.box_od_do(X0 - 0.12, Y0 - 0.12, 0, X1 + 0.12, Y1 + 0.12, 0.6, "cokol")
    # boniowanie parteru (S, E, W; front kryją podcienia)
    for z in [0.6 + 0.55 * k for k in range(1, 8)]:
        md.box_od_do(X0 - 0.07, Y0 - 0.07, z, X1 + 0.07, Y0, z + 0.36, "tynk_bez")
        md.box_od_do(X0 - 0.07, Y0, z, X0, Y1, z + 0.36, "tynk_bez")
        md.box_od_do(X1, Y0, z, X1 + 0.07, Y1, z + 0.36, "tynk_bez")
    opaska(md, X0, Y0, X1, Y1, ZP, 0.35, 0.22, "piaskowiec")
    opaska(md, X0, Y0, X1, Y1, ZG - 0.55, 0.3, 0.2, "piaskowiec")
    opaska(md, X0, Y0, X1, Y1, ZG - 0.25, 0.25, 0.45, "piaskowiec")
    # lizeny piętra
    for x in (X0 + 0.45, -5.0, 5.0, X1 - 0.45):
        for y, s in ((Y0, -1), (Y1, 1)):
            md.box((x, y + s * 0.06, (ZP + ZG) / 2), (0.9, 0.12, ZG - ZP - 0.3), "piaskowiec")
    for y in (Y0 + 0.45, Y1 - 0.45):
        for x, s in ((X0, -1), (X1, 1)):
            md.box((x + s * 0.06, y, (ZP + ZG) / 2), (0.12, 0.9, ZG - ZP - 0.3), "piaskowiec")
    # okna
    okna_z_naczolkiem(m, md, (X0, Y0), (X1, Y0), 6.4, 2.5, 1.3, 3.3)               # S piętro
    okna_z_naczolkiem(m, md, (X0, Y0), (X1, Y0), 1.5, 2.4, 1.2, 3.3, naczolek=False)
    okna_z_naczolkiem(m, md, (X1, Y1), (X0, Y1), 6.7, 2.3, 1.3, 3.3)               # N piętro (nad tarasem)
    okna_z_naczolkiem(m, md, (X0, Y1), (X0, Y0), 6.4, 2.5, 1.3, 3.3)               # W
    okna_z_naczolkiem(m, md, (X0, Y1), (X0, Y0), 1.5, 2.4, 1.2, 3.3, naczolek=False)
    for (ya, yb) in ((Y0, -3.4), (3.4, Y1)):                                        # E (obok wieży)
        okna_z_naczolkiem(m, md, (X1, ya), (X1, yb), 6.4, 2.5, 1.3, 3.0)
        okna_z_naczolkiem(m, md, (X1, ya), (X1, yb), 1.5, 2.4, 1.2, 3.0, naczolek=False)
    # wejście główne (+Y, w podcieniach)
    m.box((0, Y1 + 0.06, 1.75), (2.0, 0.12, 3.5), "drzwi_ciemne")
    md.box((0, Y1 + 0.12, 3.75), (3.0, 0.24, 0.4), "piaskowiec")
    for s in (-1, 1):
        md.box((s * 1.2, Y1 + 0.1, 1.8), (0.35, 0.2, 3.6), "piaskowiec")
    # kartusz herbowy nad wejściem (gładki)
    poly_n(md, [(0.9 * math.cos(2 * math.pi * i / 12), Y1 + 0.08, 8.0 + 1.1 * math.sin(2 * math.pi * i / 12)) for i in range(12)], "piaskowiec", (0, 1, 0))
    # dach z naczółkami + lukarny + kominy
    zdach = dach_naczolkowy(m, X0, X1, Y0, Y1, ZG, 8.2, 0.62, DACH_RATUSZA, "tynk_kremowy")
    for x in (-10.5, -4.0, 3.0):
        for s in (-1, 1):
            y = s * 4.6; zb = zdach(y) - 0.9
            m.box((x, y, zb + 1.0), (1.6, 3.0, 2.0), "tynk_kremowy")
            m.box((x, y + s * 1.52, zb + 1.0), (0.9, 0.04, 1.1), "okno")
            m.dach_dwuspadowy((x, y, zb + 2.0), 3.0, 1.6, 0.9, DACH_RATUSZA, m_szczyt="tynk_kremowy", rot_z=90, okap=0.15)
    for x, y in ((-8.0, -2.5), (6.0, 2.5)):
        m.box((x, y, zdach(y) + 0.6), (0.9, 0.9, 2.4), "cegla")
        md.box((x, y, zdach(y) + 1.85), (1.15, 1.15, 0.15), "kamien_ciemny")

    # --- WIEŻA (wschód)
    w = Siatka("Ratusz_Wieza")
    cx, A = 12.2, 3.0
    w.box_od_do(cx - A, -A, 0, cx + A, A, 21.6, "tynk_kremowy")
    for sx in (-1, 1):
        for sy in (-1, 1):
            md.box((cx + sx * (A - 0.35), sy * (A + 0.06), 15.8), (0.7, 0.12, 11.6), "piaskowiec")
            md.box((cx + sx * (A + 0.06), sy * (A - 0.35), 15.8), (0.12, 0.7, 11.6), "piaskowiec")
    opaska(md, cx - A, -A, cx + A, A, 21.3, 0.35, 0.3, "piaskowiec")
    opaska(md, cx - A, -A, cx + A, A, 18.5, 0.2, 0.12, "piaskowiec")
    for c, t, n in (((cx, -A, 0), (1, 0, 0), (0, -1, 0)), ((cx, A, 0), (-1, 0, 0), (0, 1, 0)), ((cx + A, 0, 0), (0, 1, 0), (1, 0, 0))):
        ksztalt(w, polkolisty(1.5, 2.9), (c[0], c[1], 14.6), t, n, 0.04, "piaskowiec")
        ksztalt(w, polkolisty(1.1, 2.6), (c[0], c[1], 14.75), t, n, 0.07, "okno")
    # górna kondygnacja ze ściętymi narożami, tarcze zegara
    B, sc = 2.75, 0.85
    osm = [(cx - B + sc, -B), (cx + B - sc, -B), (cx + B, -B + sc), (cx + B, B - sc), (cx + B - sc, B), (cx - B + sc, B), (cx - B, B - sc), (cx - B, -B + sc)]
    w.obrys(osm, 21.6, 28.6, "tynk_kremowy")
    for c, t, n in (((cx, -B, 25.9), (1, 0, 0), (0, -1, 0)), ((cx, B, 25.9), (-1, 0, 0), (0, 1, 0)),
                    ((cx + B, 0, 25.9), (0, 1, 0), (1, 0, 0)), ((cx - B, 0, 25.9), (0, -1, 0), (-1, 0, 0))):
        tarcza(md, c, t, n, 1.05)
        ksztalt(w, polkolisty(0.9, 1.6), (c[0], c[1], 22.6), t, n, 0.05, "okno")
    big = [(cx + (x - cx) * 1.12, y * 1.12) for x, y in osm]
    w.obrys(big, 28.6, 29.0, "piaskowiec")
    # latarnia ośmioboczna (otwarta)
    pryzmat(w, cx, 0, 29.0, 1.0, 3.6, "metal_ciemny")
    for i in range(8):
        a = math.radians(22.5 + 45 * i)
        w.box((cx + 1.85 * math.cos(a), 1.85 * math.sin(a), 30.8), (0.32, 0.32, 3.6), "tynk_kremowy", rot_z=math.degrees(a))
    pryzmat(md, cx, 0, 29.0, 2.05, 0.9, "piaskowiec", r_gora=2.05)     # balustrada latarni
    pryzmat(w, cx, 0, 32.6, 2.3, 0.4, "piaskowiec")
    # barokowy hełm
    prof = [(33.0, 2.3), (33.7, 1.55), (34.6, 1.35), (35.3, 1.6), (36.0, 1.1), (36.6, 0.55), (37.3, 0.45), (38.0, 0.25), (39.6, 0.08)]
    for (z0, r0), (z1, r1) in zip(prof, prof[1:]):
        pryzmat(w, cx, 0, z0, r0, z1 - z0, HELM_RATUSZA, r_gora=r1, gora=False)
    pryzmat(md, cx, 0, 39.6, 0.06, 1.9, "zloto", n=6, r_gora=0.03)
    pryzmat(md, cx, 0, 40.2, 0.22, 0.42, "zloto", n=8, r_gora=0.22)
    for a in range(4):
        aa = math.radians(45 * a)
        md.belka((cx + 0.35 * math.cos(aa), 0.35 * math.sin(aa), 41.1), (cx - 0.35 * math.cos(aa), -0.35 * math.sin(aa), 41.1), 0.05, "zloto")
        md.belka((cx, 0, 40.8), (cx, 0, 41.5), 0.05, "zloto")

    # --- PODCIENIA (przybudówka z tarasem, +Y)
    p = Siatka("Ratusz_Podcienia")
    YP = 12.6; GR = 0.7; ZL, ZT = 2.7, 5.3
    arkada(p, (X1, YP), (X0, YP), 7, 0.8, ZL, ZT, GR, "tynk_kremowy", "tynk_ochra")
    arkada(p, (X1, Y1), (X1, YP), 1, 0.8, ZL, ZT, GR, "tynk_kremowy", "tynk_ochra")
    arkada(p, (X0, YP), (X0, Y1), 1, 0.8, ZL, ZT, GR, "tynk_kremowy", "tynk_ochra")
    p.box_od_do(X0 - 0.15, Y1, ZT, X1 + 0.15, YP + 0.15, ZT + 0.35, "piaskowiec", bez_dolu=False, m_dol="tynk_ochra")
    p.box_od_do(X0 + 0.7, Y1, 0, X1 - 0.7, YP - 0.7, 0.15, "kamien")
    md.box_od_do(X0 - 0.25, YP - 0.1, ZT - 0.2, X1 + 0.25, YP + 0.25, ZT, "piaskowiec")
    for k in range(-7, 8):                                   # medaliony w przyłuczach
        if k % 2:
            continue
    for i in range(8):
        x = X1 - i * (30.0 / 7)
        poly_n(md, [(x + 0.32 * math.cos(2 * math.pi * j / 8), YP + 0.03, 4.55 + 0.32 * math.sin(2 * math.pi * j / 8)) for j in range(8)], "piaskowiec", (0, 1, 0))
    # balustrada tarasu
    for (xa, ya, xb, yb) in ((X0, YP, X1, YP), (X0, Y1, X0, YP), (X1, Y1, X1, YP)):
        L = math.hypot(xb - xa, yb - ya); n = max(2, int(L / 0.45))
        for i in range(n + 1):
            f = i / n
            md.box((xa + (xb - xa) * f, ya + (yb - ya) * f, ZT + 0.75), (0.13, 0.13, 0.8), "piaskowiec")
        md.belka((xa, ya, ZT + 1.2), (xb, yb, ZT + 1.2), 0.2, "piaskowiec")
        for f in (0.0, 0.5, 1.0):
            if L > 10 or f != 0.5:
                md.box((xa + (xb - xa) * f, ya + (yb - ya) * f, ZT + 0.7), (0.4, 0.4, 1.0), "piaskowiec")
    for x in (-12.0, -4.3, 4.3, 12.0):
        swiatlo((x, 10.3, 4.6), kolor="#ffcf8a", zasieg=7, moc=1.5)
    obs = [m.obiekt(), w.obiekt(), p.obiekt(), md.obiekt()]
    punkt("miasto_karta_1", (cx + A + 1.3, 0.0, 0.0), 270.0)
    punkt("ratusz_wejscie", (0.0, YP + 1.5, 0.0), 180.0)
    return obs


# ----------------------------------------------------------------------------- FONTANNA NEPTUNA
def fontanna(poz):
    m = Siatka("Fontanna_Neptun"); md = Siatka("Fontanna_Neptun_Detal_NOCOL"); wd = Siatka("Fontanna_Neptun_Woda_NOCOL")
    pryzmat(m, 0, 0, 0, 3.05, 0.2, "kamien_ciemny")
    pryzmat(m, 0, 0, 0.2, 2.7, 0.6, "kamien", gora=False)
    pryzmat(m, 0, 0, 0.8, 2.82, 0.16, "piaskowiec", gora=False)
    pierscien(m, 0, 0, 0.96, 2.32, 2.82, "piaskowiec")
    pryzmat(m, 0, 0, 0.25, 2.32, 0.71, "kamien", gora=False, do_srodka=True)
    pierscien(m, 0, 0, 0.25, 0.0, 2.32, "dno")
    poly_n(wd, [(2.32 * math.cos(math.radians(22.5 + 45 * i)), 2.32 * math.sin(math.radians(22.5 + 45 * i)), 0.72) for i in range(8)], "woda", (0, 0, 1))
    # postument
    pryzmat(m, 0, 0, 0.25, 0.85, 1.35, "kamien", n=4, rot=45)
    pryzmat(m, 0, 0, 1.6, 1.05, 0.2, "piaskowiec", n=4, rot=45)
    pryzmat(m, 0, 0, 1.8, 0.55, 0.7, "kamien", n=8)
    pryzmat(m, 0, 0, 2.5, 0.75, 0.15, "piaskowiec", n=8)
    pryzmat(m, 0, 0, 2.65, 0.5, 0.2, "kamien_ciemny", n=7, r_gora=0.42)
    # rzygacze (4 maszkarony) i strumienie
    for i in range(4):
        a = math.radians(90 * i)
        c, s = math.cos(a), math.sin(a)
        md.box((0.62 * c, 0.62 * s, 1.15), (0.3, 0.3, 0.3), "kamien_ciemny", rot_z=math.degrees(a))
        md.belka((0.75 * c, 0.75 * s, 1.15), (1.35 * c, 1.35 * s, 0.75), 0.07, "woda")
    # figura Neptuna (stylizowana)
    F = "piaskowiec"
    md.box((-0.13, 0.0, 3.3), (0.2, 0.24, 0.9), F)
    md.box((0.14, 0.1, 3.3), (0.2, 0.24, 0.9), F, rot_z=8)
    md.box((0, 0.03, 3.8), (0.5, 0.3, 0.22), F)
    pryzmat(md, 0, 0.03, 3.85, 0.25, 0.65, F, n=6, r_gora=0.33)
    md.box((0, 0.03, 4.55), (0.78, 0.32, 0.16), F)
    pryzmat(md, 0, 0.03, 4.6, 0.07, 0.12, F, n=6)
    pryzmat(md, 0, 0.03, 4.7, 0.14, 0.28, F, n=6, r_gora=0.12)
    md.tri((-0.1, 0.16, 4.78), (0.1, 0.16, 4.78), (0, 0.2, 4.55), F)                       # broda
    for i in range(5):                                                                      # korona
        a = math.radians(72 * i)
        md.belka((0.1 * math.cos(a), 0.03 + 0.1 * math.sin(a), 4.96), (0.13 * math.cos(a), 0.03 + 0.13 * math.sin(a), 5.12), 0.04, "zloto")
    md.belka((-0.38, 0.03, 4.55), (-0.45, 0.15, 3.95), 0.13, F)                              # lewa ręka w dół
    md.belka((0.38, 0.03, 4.55), (0.62, 0.12, 4.95), 0.13, F)                               # prawa w górę
    # płaszcz
    poly_n(md, [(-0.42, -0.14, 4.6), (0.42, -0.14, 4.6), (0.5, -0.3, 3.0), (-0.5, -0.26, 3.0)], F, (0, -1, 0))
    # trójząb
    tz = "metal_ciemny"
    md.belka((0.64, 0.14, 2.85), (0.64, 0.14, 5.25), 0.06, tz)
    md.belka((0.44, 0.14, 5.25), (0.84, 0.14, 5.25), 0.06, tz)
    for x in (0.44, 0.64, 0.84):
        md.belka((x, 0.14, 5.25), (x, 0.14, 5.6 if x == 0.64 else 5.5), 0.05, tz)
    obs = [m.obiekt(poz), md.obiekt(poz), wd.obiekt(poz)]
    return obs


def zbuduj():
    obs = ratusz()
    obs += fontanna((-26.0, 0.0, 0.0))
    return obs


# ----------------------------------------------------------------------------- KOŚCIÓŁ
def zbuduj_kosciol():
    m = Siatka("Kosciol_WszystkichSwietych"); md = Siatka("Kosciol_Detal_NOCOL")
    w = Siatka("Kosciol_Wieza"); pp = Siatka("Kosciol_Przypory")
    C, CC = "cegla", "cegla_ciemna"
    NX, NY0, NY1, NH = 11.0, -14.0, 12.0, 15.0
    m.box_od_do(-NX, NY0, 0, NX, NY1, NH, C)
    m.box_od_do(-NX - 0.15, NY0, 0, NX + 0.15, NY1, 1.0, "kamien_ciemny")
    md.box_od_do(-NX - 0.2, NY0, NH - 1.0, -NX, NY1, NH - 0.6, CC)
    md.box_od_do(NX, NY0, NH - 1.0, NX + 0.2, NY1, NH - 0.6, CC)
    m.dach_dwuspadowy((0, (NY0 + NY1) / 2, NH), NY1 - NY0, 2 * NX, 14.0, "dachowka_ciemna", m_szczyt=C, rot_z=90, okap=0.5)
    # szczyt wschodni (nad prezbiterium) – blendy
    for i, x in enumerate((-4.5, -1.5, 1.5, 4.5)):
        hh = 7.5 - abs(x) * 0.9
        ksztalt(md, ostrolok(1.4, hh), (x, NY1, NH + 0.4), (-1, 0, 0), (0, 1, 0), 0.05, "tynk_bialy")
    # okna i przypory nawy
    ys = [NY0 + 5.2 * k for k in range(6)]
    for s in (-1, 1):
        t = (0, -s, 0) if s < 0 else (0, 1, 0)
        n = (s, 0, 0)
        for y in ys:
            x = s * NX
            pp.box((x + s * 1.0, y, 4.5), (2.0, 1.3, 9.0), C)
            pp.box((x + s * 0.7, y, 11.3), (1.4, 1.1, 4.6), C)
            md.box((x + s * 1.05, y, 9.05), (2.1, 1.4, 0.18), "piaskowiec")
            md.box((x + s * 0.75, y, 13.65), (1.5, 1.2, 0.18), "piaskowiec")
            pp.box((x + s * 0.35, y, 14.3), (0.7, 0.9, 1.4), C)
        for y in ys[:-1]:
            okno_gotyk(m, md, (s * NX, y + 2.6, 3.6), t, n, 2.6, 9.0)
    # portal boczny (S = -X? tu: zachodnia ściana nawy)
    ksztalt(m, ostrolok(3.2, 5.2), (-NX, -3.6 + 2.6 - 2.6, 0.0), (0, -1, 0), (-1, 0, 0), 0.04, "piaskowiec")
    # prezbiterium
    R, PY = 6.5, 19.0
    ap = [(-R, NY1), (R, NY1), (R, PY)] + [(R * math.cos(math.radians(a)), PY + R * math.sin(math.radians(a))) for a in (45, 90, 135)] + [(-R, PY)]
    m.obrys(ap, 0, 13.0, C, gora=False)
    m.obrys(ap, 0, 1.0, "kamien_ciemny", gora=False)
    m.wielokat(ap, 13.0, C)
    # dach prezbiterium
    o = 0.5; zr = 21.5
    E = [(-R - o, NY1), (R + o, NY1), (R + o, PY)] + [((R + o) * math.cos(math.radians(a)), PY + (R + o) * math.sin(math.radians(a))) for a in (45, 90, 135)] + [(-R - o, PY)]
    poly_n(m, [(E[1][0], E[1][1], 12.8), (E[2][0], E[2][1], 12.8), (0, PY, zr), (0, NY1, zr)], "dachowka_ciemna", (1, 0, 1))
    poly_n(m, [(E[6][0], E[6][1], 12.8), (E[0][0], E[0][1], 12.8), (0, NY1, zr), (0, PY, zr)], "dachowka_ciemna", (-1, 0, 1))
    for i in range(2, 6):
        a, b = E[i], E[i + 1]
        poly_n(m, [(a[0], a[1], 12.8), (b[0], b[1], 12.8), (0, PY, zr)], "dachowka_ciemna", ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2 - PY, 1))
    # okna i przypory prezbiterium
    for s in (-1, 1):
        okno_gotyk(m, md, (s * R, 15.5, 3.2), (0, 1, 0) if s > 0 else (0, -1, 0), (s, 0, 0), 2.0, 8.2)
    pk = ap[2:]
    for i in range(len(pk) - 1):
        a, b = pk[i], pk[i + 1]
        mx, my = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
        L = math.hypot(b[0] - a[0], b[1] - a[1]); t = ((b[0] - a[0]) / L, (b[1] - a[1]) / L, 0)
        n = (t[1], -t[0], 0)
        if n[0] * mx + n[1] * (my - PY) < 0:
            n = (-n[0], -n[1], 0); t = (-t[0], -t[1], 0)
        okno_gotyk(m, md, (mx, my, 3.2), t, n, 1.8, 8.2)
    for k, (x, y) in enumerate(pk[:-1] + [pk[-1]]):
        if k == 0 or k == len(pk) - 1:
            a = 0 if x > 0 else 180
        else:
            a = math.degrees(math.atan2(y - PY, x))
        c, s_ = math.cos(math.radians(a)), math.sin(math.radians(a))
        pp.box((x + c * 0.9, y + s_ * 0.9, 4.2), (1.8, 1.1, 8.4), C, rot_z=a)
        pp.box((x + c * 0.6, y + s_ * 0.6, 10.2), (1.2, 0.9, 3.6), C, rot_z=a)
        md.box((x + c * 0.95, y + s_ * 0.95, 8.5), (1.9, 1.2, 0.18), "piaskowiec", rot_z=a)
    # sygnaturka na kalenicy
    sy = 7.0
    m.box((0, sy, 28.0), (1.6, 1.6, 2.4), "blacha")
    pryzmat(m, 0, sy, 29.2, 0.85, 1.6, "metal_ciemny")
    pryzmat(m, 0, sy, 30.8, 1.0, 0.25, "blacha")
    for (z0, r0), (z1, r1) in zip([(31.05, 0.9), (31.6, 0.55), (32.1, 0.7), (32.6, 0.25), (35.2, 0.04)],
                                  [(31.6, 0.55), (32.1, 0.7), (32.6, 0.25), (35.2, 0.04), (35.3, 0.0)]):
        pryzmat(m, 0, sy, z0, r0, z1 - z0, "blacha", r_gora=r1, gora=False)

    # --- WIEŻA (-Y)
    TX, TY0, TY1, TH = 5.6, -25.2, NY0, 38.0
    TC = (TY0 + TY1) / 2
    w.box_od_do(-TX, TY0, 0, TX, TY1, TH, C)
    w.box_od_do(-TX - 0.15, TY0 - 0.15, 0, TX + 0.15, TY1, 1.2, "kamien_ciemny")
    for z in (13.0, 26.0):
        opaska(md, -TX, TY0, TX, TY1, z, 0.5, 0.18, CC)
    for sx in (-1, 1):                                   # narożne przypory wieży
        pp.box((sx * (TX + 0.6), TY0 + 0.7, 9.0), (1.2, 1.4, 18.0), C)
        pp.box((sx * (TX - 0.7), TY0 - 0.6, 9.0), (1.4, 1.2, 18.0), C)
        pp.box((sx * (TX + 0.4), TY0 + 0.6, 24.0), (0.8, 1.2, 12.0), C)
        pp.box((sx * (TX - 0.6), TY0 - 0.4, 24.0), (1.2, 0.8, 12.0), C)
    # portal z wimpergą
    w.box_od_do(-3.2, TY0 - 1.0, 0, 3.2, TY0, 7.4, C)
    poly_n(w, [(-3.2, TY0 - 1.0, 7.4), (3.2, TY0 - 1.0, 7.4), (0, TY0 - 1.0, 10.4)], C, (0, -1, 0))
    poly_n(w, [(3.2, TY0 - 1.0, 7.4), (3.2, TY0, 7.4), (0, TY0, 10.4), (0, TY0 - 1.0, 10.4)], "dachowka_ciemna", (1, 0, 1))
    poly_n(w, [(-3.2, TY0, 7.4), (-3.2, TY0 - 1.0, 7.4), (0, TY0 - 1.0, 10.4), (0, TY0, 10.4)], "dachowka_ciemna", (-1, 0, 1))
    pc = (0, TY0 - 1.0, 0)
    for (ww, hh, off, mt) in ((4.6, 7.0, 0.03, "piaskowiec"), (3.8, 6.2, 0.05, CC), (3.0, 5.4, 0.07, "piaskowiec"), (2.3, 4.6, 0.09, "drzwi_ciemne")):
        ksztalt(w, ostrolok(ww, hh), pc, (1, 0, 0), (0, -1, 0), off, mt)
    md.box((0, TY0 - 1.12, 2.3), (0.08, 0.06, 4.4), "metal_ciemny")
    # okna wieży
    for c, t, n in (((0, TY0, 0), (1, 0, 0), (0, -1, 0)), ((TX, TC, 0), (0, 1, 0), (1, 0, 0)), ((-TX, TC, 0), (0, -1, 0), (-1, 0, 0))):
        if n[1] == 0:
            okno_gotyk(w, md, (c[0], c[1], 17.0), t, n, 1.4, 5.0, maswerk=False)
        else:
            okno_gotyk(w, md, (0, TY0, 15.5), t, n, 1.8, 6.5)
        for du in (-1.6, 1.6):
            okno_gotyk(w, md, W((c[0], c[1], 29.0), t, n, du, 0), t, n, 1.5, 6.0, maswerk=False)
    # pinakle na narożach
    for sx in (-1, 1):
        for sy_ in (TY0, TY1):
            pryzmat(w, sx * (TX - 0.5), sy_ + (0.5 if sy_ == TY0 else -0.5), TH, 0.55, 0.8, C, n=4, rot=45)
            pryzmat(w, sx * (TX - 0.5), sy_ + (0.5 if sy_ == TY0 else -0.5), TH + 0.8, 0.6, 3.2, "dachowka_ciemna", n=4, rot=45, r_gora=0.0)
    md.box_od_do(-TX - 0.2, TY0 - 0.2, TH - 0.4, TX + 0.2, TY1 + 0.2, TH, "piaskowiec")
    # ośmiobok
    RO = 5.15
    pryzmat(w, 0, TC, TH, RO, 11.0, C, gora=False)
    for i in range(8):
        a = math.radians(22.5 + 45 * i)
        am = math.radians(45 * (i + 1))
        cx_, cy_ = RO * math.cos(a), TC + RO * math.sin(a)
        md.box((cx_, cy_, TH + 5.5), (0.5, 0.5, 11.0), CC, rot_z=math.degrees(a))
        rf = RO * math.cos(math.radians(22.5))
        c = (rf * math.cos(am), TC + rf * math.sin(am), TH + 4.0)
        t = (-math.sin(am), math.cos(am), 0); n = (math.cos(am), math.sin(am), 0)
        okno_gotyk(w, md, c, t, n, 0.8, 4.2, maswerk=False)
    # galeria z balustradą
    ZG = TH + 11.0
    pryzmat(w, 0, TC, ZG, 6.2, 0.4, "piaskowiec", r_gora=6.2, dol=True)
    rb = 6.0
    for i in range(8):
        a, b = math.radians(22.5 + 45 * i), math.radians(22.5 + 45 * (i + 1))
        A = (rb * math.cos(a), TC + rb * math.sin(a)); B = (rb * math.cos(b), TC + rb * math.sin(b))
        for k in range(8):
            f = k / 8
            md.box((A[0] + (B[0] - A[0]) * f, A[1] + (B[1] - A[1]) * f, ZG + 0.85), (0.14, 0.14, 0.9), "piaskowiec")
        md.belka((A[0], A[1], ZG + 1.35), (B[0], B[1], ZG + 1.35), 0.16, "piaskowiec")
        pryzmat(md, A[0], A[1], ZG + 0.4, 0.22, 1.35, "piaskowiec", n=4, rot=45)
        pryzmat(md, A[0], A[1], ZG + 1.75, 0.22, 0.6, "piaskowiec", n=4, rot=45, r_gora=0.0)
    # hełm
    pryzmat(w, 0, TC, ZG + 0.4, 4.4, 1.0, C, gora=False)
    pryzmat(w, 0, TC, ZG + 1.4, 4.9, 11.4, "lupek", r_gora=0.0)
    for i in range(4):                                            # lukarny w hełmie
        am = math.radians(45 + 90 * i)
        rr = 3.15
        c = (rr * math.cos(am), TC + rr * math.sin(am), ZG + 4.4)
        md.box(c, (0.8, 0.7, 1.1), "lupek", rot_z=math.degrees(am))
        md.box((c[0] + 0.41 * math.cos(am), c[1] + 0.41 * math.sin(am), c[2]), (0.06, 0.5, 0.8), "okno", rot_z=math.degrees(am))
    zk = ZG + 12.8
    pryzmat(md, 0, TC, zk - 0.2, 0.18, 0.4, "zloto", r_gora=0.18)
    md.belka((0, TC, zk), (0, TC, zk + 1.6), 0.1, "zloto")
    md.belka((-0.45, TC, zk + 1.1), (0.45, TC, zk + 1.1), 0.1, "zloto")

    obs = [m.obiekt(), w.obiekt(), pp.obiekt(), md.obiekt()]
    punkt("miasto_karta_2", (0.0, TY0 - 2.4, 0.0), 0.0)
    punkt("kosciol_drzwi", (0.0, TY0 - 1.6, 0.0), 0.0)
    swiatlo((0.0, TY0 - 2.0, 5.2), kolor="#ffcf8a", zasieg=8, moc=1.5)
    return obs


def trojkaty(obs):
    return sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in obs if o.type == "MESH")


if __name__ == "__main__":
    wyczysc_scene()
    obs = zbuduj()
    print("TROJKATY Rynek:", trojkaty(obs), [(o.name, trojkaty([o])) for o in obs])
    eksport_fbx(obs, os.path.join(UNITY_MODELE, "Rynek.fbx"))
    zapisz_meta(os.path.join(DANE, "meta_rynek.json"))
    ziemia_podgladu(70, "bruk_jasny")
    render_podglad(os.path.join(PODGLADY, "rynek_1.png"), cel=(-6, 0, 16), odl=95, azymut=150, wysokosc_kat=18)
    render_podglad(os.path.join(PODGLADY, "rynek_2.png"), cel=(0, 0, 18), odl=90, azymut=60, wysokosc_kat=14)
    render_podglad(os.path.join(PODGLADY, "rynek_3.png"), cel=(-4, 6, 13), odl=34, azymut=235, wysokosc_kat=-19, ogniskowa=18)
    render_podglad(os.path.join(PODGLADY, "rynek_4.png"), cel=(-7, 0, 0), odl=95, azymut=0.001, wysokosc_kat=72)
    render_podglad(os.path.join(PODGLADY, "rynek_5.png"), cel=(12.2, 0, 35), odl=22, azymut=120, wysokosc_kat=5, ogniskowa=35)

    wyczysc_scene()
    obs = zbuduj_kosciol()
    print("TROJKATY Kosciol:", trojkaty(obs), [(o.name, trojkaty([o])) for o in obs])
    eksport_fbx(obs, os.path.join(UNITY_MODELE, "Kosciol.fbx"))
    zapisz_meta(os.path.join(DANE, "meta_kosciol.json"))
    ziemia_podgladu(80, "bruk_jasny")
    render_podglad(os.path.join(PODGLADY, "kosciol_1.png"), cel=(0, -5, 28), odl=130, azymut=25, wysokosc_kat=12)
    render_podglad(os.path.join(PODGLADY, "kosciol_2.png"), cel=(0, 0, 24), odl=125, azymut=215, wysokosc_kat=18)
    render_podglad(os.path.join(PODGLADY, "kosciol_3.png"), cel=(0, -22, 22), odl=42, azymut=15, wysokosc_kat=-27, ogniskowa=16)
    render_podglad(os.path.join(PODGLADY, "kosciol_4.png"), cel=(0, -20, 54), odl=30, azymut=40, wysokosc_kat=8, ogniskowa=35)
