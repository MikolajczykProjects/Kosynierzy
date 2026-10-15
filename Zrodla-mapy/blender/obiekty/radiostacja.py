# -*- coding: utf-8 -*-
"""Radiostacja Gleiwitz 1939: wieza, budynki obslugi, plot, brama, anteny, detale."""
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from lowpoly import *
from mathutils import Vector

# ----------------------------------------------------------------------------- WIEZA
H_LATTICE = 108.0      # wysokosc kratownicy (do podstawy masztu)
H_TOTAL = 111.0        # calkowita wysokosc z maszem
W0 = 20.0              # szerokosc podstawy
W_TOP = 3.0            # szerokosc kratownicy pod maszem
PLATFORMY = [40.4, 55.3, 80.0, 109.7]

def szerokosc(z):
    """Profil paraboliczny: szeroka u podstawy, waska pod maszem."""
    z = min(z, H_LATTICE)
    t = z / H_LATTICE
    return W0 - (W0 - W_TOP) * t * t

def rogi(hw, z):
    return [(hw, hw, z), (hw, -hw, z), (-hw, -hw, z), (-hw, hw, z)]

def buduj_wieze():
    m = Siatka("Wieza_Radiostacji")
    n_seg = 22
    dz = H_LATTICE / n_seg
    post_t, ring_t, brace_t = 0.55, 0.32, 0.22
    prev = None
    for i in range(n_seg + 1):
        z = i * dz
        hw = szerokosc(z) / 2
        cur = rogi(hw, z)
        if prev is not None:
            for k in range(4):
                m.belka(prev[k], cur[k], post_t, "modrzew")
            for k in range(4):
                a0, b0 = prev[k], prev[(k + 1) % 4]
                a1, b1 = cur[k], cur[(k + 1) % 4]
                m.belka(a0, b1, brace_t, "modrzew_ciemny")
                m.belka(b0, a1, brace_t, "modrzew_ciemny")
        for k in range(4):
            m.belka(cur[k], cur[(k + 1) % 4], ring_t, "modrzew_ciemny")
        prev = cur
    # maszt
    r0 = szerokosc(H_LATTICE) / 2 * 0.5
    m.walec((0, 0, H_LATTICE), r0, H_TOTAL - H_LATTICE, "modrzew_ciemny", n=8, r_gora=0.08)
    # podesty z barierka
    for pz in PLATFORMY:
        hw = szerokosc(min(pz, H_LATTICE)) / 2
        rozm = hw * 2 + 1.6
        half = rozm / 2
        m.box((0, 0, pz + 0.1), (rozm, rozm, 0.2), "modrzew_ciemny")
        bh = 1.0
        m.box_od_do(-half, -half, pz + 0.2, half, -half + 0.08, pz + 0.2 + bh, "modrzew")
        m.box_od_do(-half, half - 0.08, pz + 0.2, half, half, pz + 0.2 + bh, "modrzew")
        m.box_od_do(-half, -half, pz + 0.2, -half + 0.08, half, pz + 0.2 + bh, "modrzew")
        m.box_od_do(half - 0.08, -half, pz + 0.2, half, half, pz + 0.2 + bh, "modrzew")
    # drabina centralna (dol -> gora kratownicy)
    for k in (-0.18, 0.18):
        m.belka((k, 0, 1.0), (k, 0, H_LATTICE + 1.0), 0.07, "modrzew_ciemny")
    szczeble = int((H_LATTICE - 1.0) / 1.5)
    for i in range(szczeble):
        z = 1.0 + i * 1.5
        m.belka((-0.18, 0, z), (0.18, 0, z), 0.06, "modrzew_ciemny")
    # schody - ukosne biegi miedzy kolejnymi podestami
    poziomy = [0.0] + PLATFORMY
    for i in range(len(poziomy) - 1):
        z0, z1 = poziomy[i], poziomy[i + 1]
        hw0 = szerokosc(min(z0, H_LATTICE)) / 2 - 0.5
        hw1 = szerokosc(min(z1, H_LATTICE)) / 2 - 0.5
        m.belka((hw0 * 0.6, -hw0 * 0.6, z0 + 0.3), (hw1 * 0.6, -hw1 * 0.6, z1 - 0.3), 0.32, "modrzew_ciemny")
    return m.obiekt((0, 0, 0))

