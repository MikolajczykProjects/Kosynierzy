# Przebudowa wszystkich obiektow + eksport FBX do _Obiekty/Modele.
# W Blenderze (Scripting): exec(open(r"C:\Users\borys\Desktop\HACKATON\paczka\blender\zbuduj_wszystko.py", encoding="utf-8").read())
import bpy, os
B = r"C:\Users\borys\Desktop\HACKATON\paczka\blender"
for c in list(bpy.data.collections):
    if c.name.startswith("OB_"):
        for o in list(c.all_objects):
            bpy.data.objects.remove(o, do_unlink=True)
        bpy.data.collections.remove(c)
for n in ("T_Obiekty_Atlas", "T_Obiekty_Atlas_MS", "T_Obiekty_Atlas_N"):
    im = bpy.data.images.get(n)
    if im:
        bpy.data.images.remove(im)
g = {}
for f in ("ob_lib.py", "ob_drzwi.py", "ob_p1.py", "ob_p2.py", "ob_p3.py", "ob_v2.py", "oswietlenie.py"):
    exec(open(os.path.join(B, f), encoding="utf-8").read(), g)
for lc in bpy.context.view_layer.layer_collection.children:
    lc.exclude = False
out = [os.path.basename(g["export_fbx"](c.name)) for c in bpy.context.scene.collection.children if c.name.startswith("OB_")]
print(len(out), "FBX:", out)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(B, "obiekty.blend"))
