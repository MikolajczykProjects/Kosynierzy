# -*- coding: utf-8 -*-
"""Warstwa budynków miasta Gliwice 1939 — z danych OSM (gliwice_geo), bez tekstur."""
import sys, os, math, random, bisect
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from lowpoly import *
import gliwice_geo as G

TYNKI = ["tynk_bez", "tynk_ochra", "tynk_piasek", "tynk_szarozielony", "tynk_losos",
         "tynk_kremowy", "tynk_szary", "tynk_brudny", "tynk_rozowy", "tynk_zolty"]
DACHOWKI = ["dachowka", "dachowka", "dachowka_ciemna", "dachowka_brazowa", "dachowka"]


def _dist(p, q):
    return math.hypot(p[0] - q[0], p[1] - q[1])


def _dist_pt_seg(p, a, b):
    ax, ay = a; bx, by = b; px, py = p
    dx, dy = bx - ax, by - ay
    l2 = dx * dx + dy * dy
    if l2 < 1e-9:
        return math.hypot(px - ax, py - ay)
    t = max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / l2))
    return math.hypot(px - (ax + t * dx), py - (ay + t * dy))


def _obb(pts):
    """Minimalna OBB testując kąty z krawędzi. Zwraca dl, szer, rot_z(stopnie), srodek."""
    best = None
    n = len(pts)
    for i in range(n):
        x0, y0 = pts[i]; x1, y1 = pts[(i + 1) % n]
        dx, dy = x1 - x0, y1 - y0
        l = math.hypot(dx, dy)
        if l < 1e-6:
            continue
        ux, uy = dx / l, dy / l
        nx, ny = -uy, ux
        us = [p[0] * ux + p[1] * uy for p in pts]
        vs = [p[0] * nx + p[1] * ny for p in pts]
        w, h = max(us) - min(us), max(vs) - min(vs)
        a = w * h
        if best is None or a < best[0]:
            cu, cv = (max(us) + min(us)) / 2, (max(vs) + min(vs)) / 2
            cx, cy = cu * ux + cv * nx, cu * uy + cv * ny
            ang = math.degrees(math.atan2(uy, ux))
            best = (a, w, h, ang, (cx, cy))
    _, w, h, ang, c = best
    return w, h, ang, c


def _obwodnica(m, pts, z, grub_h, wysuniecie, mat):
    n = len(pts)
    for i in range(n):
        a, b = pts[i], pts[(i + 1) % n]
        dx, dy = b[0] - a[0], b[1] - a[1]
        l = math.hypot(dx, dy)
        if l < 1e-6:
            continue
        ux, uy = dx / l, dy / l
        nx, ny = uy, -ux
        mx, my = (a[0] + b[0]) / 2 + nx * wysuniecie / 2, (a[1] + b[1]) / 2 + ny * wysuniecie / 2
        ang = math.degrees(math.atan2(uy, ux))
        m.box((mx, my, z), (l + 0.2, max(wysuniecie, 0.05), grub_h), mat, rot_z=ang)


def _plot_prostokat(m, w, d, mat, grub=0.35, h=1.2):
    pts = [(-w / 2, -d / 2), (w / 2, -d / 2), (w / 2, d / 2), (-w / 2, d / 2)]
    for i in range(4):
        a, b = pts[i], pts[(i + 1) % 4]
        l = math.hypot(b[0] - a[0], b[1] - a[1])
        mx, my = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
        ang = math.degrees(math.atan2(b[1] - a[1], b[0] - a[0]))
        m.box((mx, my, h / 2), (l, grub, h), mat, rot_z=ang)


GIGANCI_TAGI = {"industrial", "warehouse", "retail", "sports_hall", "stadium"}


def _tory_segmenty(D):
    segs = []
    for wid, (refs, tags) in D["ways"].items():
        if tags.get("railway") != "rail":
            continue
        latlon = G.wsp_drogi(D, wid)
        if len(latlon) < 2:
            continue
        pts = [G.P(a, b) for a, b in latlon]
        for i in range(len(pts) - 1):
            segs.append((pts[i], pts[i + 1]))
    return segs


def _przy_dworcu_torach(cx, cy, tory_segs, prog=150.0):
    dx, dy, dr = G.LANDMARKI["Dworzec"]
    if math.hypot(cx - dx, cy - dy) < dr + prog:
        return True
    for a, b in tory_segs:
        if _dist_pt_seg((cx, cy), a, b) < prog:
            return True
    return False


