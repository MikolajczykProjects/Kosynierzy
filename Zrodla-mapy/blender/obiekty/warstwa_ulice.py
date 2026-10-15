# -*- coding: utf-8 -*-
"""
warstwa_ulice.py — TEREN + ULICE + WODA + TORY + MOSTY + GRANICA (Gliwice 1939).
Krok 1 (venv z shapely):  /tmp/unityref/venv/bin/python obiekty/_ulice_prep.py  -> dane/ulice_geo.json
Krok 2 (Blender):         blender -b --factory-startup -P obiekty/warstwa_ulice.py
Wyjście: UNITY_MODELE/Teren.fbx, UNITY_MODELE/Ulice_<i>_<j>.fbx (sektory 500 m od rogu GRANICE), dane/meta_ulice.json.
Współrzędne obiektów = współrzędne mapy (pivot 0,0,0).
"""
import sys, os, json, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from lowpoly import *
from gliwice_geo import GRANICE, LANDMARKI, ZAGAJNIK, STODOLA, RYNEK_PLAC, SZER_DROGI

G = json.load(open(os.path.join(DANE, "ulice_geo.json")))
Z_JEZ, Z_CHOD = 0.02, 0.15
Z_BRZEG, Z_WODA, Z_DNO = -0.9, -1.0, -1.8
DWX, DWY, _ = LANDMARKI["Dworzec"]


# ------------------------------------------------------------------ pomocniki
def plyta(s, polys, z, m, m_bok=None, z0=None):
    """polys = [[ext, [holes]]]; góra na z, opcjonalnie boki od z0 do z."""
    for ext, holes in polys:
        if len(ext) < 3:
            continue
        s.wielokat(ext, z, m, dziury=[h for h in holes if len(h) >= 3])
        if m_bok and z0 is not None:
            sciany(s, ext, holes, z0, z, m_bok, na_zewn=True)


def _ring(r, ccw=True):
    r = [tuple(p) for p in r]
    if (pole(r) > 0) != ccw:
        r = r[::-1]
    return r


def sciany(s, ext, holes, z0, z1, m, na_zewn=True):
    """Pionowe ściany pierścieni. na_zewn=True: normalne na zewnątrz wielokąta (krawężnik), False: do środka (koryto)."""
    for r, zew in [(ext, True)] + [(h, False) for h in holes]:
        if len(r) < 3:
            continue
        r = _ring(r, ccw=True)
        out = (zew == na_zewn)          # CCW + kolejność a,b,b',a' => normalna na zewnątrz pętli
        n = len(r)
        for i in range(n):
            a, b = r[i], r[(i + 1) % n]
            if not out:
                a, b = b, a
            s.quad((a[0], a[1], z0), (b[0], b[1], z0), (b[0], b[1], z1), (a[0], a[1], z1), m)


def probkuj(pts, krok):
    """[(x,y,dx,dy)] co krok po polilinii."""
    out, reszta = [], 0.0
    for a, b in zip(pts[:-1], pts[1:]):
        dx, dy = b[0] - a[0], b[1] - a[1]
        L = math.hypot(dx, dy)
        if L < 1e-6:
            continue
        ux, uy = dx / L, dy / L
        t = reszta
        while t < L:
            out.append((a[0] + ux * t, a[1] + uy * t, ux, uy))
            t += krok
        reszta = t - L
    return out


def yaw_z(dx, dy):
    return math.degrees(math.atan2(dx, dy)) % 360


# ------------------------------------------------------------------ teren + woda
def teren_i_woda():
    obs = []
    t = Siatka("Teren_Miasto"); plyta(t, G["teren_miasto"], 0.0, "zwir"); obs.append(t)
    t = Siatka("Teren_Trawa"); plyta(t, G["teren_trawa"], 0.0, "trawa"); obs.append(t)
    t = Siatka("Teren_Pola")
    for u, p in G["pola"].items():
        plyta(t, p, 0.03, u)
    obs.append(t)
    # koryto: półka nabrzeża (kamień w mieście, ziemia poza), mury
    nab = Siatka("Nabrzeze")
    plyta(nab, G["brzeg_miasto"], Z_BRZEG, "kamien")
    plyta(nab, G["brzeg_poza"], Z_BRZEG, "ziemia")
    for ext, holes in G["koryto"]:
        sciany(nab, ext, holes, Z_BRZEG, 0.0, "kamien", na_zewn=False)
        # gzyms muru (mosiężnych barierek nie ma, jest kamienna listwa)
    for ext, holes in G["woda"]:
        sciany(nab, ext, holes, Z_DNO, Z_BRZEG, "kamien_ciemny", na_zewn=False)
    obs.append(nab)
    d = Siatka("Dno_NOCOL"); plyta(d, G["woda"], Z_DNO, "dno"); obs.append(d)
    w = Siatka("Woda_Klodnica_NOCOL"); plyta(w, G["woda"], Z_WODA, "woda"); obs.append(w)
    # granica
    g = Siatka("Granica_COL")
    x0, y0, x1, y1 = GRANICE
    for wy in (y0, y1):
        g.box(((x0 + x1) / 2, wy, 12.5), (x1 - x0 + 2, 1.0, 25), "kamien")
    g.box((x0, (y0 + y1) / 2, 12.5), (1.0, y1 - y0 + 2, 25), "kamien")
    g.box((x1, (y0 + y1) / 2, 12.5), (1.0, y1 - y0 + 2, 25), "kamien")
    obs.append(g)
    return obs


