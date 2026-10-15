# Bron z epoki (1939), bez zadnych symboli. Uklad broni: lufa w -Y, gora +Z, (0,0,0) = srodek chwytu.
# Kazda funkcja zwraca slownik czesci: nazwa -> (obiekt, pivot) w ukladzie broni + punkty.
# Wymaga ob_lib, ob_p2 (rod), ob_v2 (naboj, M, grupa).
from mathutils import Euler


def hull(nazwa, punkty, tile, C, mirror_x=True):
    """Bryla z otoczki wypuklej punktow (opcjonalnie odbitych w X) — kolby, chwyty."""
    bm = bmesh.new()
    for p in punkty:
        bm.verts.new(p)
        if mirror_x:
            bm.verts.new((-p[0], p[1], p[2]))
    bmesh.ops.convex_hull(bm, input=bm.verts[:])
    bm.normal_update()
    _uv_box(bm, tile)
    return _finish(nazwa, bm, C, atlas_mat())


def naboj_karabinowy(C, mat, skala=1.0):
    s = skala
    P = [cyl("luska", 0.006 * s, 0.057 * s, (0, 0, 0.0285 * s), "mosiadz", C, seg=8, r2=0.0055 * s),
         cyl("szyjka", 0.0045 * s, 0.008 * s, (0, 0, 0.061 * s), "mosiadz", C, seg=8),
         cyl("pocisk", 0.0041 * s, 0.022 * s, (0, 0, 0.074 * s), "miedz", C, seg=8, r2=0.0015 * s)]
    return grupa(P, "naboj_k", mat)


def rot16(deg):
    return Vector((0, -math.sin(math.radians(deg)), math.cos(math.radians(deg))))


# ================================================================ Luger P08
def luger(C):
    a = rot16(35); gc = Vector((0, 0.03, -0.012))
    B = [cyl("lufa", 0.0095, 0.105, (0, -0.118, 0.068), "oksyda", C, rot=(90, 0, 0), seg=14),
         cyl("lufa_wylot", 0.0098, 0.006, (0, -0.168, 0.068), "oksyda", C, rot=(90, 0, 0), seg=14),
         cyl("otwor", 0.0045, 0.004, (0, -0.172, 0.068), "guma", C, rot=(90, 0, 0), seg=8),
         box("muszka", (0.003, 0.006, 0.008), (0, -0.162, 0.08), "oksyda", C),
         box("komora", (0.022, 0.085, 0.03), (0, -0.03, 0.068), "oksyda", C, bevel=0.003, seg=2),
         box("szkielet", (0.026, 0.1, 0.026), (0, -0.025, 0.04), "oksyda", C, bevel=0.003, seg=2),
         box("chwyt", (0.027, 0.045, 0.115), gc, "oksyda", C, rot=(35, 0, 0), bevel=0.004, seg=2),
         box("bezpiecznik", (0.004, 0.02, 0.008), (0.014, 0.025, 0.05), "stal", C)]
    for sx in (-1, 1):
        B.append(box("okladzina", (0.004, 0.038, 0.09), (sx * 0.0155, gc.y, gc.z), "drewno_ciemne", C, rot=(35, 0, 0), bevel=0.002, seg=2))
    B.append(torus("kablak", 0.022, 0.004, (0, -0.045, 0.022), "oksyda", C, rot=(0, 90, 0), seg=14, rseg=4, arc=200))
    join(B, "Bron_L")
    K = [box("kolano_t", (0.016, 0.052, 0.014), (0, 0.025, 0.084), "oksyda", C, bevel=0.002),
         box("kolano_p", (0.014, 0.03, 0.012), (0, -0.01, 0.083), "oksyda", C)]
    for sx in (-1, 1):
        K.append(cyl("galka", 0.011, 0.008, (sx * 0.012, 0.037, 0.086), "stal", C, rot=(0, 90, 0), seg=12))
    kol = join(K, "Kolano_L")
    sp = join([box("spust", (0.006, 0.006, 0.02), (0, -0.04, 0.02), "stal", C, rot=(15, 0, 0))], "Spust_L")
    mc = gc - a * 0.005
    mg = join([box("mag", (0.02, 0.03, 0.1), mc, "oksyda", C, rot=(35, 0, 0)),
               cyl("mag_galka", 0.012, 0.03, tuple(gc - a * 0.062), "drewno", C, rot=(0, 90, 0), seg=10),
               naboj(C, M(tuple(mc + a * 0.05 + Vector((0, 0.01, 0.002))), (90, 0, 0)))], "Magazynek_L")
    return {"Bron": (bpy.data.objects["Bron_L"], Vector()), "Kolano": (kol, Vector((0, 0.055, 0.078))),
            "Spust": (sp, Vector()), "Magazynek": (mg, Vector())}, \
        {"Lufa": Vector((0, -0.173, 0.068)), "Wyrzut": Vector((0, 0.01, 0.09)), "gc": gc, "os": a,
         "trig": Vector((0, -0.04, 0.02)), "celownik": Vector((0, 0.04, 0.09))}


