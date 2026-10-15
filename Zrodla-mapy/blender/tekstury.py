# -*- coding: utf-8 -*-
"""
tekstury.py — generuje proceduralne, bezszwowe tekstury ALBEDO (prawie białe, 0.72-1.0)
+ NORMAL MAP (z wysokości, Sobel) dla mapy Gliwic 1939.
Uruchamiane zwykłym Pythonem (numpy + Pillow), NIE w Blenderze.
    /tmp/unityref/venv/bin/python blender/tekstury.py

Zapis: unity/Assets/_Mapa/Tekstury/T_<nazwa>_A.png / T_<nazwa>_N.png
Mapowanie kolorów PALETA -> tekstura: dane/tekstury.json
"""
import os, json
import numpy as np
from PIL import Image

random = np.random.RandomState

TU = os.path.dirname(os.path.abspath(__file__))
KORZEN = os.path.dirname(TU)
DANE = os.path.join(KORZEN, "dane")
TEX_OUT = os.path.join(KORZEN, "unity", "Assets", "_Mapa", "Tekstury")
os.makedirs(TEX_OUT, exist_ok=True)

N = 512


def _wrap_noise(seed, oktawy=4, skala=4.0):
    """Szum Perlinowy bezszwowy przez sumę sinusów/wartości na okrągłej siatce (proste, bez zależności)."""
    rng = random(seed)
    img = np.zeros((N, N))
    for o in range(oktawy):
        rez = int(skala * (2 ** o))
        rez = max(rez, 2)
        small = rng.rand(rez, rez)
        # kafelkowanie bezszwowe: powiel brzeg = pierwszy wiersz/kolumna (periodyczne przez wrap w resize)
        big = Image.fromarray((small * 255).astype(np.uint8)).resize((N, N), Image.BICUBIC)
        arr = np.asarray(big, dtype=np.float64) / 255.0
        img += arr * (0.55 ** o)
    img -= img.min()
    img /= (img.max() + 1e-6)
    return img


def _seamless_value_noise(seed, cell, oktawy=3):
    """Szum wartości budowany na siatce periodycznej (tile'owalnej) -> naprawdę bezszwowy."""
    rng = random(seed)
    img = np.zeros((N, N))
    amp = 1.0
    total_amp = 0.0
    c = cell
    for o in range(oktawy):
        g = rng.rand(c, c)
        g = np.tile(g, (3, 3))  # periodyczna siatka do interpolacji bez krawędzi
        gi = Image.fromarray((g * 255).astype(np.uint8)).resize((N * 3, N * 3), Image.BICUBIC)
        arr = np.asarray(gi, dtype=np.float64)[N:2 * N, N:2 * N] / 255.0
        img += arr * amp
        total_amp += amp
        amp *= 0.5
        c *= 2
    img /= total_amp
    return img


def _normal_from_height(h, sila=1.0):
    """Normal map (Sobel) z mapy wysokości h (0..1), bezszwowo (np.roll)."""
    gx = (np.roll(h, -1, axis=1) - np.roll(h, 1, axis=1)) * sila
    gy = (np.roll(h, -1, axis=0) - np.roll(h, 1, axis=0)) * sila
    nx, ny, nz = -gx, -gy, np.ones_like(h)
    l = np.sqrt(nx * nx + ny * ny + nz * nz)
    nx, ny, nz = nx / l, ny / l, nz / l
    out = np.stack([(nx * 0.5 + 0.5), (ny * 0.5 + 0.5), (nz * 0.5 + 0.5)], axis=-1)
    return (out * 255).clip(0, 255).astype(np.uint8)


def _save(nazwa, albedo01, wysokosc01, sila_normal=6.0):
    a = (0.72 + albedo01 * 0.28)
    a = np.clip(a, 0.72, 1.0)
    a3 = np.stack([a, a, a], axis=-1)
    Image.fromarray((a3 * 255).clip(0, 255).astype(np.uint8)).save(os.path.join(TEX_OUT, "T_%s_A.png" % nazwa))
    nmap = _normal_from_height(wysokosc01, sila_normal)
    Image.fromarray(nmap, "RGB").save(os.path.join(TEX_OUT, "T_%s_N.png" % nazwa))
    print("  ", nazwa, "OK")


def _grid_lines(rozstaw_px, szer_px, przesun=0, osie="y"):
    """Siatka linii (fuga/spoina) bezszwowa, wartość 1=linia."""
    yy, xx = np.mgrid[0:N, 0:N]
    out = np.zeros((N, N))
    if "y" in osie:
        out = np.maximum(out, (((yy + przesun) % rozstaw_px) < szer_px).astype(float))
    if "x" in osie:
        out = np.maximum(out, (((xx + przesun) % rozstaw_px) < szer_px).astype(float))
    return out