# ------------------------------------------------------------------ mosty
def mosty():
    x0, y0, x1, y1 = GRANICE
    lst = []
    for m in G["mosty"]:
        a, b = m["pts"]
        mx, my = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
        if not (x0 <= mx <= x1 and y0 <= my <= y1):
            continue
        lst.append(m)
    # najbliższy Rynku = garbaty, przejście pod spodem
    lst.sort(key=lambda m: math.hypot(*[(m["pts"][0][k] + m["pts"][1][k]) / 2 - RYNEK_PLAC[k] for k in (0, 1)]))
    obs, info = [], []
    for i, m in enumerate(lst):
        a, b = m["pts"]
        garb = (i == 0)
        s = Siatka("Most_%d" % (i + 1))
        dx, dy = b[0] - a[0], b[1] - a[1]
        L0 = math.hypot(dx, dy); ux, uy = dx / L0, dy / L0; nx, ny = -uy, ux
        E = 10.0 if garb else 1.5                     # rampy na lądzie
        H = 2.4 if garb else 0.45                     # garb nad jezdnią
        w = m["szer"] / 2
        L = L0 + 2 * E
        ax, ay = a[0] - ux * E, a[1] - uy * E
        N = 18
        s_kor0, s_kor1 = E + 2.5, L - E - 2.5         # granice koryta wzdłuż osi
        def P(t, off, z):
            return (ax + ux * t + nx * off, ay + uy * t + ny * off, z)
        def zg(t):
            return Z_JEZ + 0.04 + H * math.sin(math.pi * t / L)
        def za(t):                                    # spód łuku
            if t <= s_kor0 or t >= s_kor1:
                return Z_DNO
            v = (t - s_kor0) / (s_kor1 - s_kor0)
            top = zg(L / 2) - 0.55
            return top - 0.5 * (1 - math.sin(math.pi * v))     # płaski łuk na wysokich przyczółkach: przejście półką
        ts = [L * k / N for k in range(N + 1)]
        for t0, t1 in zip(ts[:-1], ts[1:]):
            z0, z1 = zg(t0), zg(t1)
            s.quad(P(t0, -w, z0), P(t1, -w, z1), P(t1, w, z1), P(t0, w, z0), "bruk")       # góra
            for sg in (-1, 1):                         # balustrady
                o = sg * (w - 0.25)
                s.belka(P(t0, o, z0 + 0.5), P(t1, o, z1 + 0.5), 0.4, "piaskowiec", grub2=1.0)
            # boki (spandrel) od jezdni do łuku
            b0, b1 = za(t0), za(t1)
            s.quad(P(t0, -w, b0), P(t1, -w, b1), P(t1, -w, z1), P(t0, -w, z0), "kamien")
            s.quad(P(t1, w, b1), P(t0, w, b0), P(t0, w, z0), P(t1, w, z1), "kamien")
            # spód łuku (intrados)
            s.quad(P(t0, w, b0), P(t1, w, b1), P(t1, -w, b1), P(t0, -w, b0), "kamien_ciemny")
        # słupki na końcach balustrad
        for t in (0.8, L - 0.8):
            for sg in (-1, 1):
                c = P(t, sg * (w - 0.25), zg(t) + 0.65)
                s.box(c, (0.6, 0.6, 1.3), "piaskowiec")
        # koniec bliżej Rynku -> schodki z chodnika na półkę nabrzeża (przejście pod mostem)
        ra = math.hypot(a[0] - RYNEK_PLAC[0], a[1] - RYNEK_PLAC[1])
        rb = math.hypot(b[0] - RYNEK_PLAC[0], b[1] - RYNEK_PLAC[1])
        t_lad = (s_kor0 + 1.0) if ra <= rb else (s_kor1 - 1.0)
        if garb:
            for sg in (-1, 1):
                for k in range(5):
                    zt = Z_BRZEG + 0.9 * (k + 1) / 5.5
                    c = P(t_lad, sg * (w + 2.0 + k * 0.8), 0)
                    s.box((c[0], c[1], (Z_BRZEG + zt) / 2), (1.9, 0.8, zt - Z_BRZEG), "kamien",
                          rot_z=math.degrees(math.atan2(uy, ux)))
        obs.append(s)
        info.append({"t_lad": t_lad, "i": i + 1, "garb": garb, "a": (ax, ay), "u": (ux, uy), "n": (nx, ny), "L": L, "w": w,
                     "zg": zg, "s_kor0": s_kor0, "s_kor1": s_kor1, "nazwa": m["nazwa"]})
    return obs, info


