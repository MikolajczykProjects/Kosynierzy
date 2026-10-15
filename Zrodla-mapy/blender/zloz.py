# -*- coding: utf-8 -*-
"""zloz.py — składa całą mapę z FBX (warstwy + landmarki), zapisuje manifest.json dla Unity i robi rendery.
Uruchom: blender -b --factory-startup -P zloz.py -- [szybko]
"""
import sys, os, json, math, glob
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *
from gliwice_geo import *
import bpy

ARGV = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
MANIFEST = os.path.join(KORZEN, "unity", "Assets", "_Mapa", "Dane", "manifest.json")
OUT = os.path.join(PODGLADY, "mapa")


def kat_do_ulicy(x, y, filtr, max_d=150.0):
    """Kąt (CCW, deg) obracający lokalne +Y w stronę najbliższego punktu dróg spełniających filtr."""
    D = wczytaj()
    best = None
    for wid, (refs, t) in D["ways"].items():
        if not filtr(t):
            continue
        pts = punkty_drogi(D, wid)
        for i in range(len(pts) - 1):
            ax, ay = pts[i]; bx, by = pts[i + 1]
            vx, vy = bx - ax, by - ay
            L = vx * vx + vy * vy
            if L == 0: continue
            u = max(0, min(1, ((x - ax) * vx + (y - ay) * vy) / L))
            px, py = ax + u * vx, ay + u * vy
            d = math.hypot(px - x, py - y)
            if d < max_d and (best is None or d < best[0]):
                best = (d, px - x, py - y, vx, vy)
    if not best:
        return None
    return best


def kat_front_do(dx, dy):
    return math.degrees(math.atan2(-dx, dy))


# ------------------------------------------------------------- rozmieszczenie landmarków
LM = {}
rx, ry, _ = LANDMARKI["Ratusz"]
LM["Rynek"] = dict(poz=(rx, ry, 0.0), kat=0.0, meta="meta_rynek.json")
kx, ky, _ = LANDMARKI["Kosciol"]
LM["Kosciol"] = dict(poz=(kx, ky, 0.0), kat=-90.0, meta="meta_kosciol.json")   # wieża (−Y) na zachód
hx, hy, _ = LANDMARKI["Hotel"]
b = kat_do_ulicy(hx, hy, lambda t: "Zwycięstwa" in t.get("name", "") and t.get("highway"))
k_hotel = kat_front_do(b[1], b[2]) if b else 0.0
LM["Hotel"] = dict(poz=(hx, hy, 0.0), kat=k_hotel, meta="meta_hotel.json")
dx_, dy_, _ = LANDMARKI["Dworzec"]
b = kat_do_ulicy(dx_, dy_, lambda t: t.get("railway") == "rail", 250)
k_dw = kat_front_do(-b[1], -b[2]) if b else 180.0     # tory za budynkiem (−Y)
LM["Dworzec"] = dict(poz=(dx_, dy_, 0.0), kat=k_dw, meta="meta_dworzec.json")
wx, wy, _ = LANDMARKI["Radiostacja"]
LM["Radiostacja"] = dict(poz=(wx, wy, 0.0), kat=0.0, meta="meta_radiostacja.json")
WNETRZA = {"Wnetrze_Hotel": (4000, 0), "Wnetrze_Radio": (4200, 0), "Wnetrze_Ksiegarnia": (4400, 0), "Wnetrze_Fotograf": (4600, 0)}
for n, (x, y) in WNETRZA.items():
    LM[n] = dict(poz=(x, y, 0.0), kat=0.0, meta="meta_" + n.lower() + ".json")
print("ZLOZ katy: hotel %.1f, dworzec %.1f" % (k_hotel, k_dw))

# ------------------------------------------------------------- manifest
def tr(p, poz, kat):
    c, s = math.cos(math.radians(kat)), math.sin(math.radians(kat))
    x, y, z = p
    return [round(poz[0] + x * c - y * s, 3), round(poz[1] + x * s + y * c, 3), round(poz[2] + z, 3)]

modele = sorted(os.path.basename(f)[:-4] for f in glob.glob(os.path.join(UNITY_MODELE, "*.fbx")))
man = {"warstwy": [], "obiekty": [], "punkty": [], "swiatla": []}
GRUPA = {"Teren": "Teren", "Ulice": "Ulice", "Budynki": "Budynki", "Natura": "Natura", "Rekwizyty": "Rekwizyty"}
for m in modele:
    g = m.split("_")[0]
    if m in LM:
        continue
    if g in GRUPA:
        man["warstwy"].append({"fbx": m, "grupa": GRUPA[g]})
