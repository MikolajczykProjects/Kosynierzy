# Przebudowa i eksport postaci (rola C) + rak FPP do _Postacie/Modele.
# W Blenderze: exec(open(r"C:\Users\borys\Desktop\HACKATON\paczka\blender\zbuduj_postacie.py", encoding="utf-8").read())
import bpy, os, shutil
B = r"C:\Users\borys\Desktop\HACKATON\paczka\blender"
g = {}
exec(open(os.path.join(B, "ob_lib.py"), encoding="utf-8").read(), g)
src = open(os.path.join(B, "ob_p2.py"), encoding="utf-8").read(); exec(src[:src.index("build_aparat(); build_mapa_stol()")], g)
src = open(os.path.join(B, "ob_v2.py"), encoding="utf-8").read(); exec(src[:src.index("build_vis(); build_skrytka()")], g)
for f in ("ch_lib.py", "ch_cialo2.py", "ch_postacie.py", "ch_baza.py", "fp_rece.py"):
    exec(open(os.path.join(B, f), encoding="utf-8").read(), g)
for a in list(bpy.data.actions):
    bpy.data.actions.remove(a)
g["zbuduj_postacie"]()
g["zbuduj_animacje"]()
arm = g["zbuduj_fp"]()
g["zrob_klipy_fp"](arm)
pliki = [os.path.basename(g["eksport_postaci"](n, n)) for n in g["POSTACIE"]]
pliki.append(os.path.basename(g["eksport_postaci"]("CH_Animacje", "CH_Animacje", animacje=True)))
# FP: tylko akcje FP_* (usun ludzkie, zeby nie trafily do FBX rak)
ludzkie = [a for a in bpy.data.actions if not a.name.startswith("FP_")]
for a in ludzkie:
    a.use_fake_user = False
    bpy.data.actions.remove(a)
pliki.append(os.path.basename(g["eksport_postaci"]("FP_Rece", "FP_Rece", animacje=True)))
# kopia atlasu do _Postacie/Materialy
M = os.path.join(g["ROOT"], "Materialy"); MP = os.path.join(g["DIR_POST"], "Materialy")
for suf in ("", "_MS", "_N"):
    shutil.copyfile(os.path.join(M, "T_Obiekty_Atlas%s.png" % suf), os.path.join(MP, "T_Postacie_Atlas%s.png" % suf))
print(len(pliki), "FBX postaci")