# ------------------------------------------------------------------ ulice w sektorach
def sektory_ulic():
    klucze = set()
    for d in G["jezdnia"].values():
        klucze |= set(d)
    for k in ("chodnik", "kraweznik", "podsypka", "tory", "tram"):
        klucze |= set(G[k])
    wyn = {}
    for k in sorted(klucze):
        obs = []
        u = Siatka("Ulica_" + k)
        for m, d in G["jezdnia"].items():
            plyta(u, d.get(k, []), Z_JEZ, m)
        for a, b in G["tram"].get(k, []):                 # szyny tramwaju wpuszczone w jezdnię
            dx, dy = b[0] - a[0], b[1] - a[1]
            L = math.hypot(dx, dy)
            if L < 0.2:
                continue
            nx, ny = -dy / L, dx / L
            for sg in (-0.72, 0.72):
                u.box(((a[0] + b[0]) / 2 + nx * sg, (a[1] + b[1]) / 2 + ny * sg, Z_JEZ + 0.012), (L + 0.05, 0.08, 0.024),
                      "szyna", rot_z=math.degrees(math.atan2(dy, dx)))
        obs.append(u)
        c = Siatka("Chodnik_" + k)
        plyta(c, G["chodnik"].get(k, []), Z_CHOD, "chodnik", m_bok="kraweznik", z0=0.0)
        plyta(c, G["kraweznik"].get(k, []), Z_CHOD + 0.01, "kraweznik", m_bok="kraweznik", z0=0.0)
        obs.append(c)
        t = Siatka("Tory_" + k)
        plyta(t, G["podsypka"].get(k, []), 0.25, "zwir", m_bok="zwir", z0=0.0)
        pod = Siatka("Podklady_" + k + "_NOCOL")
        for a, b in G["tory"].get(k, []):
            dx, dy = b[0] - a[0], b[1] - a[1]
            L = math.hypot(dx, dy)
            if L < 0.2:
                continue
            nx, ny = -dy / L, dx / L
            kat = math.degrees(math.atan2(dy, dx))
            for sg in (-0.75, 0.75):
                t.box(((a[0] + b[0]) / 2 + nx * sg, (a[1] + b[1]) / 2 + ny * sg, 0.43), (L + 0.03, 0.07, 0.14), "szyna", rot_z=kat)
            mx, my = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
            if math.hypot(mx - DWX, my - DWY) < 300:
                n = int(L)
                for q in range(n):
                    f = (q + 0.5) / max(n, 1)
                    pod.box((a[0] + dx * f, a[1] + dy * f, 0.32), (0.25, 2.6, 0.14), "podklad", rot_z=kat)
        obs += [t, pod]
        wyn[k] = [o for o in obs if not o.pusta()]
    return wyn


def rynek():
    s = Siatka("Rynek_Plac")
    plyta(s, G["rynek"], 0.06, "bruk_jasny", m_bok="kraweznik", z0=0.0)
    return s


