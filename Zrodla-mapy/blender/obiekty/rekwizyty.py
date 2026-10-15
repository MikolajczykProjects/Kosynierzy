# -*- coding: utf-8 -*-
"""
rekwizyty.py — rekwizyty uliczne Gliwice 1939, hackaton.
Wzór: hotel.py. Dane osi ulic z ulice_geo.json (już w metrach mapy, origin = Rynek).
"""
import sys, os, json, math, random
sys.path.insert(0, '/Users/nataniel_bk/Documents/NBK/agent-dashboard/agent-workspace/otton/hackaton-gliwice/mapa/blender')
from lowpoly import *
from gliwice_geo import *

WYJSCIE = os.path.join(os.path.dirname(os.path.abspath(__file__)))
PODGLADY_LOK = os.path.join(WYJSCIE, "podglady")
DANE_LOK = os.path.join(WYJSCIE, "dane")
ULICE_JSON = "/Users/nataniel_bk/Documents/NBK/agent-dashboard/agent-workspace/otton/hackaton-gliwice/mapa/dane/ulice_geo.json"

RING = (-34.0, -3.0)
RING_R = 50.0


# ---------- siatki bazowe (budowane raz, potem współdzielone jako obj.data) ----------

def mesh_latarnia():
    m = Siatka("Latarnia_Gazowa")
    m.walec((0, 0, 1.6), 0.08, 3.2, "metal_ciemny", n=8)
    m.box((0, 0, 3.3), (0.35, 0.35, 0.12), "metal_ciemny")
    m.walec((0, 0, 3.6), 0.14, 0.4, "okno_swiatlo", n=8)
    m.stozek((0, 0, 3.85), 0.16, 0.2, "metal_ciemny", n=8)
    return m


def mesh_slup_ogloszeniowy():
    m = Siatka("Slup_Ogloszeniowy")
    m.walec((0, 0, 1.5), 0.6, 3.0, "zielen_okienna", n=16)
    m.stozek((0, 0, 3.15), 0.68, 0.3, "metal_ciemny", n=16)
    return m


def mesh_lawka():
    m = Siatka("Lawka")
    for znak in (-1, 1):
        m.box((znak * 0.7, 0, 0.22), (0.08, 0.5, 0.44), "metal_ciemny")
    m.box((0, 0, 0.45), (1.6, 0.45, 0.06), "drewno")
    m.box((0, -0.18, 0.72), (1.6, 0.06, 0.3), "drewno")
    return m


def mesh_kiosk():
    m = Siatka("Kiosk")
    m.box((0, 0, 1.1), (1.4, 1.2, 2.2), "drewno_ciemne")
    m.box((0, 0.05, 2.35), (1.6, 1.4, 0.15), "dachowka_ciemna")
    m.box((0, -0.61, 1.3), (1.0, 0.04, 1.4), "okno")
    return m


def mesh_hydrant():
    m = Siatka("Hydrant")
    m.walec((0, 0, 0.35), 0.1, 0.7, "czerwony", n=8)
    m.box((0, 0, 0.6), (0.3, 0.1, 0.1), "czerwony")
    m.walec((0, 0, 0.75), 0.06, 0.15, "metal_ciemny", n=8)
    return m


def mesh_kosz():
    m = Siatka("Kosz")
    m.walec((0, 0, 0.4), 0.22, 0.8, "metal_ciemny", n=10)
    m.walec((0, 0, 0.82), 0.24, 0.05, "metal_ciemny", n=10)
    return m


def mesh_skrzynka_pocztowa():
    m = Siatka("Skrzynka_Pocztowa")
    m.walec((0, 0, 0.55), 0.03, 1.1, "metal_ciemny", n=6)
    m.box((0, 0, 1.25), (0.4, 0.3, 0.5), "tynk_zolty")
    m.box((0, 0.16, 1.25), (0.3, 0.03, 0.1), "metal_ciemny")
    return m


def mesh_studzienka():
    m = Siatka("Studzienka_NOCOL")
    m.walec((0, 0, 0.02), 0.35, 0.04, "metal_ciemny", n=12)
    return m


def mesh_dorozka():
    m = Siatka("Dorozka")
    m.box((0, 0.3, 0.55), (1.1, 1.6, 0.9), "drewno_ciemne")
    m.box((0, -0.5, 0.95), (1.15, 0.1, 0.9), "drewno_ciemne")
    for znak in (-1, 1):
        for dy in (0.3, -0.5):
            m.walec((znak * 0.58, dy, 0.35), 0.35, 0.08, "drewno_ciemne", n=12)
    m.box((0, -1.3, 0.55), (0.1, 1.6, 0.08), "drewno_ciemne")
    m.box((0, -2.6, 0.5), (0.08, 0.08, 1.4), "drewno_ciemne")
    return m


