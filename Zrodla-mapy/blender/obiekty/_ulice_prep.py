# -*- coding: utf-8 -*-
"""Przygotowanie warstwy ulic w venv z shapely -> dane/ulice_geo.json (czyta warstwa_ulice.py w Blenderze).
Uruchom: /tmp/unityref/venv/bin/python obiekty/_ulice_prep.py"""
import sys, os, json, math
TU = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.dirname(TU))
from gliwice_geo import *
from shapely.geometry import LineString, Polygon, Point, MultiPolygon, box, mapping
from shapely.ops import unary_union

D = wczytaj()
X0, Y0, X1, Y1 = GRANICE
ZAP = 60.0
RAMA = box(X0 - ZAP, Y0 - ZAP, X1 + ZAP, Y1 + ZAP)
GR = box(X0, Y0, X1, Y1)          # drogi, tory i woda przycięte do granic mapy
Y_RADIO = 1270.0          # mapa: surowe 1900 -> 1000 + 900*0.3
Y_MIASTO = 1000.0
SEKTOR = 500.0

def linia(wid):
    pts = punkty_drogi(D, wid)
    if len(pts) < 2: return None
    l = LineString(pts)
    return l if l.length > 0.5 else None

# ---------------- woda
woda = []
for k, (r, t) in D["ways"].items():
    if t.get("natural") == "water" and t.get("water") in ("river", "canal"):
        pts = punkty_drogi(D, k)
        if len(pts) > 3:
            p = Polygon(pts).buffer(0)
            woda.append(p)
rzeka_l = [linia(k) for k, (r, t) in D["ways"].items() if t.get("waterway") in ("river", "canal")]
rzeka_l = [l for l in rzeka_l if l]
W = unary_union(woda + [l.buffer(7.0) for l in rzeka_l]).intersection(GR)
W = W.buffer(1.0).buffer(-1.0).simplify(0.6)
KORYTO = W.buffer(2.0, join_style=2).intersection(GR)       # z murami nabrzeża (1,2 m)

# ---------------- drogi
drogi = []
tarn = set(tarnogorska(D))
for k, (r, t) in D["ways"].items():
    if not droga_1939(t): continue
    h = t["highway"]
    if h not in SZER_DROGI: continue
    if t.get("area") == "yes": continue
    l = linia(k)
    if not l or not l.intersects(GR): continue
    l = l.intersection(GR)
    if l.is_empty: continue
    if l.geom_type == "MultiLineString":
        l = max(l.geoms, key=lambda g: g.length)
    if l.geom_type != "LineString" or l.length < 0.5: continue
    # korytarz (1939: pola i ogrody): tylko Tarnogórska i drogi główne
    if k not in tarn and not h.startswith(("primary", "secondary", "tertiary")):
        cy = l.centroid.y
        if Y_MIASTO < cy < Y_RADIO:
            continue
    drogi.append((k, h, l, SZER_DROGI[h], k in tarn, t.get("name", "")))
print("drogi", len(drogi))

# Rynek: obrys z dróg pedestrian „Rynek” + ich otoczka
rp = [Point(p) for (k, h, l, s, tn, nm) in drogi if nm == "Rynek" for p in getattr(l, "coords", [])]
RYNEK = unary_union(rp).convex_hull.buffer(5.0, join_style=2) if rp else Point(RYNEK_PLAC[:2]).buffer(RYNEK_PLAC[2])
JEZ = unary_union([l.buffer(s / 2.0, cap_style=2, join_style=2) for (k, h, l, s, tn, nm) in drogi]).buffer(0.5).buffer(-0.5)
TARN = unary_union([l.buffer(s / 2.0 + 0.3) for (k, h, l, s, tn, nm) in drogi if tn])
OS_PELNE = unary_union([l.buffer(s / 2.0 + 3.0, cap_style=2, join_style=2) for (k, h, l, s, tn, nm) in drogi]).buffer(0.5).buffer(-0.5)
JEZ = JEZ.difference(RYNEK).intersection(GR)
OS_PELNE = OS_PELNE.intersection(GR)
JEZ_PELNA = JEZ
JEZ = JEZ.difference(KORYTO)
OS_PELNE = OS_PELNE.difference(KORYTO)