for n, d in LM.items():
    man["obiekty"].append({"fbx": n, "grupa": "Wnetrza" if n.startswith("Wnetrze") else "Budynki",
                           "poz": list(d["poz"]), "kat": round(d["kat"], 2)})
widziane = set()
def dodaj_meta(plik, poz=(0, 0, 0), kat=0.0):
    f = os.path.join(DANE, plik)
    if not os.path.exists(f):
        print("ZLOZ brak meta", plik); return
    M = json.load(open(f, encoding="utf-8"))
    for p in M.get("punkty", []):
        n = p["n"]
        if n in widziane:
            print("ZLOZ duplikat punktu", n, "w", plik)
            n = n + "_" + plik[5:-5]
        widziane.add(n)
        man["punkty"].append({"n": n, "p": tr(p["p"], poz, kat), "yaw": round((p.get("yaw", 0) - kat) % 360, 2)})
    for s in M.get("swiatla", []):
        e = dict(s); e["p"] = tr(s["p"], poz, kat)
        e["noc"] = True
        man["swiatla"].append(e)
for plik in ("meta_ulice.json", "meta_budynki.json", "meta_natura.json", "meta_rekwizyty.json"):
    dodaj_meta(plik)
for n, d in LM.items():
    dodaj_meta(d["meta"], d["poz"], d["kat"])
os.makedirs(os.path.dirname(MANIFEST), exist_ok=True)
json.dump(man, open(MANIFEST, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print("ZLOZ manifest: %d warstw, %d obiektów, %d punktów, %d świateł" % (len(man["warstwy"]), len(man["obiekty"]), len(man["punkty"]), len(man["swiatla"])))

# ------------------------------------------------------------- import do sceny (podgląd)
wyczysc_scene()

def importuj(nazwa):
    przed = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=os.path.join(UNITY_MODELE, nazwa + ".fbx"), axis_forward='-Z', axis_up='Y')
    nowe = [o for o in bpy.data.objects if o not in przed]
    for o in list(nowe):
        if o.name.startswith("Kompas"):
            bpy.data.objects.remove(o, do_unlink=True); nowe.remove(o)
        elif o.name.endswith("_COL"):
            o.hide_render = True
    return nowe

for w in man["warstwy"]:
    importuj(w["fbx"])
for n, d in LM.items():
    if n.startswith("Wnetrze"):
        continue
    obs = importuj(n)
    root = bpy.data.objects.new("LM_" + n, None)
    bpy.context.scene.collection.objects.link(root)
    root.location = d["poz"]; root.rotation_euler = (0, 0, math.radians(d["kat"]))
    for o in obs:
        if o.parent is None:
            o.parent = root
print("ZLOZ obiektów w scenie:", len(bpy.data.objects))

# kolory materiałów z FBX bywają szare -> podmień na paletę po nazwie
for m in bpy.data.materials:
    base = m.name.split(".")[0]
    if base.startswith("M_") and base[2:] in PALETA:
        nm = mat(base[2:])
        for o in bpy.data.objects:
            if o.type == "MESH":
                for sl in o.material_slots:
                    if sl.material == m:
                        sl.material = nm

k_hotel_az = math.degrees(math.atan2(-math.sin(math.radians(k_hotel)), -math.cos(math.radians(k_hotel))))
os.makedirs(OUT, exist_ok=True)
UJECIA = [
    ("01_lotniczy_centrum", (150, 300, 0), 900, 200, 50),
    ("02_rynek", (rx, ry, 0), 130, 200, 30),
    ("03_rynek_gracz", (rx, ry + 5, 8), 30, 160, 10),
    ("04_hotel", (hx, hy, 0), 150, k_hotel_az, 25),
    ("05_kosciol", (kx, ky, 15), 110, 250, 18),
    ("06_dworzec", (dx_, dy_, 0), 180, 210, 30),
    ("07_radiostacja", (wx, wy, 40), 320, 210, 18),
    ("09_hotel_gracz", (hx, hy, 8), 85, k_hotel_az + 20, 8),
    ("10_rynek_noc", (rx, ry, 0), 120, 160, 30),
    ("08_cala_mapa", (565, 615, 0), 2600, 0, 60),
]
if "szybko" in ARGV:
    UJECIA = UJECIA[:3]
for nazwa, cel, odl, az, el in UJECIA:
    render_podglad(os.path.join(OUT, nazwa + ".png"), cel=cel, odl=odl, azymut=az, wysokosc_kat=el, ogniskowa=35 if odl < 1000 else 28, noc=nazwa.endswith("noc"))
    print("ZLOZ PODGLAD", nazwa)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(KORZEN, "blender", "Gliwice_calosc.blend"))
print("ZLOZ KONIEC")