# ---------------------------------------------------------------- wczytanie budynków z OSM
def _wczytaj_budynki():
    D = G.wczytaj()
    tory_segs = _tory_segmenty(D)
    out = []
    for wid, (refs, tags) in D["ways"].items():
        if not tags.get("building") or wid in G.OS_ID_LANDMARKOW or not G.zamknieta(refs):
            continue
        latlon = G.wsp_drogi(D, wid)
        if len(latlon) < 4:
            continue
        pts = [G.P(a, b) for a, b in latlon]
        if pts[0] == pts[-1]:
            pts = pts[:-1]
        if len(pts) < 3:
            continue
        clat = sum(p[0] for p in latlon) / len(latlon)
        clon = sum(p[1] for p in latlon) / len(latlon)
        if G.strefa(clat, clon) != "miasto":
            continue
        mcx = sum(p[0] for p in pts) / len(pts)
        mcy = sum(p[1] for p in pts) / len(pts)
        if not G.w_granicach(mcx, mcy) or G.w_landmarku(mcx, mcy):
            continue
        if not G.budynek_1939(tags, pts):
            continue
        x0g, y0g, x1g, y1g = G.GRANICE
        if any(not (x0g + 2 <= px <= x1g - 2 and y0g + 2 <= py <= y1g - 2) for px, py in pts):
            continue  # wystaje poza mapę (np. długa hala na wschodniej krawędzi)
        gigant = abs(G.pole_wiel(pts)) > 2500 or tags.get("building") in GIGANCI_TAGI
        magazyn = False
        if gigant:
            if not _przy_dworcu_torach(mcx, mcy, tory_segs):
                continue  # współczesny gigant poza strefą dworca/torów — niewierny OSM 1939, wycięty
            magazyn = True  # w strefie dworca/torów: zostaje jako ceglany magazyn kolejowy, max 12 m
        if pole(pts) < 0:
            pts = pts[::-1]
        out.append({"id": wid, "pts": pts, "tags": tags, "magazyn": magazyn})
    for b in out:
        dl, szer, ang, (cx, cy) = _obb(b["pts"])
        b["dl"], b["szer"], b["ang"] = dl, szer, ang
        if b["magazyn"] and min(dl, szer) > 55:
            b["usun"] = True  # dzisiejsza hala ~100x100 m przy torach — w 1939 jej nie było
        b["cx"], b["cy"] = cx, cy
        b["local"] = [(x - cx, y - cy) for x, y in b["pts"]]
        b["dist"] = math.hypot(cx, cy)
    return [b for b in out if not b.get("usun")]


def _wspolne_krawedzie(buildings):
    for b in buildings:
        pts = b["pts"]; n = len(pts)
        shared = [False] * n
        cands = [o for o in buildings if o is not b and _dist((b["cx"], b["cy"]), (o["cx"], o["cy"])) < 60]
        for i in range(n):
            a1, a2 = pts[i], pts[(i + 1) % n]
            for o in cands:
                opts = o["pts"]; m = len(opts)
                for j in range(m):
                    c1, c2 = opts[j], opts[(j + 1) % m]
                    if _dist_pt_seg(a1, c1, c2) < 0.6 and _dist_pt_seg(a2, c1, c2) < 0.6:
                        shared[i] = True
                        break
                if shared[i]:
                    break
        b["shared"] = shared


def _drogi_segmenty(D):
    segs = []
    for wid, (refs, tags) in D["ways"].items():
        if not tags.get("highway") or not G.droga_1939(tags):
            continue
        latlon = G.wsp_drogi(D, wid)
        if len(latlon) < 2:
            continue
        pts = [G.P(a, b) for a, b in latlon]
        for i in range(len(pts) - 1):
            a, b = pts[i], pts[i + 1]
            if math.hypot(*a) <= 750 or math.hypot(*b) <= 750:
                segs.append((a, b))
    return segs


def _poziomy(wid, tags, dist):
    lv = tags.get("building:levels")
    try:
        if lv:
            return max(1, int(float(lv)))
    except Exception:
        pass
    b = tags.get("building")
    seed = int(wid) if wid.isdigit() else abs(hash(wid)) % 100000
    rnd = random.Random(seed)
    if b in ("house", "detached"):
        return 2
    if b in ("industrial", "warehouse"):
        return rnd.choice([1, 2])
    if dist <= 700:
        return rnd.choice([3, 4])
    return rnd.choice([2, 3])


def _wysokosc(levels, tags):
    parter = 5.5 if tags.get("building") in ("industrial", "warehouse") else 4.0
    return parter, parter + max(0, levels - 1) * 3.4


def _kolor_sciany(rnd, tags):
    b = tags.get("building")
    if b in ("industrial", "warehouse"):
        return rnd.choice(["cegla", "cegla_ciemna", "klinkier"])
    if rnd.random() < 0.15:
        return rnd.choice(["cegla", "cegla_jasna"])
    return rnd.choice(TYNKI)


def _kolor_dachu(rnd):
    r = rnd.random()
    if r < 0.08:
        return "lupek"
    if r < 0.15:
        return "papa"
    return rnd.choice(DACHOWKI)


_DROGI = []


def _min_dist_road(p):
    if not _DROGI:
        return 999.0
    return min(_dist_pt_seg(p, a, b) for a, b in _DROGI)