# ================================================================ Walther PPK
def ppk(C):
    a = rot16(12); gc = Vector((0, 0.008, -0.01))
    B = [box("szkielet", (0.024, 0.13, 0.02), (0, -0.045, 0.03), "oksyda", C, bevel=0.003, seg=2),
         box("chwyt", (0.026, 0.044, 0.09), gc, "oksyda", C, rot=(12, 0, 0), bevel=0.004, seg=2),
         torus("kablak", 0.019, 0.004, (0, -0.045, 0.012), "oksyda", C, rot=(0, 90, 0), seg=14, rseg=4, arc=200)]
    for sx in (-1, 1):
        B.append(box("okladzina", (0.004, 0.04, 0.08), (sx * 0.0148, gc.y, gc.z), "bakelit", C, rot=(12, 0, 0), bevel=0.002, seg=2))
    join(B, "Bron_W")
    Z = [box("zamek", (0.024, 0.155, 0.027), (0, -0.045, 0.053), "oksyda", C, bevel=0.004, seg=2),
         cyl("otwor", 0.004, 0.004, (0, -0.124, 0.055), "guma", C, rot=(90, 0, 0), seg=8),
         box("okno", (0.002, 0.024, 0.01), (-0.0122, -0.02, 0.057), "guma", C)]
    for i in range(8):
        for sx in (-1, 1):
            Z.append(box("nac", (0.002, 0.0022, 0.02), (sx * 0.0121, 0.012 + i * 0.004, 0.053), "grafit", C))
    zam = join(Z, "Zamek_W")
    kur = join([box("kurek", (0.008, 0.008, 0.016), (0, 0.036, 0.062), "stal", C, rot=(-25, 0, 0), bevel=0.001)], "Kurek_W", origin=(0, 0.034, 0.055))
    sp = join([box("spust", (0.006, 0.006, 0.018), (0, -0.04, 0.012), "stal", C, rot=(15, 0, 0))], "Spust_W")
    mc = gc - a * 0.004
    mg = join([box("mag", (0.019, 0.03, 0.085), mc, "oksyda", C, rot=(12, 0, 0)),
               box("stopka", (0.024, 0.042, 0.008), tuple(gc - a * 0.047), "bakelit", C, rot=(12, 0, 0), bevel=0.002),
               naboj(C, M(tuple(mc + a * 0.043 + Vector((0, 0.008, 0.002))), (90, 0, 0)), 0.9)], "Magazynek_W")
    return {"Bron": (bpy.data.objects["Bron_W"], Vector()), "Zamek": (zam, Vector()), "Kurek": (kur, Vector((0, 0.034, 0.055))),
            "Spust": (sp, Vector()), "Magazynek": (mg, Vector())}, \
        {"Lufa": Vector((0, -0.125, 0.055)), "Wyrzut": Vector((-0.014, -0.02, 0.057)), "gc": gc, "os": a,
         "trig": Vector((0, -0.04, 0.012)), "celownik": Vector((0, 0.02, 0.07))}


