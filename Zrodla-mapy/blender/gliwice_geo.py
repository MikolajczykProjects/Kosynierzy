# -*- coding: utf-8 -*-
"""
gliwice_geo.py — WSPÓLNE współrzędne mapy Gliwic 1939 (z OSM). Używają tego WSZYSCY budujący warstwy miasta.
Czysty Python (działa w Blenderze i poza nim).

  from gliwice_geo import *
  D = wczytaj()                     # {'nodes':{id:(lat,lon,tags)}, 'ways':{id:(refs,tags)}, 'rels':{id:(members,tags)}}
  pts = punkty_drogi(D, way_id)     # [(x,y), ...] w metrach mapy (X wschód, Y północ), origin = Rynek
  x, y = P(lat, lon)

Kompresja: prawdziwa radiostacja jest 2,2 km na N od Rynku. Pas między Y_raw 1000 a 1900 ściskamy do 30%
(i przesuwamy na zachód), żeby gracz doszedł tam pieszo. Wynik: wieża ~ (1012, 1516). Mapa ~ X -420..1550, Y -420..1650.
"""
import json, math, os

OSM_JSON = "/tmp/unityref/osm/osm.json"
LAT0, LON0 = 50.29403, 18.66609          # Rynek (Ring)
M_LAT, M_LON = 111200.0, 71100.0

def surowe(lat, lon):
    return (lon - LON0) * M_LON, (lat - LAT0) * M_LAT

def _shear(n):
    if n <= 1000: return 0.0
    if n >= 1900: return 600.0
    return 600.0 * (n - 1000) / 900.0

def warp(e, n):
    if n <= 1000: n2 = n
    elif n <= 1900: n2 = 1000 + (n - 1000) * 0.3
    else: n2 = n - 630
    return e - _shear(n), n2

def P(lat, lon):
    return warp(*surowe(lat, lon))

# --- strefy (w SUROWYCH metrach Y od Rynku)
def strefa(lat, lon):
    """'miasto' (Y<=1000), 'korytarz' (1000..1900: tylko wille/ogrody, budynki OSM usunąć), 'radio' (>1900: pola, budynki tylko radiostacji)."""
    _, n = surowe(lat, lon)
    return "miasto" if n <= 1000 else ("korytarz" if n <= 1900 else "radio")

GRANICE = (-420.0, -420.0, 1550.0, 1650.0)   # xmin, ymin, xmax, ymax mapy

# --- landmarki robione RĘCZNIE (osobne moduły). Warstwa budynków POMIJA wszystko w tych kołach (x, y, promień m).
LANDMARKI = {
    "Ratusz":        P(50.29400, 18.66561) + (24.0,),     # stoi na środku Rynku
    "Fontanna_Neptun": P(50.29400, 18.66525) + (6.0,),
    "Kosciol":       P(50.29509, 18.66383) + (38.0,),
    "Hotel":         P(50.29628, 18.66929) + (48.0,),     # Haus Oberschlesien, ul. Zwycięstwa 21
    "Dworzec":       P(50.30071, 18.67619) + (70.0,),
    "Radiostacja":   P(50.31334, 18.68876) + (110.0,),
}
OS_ID_LANDMARKOW = {"158058194", "102301126", "279815461", "89954086"}   # ratusz, kościół, dworzec, wieża

def w_landmarku(x, y, zapas=0.0):
    for k, (lx, ly, r) in LANDMARKI.items():
        if (x - lx) ** 2 + (y - ly) ** 2 < (r + zapas) ** 2:
            return k
    return None

_D = None
def wczytaj():
    global _D
    if _D is None:
        with open(OSM_JSON, encoding="utf-8") as f:
            _D = json.load(f)
    return _D

def wsp_drogi(D, wid):
    """[(lat,lon)] węzłów drogi (pomija brakujące)."""
    refs = D["ways"][wid][0]
    out = []
    for r in refs:
        nd = D["nodes"].get(str(r))
        if nd: out.append((nd[0], nd[1]))
    return out

def punkty_drogi(D, wid):
    return [P(a, b) for a, b in wsp_drogi(D, wid)]

def zamknieta(refs):
    return len(refs) > 3 and refs[0] == refs[-1]

def w_granicach(x, y, zapas=0.0):
    x0, y0, x1, y1 = GRANICE
    return x0 - zapas <= x <= x1 + zapas and y0 - zapas <= y <= y1 + zapas

def pole_wiel(pts):
    a = 0.0
    for i in range(len(pts)):
        x0, y0 = pts[i]; x1, y1 = pts[(i + 1) % len(pts)]
        a += x0 * y1 - x1 * y0
    return a / 2

# --- filtr budynków „czy stał w 1939” (heurystyka)
def budynek_1939(tags, pts):
    b = tags.get("building")
    if not b or b in ("garage", "garages", "roof", "construction", "parking", "carport", "shed", "container", "kiosk", "service", "ruins"):
        return False
    try: lv = int(float(tags.get("building:levels", "0")))
    except Exception: lv = 0
    if lv >= 6: return False
    pole = abs(pole_wiel(pts))
    if b in ("retail", "commercial", "supermarket") and pole > 2000: return False
    if b == "apartments" and lv >= 4:
        xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
        if max(max(xs) - min(xs), max(ys) - min(ys)) > 45: return False
    if pole < 12: return False
    return True

# --- drogi: co wolno w 1939
DROGI_POMIN = {"footway", "cycleway", "steps", "path", "service", "track", "bridleway", "construction", "proposed", "corridor", "platform", "elevator"}
def droga_1939(tags):
    h = tags.get("highway")
    if not h or h in DROGI_POMIN: return False
    if h.startswith("motorway") or h.startswith("trunk"): return False
    if tags.get("tunnel") in ("yes", "building_passage"): return False
    return True

SZER_DROGI = {"primary": 14, "primary_link": 9, "secondary": 12, "secondary_link": 8, "tertiary": 10, "tertiary_link": 7,
              "residential": 8, "unclassified": 7, "living_street": 6, "pedestrian": 10}

# --- punkty dla klimatu: Tarnogórska (droga do radiostacji) — przebieg w mapie
def tarnogorska(D):
    """Lista dróg (way id) z name zawierającym 'Tarnogórska'."""
    return [k for k, (r, t) in D["ways"].items() if "Tarnogórska" in t.get("name", "") and t.get("highway")]

# --- miejsca fikcyjne / fabularne (współrzędne mapy)
ZAGAJNIK = (1135.0, 1395.0, 45.0)    # x, y, promień: gęsty zagajnik na SE od radiostacji (scena „Konserwa”)
STODOLA = (1400.0, 1350.0)           # stodoła kuriera „Sowa” na wschodzie, wśród pól
RYNEK_PLAC = (-34.0, -3.0, 55.0)     # środek i przybliżony promień placu Rynku