# --------------------------------------------------------------------------- cegła
def cegla(seed=1):
    rng = random(seed)
    cegla_w, cegla_h = 64, 24  # px (cegła ~0.25m x 0.065m przy kafel 1 m -> 512px/m)
    yy, xx = np.mgrid[0:N, 0:N]
    rzad = yy // cegla_h
    przesun_rzad = (rzad % 2) * (cegla_w // 2)
    kol = ((xx + przesun_rzad) // cegla_w)
    spoina_y = (yy % cegla_h) < 2
    spoina_x = ((xx + przesun_rzad) % cegla_w) < 2
    spoina = (spoina_y | spoina_x).astype(float)
    # unikalna lekka różnica każdej cegły
    idc = (rzad.astype(np.int64) * 10007 + kol.astype(np.int64) * 131) % 97
    rngtab = rng.rand(97)
    var = rngtab[idc]
    h = 0.55 + var * 0.3
    h = h * (1 - spoina * 0.5)
    alb = 0.6 + var * 0.35
    alb = alb * (1 - spoina * 0.55)
    _save("cegla", alb, h, sila_normal=10.0)


# --------------------------------------------------------------------------- tynk
def tynk(seed=2):
    base = _seamless_value_noise(seed, 6, oktawy=4)
    smugi = np.zeros((N, N))
    yy, xx = np.mgrid[0:N, 0:N]
    rng = random(seed + 1)
    for _ in range(14):
        cx = rng.randint(0, N)
        w = rng.uniform(8, 22)
        d = np.minimum(np.abs(xx - cx), N - np.abs(xx - cx))
        smugi += np.exp(-(d ** 2) / (2 * w ** 2)) * (yy / N) * rng.uniform(0.3, 0.6)
    h = 0.5 + (base - 0.5) * 0.3 - smugi * 0.4
    h = np.clip(h, 0, 1)
    alb = 0.75 + (base - 0.5) * 0.25 - smugi * 0.35
    _save("tynk", alb, h, sila_normal=4.0)


# --------------------------------------------------------------------------- bruk (kocie łby)
def bruk(seed=3):
    rng = random(seed)
    pts = rng.rand(140, 2) * N
    yy, xx = np.mgrid[0:N, 0:N]
    h = np.zeros((N, N)) + 1.0
    idmap = np.full((N, N), -1)
    best = np.full((N, N), 1e9)
    for i, (px, py) in enumerate(pts):
        for ox in (-N, 0, N):
            for oy in (-N, 0, N):
                d = (xx - (px + ox)) ** 2 + (yy - (py + oy)) ** 2
                m = d < best
                best = np.where(m, d, best)
                idmap = np.where(m, i, idmap)
    r = np.sqrt(best)
    rngtab = rng.rand(len(pts))
    var = rngtab[idmap.clip(0)]
    dome = np.clip(1.0 - r / 18.0, 0, 1) ** 1.5
    h = 0.3 + dome * (0.4 + var * 0.3)
    spoina = (r > 16).astype(float)
    alb = (0.7 + var * 0.25) * (1 - spoina * 0.45)
    _save("bruk", alb, h, sila_normal=8.0)


# --------------------------------------------------------------------------- kostka (granit, rzędy)
def kostka(seed=4):
    w, hh = 44, 44
    lin = _grid_lines(w, 2, osie="xy")
    rng = random(seed)
    yy, xx = np.mgrid[0:N, 0:N]
    idc = ((yy // hh).astype(np.int64) * 977 + (xx // w).astype(np.int64) * 131) % 211
    var = rng.rand(211)[idc]
    base = _seamless_value_noise(seed + 1, 8, oktawy=3)
    h = (0.55 + var * 0.3 + (base - 0.5) * 0.15) * (1 - lin * 0.5)
    alb = (0.75 + var * 0.2) * (1 - lin * 0.5)
    _save("kostka", alb, h, sila_normal=6.0)


# --------------------------------------------------------------------------- chodnik (płyty 0.5m)
def chodnik(seed=5):
    w = N // 1  # kafel = 1 tekstura na "metr" wg kafel w JSON; płyty 0.5m => 2 płyty na teksturę przy kafel=1
    rozm = N // 2
    lin = _grid_lines(rozm, 3, osie="xy")
    base = _seamless_value_noise(seed, 4, oktawy=3)
    rng = random(seed)
    yy, xx = np.mgrid[0:N, 0:N]
    idc = ((yy // rozm).astype(np.int64) * 13 + (xx // rozm).astype(np.int64) * 7) % 23
    var = rng.rand(23)[idc]
    h = (0.55 + (base - 0.5) * 0.15 + var * 0.1) * (1 - lin * 0.55)
    alb = (0.82 + (base - 0.5) * 0.1 + var * 0.06) * (1 - lin * 0.5)
    _save("chodnik", alb, h, sila_normal=5.0)


# --------------------------------------------------------------------------- dachówka (karpiówka, łuski)
def dachowka(seed=6):
    w, hh = 46, 40
    yy, xx = np.mgrid[0:N, 0:N]
    rzad = yy // hh
    przesun = (rzad % 2) * (w // 2)
    kol = (xx + przesun) // w
    ly = (yy % hh) / hh
    lx = ((xx + przesun) % w) / w
    # łuska: łuk w dolnej części
    luska = np.clip(1.0 - ((lx - 0.5) ** 2 * 3.2 + ((ly - 0.15) * 1.3) ** 2), 0, 1)
    rng = random(seed)
    idc = (rzad.astype(np.int64) * 977 + kol.astype(np.int64) * 131) % 131
    var = rng.rand(131)[idc]
    h = 0.4 + luska * (0.45 + var * 0.15)
    alb = 0.7 + luska * 0.2 + var * 0.1
    _save("dachowka", alb, h, sila_normal=7.0)


# --------------------------------------------------------------------------- łupek
def lupek(seed=7):
    w, hh = 60, 30
    yy, xx = np.mgrid[0:N, 0:N]
    rzad = yy // hh
    przesun = (rzad % 2) * (w // 2)
    lin = (((yy % hh) < 2) | (((xx + przesun) % w) < 2)).astype(float)
    rng = random(seed)
    idc = (rzad.astype(np.int64) * 977 + ((xx + przesun) // w).astype(np.int64) * 131) % 131
    var = rng.rand(131)[idc]
    base = _seamless_value_noise(seed, 5, oktawy=3)
    h = (0.5 + var * 0.25 + (base - 0.5) * 0.15) * (1 - lin * 0.4)
    alb = (0.74 + var * 0.18) * (1 - lin * 0.35)
    _save("lupek", alb, h, sila_normal=6.0)


# --------------------------------------------------------------------------- blacha (rąbek co 0.6m)
def blacha(seed=8):
    rozstaw = int(N * 0.6)  # kafel=1m -> 0.6m rąbek
    lin = _grid_lines(rozstaw, 4, osie="x")
    base = _seamless_value_noise(seed, 6, oktawy=3) * 0.1
    yy, xx = np.mgrid[0:N, 0:N]
    fala = 0.04 * np.sin((xx % rozstaw) / rozstaw * 2 * np.pi)
    h = 0.6 + fala + base - lin * 0.3
    alb = 0.85 + base * 0.5 - lin * 0.2
    _save("blacha", alb, h, sila_normal=5.0)


# --------------------------------------------------------------------------- drewno (deski, słoje)
def drewno(seed=9):
    deska_w = N // 6
    lin = _grid_lines(deska_w, 2, osie="x")
    yy, xx = np.mgrid[0:N, 0:N]
    rng = random(seed)
    sloje = np.zeros((N, N))
    for _ in range(10):
        cx = rng.uniform(0, deska_w)
        for k in range(6):
            cx2 = (xx % deska_w)
            d = np.abs(cx2 - cx)
            sloje += np.exp(-(d ** 2) / (2 * (3 + k * 2) ** 2)) * 0.06
    base = _seamless_value_noise(seed, 4, oktawy=2) * 0.1
    h = 0.6 + sloje + base - lin * 0.35
    alb = 0.8 + sloje * 0.6 + base * 0.3 - lin * 0.25
    _save("drewno", alb, h, sila_normal=6.0)


# --------------------------------------------------------------------------- kamień (bloki ciosowe)
def kamien(seed=10):
    w, hh = 90, 46
    yy, xx = np.mgrid[0:N, 0:N]
    rzad = yy // hh
    przesun = (rzad % 2) * (w // 2)
    lin = (((yy % hh) < 3) | (((xx + przesun) % w) < 3)).astype(float)
    rng = random(seed)
    idc = (rzad.astype(np.int64) * 977 + ((xx + przesun) // w).astype(np.int64) * 131) % 131
    var = rng.rand(131)[idc]
    base = _seamless_value_noise(seed, 5, oktawy=3)
    h = (0.55 + var * 0.25 + (base - 0.5) * 0.15) * (1 - lin * 0.45)
    alb = (0.78 + var * 0.18) * (1 - lin * 0.4)
    _save("kamien", alb, h, sila_normal=6.0)


# --------------------------------------------------------------------------- papa
def papa(seed=11):
    base = _seamless_value_noise(seed, 8, oktawy=4)
    lin = _grid_lines(N // 4, 2, osie="y")
    h = 0.5 + (base - 0.5) * 0.25 - lin * 0.15
    alb = 0.78 + (base - 0.5) * 0.18 - lin * 0.1
    _save("papa", alb, h, sila_normal=4.0)


# --------------------------------------------------------------------------- trawa
def trawa(seed=12):
    base = _seamless_value_noise(seed, 5, oktawy=4)
    kepki = _seamless_value_noise(seed + 1, 20, oktawy=3)
    plamy = (_seamless_value_noise(seed + 2, 6, oktawy=2) > 0.6).astype(float)
    h = 0.4 + base * 0.25 + kepki * 0.25
    alb = 0.78 + (base - 0.5) * 0.2 + (kepki - 0.5) * 0.15 - plamy * 0.12
    _save("trawa", alb, h, sila_normal=3.0)


# --------------------------------------------------------------------------- ziemia
def ziemia(seed=13):
    base = _seamless_value_noise(seed, 6, oktawy=4)
    bruzdy = _seamless_value_noise(seed + 1, 30, oktawy=2)
    h = 0.4 + base * 0.3 + bruzdy * 0.15
    alb = 0.8 + (base - 0.5) * 0.22 + (bruzdy - 0.5) * 0.1
    _save("ziemia", alb, h, sila_normal=4.0)


# --------------------------------------------------------------------------- żwir
def zwir(seed=14):
    base = _seamless_value_noise(seed, 40, oktawy=3)
    drobny = _seamless_value_noise(seed + 1, 80, oktawy=2)
    h = 0.5 + (base - 0.5) * 0.3 + (drobny - 0.5) * 0.2
    alb = 0.82 + (base - 0.5) * 0.22 + (drobny - 0.5) * 0.14
    _save("zwir", alb, h, sila_normal=5.0)


# --------------------------------------------------------------------------- kora (bruzdy pionowe)
def kora(seed=15):
    yy, xx = np.mgrid[0:N, 0:N]
    rng = random(seed)
    bruzdy = np.zeros((N, N))
    for _ in range(26):
        cx = rng.uniform(0, N)
        w = rng.uniform(4, 10)
        d = np.minimum(np.abs(xx - cx), N - np.abs(xx - cx))
        bruzdy += np.exp(-(d ** 2) / (2 * w ** 2)) * rng.uniform(0.4, 0.8)
    base = _seamless_value_noise(seed, 8, oktawy=3) * 0.15
    h = 0.6 - bruzdy * 0.4 + base
    alb = 0.8 - bruzdy * 0.3 + base * 0.3
    _save("kora", alb, h, sila_normal=6.0)


# --------------------------------------------------------------------------- liść (plamki)
def lisc(seed=16):
    base = _seamless_value_noise(seed, 30, oktawy=4)
    plamki = _seamless_value_noise(seed + 1, 60, oktawy=2)
    h = 0.45 + base * 0.3 + plamki * 0.2
    alb = 0.8 + (base - 0.5) * 0.24 + (plamki - 0.5) * 0.16
    _save("lisc", alb, h, sila_normal=3.0)


# --------------------------------------------------------------------------- pole (bruzdy)
def pole(seed=17):
    rozstaw = N // 22
    yy, xx = np.mgrid[0:N, 0:N]
    bruzda = np.abs(((yy % rozstaw) / rozstaw) - 0.5) * 2
    base = _seamless_value_noise(seed, 8, oktawy=3) * 0.12
    h = 0.4 + bruzda * 0.4 + base
    alb = 0.78 + (bruzda - 0.5) * 0.2 + base * 0.3
    _save("pole", alb, h, sila_normal=5.0)


# --------------------------------------------------------------------------- woda
def woda(seed=18):
    base = _seamless_value_noise(seed, 10, oktawy=5)
    fala = _seamless_value_noise(seed + 1, 3, oktawy=2)
    h = 0.5 + (base - 0.5) * 0.08 + (fala - 0.5) * 0.04
    alb = 0.88 + (base - 0.5) * 0.08
    _save("woda", alb, h, sila_normal=2.0)


def main():
    print("Generuję tekstury ->", TEX_OUT)
    cegla(); tynk(); bruk(); kostka(); chodnik(); dachowka(); lupek()
    blacha(); drewno(); kamien(); papa(); trawa(); ziemia(); zwir()
    kora(); lisc(); pole(); woda()

    mapowanie = {
        "tynk_bez": {"tex": "tynk", "kafel": 3}, "tynk_ochra": {"tex": "tynk", "kafel": 3},
        "tynk_piasek": {"tex": "tynk", "kafel": 3}, "tynk_szarozielony": {"tex": "tynk", "kafel": 3},
        "tynk_losos": {"tex": "tynk", "kafel": 3}, "tynk_kremowy": {"tex": "tynk", "kafel": 3},
        "tynk_szary": {"tex": "tynk", "kafel": 3}, "tynk_brudny": {"tex": "tynk", "kafel": 3},
        "tynk_rozowy": {"tex": "tynk", "kafel": 3}, "tynk_zolty": {"tex": "tynk", "kafel": 3},
        "tynk_bialy": {"tex": "tynk", "kafel": 3}, "tynk_niebieski": {"tex": "tynk", "kafel": 3},
        "cegla": {"tex": "cegla", "kafel": 1.2}, "cegla_ciemna": {"tex": "cegla", "kafel": 1.2},
        "cegla_jasna": {"tex": "cegla", "kafel": 1.2}, "klinkier": {"tex": "cegla", "kafel": 1.2},
        "dachowka": {"tex": "dachowka", "kafel": 1.5}, "dachowka_ciemna": {"tex": "dachowka", "kafel": 1.5},
        "dachowka_brazowa": {"tex": "dachowka", "kafel": 1.5},
        "lupek": {"tex": "lupek", "kafel": 1.5},
        "blacha": {"tex": "blacha", "kafel": 1.5}, "blacha_miedz": {"tex": "blacha", "kafel": 1.5},
        "bruk": {"tex": "bruk", "kafel": 1.5}, "bruk_jasny": {"tex": "bruk", "kafel": 1.5},
        "asfalt": {"tex": "kostka", "kafel": 1.5},
        "chodnik": {"tex": "chodnik", "kafel": 1.0},
        "trawa": {"tex": "trawa", "kafel": 4}, "trawa_ciemna": {"tex": "trawa", "kafel": 4},
        "trawa_sucha": {"tex": "trawa", "kafel": 4}, "zywoplot": {"tex": "trawa", "kafel": 4},
        "ziemia": {"tex": "ziemia", "kafel": 1.5}, "polna_droga": {"tex": "ziemia", "kafel": 1.5},
        "zwir": {"tex": "zwir", "kafel": 1.0},
        "pole_zaorane": {"tex": "pole", "kafel": 6}, "pole_zielone": {"tex": "pole", "kafel": 6},
        "zyto_scierniska": {"tex": "pole", "kafel": 6},
        "kora": {"tex": "kora", "kafel": 1.0}, "kora_jasna": {"tex": "kora", "kafel": 1.0},
        "lisc_1": {"tex": "lisc", "kafel": 2}, "lisc_2": {"tex": "lisc", "kafel": 2},
        "lisc_3": {"tex": "lisc", "kafel": 2}, "lisc_jesien": {"tex": "lisc", "kafel": 2},
        "iglaste": {"tex": "lisc", "kafel": 2},
        "drewno": {"tex": "drewno", "kafel": 1.0}, "drewno_ciemne": {"tex": "drewno", "kafel": 1.0},
        "modrzew": {"tex": "drewno", "kafel": 1.0}, "modrzew_ciemny": {"tex": "drewno", "kafel": 1.0},
        "deska_szara": {"tex": "drewno", "kafel": 1.0},
        "kamien": {"tex": "kamien", "kafel": 1.0}, "kamien_ciemny": {"tex": "kamien", "kafel": 1.0},
        "piaskowiec": {"tex": "kamien", "kafel": 1.0}, "cokol": {"tex": "kamien", "kafel": 1.0},
        "papa": {"tex": "papa", "kafel": 1.0},
        "woda": {"tex": "woda", "kafel": 8},
    }
    os.makedirs(DANE, exist_ok=True)
    with open(os.path.join(DANE, "tekstury.json"), "w", encoding="utf-8") as f:
        json.dump(mapowanie, f, ensure_ascii=False, indent=1)
    print("Zapisano dane/tekstury.json (%d kolorów)" % len(mapowanie))


if __name__ == "__main__":
    main()
