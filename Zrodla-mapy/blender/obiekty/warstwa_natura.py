# -*- coding: utf-8 -*-
"""
warstwa_natura.py — warstwa NATURA (drzewa, krzaki) dla mapy Gliwice 1939.
Pisany u siebie (asystent), ale sys.path wskazuje na wspólny folder mapa/blender.
"""
import sys, os, math, random, json
sys.path.insert(0, '/Users/nataniel_bk/Documents/NBK/agent-dashboard/agent-workspace/otton/hackaton-gliwice/mapa/blender')
from lowpoly import *
from gliwice_geo import *

import bpy

WYJ = os.path.dirname(os.path.abspath(__file__))
PODGLADY_LOK = os.path.join(WYJ, "podglady")
DANE_LOK = os.path.join(WYJ, "dane")

LICZNIK = {"drzewo": 0, "krzak": 0}
OBJ_DRZEWA = []
OBJ_KRZAKI = []

# ----------------------------------------------------------------------------- SIATKI WARIANTÓW (budujemy raz na typ+ziarno)
N_WARIANTOW = 12
_mesh_drzewo = {}   # (typ) -> lista 12 Siatka (bpy mesh) gotowych do wspólnego użycia
_mesh_krzak = []

def przygotuj_siatki():
    for typ in range(5):
        lst = []
        for z in range(N_WARIANTOW):
            s = drzewo("SiatkaDrzewo_%d_%d" % (typ, z), typ=typ, ziarno=1000 * typ + z, skala=1.0)
            me = bpy.data.meshes.new(s.nazwa)
            nazwy = sorted(set(s.fm))
            idx = {n: i for i, n in enumerate(nazwy)}
            me.from_pydata(s.v, [], s.f)
            for n in nazwy:
                me.materials.append(mat(n))
            for i, p in enumerate(me.polygons):
                p.material_index = idx[s.fm[i]]
                p.use_smooth = False
            me.update()
            lst.append(me)
        _mesh_drzewo[typ] = lst
    for z in range(N_WARIANTOW):
        s = krzak("SiatkaKrzak_%d" % z, ziarno=5000 + z, skala=1.0)
        me = bpy.data.meshes.new(s.nazwa)
        nazwy = sorted(set(s.fm))
        idx = {n: i for i, n in enumerate(nazwy)}
        me.from_pydata(s.v, [], s.f)
        for n in nazwy:
            me.materials.append(mat(n))
        for i, p in enumerate(me.polygons):
            p.material_index = idx[s.fm[i]]
            p.use_smooth = False
        me.update()
        _mesh_krzak.append(me)

def postaw_drzewo(x, y, typ=None, skala=None, rnd=None):
    rnd = rnd or random
    if typ is None:
        typ = rnd.choice([0, 0, 1, 2, 3, 4])
    z = rnd.randrange(N_WARIANTOW)
    s = (skala if skala is not None else rnd.uniform(0.8, 1.25))
    n = "Drzewo_%d" % LICZNIK["drzewo"]
    LICZNIK["drzewo"] += 1
    ob = bpy.data.objects.new(n, _mesh_drzewo[typ][z])
    ob.location = (x, y, 0.0)
    ob.rotation_euler = (0, 0, math.radians(rnd.uniform(0, 360)))
    ob.scale = (s, s, s)
    bpy.context.scene.collection.objects.link(ob)
    OBJ_DRZEWA.append(ob)
    return ob

def postaw_krzak(x, y, skala=None, rnd=None):
    rnd = rnd or random
    z = rnd.randrange(N_WARIANTOW)
    s = (skala if skala is not None else rnd.uniform(0.75, 1.2))
    n = "Krzak_%d" % LICZNIK["krzak"]
    LICZNIK["krzak"] += 1
    ob = bpy.data.objects.new(n, _mesh_krzak[z])
    ob.location = (x, y, 0.0)
    ob.rotation_euler = (0, 0, math.radians(rnd.uniform(0, 360)))
    ob.scale = (s, s, s)
    bpy.context.scene.collection.objects.link(ob)
    OBJ_KRZAKI.append(ob)
    return ob

# ----------------------------------------------------------------------------- FILTRY / POMOCNICZE
def pt_w_wielokacie(x, y, pts):
    n = len(pts)
    wew = False
    j = n - 1
    for i in range(n):
        xi, yi = pts[i]; xj, yj = pts[j]
        if ((yi > y) != (yj > y)) and (x < (xj - xi) * (y - yi) / (yj - yi + 1e-12) + xi):
            wew = not wew
        j = i
    return wew