def buduj_wieze_col():
    m = Siatka("Wieza_Radiostacji_COL")
    hw0, hw1 = szerokosc(0) / 2, szerokosc(6) / 2
    for sx in (1, -1):
        for sy in (1, -1):
            x0, y0 = sx * hw0, sy * hw0
            x1, y1 = sx * hw1, sy * hw1
            m.box(((x0 + x1) / 2, (y0 + y1) / 2, 3.0), (1.4, 1.4, 6.0), "modrzew")
    return m.obiekt((0, 0, 0))

# ----------------------------------------------------------------------------- ANTENY
def buduj_anteny():
    m = Siatka("Radio_Anteny_NOCOL")
    top = (0, 0, H_TOTAL - 1.0)
    for (x, y, h) in ((25, 8, 3.2), (-25, 8, 3.2)):
        m.belka((x, y, 0), (x, y, h), 0.15, "modrzew_ciemny")
        m.belka(top, (x, y, h), 0.035, "metal_ciemny")
    return m.obiekt((0, 0, 0))

# ----------------------------------------------------------------------------- BUDYNEK NADAJNIKA
def buduj_budynek_nadajnika():
    sx, sy, h = 30.0, 12.0, 8.0
    m = Siatka("Radio_Budynek_Nadajnika")
    m.box((0, 0, h / 2), (sx, sy, h), "cegla")
    m.box((0, 0, 0.5), (sx + 0.1, sy + 0.1, 1.0), "klinkier")
    m.box((0, 0, h), (sx + 0.3, sy + 0.3, 0.3), "kamien")
    m.dach_czterospadowy((0, 0, h + 0.3), sx, sy, 2.2, "dachowka_ciemna")
    pts = [(sx / 2, sy / 2), (-sx / 2, sy / 2), (-sx / 2, -sy / 2), (sx / 2, -sy / 2)]
    for z0 in (1.4, 4.6):
        m.okna_rzad(pts[0], pts[1], z0, 1.8, 1.3, 3.3)   # front +Y
        m.okna_rzad(pts[2], pts[3], z0, 1.8, 1.3, 3.3)   # tyl -Y
    m.okna_rzad(pts[1], pts[2], 1.4, 1.8, 1.2, 4.5)      # zachod
    m.okna_rzad(pts[3], pts[0], 1.4, 1.8, 1.2, 4.5)      # wschod
    # wejscie frontowe (+Y) z daszkiem
    m.box((0, sy / 2 + 0.03, 1.1), (1.6, 0.06, 2.2), "drzwi_ciemne")
    m.box((0, sy / 2 + 0.35, 2.45), (2.3, 0.75, 0.15), "dachowka_ciemna")
    for dx in (-0.95, 0.95):
        m.belka((dx, sy / 2, 2.4), (dx, sy / 2 + 0.7, 1.0), 0.09, "metal_ciemny")
    # drzwi tylne (-Y, poludnie)
    m.box((0, -sy / 2 - 0.03, 1.05), (1.3, 0.06, 2.1), "drzwi")
    # kominy
    for cx in (-sx / 2 + 4, sx / 2 - 4):
        m.box((cx, 0, h + 2.2 + 1.0), (1.0, 1.0, 2.0), "cegla_ciemna")
    # izolatory na dachu
    for cx in (-sx / 2 + 8, 0, sx / 2 - 8):
        m.walec((cx, 2.0, h + 2.2), 0.12, 0.5, "kamien", n=6)
    return m.obiekt((0, -45, 0))