# ================================================================ Karabiny (Mauser Kar98k i polski wz. 29)
def karabin(C, wz29=False):
    drewno = "drewno" if wz29 else "drewno_ciemne"
    sfx = "_29" if wz29 else "_98"
    S = [hull("kolba", [(0.019, 0.09, 0.034), (0.019, 0.09, -0.05), (0.022, 0.2, 0.038), (0.022, 0.2, -0.075),
                        (0.023, 0.42, 0.03), (0.023, 0.42, -0.115), (0.02, 0.32, -0.1)], drewno, C),
         hull("szyjka", [(0.018, -0.03, 0.03), (0.018, -0.03, -0.035), (0.019, 0.11, 0.035), (0.02, 0.11, -0.06),
                         (0.016, 0.03, -0.065)], drewno, C),
         hull("lozysko", [(0.021, -0.04, 0.05), (0.021, -0.04, 0.0), (0.019, -0.62 if wz29 else -0.6, 0.06),
                          (0.018, -0.62 if wz29 else -0.6, 0.035)], drewno, C),
         hull("nakladka", [(0.013, -0.25, 0.085), (0.013, -0.25, 0.065), (0.012, -0.6 if wz29 else -0.55, 0.08),
                           (0.012, -0.6 if wz29 else -0.55, 0.065)], drewno, C),
         box("stopka_kolby", (0.048, 0.008, 0.165), (0, 0.423, -0.045), "oksyda", C, rot=(0, 0, 0)),
         cyl("lufa", 0.009, 0.62, (0, -0.52, 0.066), "oksyda", C, rot=(90, 0, 0), seg=12),
         cyl("komora", 0.017, 0.2, (0, -0.11, 0.063), "oksyda", C, rot=(90, 0, 0), seg=14),
         box("celownik_t", (0.02, 0.05, 0.016), (0, -0.2, 0.082), "oksyda", C, bevel=0.002),
         box("muszka_podst", (0.016, 0.02, 0.014), (0, -0.815, 0.078), "oksyda", C),
         box("muszka", (0.003, 0.006, 0.012), (0, -0.815, 0.09), "oksyda", C),
         box("oslona_muszki", (0.022, 0.025, 0.004), (0, -0.815, 0.098), "oksyda", C),
         box("kablak", (0.014, 0.075, 0.006), (0, -0.04, -0.012), "oksyda", C),
         box("dno_mag", (0.024, 0.08, 0.006), (0, -0.085, 0.002), "oksyda", C),
         cyl("otwor", 0.0045, 0.004, (0, -0.832, 0.066), "guma", C, rot=(90, 0, 0), seg=8)]
    for y in ((-0.38, -0.6) if not wz29 else (-0.35, -0.5, -0.62)):
        S.append(torus("obejma", 0.024, 0.004, (0, y, 0.05), "stal", C, rot=(90, 0, 0), seg=12, rseg=4))
    S.append(torus("kablak_l", 0.02, 0.004, (0, -0.04, 0.0), "oksyda", C, rot=(0, 90, 0), seg=12, rseg=4, arc=180))
    S.append(torus("strzemiaczko", 0.012, 0.003, (0.022, 0.2, -0.04), "stal", C, rot=(0, 90, 0), seg=10, rseg=3))
    join(S, "Bron" + sfx)
    # zamek: rygiel + raczka (Kar98k: odgieta w dol, wz.29: prosta)
    Z = [cyl("rygiel", 0.0095, 0.13, (0, -0.06, 0.078), "stal_polerowana", C, rot=(90, 0, 0), seg=12),
         cyl("kurek_z", 0.012, 0.03, (0, 0.012, 0.078), "oksyda", C, rot=(90, 0, 0), seg=10)]
    if wz29:
        Z.append(rod("raczka", (0, 0.0, 0.078), (-0.07, 0.0, 0.074), 0.0045, "stal_polerowana", C))
        Z.append(sphere("galka", 0.011, (-0.072, 0.0, 0.074), "stal_polerowana", C, seg=10, rings=6))
    else:
        Z.append(rod("raczka", (0, 0.0, 0.078), (-0.045, 0.012, 0.045), 0.0045, "stal_polerowana", C))
        Z.append(sphere("galka", 0.011, (-0.048, 0.014, 0.04), "stal_polerowana", C, seg=10, rings=6))
    zam = join(Z, "Zamek" + sfx)
    sp = join([box("spust", (0.005, 0.005, 0.02), (0, -0.04, 0.002), "stal", C, rot=(15, 0, 0))], "Spust" + sfx)
    L = [box("lodka", (0.013, 0.004, 0.055), (0.0, 0.003, 0.0), "mosiadz", C)]
    for k in range(5):
        L.append(naboj_karabinowy(C, M((0, 0.0, -0.02 + k * 0.01), (90, 0, 0))))
    lod = join(L, "Lodka" + sfx)
    lod.data.transform(M((0, -0.07, 0.11)))
    return {"Bron": (bpy.data.objects["Bron" + sfx], Vector()), "Zamek": (zam, Vector((0, 0, 0.078))),
            "Spust": (sp, Vector()), "Lodka": (lod, Vector((0, -0.07, 0.11)))}, \
        {"Lufa": Vector((0, -0.835, 0.066)), "Wyrzut": Vector((-0.02, -0.09, 0.085)), "gc": Vector((0, 0.035, -0.02)),
         "os": rot16(40), "trig": Vector((0, -0.04, 0.002)),
         "celownik": Vector((0, -0.2, 0.092)), "muszka": Vector((0, -0.815, 0.096)), "lozysko": Vector((0, -0.38, 0.0)),
         "galka": Vector((-0.072, 0.0, 0.074)) if wz29 else Vector((-0.048, 0.014, 0.04)), "kolba": Vector((0, 0.42, -0.04))}