def dist_pt_seg(px, py, ax, ay, bx, by):
    dx, dy = bx - ax, by - ay
    l2 = dx * dx + dy * dy
    if l2 < 1e-9:
        return math.hypot(px - ax, py - ay)
    t = max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / l2))
    cx, cy = ax + t * dx, ay + t * dy
    return math.hypot(px - cx, py - cy)

def min_dist_do_drog(x, y, drogi_pts):
    best = 1e9
    for pts, szer in drogi_pts:
        for i in range(len(pts) - 1):
            d = dist_pt_seg(x, y, pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1])
            if d - szer / 2.0 < best:
                best = d - szer / 2.0
    return best

def zbyt_blisko_drogi(x, y, drogi_pts, zapas=1.0):
    for pts, szer in drogi_pts:
        d = min_dist_do_drog(x, y, [(pts, szer)])
        if d < zapas:
            return True
    return False

def w_budynku(x, y, budynki_poly):
    for pts in budynki_poly:
        xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
        if min(xs) - 2 <= x <= max(xs) + 2 and min(ys) - 2 <= y <= max(ys) + 2:
            if pt_w_wielokacie(x, y, pts):
                return True
    return False

def wolny(x, y, drogi_pts, budynki_poly, zapas_droga=1.0):
    if not w_granicach(x, y):
        return False
    if w_landmarku(x, y) and w_landmarku(x, y) != "Radiostacja":
        return False
    if zbyt_blisko_drogi(x, y, drogi_pts, zapas_droga):
        return False
    if w_budynku(x, y, budynki_poly):
        return False
    return True

# ----------------------------------------------------------------------------- ZBIERANIE DANYCH OSM
def zbierz(D):
    drzewa_osm = []       # [(x,y)]
    rzedy = []             # [[(x,y),...]]  tree_row
    parki = []             # [[(x,y)...]]
    lasy = []               # [[(x,y)...]]
    budynki_poly = []
    drogi_glowne = []       # [(pts, szer)]  primary/secondary do szpaleru
    drogi_wszystkie = []    # [(pts, szer)]  do testu "nie na drodze"

    for wid, (refs, tags) in D["ways"].items():
        pts_ll = []
        for r in refs:
            nd = D["nodes"].get(str(r))
            if nd:
                pts_ll.append((nd[0], nd[1]))
        if len(pts_ll) < 2:
            continue
        pts = [P(a, b) for a, b in pts_ll]

        if droga_1939(tags):
            h = tags.get("highway")
            szer = SZER_DROGI.get(h, 7)
            drogi_wszystkie.append((pts, szer))
            if h in ("primary", "secondary"):
                drogi_glowne.append((pts, szer))

        if tags.get("building") and zamkniete(refs) and budynek_1939(tags, pts):
            st = strefa(*pts_ll[0])
            if st == "miasto":
                budynki_poly.append(pts)

        if tags.get("natural") == "tree_row":
            rzedy.append(pts)

        if zamkniete(refs):
            if tags.get("leisure") == "park" or tags.get("landuse") == "grass" or tags.get("leisure") == "recreation_ground":
                parki.append(pts)
            if tags.get("landuse") == "forest" or tags.get("natural") == "wood":
                lasy.append(pts)

    for nid, nd in D["nodes"].items():
        lat, lon = nd[0], nd[1]
        tags = nd[2] if len(nd) > 2 else {}
        if tags.get("natural") == "tree":
            x, y = P(lat, lon)
            if w_granicach(x, y):
                drzewa_osm.append((x, y))

    return drzewa_osm, rzedy, parki, lasy, budynki_poly, drogi_glowne, drogi_wszystkie

def zamkniete(refs):
    return len(refs) > 3 and refs[0] == refs[-1]

def rozstaw_po_lamanej(pts, krok):
    """Punkty co 'krok' metrów wzdłuż łamanej pts (lista (x,y))."""
    out = []
    reszta = 0.0
    for i in range(len(pts) - 1):
        ax, ay = pts[i]; bx, by = pts[i + 1]
        dl = math.hypot(bx - ax, by - ay)
        if dl < 1e-6:
            continue
        ux, uy = (bx - ax) / dl, (by - ay) / dl
        d = reszta
        while d < dl:
            out.append((ax + ux * d, ay + uy * d, ux, uy))
            d += krok
        reszta = d - dl
    return out

def bbox(pts):
    xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
    return min(xs), min(ys), max(xs), max(ys)