# ------------------------------------------------------------------ punkty
def punkty(mi):
    drogi = G["drogi"]
    hx, hy, _ = LANDMARKI["Hotel"]
    rx, ry, rr = LANDMARKI["Radiostacja"]
    szer = lambda d: SZER_DROGI.get(d["h"], 8)

    # most przy Rynku (garbaty)
    m = mi[0]
    def na_osi(t, off=0.0):
        return (m["a"][0] + m["u"][0] * t + m["n"][0] * off, m["a"][1] + m["u"][1] * t + m["n"][1] * off)
    x, y = na_osi(m["L"] / 2)
    punkt("miasto_karta_5", (x, y, m["zg"](m["L"] / 2)), yaw_z(*m["u"]))
    x, y = na_osi(m["t_lad"], m["w"] * 0.3)              # na półce nabrzeża pod mostem
    punkt("miasto_skrytka_most", (x, y, Z_BRZEG), yaw_z(*m["n"]))
    x, y = na_osi(m["t_lad"], -m["w"] - 6.5)             # góra schodków na półkę
    punkt("miasto_most_zejscie", (x, y, Z_CHOD), yaw_z(*m["n"]))

    # Tarnogórska: od hotelu na północ do zagajnika
    tarn = [p for d in drogi if d["tarn"] for p in d["pts"]]
    zx, zy, zr = ZAGAJNIK
    tarn = sorted([p for p in tarn if hy < p[1] < zy + 50], key=lambda p: p[1])
    for i, f in enumerate((0.15, 0.4, 0.65, 0.88)):
        ty = hy + (zy - hy) * f
        p = min(tarn, key=lambda q: abs(q[1] - ty))
        nxt = min([q for q in tarn if q[1] > p[1] + 5] or [(p[0], p[1] + 1)], key=lambda q: q[1])
        punkt("miasto_auto_sd_trasa_%d" % (i + 2), (p[0], p[1], Z_JEZ), yaw_z(nxt[0] - p[0], nxt[1] - p[1]))
    # auto przy drodze na skraju zagajnika
    wsz = [(p, d) for d in drogi for p in d["pts"]]
    p, d = min(wsz, key=lambda e: abs(math.hypot(e[0][0] - zx, e[0][1] - zy) - (zr + 6)))
    vx, vy = zx - p[0], zy - p[1]; L = math.hypot(vx, vy)
    ax, ay = p[0] + vx / L * (szer(d) / 2 + 1.5), p[1] + vy / L * (szer(d) / 2 + 1.5)
    punkt("miasto_zagajnik_auto", (ax, ay, 0.0), yaw_z(vx, vy) - 90)
    punkt("miasto_zagajnik_krzaki", (ax + vx / L * 15, ay + vy / L * 15, 0.0), yaw_z(-vx, -vy))

    # skrzyżowania między hotelem a radiostacją (węzły wspólne >= 2 dróg, blisko trasy)
    licz = {}
    for d in drogi:
        for q in set(tuple(map(lambda v: round(v, 2), p)) for p in d["pts"]):
            licz[q] = licz.get(q, 0) + 1
    tarn_pts = [tuple(p) for p in tarn]
    skrz = [q for q, n in licz.items() if n >= 2 and hy + 40 < q[1] < ry - 60
            and min(math.hypot(q[0] - t[0], q[1] - t[1]) for t in tarn_pts[::3] or [(hx, hy)]) < 60]
    skrz.sort(key=lambda q: q[1])
    wyb = []
    for q in skrz:
        if all(math.hypot(q[0] - w[0], q[1] - w[1]) > 90 for w in wyb):
            wyb.append(q)
    if len(wyb) > 6:
        wyb = [wyb[int(i * (len(wyb) - 1) / 5)] for i in range(6)]
    for i, q in enumerate(wyb):
        punkt("miasto_patrol_sd_%d" % (i + 1), (q[0] + 6, q[1] - 6, 0.0), 315)

    # drogi wg nazwy
    def droga(nazwa):
        return [d for d in drogi if d["nazwa"] == nazwa]
    def chod_wzdl(lista, krok, off, ile, start=0.0):
        """punkty co krok po chodniku (off od osi; znak naprzemiennie), zwraca [(x,y,yaw)]."""
        out = []
        for d in sorted(lista, key=lambda d: -len(d["pts"])):
            for j, (x, y, ux, uy) in enumerate(probkuj(d["pts"], krok)):
                sg = 1 if j % 2 == 0 else -1
                o = szer(d) / 2 + off
                out.append((x - uy * o * sg, y + ux * o * sg, yaw_z(uy * sg, -ux * sg)))
                if len(out) >= ile:
                    return out
        return out
    zw = droga("Zwycięstwa")
    zw_tn = [d for d in drogi if d["tarn"] and d["pts"][0][1] < 1000]

    # Rynek: obwód placu
    ryn = G["rynek"][0][0] + [G["rynek"][0][0][0]]
    rcx, rcy = sum(p[0] for p in ryn[:-1]) / (len(ryn) - 1), sum(p[1] for p in ryn[:-1]) / (len(ryn) - 1)
    ryn_pr = probkuj(ryn, 25.0)

    punkt("miasto_schupo_1", (rcx + 14, rcy - 18, 0.06), 0)
    p, d = min(wsz, key=lambda e: abs(math.hypot(e[0][0] - DWX, e[0][1] - DWY) - (LANDMARKI["Dworzec"][2] + 10)))
    punkt("miasto_schupo_2", (p[0], p[1], Z_JEZ), yaw_z(DWX - p[0], DWY - p[1]))
    zp = chod_wzdl(zw, 60.0, 1.5, 3)
    punkt("miasto_schupo_3", (zp[1][0], zp[1][1], Z_CHOD), zp[1][2])

    tl = [(x + (rcx - x) * 0.12, y + (rcy - y) * 0.12, yaw_z(rcx - x, rcy - y)) for (x, y, _, _) in ryn_pr[1::3]][:3]
    tl += chod_wzdl(zw, 22.0, 1.5, 4, )
    for i, (x, y, yw) in enumerate(tl[:7]):
        punkt("miasto_tlum_%d" % (i + 4), (x, y, Z_CHOD if i >= 3 else 0.06), yw)

    os_ = chod_wzdl(zw, 30.0, 2.6, 6) + chod_wzdl(zw_tn, 45.0, 2.6, 6)
    for i, (x, y, yw) in enumerate(os_[:12]):
        punkt("miasto_oslona_%d" % (i + 1), (x, y, Z_CHOD), yw)

    lat = [(x + (rcx - x) * 0.04, y + (rcy - y) * 0.04, 0.06) for (x, y, _, _) in ryn_pr[:6]]
    for (x, y, yw) in chod_wzdl(zw, 25.0, 0.5, 7) + chod_wzdl(zw_tn, 25.0, 0.5, 7):
        lat.append((x, y, Z_CHOD))
    for i, p in enumerate(lat[:20]):
        punkt("miasto_latarnia_%d" % (i + 1), p, 0)
        swiatlo((p[0], p[1], p[2] + 4.2), kolor="#ffcf8a", zasieg=14.0, moc=2.0)

    punkt("miasto_rower", (STODOLA[0] + 9, STODOLA[1] - 7, 0.0), 200)
    punkt("miasto_ciezarowka", (rx + 75, ry - 85, 0.0), 315)