# ================================================================ Noze
def bagnet(C):
    B = [hull("glownia", [(0.0025, -0.03, 0.012), (0.0025, -0.03, -0.012), (0.0015, -0.24, 0.01), (0.0015, -0.24, -0.008),
                          (0.0, -0.275, 0.0)], "stal_polerowana", C),
         box("zbrocze", (0.0055, 0.2, 0.004), (0, -0.13, 0.002), "stal", C),
         box("jelec", (0.012, 0.012, 0.06), (0, -0.025, 0.012), "oksyda", C, bevel=0.002),
         box("rekojesc", (0.016, 0.11, 0.026), (0, 0.03, 0.0), "drewno_ciemne", C, bevel=0.005, seg=2),
         box("glowica", (0.02, 0.03, 0.03), (0, 0.095, 0.0), "oksyda", C, bevel=0.004, seg=2)]
    for y in (0.0, 0.05):
        for sx in (-1, 1):
            B.append(cyl("nit", 0.004, 0.003, (sx * 0.0085, y, 0.0), "stal", C, rot=(0, 90, 0), seg=8))
    join(B, "Bron_B")
    return {"Bron": (bpy.data.objects["Bron_B"], Vector())}, {"gc": Vector((0, 0.035, 0)), "os": Vector((0, -1, 0))}


def noz(C):
    B = [hull("glownia", [(0.0018, -0.02, 0.01), (0.0018, -0.02, -0.012), (0.0012, -0.1, 0.012), (0.0, -0.13, 0.004),
                          (0.0012, -0.11, -0.006)], "stal_polerowana", C),
         box("tulejka", (0.018, 0.012, 0.024), (0, -0.016, 0.0), "mosiadz", C, bevel=0.003),
         hull("rekojesc", [(0.009, -0.01, 0.012), (0.009, -0.01, -0.012), (0.011, 0.05, 0.014), (0.011, 0.05, -0.014),
                           (0.008, 0.1, 0.01), (0.008, 0.1, -0.01)], "drewno_jasne", C),
         box("glowka", (0.016, 0.01, 0.02), (0, 0.105, 0.0), "mosiadz", C, bevel=0.003)]
    join(B, "Bron_N")
    return {"Bron": (bpy.data.objects["Bron_N"], Vector())}, {"gc": Vector((0, 0.045, 0)), "os": Vector((0, -1, 0))}


def eksport_obiektu_broni(nazwa_ob, fn, *args):
    """OB_* do _Obiekty (dla NPC/podloga): wszystkie czesci + punkty, pivot = chwyt."""
    C = new_collection(nazwa_ob)
    cz, pk = fn(C, *args)
    if "Lodka" in cz:
        bpy.data.objects.remove(cz.pop("Lodka")[0])
    for n, (o, piv) in cz.items():
        if piv.length > 0:
            o.data.transform(Matrix.Translation(-piv)); o.location = piv
        o.name = n
    for n in ("Lufa", "Wyrzut"):
        if n in pk:
            empty(n, tuple(pk[n]), C, size=0.03)
    empty("Uchwyt", tuple(pk["gc"]), C, size=0.03)
    report(nazwa_ob)
    return export_fbx(nazwa_ob)
