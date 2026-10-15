# Przebudowa postaci CH_* + CH_Animacje (bez rak FPP). Uruchom:
#   Blender -b obiekty.blend -P zbuduj_postacie_mac.py  [-- CH_Kruk CH_Zofia ...]
import bpy, os, sys
B = os.path.dirname(os.path.abspath(__file__)) if "__file__" in dir() else os.getcwd()
B = os.environ.get("BLENDER_SKRYPTY", B)
g = {}
src = open(os.path.join(B, "ob_lib.py"), encoding="utf-8").read()
src = src.replace(r'ROOT = r"C:\Users\borys\Desktop\HACKATON\paczka\_Obiekty"', 'ROOT = %r' % os.path.join(os.path.dirname(B), "_Obiekty"))
exec(src, g)
src = open(os.path.join(B, "ob_p2.py"), encoding="utf-8").read(); exec(src[:src.index("build_aparat(); build_mapa_stol()")], g)
src = open(os.path.join(B, "ob_v2.py"), encoding="utf-8").read(); exec(src[:src.index("build_vis(); build_skrytka()")], g)
for f in ("ch_lib.py", "ch_cialo2.py", "ch_postacie.py", "ch_baza.py", "ch_cialo3.py"):
    exec(open(os.path.join(B, f), encoding="utf-8").read(), g)
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
tylko = argv or None
for a in list(bpy.data.actions):
    bpy.data.actions.remove(a)
g["zbuduj_postacie"](tylko)
pliki = [os.path.basename(g["eksport_postaci"](n, n)) for n in g["POSTACIE"] if not tylko or n in tylko]
if not tylko or "CH_Animacje" in tylko:
    g["zbuduj_animacje"]()
    pliki.append(os.path.basename(g["eksport_postaci"]("CH_Animacje", "CH_Animacje", animacje=True)))
print("ZBUDOWANO", pliki)