ZONA_BRUK = Point(0, 0).buffer(900, 48).union(TARN)
ZONA_RADIO = box(X0 - ZAP, Y_RADIO, X1 + ZAP, Y1 + ZAP)
ZONA_MIASTO = box(X0 - ZAP, Y0 - ZAP, X1 + ZAP, Y_MIASTO)
j_bruk = JEZ.intersection(ZONA_BRUK)
reszta = JEZ.difference(ZONA_BRUK)
j_polna = reszta.intersection(ZONA_RADIO)
reszta = reszta.difference(ZONA_RADIO)
# główne (primary/secondary) asfalt, reszta bruk_jasny
GLOWNE = unary_union([l.buffer(s / 2.0 + 0.3) for (k, h, l, s, tn, nm) in drogi if h.startswith(("primary", "secondary"))])
j_asfalt = reszta.intersection(GLOWNE)
j_bjasny = reszta.difference(GLOWNE)
# chodniki: tylko poza strefą radio, bez placu Rynku
CHOD = OS_PELNE.difference(JEZ_PELNA).difference(RYNEK.buffer(-0.01)).difference(ZONA_RADIO)
CHOD = CHOD.difference(Point(0, 0).buffer(1)).buffer(0)
CHOD = CHOD.difference(RYNEK)
KRAW = CHOD.intersection(JEZ_PELNA.buffer(0.3, join_style=2))
CHOD = CHOD.difference(KRAW)

# ---------------- tory
tory, tram = [], []
for k, (r, t) in D["ways"].items():
    rw = t.get("railway")
    if rw == "rail" and t.get("tunnel") not in ("yes",) and t.get("service") not in ("spur_x",):
        l = linia(k)
        if l and l.intersects(GR): tory.append(l.intersection(GR))
    elif rw in ("tram", "razed"):
        l = linia(k)
        if l and l.intersects(GR): tram.append(l.intersection(GR))
def linie(g):
    if g.is_empty: return []
    if g.geom_type == "LineString": return [list(g.coords)]
    if hasattr(g, "geoms"): return [c for x in g.geoms for c in linie(x)]
    return []
tory_l = [c for l in tory for c in linie(l)]
tram_l = [c for l in tram for c in linie(l)]
PODS = unary_union([LineString(c).buffer(2.0, cap_style=2) for c in tory_l if len(c) > 1]).difference(KORYTO)
PODS = PODS.difference(JEZ.buffer(0.2)).intersection(GR)

# ---------------- mosty: drogi i tory przez wodę
mosty = []
for (k, h, l, s, tn, nm) in drogi:
    if not l.intersects(W): continue
    for seg in linie(l.intersection(KORYTO.buffer(2.5))):
        if len(seg) < 2: continue
        L = LineString(seg)
        if L.length < 4: continue
        mosty.append({"pts": [list(seg[0]), list(seg[-1])], "szer": s + 6.0, "nazwa": nm or k})
print("mosty", len(mosty))

# ---------------- teren
TEREN = RAMA.difference(KORYTO)
# beż tylko w pasie 25 m wokół ulic i budynków, reszta miasta trawa
bud = []
for k, (r, t) in D["ways"].items():
    if not t.get("building"): continue
    pts = punkty_drogi(D, k)
    if len(pts) < 4 or not budynek_1939(t, pts): continue
    try: pb = Polygon(pts)
    except Exception: continue
    if pb.centroid.y > Y_MIASTO or not pb.intersects(GR): continue
    bud.append(pb.buffer(0).simplify(1.0))
print("budynki", len(bud))
BEZ = unary_union([OS_PELNE.simplify(1.0)] + bud).buffer(25.0, 6).buffer(-6.0, 6).simplify(1.5)
BEZ = BEZ.union(RYNEK.buffer(25.0)).intersection(GR)
T_MIASTO_ALL = TEREN.intersection(ZONA_MIASTO)
T_MIASTO = T_MIASTO_ALL.intersection(BEZ)
T_MIASTO_TRAWA = T_MIASTO_ALL.difference(BEZ)
T_KORYT = TEREN.intersection(box(X0 - ZAP, Y_MIASTO, X1 + ZAP, Y_RADIO))
T_RADIO = TEREN.intersection(ZONA_RADIO)
# pola w pasach (wschód-zachód), miedze 2 m trawy; wokół radiostacji, zagajnika, stodoły trawa
rx, ry, rr = LANDMARKI["Radiostacja"]
WOLNE = unary_union([Point(rx, ry).buffer(rr + 40), Point(ZAGAJNIK[:2]).buffer(ZAGAJNIK[2] + 15),
                     Point(STODOLA).buffer(35), OS_PELNE.buffer(2.0)])