# ----------------------------------------------------------------------------- DOM OBSLUGI
def buduj_dom_obslugi():
    sx, sy, h = 14.0, 10.0, 5.6
    m = Siatka("Radio_Dom_Obslugi")
    m.box((0, 0, h / 2), (sx, sy, h), "tynk_bez")
    m.box((0, 0, 0.4), (sx + 0.1, sy + 0.1, 0.8), "cokol")
    m.dach_dwuspadowy((0, 0, h), sx, sy, 3.6, "dachowka", m_szczyt="tynk_bez")
    pts = [(sx / 2, sy / 2), (-sx / 2, sy / 2), (-sx / 2, -sy / 2), (sx / 2, -sy / 2)]
    for z0 in (1.1, 3.4):
        m.okna_rzad(pts[0], pts[1], z0, 1.5, 1.1, 3.0)
        m.okna_rzad(pts[2], pts[3], z0, 1.5, 1.1, 3.0)
    m.okna_rzad(pts[1], pts[2], 1.1, 1.5, 1.1, 4.0)
    m.okna_rzad(pts[3], pts[0], 1.1, 1.5, 1.1, 4.0)
    m.box((0, sy / 2 + 0.03, 1.0), (1.1, 0.06, 2.0), "drzwi")
    m.box((0, sy / 2 + 0.28, 2.1), (1.7, 0.55, 0.12), "dachowka")
    m.box((sx / 2 - 2.0, 0, h + 2.6), (0.8, 0.8, 1.6), "cegla")
    return m.obiekt((-22, -58, 0))

# ----------------------------------------------------------------------------- DETALE
def buduj_zbiornik():
    m = Siatka("Radio_Zbiornik")
    for sx in (-1, 1):
        for sy in (-1, 1):
            m.belka((sx * 1.3, sy * 1.3, 0), (sx * 1.1, sy * 1.1, 3.2), 0.16, "metal_ciemny")
    m.walec((0, 0, 3.2), 1.8, 2.3, "blacha", n=10)
    m.stozek((0, 0, 5.5), 1.8, 0.8, "blacha", n=10)
    return m.obiekt((14, -30, 0))

def buduj_szopa():
    m = Siatka("Radio_Szopa")
    m.box((0, 0, 1.4), (4.0, 3.0, 2.8), "deska_szara")
    m.dach_dwuspadowy((0, 0, 2.8), 4.0, 3.0, 1.0, "papa")
    m.box((0, 1.5 + 0.02, 1.0), (1.0, 0.04, 2.0), "drzwi_ciemne")
    return m.obiekt((-14, -33, 0))

def buduj_zwir():
    m = Siatka("Radio_Zwir")
    pasy = [((70, -70), (0, -45), 3.0), ((0, -45), (0, -6), 2.4), ((-10, -53), (-22, -58), 2.0)]
    for (a, b, w) in pasy:
        ax, ay = a; bx, by = b
        dx, dy = bx - ax, by - ay
        dl = math.hypot(dx, dy)
        rot = math.degrees(math.atan2(dy, dx))
        m.box(((ax + bx) / 2, (ay + by) / 2, 0.02), (dl, w, 0.04), "zwir", rot_z=rot)
    return m.obiekt((0, 0, 0))

# ----------------------------------------------------------------------------- PLOT I BRAMA
def _plot_segment(nazwa, p0, p1, wys=2.0, rozstaw=2.5):
    p0v, p1v = Vector((p0[0], p0[1], 0)), Vector((p1[0], p1[1], 0))
    d = p1v - p0v; dl = d.length
    if dl < 0.2:
        return None
    mid = (p0v + p1v) / 2
    t = d.normalized()
    kat = math.degrees(math.atan2(t.y, t.x))
    m = Siatka(nazwa)
    n_bay = max(1, round(dl / rozstaw))
    bay = dl / n_bay
    for i in range(n_bay + 1):
        off = -dl / 2 + bay * i
        lx, ly = t.x * off, t.y * off
        m.box((lx, ly, wys / 2 + 0.1), (0.28, 0.28, wys + 0.2), "cokol", rot_z=kat)
    n_slat, slat_w = 5, bay / 5 * 0.55
    for i in range(n_bay):
        base = -dl / 2 + bay * i
        for j in range(n_slat):
            off = base + bay * (j + 0.5) / n_slat
            lx, ly = t.x * off, t.y * off
            m.box((lx, ly, wys / 2), (slat_w, 0.04, wys), "drewno", rot_z=kat)
    return m.obiekt((mid.x, mid.y, 0))