# ----------------------------------------------------------------------------- GŁÓWNA BUDOWA
def main():
    wyczysc_scene()
    przygotuj_siatki()
    D = wczytaj()
    drzewa_osm, rzedy, parki, lasy, budynki_poly, drogi_glowne, drogi_wszystkie = zbierz(D)

    rnd = random.Random(42)
    LIMIT = 4000

    def limit_ok():
        return LICZNIK["drzewo"] < LIMIT

    # 1) węzły natural=tree (prawdziwe drzewa z OSM)
    for x, y in drzewa_osm:
        if not limit_ok():
            break
        if wolny(x, y, drogi_wszystkie, budynki_poly, 1.0):
            postaw_drzewo(x, y, rnd=rnd)

    # 2) tree_row co 8 m
    for pts in rzedy:
        if not limit_ok():
            break
        for (x, y, ux, uy) in rozstaw_po_lamanej(pts, 8.0):
            if not limit_ok():
                break
            if wolny(x, y, drogi_wszystkie, budynki_poly, 1.0):
                postaw_drzewo(x, y, rnd=rnd)

    # 3) parki / grass / recreation_ground: losowo co ~14 m z wolnymi polanami
    for pts in parki:
        if not limit_ok():
            break
        x0, y0, x1, y1 = bbox(pts)
        nx = max(1, int((x1 - x0) / 14))
        ny = max(1, int((y1 - y0) / 14))
        for i in range(nx):
            for j in range(ny):
                if not limit_ok():
                    break
                px = x0 + (i + rnd.uniform(0.15, 0.85)) * (x1 - x0) / nx
                py = y0 + (j + rnd.uniform(0.15, 0.85)) * (y1 - y0) / ny
                if not pt_w_wielokacie(px, py, pts):
                    continue
                if rnd.random() < 0.35:   # polanki - zostawiamy część pól puste
                    continue
                if wolny(px, py, drogi_wszystkie, budynki_poly, 1.0):
                    postaw_drzewo(px, py, rnd=rnd)
                    if rnd.random() < 0.25:
                        postaw_krzak(px + rnd.uniform(-2, 2), py + rnd.uniform(-2, 2), rnd=rnd)

    # 4) lasy / wood: gęsto co 7 m, sosny i brzozy
    for pts in lasy:
        if not limit_ok():
            break
        x0, y0, x1, y1 = bbox(pts)
        nx = max(1, int((x1 - x0) / 7))
        ny = max(1, int((y1 - y0) / 7))
        for i in range(nx):
            for j in range(ny):
                if not limit_ok():
                    break
                px = x0 + (i + rnd.uniform(0.2, 0.8)) * (x1 - x0) / nx
                py = y0 + (j + rnd.uniform(0.2, 0.8)) * (y1 - y0) / ny
                if not pt_w_wielokacie(px, py, pts):
                    continue
                if wolny(px, py, drogi_wszystkie, budynki_poly, 1.0):
                    typ = rnd.choice([2, 2, 4])  # sosny i brzozy
                    postaw_drzewo(px, py, typ=typ, rnd=rnd)

    # 5) szpaler lip po obu stronach głównych ulic (primary/secondary), promień 1000 m od (0,0), co 11 m
    for pts, szer in drogi_glowne:
        if all(math.hypot(px, py) > 1000 for px, py in pts):
            continue
        for strona in (1, -1):
            for (x, y, ux, uy) in rozstaw_po_lamanej(pts, 11.0):
                if not limit_ok():
                    break
                if math.hypot(x, y) > 1000:
                    continue
                nx_, ny_ = -uy, ux
                odsun = szer / 2.0 + 2.5
                px = x + nx_ * odsun * strona
                py = y + ny_ * odsun * strona
                if wolny(px, py, drogi_wszystkie, budynki_poly, 0.5):
                    postaw_drzewo(px, py, typ=0, rnd=rnd)   # lipa

    # 6) ZAGAJNIK: gęsty las mieszany 35-60 drzew + 20 krzaków, w kole, z polanką przy drodze
    zx, zy, zr = ZAGAJNIK
    n_drz_zag = rnd.randint(35, 60)
    i = 0
    prob = 0
    while i < n_drz_zag and prob < n_drz_zag * 20:
        prob += 1
        ang = rnd.uniform(0, 2 * math.pi)
        rad = zr * math.sqrt(rnd.uniform(0, 1))
        px, py = zx + math.cos(ang) * rad, zy + math.sin(ang) * rad
        # polanka przy drodze: strona najbliższa do drogi 1939 w promieniu zr ma mniejszą gęstość
        if min_dist_do_drog(px, py, drogi_wszystkie) < 10.0 and rnd.random() < 0.7:
            continue
        if wolny(px, py, drogi_wszystkie, budynki_poly, 1.0):
            postaw_drzewo(px, py, typ=rnd.choice([0, 1, 2, 2, 4]), rnd=rnd)
            i += 1
    for i in range(20):
        ang = rnd.uniform(0, 2 * math.pi)
        rad = zr * math.sqrt(rnd.uniform(0, 1))
        px, py = zx + math.cos(ang) * rad, zy + math.sin(ang) * rad
        if wolny(px, py, drogi_wszystkie, budynki_poly, 1.0):
            postaw_krzak(px, py, rnd=rnd)

    # 7) wokół STODOLA kilka drzew
    sx, sy = STODOLA
    for i in range(7):
        ang = rnd.uniform(0, 2 * math.pi)
        rad = rnd.uniform(6, 14)
        px, py = sx + math.cos(ang) * rad, sy + math.sin(ang) * rad
        if wolny(px, py, drogi_wszystkie, budynki_poly, 1.0):
            postaw_drzewo(px, py, typ=rnd.choice([0, 3]), rnd=rnd)

    # 8) miedze pól w strefie "radio" (pojedyncze drzewa/krzaki) - przybliżenie: wzdłuż dróg polnych/track
    for wid, (refs, tags) in D["ways"].items():
        if tags.get("highway") not in ("track", "unclassified", "residential", "service"):
            continue
        pts_ll = []
        for r in refs:
            nd = D["nodes"].get(str(r))
            if nd:
                pts_ll.append((nd[0], nd[1]))
        if len(pts_ll) < 2:
            continue
        if strefa(*pts_ll[0]) != "radio":
            continue
        pts = [P(a, b) for a, b in pts_ll]
        for (x, y, ux, uy) in rozstaw_po_lamanej(pts, 35.0):
            if not limit_ok():
                break
            if rnd.random() < 0.5:
                continue
            nx_, ny_ = -uy, ux
            odsun = 5.0 + rnd.uniform(0, 3)
            strona = rnd.choice([1, -1])
            px, py = x + nx_ * odsun * strona, y + ny_ * odsun * strona
            if wolny(px, py, drogi_wszystkie, budynki_poly, 1.0):
                if rnd.random() < 0.7:
                    postaw_drzewo(px, py, typ=rnd.choice([0, 4]), rnd=rnd)
                else:
                    postaw_krzak(px, py, rnd=rnd)

    print("DRZEWA:", LICZNIK["drzewo"], "KRZAKI:", LICZNIK["krzak"])

    # ----------------------------------------------------------------------- EKSPORT SEKTOROWY (500 m)
    x0, y0, x1, y1 = GRANICE
    SEK = 500.0
    eksportowane = []

    def sektor_ij(x, y):
        return int(math.floor((x - x0) / SEK)), int(math.floor((y - y0) / SEK))

    kubelki = {}
    for ob in OBJ_DRZEWA + OBJ_KRZAKI:
        i, j = sektor_ij(ob.location.x, ob.location.y)
        kubelki.setdefault((i, j), []).append(ob)

    os.makedirs(PODGLADY_LOK, exist_ok=True)
    os.makedirs(DANE_LOK, exist_ok=True)

    for (i, j), obs in sorted(kubelki.items()):
        nazwa = "Natura_%d_%d.fbx" % (i, j)
        plik = os.path.join(WYJ, nazwa)
        eksport_fbx(obs, plik)
        eksportowane.append({"sektor": [i, j], "plik": nazwa, "ile": len(obs)})

    zapisz_meta(os.path.join(DANE_LOK, "meta_natura.json"))
    # dopisz statystyki do meta na dysku (osobny plik, nie psujemy formatu zapisz_meta)
    with open(os.path.join(DANE_LOK, "meta_natura.json")) as f:
        meta_zawartosc = json.load(f)
    meta_zawartosc["statystyki"] = {
        "drzewa": LICZNIK["drzewo"], "krzaki": LICZNIK["krzak"],
        "sektory": eksportowane,
    }
    with open(os.path.join(DANE_LOK, "meta_natura.json"), "w", encoding="utf-8") as f:
        json.dump(meta_zawartosc, f, ensure_ascii=False, indent=1)

    # ----------------------------------------------------------------------- RENDER 2 PNG
    cx, cy = (x0 + x1) / 2.0, (y0 + y1) / 2.0
    render_podglad(os.path.join(PODGLADY_LOK, "natura_mapa.png"),
                    cel=(cx, cy, 0), odl=1900.0, azymut=200.0, wysokosc_kat=62.0, ogniskowa=24, szer=1600, wys=1200)
    zx, zy, zr = ZAGAJNIK
    render_podglad(os.path.join(PODGLADY_LOK, "natura_zagajnik.png"),
                    cel=(zx, zy, 2.0), odl=zr * 2.6, azymut=215.0, wysokosc_kat=32.0, ogniskowa=35, szer=1600, wys=900)

    print("GOTOWE. Sektory:", len(eksportowane))

if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        import traceback
        traceback.print_exc()
        raise