upr = ["pole_zaorane", "zyto_scierniska", "pole_zielone", "zyto_scierniska", "pole_zaorane", "pole_zielone"]
pola = {u: [] for u in set(upr)}
y = Y_RADIO + 10
i = 0
while y < Y1 + ZAP:
    h = 42 + (i * 17) % 23
    x = X0 - ZAP
    j = 0
    while x < X1 + ZAP:
        w = 150 + ((i * 31 + j * 53) % 120)
        pas = box(x + 1.2, y + 1.2, x + w - 1.2, y + h - 1.2).intersection(T_RADIO).difference(WOLNE)
        if not pas.is_empty and pas.area > 50:
            pola[upr[(i * 2 + j) % len(upr)]].append(pas)
        x += w; j += 1
    y += h; i += 1
POLA = {u: unary_union(v) for u, v in pola.items()}
T_RADIO_TRAWA = T_RADIO.difference(unary_union(list(POLA.values())))

# ---------------- zapis, z podziałem na sektory
def pol(g):
    if g.is_empty: return []
    if g.geom_type == "Polygon":
        if g.area < 0.5: return []
        return [[list(map(lambda c: [round(c[0], 2), round(c[1], 2)], g.exterior.coords))[:-1],
                 [list(map(lambda c: [round(c[0], 2), round(c[1], 2)], r.coords))[:-1] for r in g.interiors]]]
    if hasattr(g, "geoms"): return [p for x in g.geoms for p in pol(x)]
    return []

def sektory(g):
    out = {}
    if g.is_empty: return out
    minx, miny, maxx, maxy = g.bounds
    for i in range(int(math.floor((minx - X0) / SEKTOR)), int(math.floor((maxx - X0) / SEKTOR)) + 1):
        for j in range(int(math.floor((miny - Y0) / SEKTOR)), int(math.floor((maxy - Y0) / SEKTOR)) + 1):
            b = box(X0 + i * SEKTOR, Y0 + j * SEKTOR, X0 + (i + 1) * SEKTOR, Y0 + (j + 1) * SEKTOR)
            p = pol(g.intersection(b).buffer(0))
            if p: out["%d_%d" % (i, j)] = p
    return out

def lin_sek(lst):
    out = {}
    for c in lst:
        for a, b in zip(c[:-1], c[1:]):
            m = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
            key = "%d_%d" % (int(math.floor((m[0] - X0) / SEKTOR)), int(math.floor((m[1] - Y0) / SEKTOR)))
            out.setdefault(key, []).append([list(a), list(b)])
    return out

wyn = {
    "jezdnia": {"bruk": sektory(j_bruk), "asfalt": sektory(j_asfalt), "bruk_jasny": sektory(j_bjasny), "polna_droga": sektory(j_polna)},
    "chodnik": sektory(CHOD), "kraweznik": sektory(KRAW),
    "rynek": pol(RYNEK),
    "podsypka": sektory(PODS), "tory": lin_sek(tory_l), "tram": lin_sek(tram_l),
    "woda": pol(W), "brzeg_miasto": pol(KORYTO.difference(W).intersection(ZONA_MIASTO)), "brzeg_poza": pol(KORYTO.difference(W).difference(ZONA_MIASTO)), "koryto": pol(KORYTO), "w_miescie": pol(KORYTO.intersection(ZONA_MIASTO)),
    "teren_miasto": pol(T_MIASTO), "teren_trawa": pol(unary_union([T_KORYT, T_RADIO_TRAWA, T_MIASTO_TRAWA])),
    "pola": {u: pol(g) for u, g in POLA.items()},
    "mosty": mosty,
    "drogi": [{"id": k, "h": h, "nazwa": nm, "tarn": tn, "pts": [list(c) for c in l.coords]} for (k, h, l, s, tn, nm) in drogi if l.geom_type == "LineString"],
}
os.makedirs(DANE_DIR := os.path.join(os.path.dirname(os.path.dirname(TU)), "dane"), exist_ok=True)
with open(os.path.join(DANE_DIR, "ulice_geo.json"), "w") as f:
    json.dump(wyn, f)
print("ok", {k: (len(v) if isinstance(v, (list, dict)) else 0) for k, v in wyn.items()})
