# OB_Drzwi: 1,0 x 2,1 m, oscieznica, pivot w zawiasie (dol). Skrzydlo = osobny mesh z pivotem w zawiasie.
# Przod = -Y (Unity +Z). Skrzydlo od zawiasu w strone -X Blendera (= +X Unity).
C = new_collection("OB_Drzwi")
W, H, T = 1.0, 2.1, 0.05
fr = []
# oscieznica: dwie pionowe belki + nadproze, wokol skrzydla (luz 5 mm)
fr.append(box("a", (0.09, 0.14, H + 0.09), (0.045 + 0.005, 0, (H + 0.09) / 2), "drewno_ciemne", C, bevel=0.008))
fr.append(box("b", (0.09, 0.14, H + 0.09), (-W - 0.045 - 0.005, 0, (H + 0.09) / 2), "drewno_ciemne", C, bevel=0.008))
fr.append(box("c", (W + 0.19, 0.14, 0.09), (-W / 2, 0, H + 0.045 + 0.005), "drewno_ciemne", C, bevel=0.008))
fr.append(box("prog", (W + 0.01, 0.16, 0.02), (-W / 2, 0, 0.01), "drewno_ciemne", C))
korpus = join(fr, "Oscieznica")

s = []
s.append(box("plyta", (W - 0.01, T, H - 0.01), (-W / 2, 0, H / 2), "drewno_drzwi", C, bevel=0.006))
# plyciny (panele) na obu stronach
for side in (-1, 1):
    y = side * (T / 2 + 0.006)
    for (zc, h) in ((0.55, 0.75), (1.5, 0.95)):
        for xc in (-0.27, -0.73):
            s.append(box("plycina", (0.34, 0.014, h), (xc, y, zc), "drewno", C, bevel=0.004))
# klamka + szyld po obu stronach
for side in (-1, 1):
    y = side * (T / 2 + 0.01)
    s.append(box("szyld", (0.045, 0.012, 0.22), (-W + 0.09, y, 1.0), "mosiadz", C, bevel=0.003))
    s.append(cyl("trzon", 0.008, 0.05, (-W + 0.09, y + side * 0.025, 1.05), "mosiadz", C, rot=(90, 0, 0), seg=8))
    s.append(cyl("raczka", 0.011, 0.12, (-W + 0.14, y + side * 0.05, 1.05), "mosiadz", C, rot=(0, 90, 0), seg=8))
# zawiasy
for z in (0.25, 1.85):
    s.append(cyl("zawias", 0.012, 0.11, (0.0, -T / 2, z), "zeliwo", C, seg=8))
skrzydlo = join(s, "Skrzydlo", origin=(0, 0, 0))

empty("PunktInterakcji", (-W / 2, -0.8, 0), C)
report("OB_Drzwi")
solo("OB_Drzwi")