def _buduj_budynek(b):
    wid, tags, local = b["id"], b["tags"], b["local"]
    cx, cy, dl, szer, ang, dist = b["cx"], b["cy"], b["dl"], b["szer"], b["ang"], b["dist"]
    shared = b["shared"]
    seed = int(wid) if wid.isdigit() else abs(hash(wid)) % 100000
    rnd = random.Random(seed)
    magazyn = b.get("magazyn", False)
    levels = 1 if magazyn else _poziomy(wid, tags, dist)
    parter_h, total_h = _wysokosc(levels, tags)
    wall = "cegla" if magazyn else _kolor_sciany(rnd, tags)
    roof_c = _kolor_dachu(rnd)
    m = Siatka("Bud_%s" % wid)
    m.obrys(local, 0.0, 0.8, "cokol", gora=False)
    m.obrys(local, 0.8, total_h, wall, gora=False)
    if dist <= 700:
        _obwodnica(m, local, 0.8 + parter_h, 0.18, 0.12, wall)
    _obwodnica(m, local, total_h, 0.22, 0.3, wall)

    n = len(local)
    edge_len = [math.hypot(local[(k + 1) % n][0] - local[k][0], local[(k + 1) % n][1] - local[k][1]) for k in range(n)]
    max_len = max(edge_len) if edge_len else 0
    drzwi_dodane = False
    for i in range(n):
        if shared[i] or edge_len[i] < 2.0:
            continue
        if dist > 900 and edge_len[i] < max_len - 0.01:
            continue
        a_, b_ = local[i], local[(i + 1) % n]
        witryna = False
        if dist <= 600:
            mid = ((a_[0] + b_[0]) / 2 + cx, (a_[1] + b_[1]) / 2 + cy)
            witryna = _min_dist_road(mid) < 14.0
        dxk, dyk = b_[0] - a_[0], b_[1] - a_[1]
        lk = math.hypot(dxk, dyk) or 1.0
        uxk, uyk = dxk / lk, dyk / lk
        rozst_bazowy = 2.8 if dist <= 500 else (3.6 if dist <= 900 else 4.6)
        for floor in range(1, levels + 1):
            if floor == 1:
                z0, wys = 1.0, min(parter_h - 0.6, 2.4 if witryna else 2.6)
                szer_o, rozst = (2.2, 3.0) if witryna else (1.1, rozst_bazowy)
            else:
                z0, wys = 0.8 + parter_h + (floor - 2) * 3.4 + 0.3, 1.7
                szer_o, rozst = 1.1, rozst_bazowy
            ile = m.okna_rzad(a_, b_, z0, wys, szer_o, rozst, parapet=False)
            if floor == 1 and witryna and ile and rnd.random() < 0.4:
                nxk, nyk = uyk, -uxk
                srod = ((a_[0] + b_[0]) / 2, (a_[1] + b_[1]) / 2)
                mp = (srod[0] + nxk * 0.5, srod[1] + nyk * 0.5, z0 + wys + 0.15)
                kol = rnd.choice(["markiza_czerwona", "markiza_zielona", "markiza_kremowa"])
                m.box(mp, (min(lk, 4.0), 1.0, 0.08), kol, rot_z=math.degrees(math.atan2(uyk, uxk)))
        if witryna and not drzwi_dodane:
            dp = (a_[0] + uxk * 1.2, a_[1] + uyk * 1.2, 1.1)
            m.box(dp, (1.1, 0.14, 2.2), "drzwi", rot_z=math.degrees(math.atan2(uyk, uxk)))
            drzwi_dodane = True

    rect = abs(pole(local)) / (dl * szer) if dl * szer > 1e-6 else 0
    if magazyn:
        rh = min(max(2.2, min(dl, szer) * 0.42), 4.0)
        m.dach_dwuspadowy((0, 0, total_h), dl, szer, rh, roof_c, m_szczyt=wall, rot_z=ang)
        roof_top = total_h + rh
    elif rect > 0.88 and dl > 3 and szer > 3:
        rh = min(max(2.2, min(dl, szer) * 0.42), 5.0)
        if dl / max(szer, 0.01) > 1.6:
            m.dach_dwuspadowy((0, 0, total_h), dl, szer, rh, roof_c, m_szczyt=wall, rot_z=ang)
        else:
            m.dach_czterospadowy((0, 0, total_h), dl, szer, rh, roof_c, rot_z=ang)
        roof_top = total_h + rh
    else:
        m.wielokat(local, total_h + 0.02, roof_c)
        _obwodnica(m, local, total_h + 0.6, 0.5, 0.05, wall)
        roof_top = total_h + 0.6
    n_komin = rnd.randint(1, 3) if dist <= 700 else rnd.randint(0, 1)
    for _ in range(n_komin):
        kx = rnd.uniform(-dl * 0.25, dl * 0.25)
        ky = rnd.uniform(-szer * 0.25, szer * 0.25)
        m.box((kx, ky, roof_top + 0.45), (0.5, 0.5, 0.9), "cegla")
    return m.obiekt(poz=(cx, cy, 0)), b


# ---------------------------------------------------------------- kamienice pierzei Rynku
RYNEK_OBRYS = [(-26.01, -57.41), (-86.9, -1.44), (-32.16, 57.64), (25.99, 5.39)]  # z ulice_geo.json
RYNEK_SR = (sum(p[0] for p in RYNEK_OBRYS) / 4, sum(p[1] for p in RYNEK_OBRYS) / 4)
TYNKI_RYNEK = ["tynk_kremowy", "tynk_ochra", "tynk_losos", "tynk_szarozielony", "tynk_piasek",
               "tynk_rozowy", "tynk_zolty", "tynk_bez"]


def _w_rynku(p):
    ins = False
    n = len(RYNEK_OBRYS)
    for i in range(n):
        a, b = RYNEK_OBRYS[i], RYNEK_OBRYS[(i + 1) % n]
        if (a[1] > p[1]) != (b[1] > p[1]):
            if p[0] < a[0] + (p[1] - a[1]) * (b[0] - a[0]) / (b[1] - a[1]):
                ins = not ins
    return ins


