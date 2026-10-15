# -*- coding: utf-8 -*-
"""Rendery z oczu gracza (1,7 m nad ziemią) na złożonej mapie Gliwice_calosc.blend.
Uruchom: blender -b Gliwice_calosc.blend --python gracz_widoki.py
Dla każdego celu szuka punktu na ulicy (nie na dachu, nie w ścianie) z widokiem na cel."""
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lowpoly import *
from gliwice_geo import LANDMARKI
import bpy
from mathutils import Vector

OUT = os.path.join(PODGLADY, "gracz")
os.makedirs(OUT, exist_ok=True)
sc = bpy.context.scene
dg = bpy.context.evaluated_depsgraph_get()


def rc(o, d, dist):
    hit, loc, nor, idx, ob, mx = sc.ray_cast(dg, Vector(o), Vector(d).normalized(), distance=dist)
    return (loc, ob) if hit else (None, None)


def ziemia(x, y):
    loc, ob = rc((x, y, 300), (0, 0, -1), 400)
    if loc is None:
        return None
    return loc.z if loc.z < 1.2 else None


def wolne_kierunki(oko):
    """Czy wokół oka jest choć 1,2 m wolnego (nie stoimy w ścianie)."""
    for a in range(0, 360, 45):
        d = (math.cos(math.radians(a)), math.sin(math.radians(a)), 0)
        loc, _ = rc(oko, d, 1.2)
        if loc is not None:
            return False
    return True


def szukaj(cx, cy, cz, odl_lista, az_lista=range(0, 360, 15)):
    najlepszy = None
    for odl in odl_lista:
        for az in az_lista:
            x = cx + math.cos(math.radians(az)) * odl
            y = cy + math.sin(math.radians(az)) * odl
            z = ziemia(x, y)
            if z is None:
                continue
            oko = Vector((x, y, z + 1.7))
            if not wolne_kierunki(oko):
                continue
            cel = Vector((cx, cy, cz))
            d = cel - oko
            loc, _ = rc(oko, d, d.length)
            wolne = 1.0 if loc is None else (loc - oko).length / d.length
            ocena = wolne - abs(odl - odl_lista[len(odl_lista) // 2]) / 400.0
            if najlepszy is None or ocena > najlepszy[0]:
                najlepszy = (ocena, oko, cel, wolne)
        if najlepszy and najlepszy[3] > 0.85:
            break
    return najlepszy


CELE = [
    ("g01_rynek_ratusz", "Ratusz", 14, [28, 36, 45, 60], False, 20),
    ("g02_rynek_noc", "Ratusz", 14, [28, 36, 45, 60], True, 20),
    ("g03_kosciol", "Kosciol", 22, [45, 60, 80, 100], False, 20),
    ("g04_hotel_ulica", "Hotel", 9, [35, 50, 70, 90], False, 22),
    ("g05_dworzec", "Dworzec", 8, [40, 60, 80, 110], False, 22),
    ("g06_radiostacja_wieza", "Radiostacja", 45, [70, 100, 140, 180], False, 18),
    ("g07_radiostacja_noc", "Radiostacja", 30, [70, 100, 140, 180], True, 18),
]

for nazwa, lm, cz, odl, noc, f in CELE:
    cx, cy, _ = LANDMARKI[lm]
    w = szukaj(cx, cy, cz, odl)
    if not w:
        print("GRACZ BRAK", nazwa)
        continue
    _, oko, cel, wolne = w
    print("GRACZ", nazwa, tuple(round(v, 1) for v in oko), "widocznosc", round(wolne, 2))
    d = oko - cel
    e = math.degrees(math.asin(d.z / d.length))
    a = math.degrees(math.atan2(d.x, -d.y))
    render_podglad(os.path.join(OUT, nazwa + ".png"), cel=tuple(cel), odl=d.length, azymut=a,
                   wysokosc_kat=e, ogniskowa=f, noc=noc)
print("GRACZ KONIEC")