def mesh_tramwaj():
    m = Siatka("Tramwaj")
    dl, szer, wys = 10.0, 2.2, 3.0
    m.box((0, 0, 1.2 + wys / 2 - 1.2), (dl, szer, wys - 1.2), "tynk_kremowy")
    m.box((0, 0, 0.5), (dl - 0.4, szer - 0.1, 1.0), "czerwony")
    n_okna = int(dl // 1.3)
    for i in range(n_okna):
        ox = -dl / 2 + 0.9 + i * 1.3
        m.box((ox, szer / 2 + 0.02, 1.9), (0.8, 0.04, 0.8), "okno_swiatlo")
        m.box((ox, -szer / 2 - 0.02, 1.9), (0.8, 0.04, 0.8), "okno_swiatlo")
    for ex in (-dl / 2 + 0.6, dl / 2 - 0.6):
        for ey in (szer / 2, -szer / 2):
            m.box((ex, ey * 0.55, 1.9), (0.5, 0.04, 0.9), "okno_swiatlo")
    for kx in (-dl / 2 + 1.2, -1.2, 1.2, dl / 2 - 1.2):
        for ky in (-szer / 2 + 0.15, szer / 2 - 0.15):
            m.walec((kx, ky, 0.35), 0.35, 0.25, "metal_ciemny", n=10)
    m.box((0, 0, wys + 0.1), (0.1, 0.5, 0.05), "metal_ciemny")
    m.box((0, 0.1, wys + 0.5), (0.08, 0.08, 0.9), "metal_ciemny")
    m.box((0, -0.1, wys + 0.45), (0.08, 0.08, 0.8), "metal_ciemny")
    m.box((0, 0, wys + 0.9), (0.08, 0.6, 0.06), "metal_ciemny")
    return m


# mapowanie: typ -> (budowniczy siatki, wysokosc osadzenia pivotu juz u podstawy)
BUDOWNICZE = {
    "Latarnia_Gazowa": mesh_latarnia,
    "Slup_Ogloszeniowy": mesh_slup_ogloszeniowy,
    "Lawka": mesh_lawka,
    "Kiosk": mesh_kiosk,
    "Hydrant": mesh_hydrant,
    "Kosz": mesh_kosz,
    "Skrzynka_Pocztowa": mesh_skrzynka_pocztowa,
    "Studzienka_NOCOL": mesh_studzienka,
    "Dorozka": mesh_dorozka,
    "Tramwaj": mesh_tramwaj,
}


def zbuduj_bazowe_mesh_data():
    """Budowanie każdej siatki raz, zwraca dict typ -> bpy mesh data (blender)."""
    import bpy
    dane = {}
    for typ, fn in BUDOWNICZE.items():
        m = fn()
        ob = m.obiekt((0, 0, 0), kat=0.0)
        dane[typ] = ob.data
        # usuwamy pomocniczy obiekt, mesh-data zostaje
        bpy.data.objects.remove(ob, do_unlink=True)
    return dane


_LICZNIK = {}


def postaw(typ, mesh_data_dict, x, y, yaw=0.0):
    import bpy
    _LICZNIK[typ] = _LICZNIK.get(typ, 0) + 1
    nazwa = "%s_%04d" % (typ, _LICZNIK[typ])
    ob = bpy.data.objects.new(nazwa, mesh_data_dict[typ])
    bpy.context.scene.collection.objects.link(ob)
    ob.location = (x, y, 0.0)
    ob.rotation_euler = (0.0, 0.0, math.radians(yaw))
    return ob


# ---------- geometria dróg / kolizje ----------

def dist_seg(px, py, ax, ay, bx, by):
    dx, dy = bx - ax, by - ay
    l2 = dx * dx + dy * dy
    if l2 < 1e-9:
        return math.hypot(px - ax, py - ay), 0.0
    t = max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / l2))
    cx, cy = ax + t * dx, ay + t * dy
    return math.hypot(px - cx, py - cy), t


def punkty_wzdluz(pts, krok, odsuniecie=0.0):
    """Generuje punkty (x,y,yaw_wzdluz,strona) co `krok` m wzdłuż polilinii pts,
    yaw = kierunek drogi (dla rekwizytu patrzącego wzdłuż ulicy nieistotne, liczymy normalną)."""
    wyniki = []
    dl_calk = 0.0
    segs = []
    for i in range(len(pts) - 1):
        ax, ay = pts[i]
        bx, by = pts[i + 1]
        dl = math.hypot(bx - ax, by - ay)
        segs.append((ax, ay, bx, by, dl))
        dl_calk += dl
    if dl_calk < 1e-6:
        return wyniki
    d = 0.0
    nastepny = 0.0
    for ax, ay, bx, by, dl in segs:
        if dl < 1e-9:
            continue
        ux, uy = (bx - ax) / dl, (by - ay) / dl
        nx, ny = -uy, ux  # normalna
        while nastepny <= d + dl:
            t = nastepny - d
            px, py = ax + ux * t, ay + uy * t
            wyniki.append((px, py, nx, ny))
            nastepny += krok
        d += dl
    return wyniki