def _odl_rynek(p):
    if _w_rynku(p):
        return 0.0
    n = len(RYNEK_OBRYS)
    return min(_dist_pt_seg(p, RYNEK_OBRYS[i], RYNEK_OBRYS[(i + 1) % n]) for i in range(n))


def _fronty_rynku(b):
    """Krawędzie budynku zwrócone do placu, ≤12 m od niego."""
    local, cx, cy = b["local"], b["cx"], b["cy"]
    n = len(local)
    out = []
    for i in range(n):
        a_, b_ = local[i], local[(i + 1) % n]
        l = _dist(a_, b_)
        if l < 4.0:
            continue
        ux, uy = (b_[0] - a_[0]) / l, (b_[1] - a_[1]) / l
        nx, ny = uy, -ux
        mx, my = (a_[0] + b_[0]) / 2 + cx, (a_[1] + b_[1]) / 2 + cy
        if _odl_rynek((mx, my)) > 12.0 and min(_odl_rynek((a_[0] + cx, a_[1] + cy)), _odl_rynek((b_[0] + cx, b_[1] + cy))) > 12.0:
            continue
        tx, ty = RYNEK_SR[0] - mx, RYNEK_SR[1] - my
        tl = math.hypot(tx, ty) or 1.0
        if (nx * tx + ny * ty) / tl < 0.35:
            continue
        out.append(i)
    return out


def _q2(m, a, b, c, d, mat_):
    m.quad(a, b, c, d, mat_)
    m.quad(d, c, b, a, mat_)


