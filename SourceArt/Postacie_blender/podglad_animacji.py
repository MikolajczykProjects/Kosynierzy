# Podglad animacji w Blenderze — te same ruchy co klipy Unity z BudujObiekty*.cs.
# Przeliczenie obrotow Unity -> Blender: x_b = x_u, y_b = z_u, z_b = -y_u.  24 kl./s.
# Uruchom PO zbuduj_wszystko.py. Nie zapisuj potem .blend z kluczami (eksport ich nie potrzebuje).
import bpy, math

FPS = 24
sc = bpy.context.scene
sc.render.fps = FPS
sc.frame_start, sc.frame_end = 1, 96


def ob(nazwa, kolekcja):
    for o in bpy.data.collections[kolekcja].all_objects:
        if o.name.split(".")[0] == nazwa:
            return o
    raise KeyError(nazwa)


def klucze(o, os_, pary, start=0.0, path="rotation_euler", stopnie=True):
    """pary: (czas_s, wartosc). os_: 0/1/2."""
    for t, v in pary:
        getattr(o, path)[os_] = math.radians(v) if stopnie else v
        o.keyframe_insert(path, index=os_, frame=1 + (start + t) * FPS)


def u2b_y(v):   # obrot Unity wokol Y -> Blender wokol Z
    return -v


def czysc():
    for c in bpy.data.collections:
        if c.name.startswith("OB_"):
            for o in c.all_objects:
                o.animation_data_clear()


czysc()

# OB_Drzwi: Otworz = 0 -> 100 st. (0,4 s), czeka, zamyka (1,8 s)
s = ob("Skrzydlo", "OB_Drzwi")
klucze(s, 2, [(0, 0), (.4, u2b_y(100)), (1.4, u2b_y(100)), (1.8, 0)], start=0.3)

# OB_Lacznica: Przetnij na kolejnych kablach co 1 s
for i in range(1, 4):
    g = ob(f"Kabel_{i}_Gora", "OB_Lacznica"); d = ob(f"Kabel_{i}_Dol", "OB_Lacznica")
    st = 0.3 + (i - 1) * 1.0
    klucze(g, 0, [(0, 0), (.15, -34), (.3, -20), (.5, -25)], st)
    klucze(g, 2, [(0, 0), (.5, u2b_y(4))], st)
    klucze(d, 0, [(0, 0), (.25, 138), (.4, 110), (.6, 118)], st)
    klucze(d, 2, [(0, 0), (.6, u2b_y(-6))], st)
    klucze(d, 1, [(0, 0), (.6, 8)], st)

# OB_Skrytka: Otworz
w = ob("Wieko", "OB_Skrytka")
klucze(w, 0, [(0, 0), (.45, -112), (.6, -102), (.7, -105)], 0.3)

# OB_Karta_Historii: obrot 4 s + unoszenie
k = ob("Karta", "OB_Karta_Historii")
klucze(k, 2, [(0, 0), (4, u2b_y(360))])
klucze(k, 2, [(0, 1.0), (1, 1.08), (2, 1.0), (3, 1.08), (4, 1.0)], path="location", stopnie=False)

# OB_Auto_SD: Speed = 3 m/s (kola), OtworzDrzwi
obw = 2 * math.pi * 0.36
for n in ("Kolo_PP", "Kolo_LP", "Kolo_PT", "Kolo_LT"):
    klucze(ob(n, "OB_Auto_SD"), 0, [(0, 0), (4, 360 * 4 * 3 / obw)])
klucze(ob("Drzwi_Kierowcy", "OB_Auto_SD"), 2, [(0, 0), (.4, u2b_y(70)), (1.4, u2b_y(70)), (1.8, 0)], 1.0)

# OB_Rower: Speed = 4 m/s
obw = 2 * math.pi * 0.34
for n in ("Kolo_P", "Kolo_T"):
    klucze(ob(n, "OB_Rower"), 0, [(0, 0), (4, 360 * 4 * 4 / obw)])
klucze(ob("Pedaly", "OB_Rower"), 0, [(0, 0), (4, 180 * 4 * 4 / obw)])

# OB_Pistolet_Vis: strzal (0,3 s), strzal (0,8 s), przeladowanie (od 1,5 s)
# Unity z -> Blender -y; magazynek wysuwa sie wzdluz osi chwytu (0, 0,276, -0,961)
zam = ob("Zamek", "OB_Pistolet_Vis"); kur = ob("Kurek", "OB_Pistolet_Vis")
spu = ob("Spust", "OB_Pistolet_Vis"); mag = ob("Magazynek", "OB_Pistolet_Vis")
for st in (0.3, 0.8):
    klucze(zam, 1, [(0, 0), (.035, 0.035), (.14, 0)], st, path="location", stopnie=False)
    klucze(kur, 0, [(0, 0), (.012, 45), (.05, 0)], st)
    klucze(spu, 1, [(0, 0), (.015, 0.005), (.15, 0)], st, path="location", stopnie=False)
st = 1.5
for t, dd in ((0, 0), (.15, .01), (.45, .25), (.46, .12), (.8, .12), (1.2, 0)):
    klucze(mag, 1, [(t, 0.276 * dd)], st, path="location", stopnie=False)
    klucze(mag, 2, [(t, -0.961 * dd)], st, path="location", stopnie=False)
for t, s_ in ((0, 1), (.44, 1), (.45, 0), (.79, 0), (.8, 1)):
    for i in range(3):
        klucze(mag, i, [(t, s_)], st, path="scale", stopnie=False)
klucze(zam, 1, [(1.35, 0), (1.55, 0.045), (1.65, 0.045), (1.75, 0)], st, path="location", stopnie=False)
klucze(kur, 0, [(1.35, 0), (1.55, -12), (1.75, 0)], st)

# OB_Ciezarowka: Speed = 2 m/s, OtworzDrzwi
obw = 2 * math.pi * 0.46
for n in ("Kolo_PP", "Kolo_LP", "Kolo_PT", "Kolo_LT"):
    klucze(ob(n, "OB_Ciezarowka"), 0, [(0, 0), (4, 360 * 4 * 2 / obw)])
klucze(ob("Drzwi_Kierowcy", "OB_Ciezarowka"), 2, [(0, 0), (.4, u2b_y(70)), (1.4, u2b_y(70)), (1.8, 0)], 1.0)

# liniowo dla obrotow ciaglych
for c in ("OB_Auto_SD", "OB_Rower", "OB_Karta_Historii", "OB_Ciezarowka"):
    for o in bpy.data.collections[c].all_objects:
        if o.animation_data and o.animation_data.action:
            act = o.animation_data.action
            curves = act.fcurves if hasattr(act, "fcurves") else [fc for l in act.layers for s_ in l.strips for cb in s_.channelbags for fc in cb.fcurves]
            for fc in curves:
                if fc.data_path == "rotation_euler":
                    for kp in fc.keyframe_points:
                        kp.interpolation = "LINEAR"
print("podglad: klatki 1-96")