def najblizsza_droga(x, y, drogi):
    best = 1e18
    for pts in drogi:
        for i in range(len(pts) - 1):
            dd, _ = dist_seg(x, y, pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1])
            if dd < best:
                best = dd
    return best


def bezpieczny(x, y, drogi_bbox=None):
    if not w_granicach(x, y):
        return False
    if w_landmarku(x, y, zapas=2.0) not in (None, "Radiostacja"):
        return False
    return True


def main():
    random.seed(7)
    D = json.load(open(ULICE_JSON, encoding="utf-8"))
    drogi_raw = D["drogi"]

    # drogi w strefie miasta (y<1000) wg Y średniego punktów
    drogi_miasto = []
    drogi_glowne = []  # primary/secondary do szpalerów latarni
    for d in drogi_raw:
        pts = d["pts"]
        if not pts:
            continue
        ys = [p[1] for p in pts]
        if min(ys) > 1000:
            continue
        drogi_miasto.append(pts)
        if d.get("h") in ("primary", "secondary"):
            drogi_glowne.append(pts)

    mesh_data = zbuduj_bazowe_mesh_data()

    obiekty_latarnie = []  # (x,y) do świateł
    n_latarnia = n_litfas = n_lawka = n_kiosk = n_skrzynka = n_tramwaj = n_dorozka = 0

    # --- latarnie wzdluz ulic miasta, co 30 m, na przemian po obu stronach, 6 m od osi ---
    strona = 1
    for pts in drogi_miasto:
        for (px, py, nx, ny) in punkty_wzdluz(pts, 30.0):
            strona = -strona
            x = px + nx * 6.0 * strona
            y = py + ny * 6.0 * strona
            if not bezpieczny(x, y):
                continue
            postaw("Latarnia_Gazowa", mesh_data, x, y, yaw=random.uniform(0, 360))
            obiekty_latarnie.append((x, y))
            n_latarnia += 1

    # --- litfasy, lawki, kioski, skrzynki rzadziej: co 150 m, losowo z ziarnem ---
    rodzaje_rzadkie = ["Slup_Ogloszeniowy", "Lawka", "Kiosk", "Skrzynka_Pocztowa"]
    idx_r = 0
    for pts in drogi_miasto:
        for (px, py, nx, ny) in punkty_wzdluz(pts, 150.0):
            typ = rodzaje_rzadkie[idx_r % len(rodzaje_rzadkie)]
            idx_r += 1
            strona_r = 1 if idx_r % 2 == 0 else -1
            x = px + nx * 4.5 * strona_r
            y = py + ny * 4.5 * strona_r
            if not bezpieczny(x, y):
                continue
            yaw = math.degrees(math.atan2(nx, ny)) if strona_r > 0 else math.degrees(math.atan2(-nx, -ny))
            postaw(typ, mesh_data, x, y, yaw=yaw)
            if typ == "Slup_Ogloszeniowy":
                n_litfas += 1
            elif typ == "Lawka":
                n_lawka += 1
            elif typ == "Kiosk":
                n_kiosk += 1
            elif typ == "Skrzynka_Pocztowa":
                n_skrzynka += 1

    # --- Ring: 6 latarni + 6 lawek w kole wokol (-34,-3), promień do 50 m ---
    rx, ry = RING
    for i in range(6):
        ang = math.radians(i * 60)
        x = rx + math.cos(ang) * (RING_R - 5)
        y = ry + math.sin(ang) * (RING_R - 5)
        if bezpieczny(x, y):
            postaw("Latarnia_Gazowa", mesh_data, x, y, yaw=math.degrees(ang))
            obiekty_latarnie.append((x, y))
            n_latarnia += 1
    for i in range(6):
        ang = math.radians(i * 60 + 30)
        x = rx + math.cos(ang) * (RING_R - 12)
        y = ry + math.sin(ang) * (RING_R - 12)
        if bezpieczny(x, y):
            postaw("Lawka", mesh_data, x, y, yaw=math.degrees(ang) + 90)
            n_lawka += 1

    # --- 2 tramwaje na najdluzszej primary w centrum ---
    primary_centrum = [p for p in drogi_raw if p.get("h") == "primary" and p["pts"] and min(y for _, y in p["pts"]) < 1000]
    if primary_centrum:
        najdluzsza = max(primary_centrum, key=lambda d: sum(
            math.hypot(d["pts"][i + 1][0] - d["pts"][i][0], d["pts"][i + 1][1] - d["pts"][i][1])
            for i in range(len(d["pts"]) - 1)))
        pts = najdluzsza["pts"]
        pozycje = punkty_wzdluz(pts, 1.0)
        idxs = [len(pozycje) // 3, 2 * len(pozycje) // 3] if pozycje else []
        for i in idxs:
            px, py, nx, ny = pozycje[i]
            ux, uy = -ny, nx
            yaw = math.degrees(math.atan2(ux, uy))
            if bezpieczny(px, py):
                postaw("Tramwaj", mesh_data, px, py, yaw=yaw)
                n_tramwaj += 1

    # --- 2 dorozki przy Ringu ---
    for i in range(2):
        ang = math.radians(i * 180 + 15)
        x = rx + math.cos(ang) * (RING_R - 20)
        y = ry + math.sin(ang) * (RING_R - 20)
        if bezpieczny(x, y):
            postaw("Dorozka", mesh_data, x, y, yaw=math.degrees(ang))
            n_dorozka += 1

    # kilka hydrantów, koszy, studzienek dla kompletu (po mieście, rzadko)
    n_hydrant = n_kosz = n_studzienka = 0
    idx_h = 0
    for pts in drogi_miasto:
        for (px, py, nx, ny) in punkty_wzdluz(pts, 220.0):
            idx_h += 1
            typ = ["Hydrant", "Kosz", "Studzienka_NOCOL"][idx_h % 3]
            strona_h = 1 if idx_h % 2 == 0 else -1
            x = px + nx * 3.0 * strona_h
            y = py + ny * 3.0 * strona_h
            if not bezpieczny(x, y):
                continue
            postaw(typ, mesh_data, x, y, yaw=random.uniform(0, 360))
            if typ == "Hydrant":
                n_hydrant += 1
            elif typ == "Kosz":
                n_kosz += 1
            else:
                n_studzienka += 1

    # --- światła przy każdej latarni ---
    for (x, y) in obiekty_latarnie:
        swiatlo((x, y, 3.8), kolor="#ffcf8a", zasieg=9, moc=1.5)

    # --- punkty gry opcjonalne (brief nie wymaga konkretnych, ale kompas wymaga min.?) ---
    # (brak wymaganych punktów gry w tym zadaniu — pomijamy)

    import bpy
    wszystkie = [ob for ob in bpy.data.objects if ob.type == "MESH"]

    # --- eksport sektorami 500 m ---
    x0, y0, x1, y1 = GRANICE
    sek_i = 0
    liczba_plikow = 0
    xs = list(range(int(x0 // 500), int(x1 // 500) + 1))
    ys = list(range(int(y0 // 500), int(y1 // 500) + 1))
    for i in xs:
        for j in ys:
            sx0, sy0 = i * 500, j * 500
            sx1, sy1 = sx0 + 500, sy0 + 500
            grupa = [ob for ob in wszystkie if sx0 <= ob.location.x < sx1 and sy0 <= ob.location.y < sy1]
            if not grupa:
                continue
            eksport_fbx(grupa, os.path.join(WYJSCIE, "Rekwizyty_%d_%d.fbx" % (i, j)))
            liczba_plikow += 1

    zapisz_meta(os.path.join(DANE_LOK, "meta_rekwizyty.json"))

    ziemia_podgladu(90)
    # zblizenie na wszystkie typy obok siebie - osobna mini-scena nie jest tworzona,
    # wykorzystujemy kadr na skupisko przy Ringu, gdzie wystepuje najwiecej typow na raz
    render_podglad(os.path.join(PODGLADY_LOK, "rekwizyty_1.png"), cel=(rx, ry, 1.0), odl=55, azymut=35, wysokosc_kat=35)
    render_podglad(os.path.join(PODGLADY_LOK, "rekwizyty_2.png"), cel=(rx, ry, 0.0), odl=90, azymut=0, wysokosc_kat=80)

    print("LICZBY:", {
        "latarnia": n_latarnia, "litfas": n_litfas, "lawka": n_lawka, "kiosk": n_kiosk,
        "skrzynka": n_skrzynka, "tramwaj": n_tramwaj, "dorozka": n_dorozka,
        "hydrant": n_hydrant, "kosz": n_kosz, "studzienka": n_studzienka,
        "pliki_fbx": liczba_plikow,
    })


if __name__ == "__main__":
    wyczysc_scene()
    main()