def _buduj_kamienica(b, fronty):
    wid, local, cx, cy = b["id"], b["local"], b["cx"], b["cy"]
    seed = int(wid) if wid.isdigit() else abs(hash(wid)) % 100000
    rnd = random.Random(seed * 7 + 3)
    m = Siatka("Kamienica_%s" % wid)
    n = len(local)
    glebokosc = max(6.0, min(12.0, min(b["dl"], b["szer"]) * 0.85))
    segs = []
    for i in fronty:
        a_, b_ = local[i], local[(i + 1) % n]
        L = _dist(a_, b_)
        k = max(1, int(round(L / 10.0)))
        if L / k < 6.5 and k > 1:
            k -= 1
        for s in range(k):
            t0, t1 = s / k, (s + 1) / k
            pa = (a_[0] + (b_[0] - a_[0]) * t0, a_[1] + (b_[1] - a_[1]) * t0)
            pb = (a_[0] + (b_[0] - a_[0]) * t1, a_[1] + (b_[1] - a_[1]) * t1)
            segs.append((pa, pb))
    hs = []
    for _ in segs:
        lv = rnd.choice([3, 4, 4])
        hs.append(4.4 + (lv - 1) * 3.4 + 0.6 + rnd.uniform(-0.6, 0.6))
    h_body = min(hs) - 0.2
    sc_kol = rnd.choice(TYNKI_RYNEK)
    m.obrys(local, 0.0, 0.7, "cokol", gora=False)
    m.obrys(local, 0.7, h_body, sc_kol, gora=False)
    m.wielokat(local, h_body, "papa")
    # podcienia: zachodnia połowa placu (pierzeja W i N-W), nigdy na księgarni
    arkady_bud = (not b.get("ksiegarnia")) and (cx - RYNEK_SR[0]) < -18
    for si, ((pa, pb), H) in enumerate(zip(segs, hs)):
        L = _dist(pa, pb)
        ux, uy = (pb[0] - pa[0]) / L, (pb[1] - pa[1]) / L
        nx, ny = uy, -ux
        ang_u = math.degrees(math.atan2(uy, ux))
        ang_n = math.degrees(math.atan2(ny, nx))
        kol = TYNKI_RYNEK[(seed + si * 3) % len(TYNKI_RYNEK)]
        rama = rnd.choice(["piaskowiec", "tynk_kremowy", "piaskowiec"])
        if kol == "tynk_kremowy":
            rama = "piaskowiec"

        def P(s, d, z=None):  # s wzdłuż frontu, d na zewnątrz od lica
            x, y = pa[0] + ux * s + nx * d, pa[1] + uy * s + ny * d
            return (x, y) if z is None else (x, y, z)

        arkady = arkady_bud and L >= 6.0
        D = 3.0 if arkady else 0.0
        z_par = 4.4
        # bryła segmentu
        if arkady:
            c = P(L / 2, D / 2 + 0.05 - glebokosc / 2 + D / 2)
            dep = glebokosc + D
            c = P(L / 2, 0.1 + D - dep / 2)
            m.box((c[0], c[1], (z_par + H) / 2), (L, dep, H - z_par), kol, rot_z=ang_u, bez_dolu=False)
            c2 = P(L / 2, 0.05 - glebokosc / 2)
            m.box((c2[0], c2[1], z_par / 2), (L - 0.1, glebokosc, z_par), kol, rot_z=ang_u)
            # filary i łuki
            nb = max(2, int(round(L / 3.4)))
            bw = L / nb
            for k_ in range(nb + 1):
                s = min(max(k_ * bw, 0.35), L - 0.35)
                fp = P(s, 0.1 + D - 0.35)
                m.box((fp[0], fp[1], z_par / 2), (0.7, 0.7, z_par), "piaskowiec", rot_z=ang_u)
            zs, r = 2.75, None
            for k_ in range(nb):
                s0, s1 = k_ * bw + 0.35, (k_ + 1) * bw - 0.35
                r = (s1 - s0) / 2
                sm = (s0 + s1) / 2
                for dd in (0.1 + D + 0.02, 0.1 + D - 0.72):
                    kroki = 6
                    for j in range(kroki):
                        a0 = math.pi * j / kroki; a1 = math.pi * (j + 1) / kroki
                        s_a, z_a = sm - r * math.cos(a0), zs + min(r, z_par - zs - 0.3) * math.sin(a0)
                        s_b, z_b = sm - r * math.cos(a1), zs + min(r, z_par - zs - 0.3) * math.sin(a1)
                        _q2(m, P(s_a, dd, z_a), P(s_b, dd, z_b), P(s_b, dd, z_par), P(s_a, dd, z_par), "piaskowiec")
                # witryna i drzwi w głębi podcienia
                w = P(sm, 0.12, 0)
                m.box((w[0], w[1], 1.6), (min(2.4, bw - 1.2), 0.08, 2.2), "okno", rot_z=ang_u)
                m.box((w[0], w[1], 2.85), (min(2.6, bw - 1.0), 0.1, 0.3), "drewno_ciemne" if k_ % 2 else "drewno", rot_z=ang_u)
        else:
            c = P(L / 2, 0.1 - glebokosc / 2)
            m.box((c[0], c[1], H / 2), (L, glebokosc, H), kol, rot_z=ang_u)
            # parter: witryny, drzwi, markizy
            nw = max(1, int(L // 3.6))
            szw = L / nw
            for k_ in range(nw):
                sm = (k_ + 0.5) * szw
                w = P(sm, 0.14)
                if k_ == nw // 2 and nw >= 2:
                    m.box((w[0], w[1], 1.15), (1.2, 0.1, 2.3), "drzwi", rot_z=ang_u)
                    m.box((w[0], w[1], 2.45), (1.6, 0.16, 0.25), rama, rot_z=ang_u)
                    continue
                ww = min(2.6, szw - 0.9)
                m.box((w[0], w[1], 0.4), (ww + 0.3, 0.16, 0.8), "cokol", rot_z=ang_u)
                m.box((w[0], w[1], 1.95), (ww + 0.3, 0.12, 2.5), "drewno_ciemne", rot_z=ang_u)
                w2 = P(sm, 0.2)
                m.box((w2[0], w2[1], 1.95), (ww, 0.06, 2.2), "okno", rot_z=ang_u)
                if (k_ + si) % 3 != 2:
                    mk = ["markiza_czerwona", "markiza_zielona", "markiza_kremowa"][(seed + k_ + si) % 3]
                    a1, b1 = P(sm - ww / 2 - 0.1, 0.12, 3.55), P(sm + ww / 2 + 0.1, 0.12, 3.55)
                    b2, a2 = P(sm + ww / 2 + 0.1, 1.5, 3.0), P(sm - ww / 2 - 0.1, 1.5, 3.0)
                    _q2(m, a1, b1, b2, a2, mk)
                    _q2(m, a2, b2, P(sm + ww / 2 + 0.1, 1.5, 2.75), P(sm - ww / 2 - 0.1, 1.5, 2.75), mk)
        dF = 0.1 + D  # lico fasady pięter
        # cokół i gzymsy
        g = P(L / 2, dF + 0.12)
        m.box((g[0], g[1], z_par), (L + 0.1, 0.3, 0.32), rama, rot_z=ang_u)
        nfl = int(round((H - 0.6 - z_par) / 3.4))
        for f in range(1, nfl):
            g = P(L / 2, dF + 0.06)
            m.box((g[0], g[1], z_par + f * 3.4), (L, 0.14, 0.16), rama, rot_z=ang_u)
        g = P(L / 2, dF + 0.25)
        m.box((g[0], g[1], H - 0.2), (L + 0.4, 0.5, 0.4), rama, rot_z=ang_u)
        # okna pięter z obramieniem i naczółkiem
        no = max(2, int(round(L / 2.8)))
        for f in range(nfl):
            z0 = z_par + 0.5 + f * 3.4
            wys = 1.9 if f == 0 else 1.7
            for k_ in range(no):
                s = (k_ + 0.5) * L / no
                p0 = P(s, dF + 0.04)
                m.box((p0[0], p0[1], z0 + wys / 2), (1.45, 0.1, wys + 0.3), rama, rot_z=ang_u)
                p1 = P(s, dF + 0.1)
                m.box((p1[0], p1[1], z0 + wys / 2), (1.05, 0.06, wys), "okno", rot_z=ang_u)
                p2 = P(s, dF + 0.14)
                m.box((p2[0], p2[1], z0 - 0.08), (1.4, 0.26, 0.1), "kamien", rot_z=ang_u)
                if f == 0:
                    m.box((p2[0], p2[1], z0 + wys + 0.3), (1.7, 0.28, 0.18), rama, rot_z=ang_u)
        # dach i szczyt
        typ = ["szczyt", "barok", "kalenica"][(seed + si) % 3] if L <= 13 else "kalenica"
        dach = rnd.choice(["dachowka", "dachowka", "dachowka_ciemna", "dachowka_brazowa"])
        if typ in ("szczyt", "barok"):
            hr = min(L * 0.55, 6.5)
            c = P(L / 2, 0.1 - glebokosc / 2 + 0.2 - (0.6 if typ == "barok" else 0.0))
            m.dach_dwuspadowy((c[0], c[1], H), glebokosc + 0.4, L, hr, dach, m_szczyt=kol, rot_z=ang_n, okap=0.3)
            if typ == "barok":
                kroki = [(0.92, 0.0, 0.42), (0.62, 0.42, 0.36), (0.36, 0.78, 0.34)]
                for fw, fz, fh in kroki:
                    pp = P(L / 2, 0.45)
                    m.box((pp[0], pp[1], H + (fz + fh / 2) * hr), (L * fw, 0.5, fh * hr), kol, rot_z=ang_u)
                    pp = P(L / 2, 0.55)
                    m.box((pp[0], pp[1], H + (fz + fh) * hr), (L * fw + 0.3, 0.6, 0.18), rama, rot_z=ang_u)
                    for sg in (-1, 1):
                        pk = P(L / 2 + sg * L * fw / 2, 0.55)
                        m.box((pk[0], pk[1], H + (fz + fh) * hr + 0.3), (0.35, 0.35, 0.45), rama, rot_z=ang_u)
                po = P(L / 2, 0.72)
                m.box((po[0], po[1], H + 0.55 * hr), (0.9, 0.06, 1.1), "okno", rot_z=ang_u)
            else:
                po = P(L / 2, 0.16)
                m.box((po[0], po[1], H + 0.3 * hr), (0.9, 0.06, 1.2), "okno", rot_z=ang_u)
                m.box((po[0], po[1], H + 0.3 * hr - 0.7), (1.2, 0.2, 0.12), rama, rot_z=ang_u)
            kom = P(L * 0.3, 0.1 - glebokosc * 0.6)
            m.box((kom[0], kom[1], H + hr * 0.8), (0.6, 0.6, 2.0), "cegla", rot_z=ang_u)
        else:
            hr = min(glebokosc * 0.5, 5.0)
            c = P(L / 2, 0.1 - glebokosc / 2)
            m.dach_dwuspadowy((c[0], c[1], H), L, glebokosc, hr, dach, m_szczyt=kol, rot_z=ang_u, okap=0.35)
            nl = max(1, int(L // 4.5))
            for k_ in range(nl):
                s = (k_ + 0.5) * L / nl
                dd = 1.7
                zb = H + hr * (dd - 0.6) / (glebokosc / 2)
                cl = P(s, 0.1 - dd)
                m.box((cl[0], cl[1], zb + 0.8), (1.5, 1.6, 1.6), kol, rot_z=ang_u)
                ol = P(s, 0.1 - dd + 0.82)
                m.box((ol[0], ol[1], zb + 0.8), (0.9, 0.06, 1.1), "okno", rot_z=ang_u)
                m.dach_dwuspadowy((cl[0], cl[1], zb + 1.6), 1.7, 1.7, 0.75, dach, m_szczyt=kol, rot_z=ang_n, okap=0.1)
            for sk in (0.2, 0.8):
                kom = P(L * sk, 0.1 - glebokosc * 0.55)
                m.box((kom[0], kom[1], H + hr * 0.8), (0.6, 0.6, 2.0), "cegla", rot_z=ang_u)
    if b.get("ksiegarnia"):
        # wejście do księgarni Zofii dokładnie za punktem miasto_drzwi_ksiegarnia
        (px, py, _), yaw = _drzwi_na_budynku(b, (G.RYNEK_PLAC[0], G.RYNEK_PLAC[1]))
        dx_, dy_ = math.sin(math.radians(yaw)), math.cos(math.radians(yaw))
        rz = math.degrees(math.atan2(dx_, -dy_))
        lx, ly = px - cx, py - cy
        for d_, wym, mt in ((0.72, (3.6, 0.36, 3.3), "drewno_ciemne"), (0.6, (1.2, 0.2, 2.3), "drzwi")):
            m.box((lx + dx_ * d_, ly + dy_ * d_, wym[2] / 2), wym, mt, rot_z=rz)
        for sg in (-1, 1):
            ox, oy = dy_ * sg * 1.0, -dx_ * sg * 1.0
            m.box((lx + dx_ * 0.56 + ox * 0.0 + (-dy_) * 0 + (dy_ * sg * 1.05), ly + dy_ * 0.56 + (-dx_ * sg * 1.05), 1.55), (0.7, 0.06, 2.1), "okno", rot_z=rz)
        a1 = (lx + dx_ * 0.6 - dy_ * 1.9, ly + dy_ * 0.6 + dx_ * 1.9, 3.5)
        b1 = (lx + dx_ * 0.6 + dy_ * 1.9, ly + dy_ * 0.6 - dx_ * 1.9, 3.5)
        b2 = (b1[0] - dx_ * 1.4, b1[1] - dy_ * 1.4, 2.95)
        a2 = (a1[0] - dx_ * 1.4, a1[1] - dy_ * 1.4, 2.95)
        _q2(m, a1, b1, b2, a2, "markiza_zielona")
    return m.obiekt(poz=(cx, cy, 0)), b


# ---------------------------------------------------------------- wille korytarza + stodoła
def _tarnogorska_pts(D):
    ids = G.tarnogorska(D)
    pts = []
    for wid in ids:
        pts.extend(G.punkty_drogi(D, wid))
    pts = sorted(set(pts), key=lambda p: p[1])
    return pts


def _droga_x_dla_y(pts, y):
    ys = [p[1] for p in pts]
    i = bisect.bisect_left(ys, y)
    i = max(1, min(i, len(pts) - 1))
    p0, p1 = pts[i - 1], pts[i]
    t = (y - p0[1]) / (p1[1] - p0[1]) if p1[1] != p0[1] else 0.0
    x = p0[0] + t * (p1[0] - p0[0])
    dx, dy = p1[0] - p0[0], p1[1] - p0[1]
    l = math.hypot(dx, dy) or 1.0
    return x, (dx / l, dy / l)


def _buduj_willa(i, cx, cy, kat, rnd):
    m = Siatka("Willa_%d" % i)
    szx, szy = rnd.uniform(11, 13), rnd.uniform(9, 10)
    wall = rnd.choice(["tynk_bez", "tynk_kremowy", "tynk_piasek", "tynk_szary", "tynk_bialy"])
    roof_c = rnd.choice(["dachowka", "dachowka_ciemna", "dachowka_brazowa"])
    h1 = 3.6
    m.box((0, 0, h1 / 2), (szx, szy, h1), wall)
    m.box((0, 0, 0.4), (szx + 0.1, szy + 0.1, 0.8), "cokol")
    m.dach_czterospadowy((0, 0, h1), szx, szy, 3.2, roof_c)
    m.okna_rzad((szx / 2, szy / 2), (-szx / 2, szy / 2), 1.1, 1.6, 1.1, 2.8)
    m.okna_rzad((-szx / 2, -szy / 2), (szx / 2, -szy / 2), 1.1, 1.6, 1.1, 2.8)
    m.box((0, szy / 2 + 0.05, 1.3), (1.2, 0.14, 2.3), "drzwi")
    m.box((0, szy / 2 + 0.05, 2.55), (1.8, 0.5, 0.1), "dachowka")
    obj = m.obiekt(poz=(cx, cy, 0), kat=-kat)
    hedge = Siatka("Zywoplot_%d" % i)
    _plot_prostokat(hedge, szx + 10, szy + 10, "zywoplot")
    hobj = hedge.obiekt(poz=(cx, cy, 0), kat=-kat)
    return obj, hobj, (cx, cy, -kat)


def _buduj_wille(D, n=20):
    pts = _tarnogorska_pts(D)
    obiekty = []
    polowa = n // 2
    pierwsza = None
    idx = 0
    for strona in (1, -1):
        for k in range(polowa):
            y = 1000 + (1420 - 1000) * k / max(1, polowa - 1)
            rnd = random.Random(2000 + idx)
            odl = rnd.uniform(18, 30)
            rx, (tx, ty) = _droga_x_dla_y(pts, y)
            nx, ny = ty, -tx
            cx, cy = rx + nx * odl * strona, y + ny * odl * strona
            kat = math.degrees(math.atan2(-nx * strona, -ny * strona))
            o1, o2, info = _buduj_willa(idx, cx, cy, kat, rnd)
            obiekty += [o1, o2]
            if pierwsza is None:
                pierwsza = info
            idx += 1
    return obiekty, pierwsza


def _buduj_stodola():
    m = Siatka("Stodola")
    szx, szy, h = 16.0, 10.0, 4.5
    m.box((0, 0, h / 2), (szx, szy, h), "drewno")
    m.box((0, 0, 0.3), (szx + 0.1, szy + 0.1, 0.6), "cokol")
    m.dach_dwuspadowy((0, 0, h), szx, szy, 3.5, "dachowka_ciemna", m_szczyt="drewno")
    m.box((0, szy / 2 + 0.03, 2.0), (3.2, 0.1, 4.0), "drzwi_ciemne")
    return m.obiekt(poz=(G.STODOLA[0], G.STODOLA[1], 0)), szx, szy


# ---------------------------------------------------------------- drzwi na istniejącym budynku
def _drzwi_na_budynku(b, cel):
    local, cx, cy = b["local"], b["cx"], b["cy"]
    n = len(local)
    best = None
    for i in range(n):
        a_, b_ = local[i], local[(i + 1) % n]
        mx, my = (a_[0] + b_[0]) / 2 + cx, (a_[1] + b_[1]) / 2 + cy
        d = _dist((mx, my), cel)
        if best is None or d < best[0]:
            best = (d, a_, b_, mx, my)
    _, a_, b_, mx, my = best
    dxk, dyk = b_[0] - a_[0], b_[1] - a_[1]
    lk = math.hypot(dxk, dyk) or 1.0
    uxk, uyk = dxk / lk, dyk / lk
    nxk, nyk = uyk, -uxk
    if (mx - cx) * nxk + (my - cy) * nyk < 0:
        nxk, nyk = -nxk, -nyk
    px, py = mx + nxk * 1.0, my + nyk * 1.0
    yaw = math.degrees(math.atan2(-nxk, -nyk))
    return (px, py, 1.0), yaw


# ---------------------------------------------------------------- GŁÓWNA FUNKCJA
def zbuduj():
    global _DROGI
    D = G.wczytaj()
    _DROGI = _drogi_segmenty(D)
    buildings = _wczytaj_budynki()
    _wspolne_krawedzie(buildings)

    rc = (G.RYNEK_PLAC[0], G.RYNEK_PLAC[1]); rr = G.RYNEK_PLAC[2]
    target = (rc[0] + rr * 1.3, rc[1])
    ksiegarnia = min((b for b in buildings if b["cx"] > rc[0]), key=lambda b: _dist((b["cx"], b["cy"]), target), default=None)
    if ksiegarnia:
        ksiegarnia["ksiegarnia"] = True

    obiekty = []
    n_kam = 0
    for b in buildings:
        fr = _fronty_rynku(b) if _dist((b["cx"], b["cy"]), RYNEK_SR) < 140 else []
        if fr:
            obj, _ = _buduj_kamienica(b, fr)
            n_kam += 1
            ntri = sum(len(p.vertices) - 2 for p in obj.data.polygons)
            print("KAMIENICA", b["id"], "frontow", len(fr), "tri", ntri)
        else:
            obj, _ = _buduj_budynek(b)
        obiekty.append(obj)
    print("KAMIENIC RYNKU:", n_kam)

    wille_obj, willa1 = _buduj_wille(D)
    obiekty += wille_obj
    stodola_obj, st_szx, st_szy = _buduj_stodola()
    obiekty.append(stodola_obj)

    # --- punkty
    if ksiegarnia:
        p, yaw = _drzwi_na_budynku(ksiegarnia, rc)
        punkt("miasto_drzwi_ksiegarnia", p, yaw)

    hotel = G.LANDMARKI["Hotel"]
    zw_ids = [k for k, (r, t) in D["ways"].items() if "Zwyci" in t.get("name", "") and t.get("highway")]
    zw_pts = []
    for wid in zw_ids:
        zw_pts.extend(G.punkty_drogi(D, wid))
    mid = ((rc[0] + hotel[0]) / 2, (rc[1] + hotel[1]) / 2)
    fotograf = None
    if zw_pts:
        cands = [b for b in buildings if min(_dist((b["cx"], b["cy"]), p) for p in zw_pts) < 25]
        if cands:
            fotograf = min(cands, key=lambda b: _dist((b["cx"], b["cy"]), mid))
    if fotograf:
        cel = min(zw_pts, key=lambda p: _dist((fotograf["cx"], fotograf["cy"]), p))
        p, yaw = _drzwi_na_budynku(fotograf, cel)
        punkt("miasto_drzwi_fotograf", p, yaw)

    if willa1:
        wx, wy, wkat = willa1
        punkt("miasto_karta_3", (wx, wy - 8.0, 0.3), 0.0)

    sx, sy = G.STODOLA
    punkt("miasto_stodola", (sx, sy + st_szy / 2 + 1.0, 1.0), 180.0)
    punkt("miasto_karta_6", (sx + st_szx / 2 + 3.0, sy, 0.0), 0.0)

    print("BUDYNKI:", len(buildings), "WILLE:", len(wille_obj) // 2, "obiektow:", len(obiekty))
    return obiekty


if __name__ == "__main__":
    wyczysc_scene()
    obs = zbuduj()
    x0, y0, x1, y1 = G.GRANICE
    SEK = 500
    sektory = {}
    for o in obs:
        i = int((o.location.x - x0) // SEK)
        j = int((o.location.y - y0) // SEK)
        sektory.setdefault((i, j), []).append(o)
    for (i, j), grp in sektory.items():
        eksport_fbx(grp, os.path.join(UNITY_MODELE, "Budynki_%d_%d.fbx" % (i, j)))
    zapisz_meta(os.path.join(DANE, "meta_budynki.json"))
    ziemia_podgladu(900)
    D = G.wczytaj()
    rc = (G.RYNEK_PLAC[0], G.RYNEK_PLAC[1])
    zw_ids = ["4602492", "29987646", "33039523", "49814896", "147397625", "185810501",
              "230316922", "489205979", "179154365", "179154366", "603773766", "772264326", "1075173146"]
    zw_pts_sorted = sorted(
        (p for wid in zw_ids for p in G.punkty_drogi(D, wid)),
        key=lambda p: (p[0] - rc[0]) ** 2 + (p[1] - rc[1]) ** 2)
    p0, p1 = zw_pts_sorted[0], zw_pts_sorted[1]
    dxz, dyz = p1[0] - p0[0], p1[1] - p0[1]
    azy = math.degrees(math.atan2(-dxz, dyz))
    render_podglad(os.path.join(PODGLADY, "budynki_1.png"), cel=(0, 0, 0), odl=700, azymut=200, wysokosc_kat=42)
    render_podglad(os.path.join(PODGLADY, "budynki_2.png"), cel=(rc[0], rc[1], 0.0), odl=170, azymut=200, wysokosc_kat=38)
    # z oczu gracza na pierzeję: kamera ok. 1,7 m nad placem (ujemny kąt = cel wyżej)
    for nr, (kx, ky, tx, ty) in enumerate([(-10, -10, -80, 40), (-50, 10, 20, -40), (-40, -20, 30, 30)], start=3):
        d = math.hypot(tx - kx, ty - ky)
        az = math.degrees(math.atan2(kx - tx, ty - ky))
        render_podglad(os.path.join(PODGLADY, "budynki_%d.png" % nr), cel=(tx, ty, 1.7 + d * math.tan(math.radians(9))),
                       odl=d / math.cos(math.radians(9)), azymut=az, wysokosc_kat=-9)