# ------------------------------------------------------------------ main
if __name__ == "__main__":
    wyczysc_scene()
    tw = [s.obiekt() for s in teren_i_woda() if not s.pusta()]
    mo, mi = mosty()
    tw += [s.obiekt() for s in mo]
    eksport_fbx(tw, os.path.join(UNITY_MODELE, "Teren.fbx"))
    tri = {o.name: sum(len(p.vertices) - 2 for p in o.data.polygons) for o in tw}
    sek = sektory_ulic()
    ryn = rynek()
    rx, ry = RYNEK_PLAC[0], RYNEK_PLAC[1]
    k_ryn = "%d_%d" % (int((rx - GRANICE[0]) // 500), int((ry - GRANICE[1]) // 500))
    wszystkie = list(tw)
    for k, siatki in sek.items():
        obs = [s.obiekt() for s in siatki]
        if k == k_ryn:
            obs.append(ryn.obiekt())
        eksport_fbx(obs, os.path.join(UNITY_MODELE, "Ulice_%s.fbx" % k))
        for o in obs:
            tri[o.name] = sum(len(p.vertices) - 2 for p in o.data.polygons)
        wszystkie += obs
    punkty(mi)
    zapisz_meta(os.path.join(DANE, "meta_ulice.json"))
    print("TROJKATY", sum(tri.values()), json.dumps(tri))
    print("PUNKTY", len(META["punkty"]), [p["n"] for p in META["punkty"]])
    # podglądy
    render_podglad(os.path.join(PODGLADY, "ulice_1.png"), cel=(565, 615, 0), odl=2900, azymut=0.001, wysokosc_kat=89.9, ogniskowa=35, szer=1400, wys=1500)
    render_podglad(os.path.join(PODGLADY, "ulice_2.png"), cel=(-20, 0, 0), odl=230, azymut=200, wysokosc_kat=50)
    b = mi[0]
    cx, cy = b["a"][0] + b["u"][0] * b["L"] / 2, b["a"][1] + b["u"][1] * b["L"] / 2
    render_podglad(os.path.join(PODGLADY, "ulice_3.png"), cel=(cx, cy, -0.5), odl=55, azymut=yaw_z(*b["n"]) + 25, wysokosc_kat=22)
    tx, ty = b["a"][0] + b["u"][0] * b["t_lad"], b["a"][1] + b["u"][1] * b["t_lad"]
    render_podglad(os.path.join(PODGLADY, "ulice_4.png"), cel=(tx, ty, -0.2), odl=26, azymut=yaw_z(*b["n"]) + 190, wysokosc_kat=8)