def buduj_brama(x0, x1, y):
    m = Siatka("Radio_Brama")
    szer = (x1 - x0) / 2 - 0.22
    alpha = 72.0
    for side in (x0, x1):
        m.box((side - (x0 + x1) / 2, 0, 1.2), (0.3, 0.3, 2.6), "cokol")
    a = math.radians(alpha)
    tx, ty = szer * math.cos(a), szer * math.sin(a)
    m.box((tx / 2 + (x0 - (x0 + x1) / 2), ty / 2, 1.15), (szer, 0.08, 2.1), "modrzew", rot_z=alpha)
    a2 = math.radians(180 - alpha)
    tx2, ty2 = szer * math.cos(a2), szer * math.sin(a2)
    m.box((tx2 / 2 + (x1 - (x0 + x1) / 2), ty2 / 2, 1.15), (szer, 0.08, 2.1), "modrzew", rot_z=180 - alpha)
    return m.obiekt(((x0 + x1) / 2, y, 0))

# ----------------------------------------------------------------------------- ZESPOL
def zbuduj():
    obs = [
        buduj_wieze(), buduj_wieze_col(), buduj_anteny(),
        buduj_budynek_nadajnika(), buduj_dom_obslugi(),
        buduj_zbiornik(), buduj_szopa(), buduj_zwir(),
    ]
    X, Y = 85.0, 70.0
    BX0, BX1 = 65.0, 75.0      # brama (wzdluz X, poludniowy plot - blisko drogi na wschodzie)
    HY0, HY1 = -10.6, -9.4     # dziura w plocie (zachod)

    def seg(nazwa, p0, p1):
        o = _plot_segment(nazwa, p0, p1)
        if o:
            obs.append(o)

    seg("Radio_Plot_Polnoc", (-X, Y), (X, Y))
    seg("Radio_Plot_Poludnie", (-X, -Y), (BX0, -Y))
    seg("Radio_Plot_Poludnie_Brama", (BX1, -Y), (X, -Y))
    seg("Radio_Plot_Wschod", (X, -Y), (X, Y))
    seg("Radio_Plot_Zachod_Poludnie", (-X, -Y), (-X, HY0))
    seg("Radio_Plot_Zachod_Polnoc", (-X, HY1), (-X, Y))
    obs.append(buduj_brama(BX0, BX1, -Y))

    # punkty gry (lokalne wzgledem (0,0,0) = podnoze wiezy)
    punkt("miasto_drzwi_radio", (0, -52.0, 1.0), 0.0)         # 1 m przed tylnymi drzwiami budynku nadajnika
    punkt("miasto_dziura_plot", (-X - 1.0, (HY0 + HY1) / 2, 0.2), 90.0)  # 1 m przed dziura, z zewnatrz
    punkt("spawn_radio_brama", ((BX0 + BX1) / 2, -Y + 2.0, 0.0), 0.0)
    punkt("radio_latarnia_1", (-8.0, -38.0, 0.0), 0.0)
    punkt("radio_latarnia_2", (8.0, -38.0, 0.0), 0.0)
    punkt("radio_latarnia_3", (-22.0, -51.5, 0.0), 0.0)
    punkt("radio_latarnia_4", ((BX0 + BX1) / 2, -Y + 3.0, 0.0), 0.0)
    swiatlo((-8.0, -38.0, 3.0), zasieg=10)
    swiatlo((8.0, -38.0, 3.0), zasieg=10)
    swiatlo((-22.0, -51.5, 3.0), zasieg=10)
    swiatlo(((BX0 + BX1) / 2, -Y + 3.0, 3.0), zasieg=10)
    return obs

if __name__ == "__main__":
    wyczysc_scene()
    obs = zbuduj()
    tris = 0
    for o in obs:
        if o and o.type == "MESH":
            tris += sum(max(0, len(p.vertices) - 2) for p in o.data.polygons)
    print("TROJKATY:", tris)
    eksport_fbx(obs, os.path.join(UNITY_MODELE, "Radiostacja.fbx"))
    zapisz_meta(os.path.join(DANE, "meta_radiostacja.json"))
    ziemia_podgladu(140)
    render_podglad(os.path.join(PODGLADY, "radiostacja_1.png"), cel=(0, -15, 30), odl=260, azymut=215, wysokosc_kat=16, ogniskowa=22)
    render_podglad(os.path.join(PODGLADY, "radiostacja_2.png"), cel=(0, -15, 30), odl=260, azymut=35, wysokosc_kat=16, ogniskowa=22)
    render_podglad(os.path.join(PODGLADY, "radiostacja_3.png"), cel=(0, -40, 6), odl=90, azymut=0, wysokosc_kat=10)
