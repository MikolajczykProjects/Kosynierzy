# -*- coding: utf-8 -*-
"""tekstury_real.py — podmienia proceduralne tekstury na zdjęciowe PBR (CC0, ambientCG, 1K).
Albedo jest neutralizowane (średnia kanałów ~0.85), bo w materiale mnoży się przez kolor z palety 1939.
Normal: NormalGL (konwencja Blendera i Unity). Uruchom: python3 tekstury_real.py
"""
import os, io, json, zipfile, urllib.request, sys
import numpy as np
from PIL import Image

TU = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(os.path.dirname(TU), "unity", "Assets", "_Mapa", "Tekstury")
CACHE = "/tmp/tex_real"
os.makedirs(CACHE, exist_ok=True)
API = "https://ambientcg.com/api/v2/full_json?type=Material&include=downloadData&limit=6&sort=Popular&q="

# nasza nazwa -> (zapytanie, który wynik, nasycenie zachowane 0..1)
MAPA = {
    "cegla": ("bricks", 0, 0.55),
    "tynk": ("plaster", 0, 0.2),
    "bruk": ("paving stones", 1, 0.3),
    "kostka": ("paving stones", 0, 0.3),
    "chodnik": ("paving stones", 2, 0.2),
    "dachowka": ("roofing tiles", 0, 0.5),
    "lupek": ("roofing tiles", 1, 0.3),
    "blacha": ("corrugated steel", 0, 0.2),
    "drewno": ("planks", 0, 0.5),
    "kamien": ("rock", 0, 0.3),
    "papa": ("asphalt", 0, 0.1),
    "trawa": ("grass", 0, 0.6),
    "ziemia": ("ground", 0, 0.5),
    "zwir": ("gravel", 1, 0.4),
    "kora": ("bark", 0, 0.5),
    "lisc": ("leaves", 0, 0.6),
    "pole": ("ground", 2, 0.5),
}
UA = {"User-Agent": "Mozilla/5.0"}


def get(url):
    return urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=60).read()


def zrob(nazwa, q, idx, nas):
    d = json.loads(get(API + urllib.parse.quote(q)))
    assets = d["foundAssets"]
    a = assets[min(idx, len(assets) - 1)]
    url = None
    for fol in a["downloadFolders"].values():
        for cat in fol["downloadFiletypeCategories"].values():
            for x in cat["downloads"]:
                if x["attribute"] == "1K-JPG":
                    url = x["fullDownloadPath"]
    zp = os.path.join(CACHE, a["assetId"] + ".zip")
    if not os.path.exists(zp):
        open(zp, "wb").write(get(url))
    z = zipfile.ZipFile(zp)
    col = [n for n in z.namelist() if n.endswith("_Color.jpg")][0]
    nrm = [n for n in z.namelist() if n.endswith("_NormalGL.jpg")][0]
    c = np.asarray(Image.open(io.BytesIO(z.read(col))).convert("RGB").resize((1024, 1024), Image.LANCZOS), dtype=np.float64) / 255.0
    lum = c.mean(axis=2, keepdims=True)
    c = lum + (c - lum) * nas                       # częściowe odbarwienie
    c = c / (c.reshape(-1, 3).mean(axis=0) + 1e-6)  # neutralny odcień, średnia 1
    c = np.clip(c * 0.85, 0.08, 1.0)
    Image.fromarray((c * 255).astype(np.uint8)).save(os.path.join(OUT, "T_%s_A.png" % nazwa))
    Image.open(io.BytesIO(z.read(nrm))).convert("RGB").resize((1024, 1024), Image.LANCZOS).save(os.path.join(OUT, "T_%s_N.png" % nazwa))
    print("TEX", nazwa, "<-", a["assetId"], flush=True)


import urllib.parse
from concurrent.futures import ThreadPoolExecutor
with ThreadPoolExecutor(8) as ex:
    futs = {ex.submit(zrob, n, *v): n for n, v in MAPA.items()}
    for f, n in futs.items():
        try:
            f.result()
        except Exception as e:
            print("TEX BLAD", n, e, flush=True)
print("TEX KONIEC")
