# Oswietlenie podgladowe w Blenderze (jak dzien w Unity: slonce + niebo). Modele go nie eksportuja.
# Niebo ma gradient (ziemia / horyzont / niebo), zeby metal mial co odbijac — jak skybox w Unity.
import bpy, math

sc = bpy.context.scene
for n in ("Light", "Podglad_Slonce"):
    o = bpy.data.objects.get(n)
    if o:
        bpy.data.objects.remove(o, do_unlink=True)
sun = bpy.data.lights.get("Podglad_Slonce") or bpy.data.lights.new("Podglad_Slonce", "SUN")
sun.energy = 4.0
sun.color = (1.0, 0.95, 0.88)
sun.angle = math.radians(3)
o = bpy.data.objects.new("Podglad_Slonce", sun)
sc.collection.objects.link(o)
o.rotation_euler = (math.radians(50), 0, math.radians(35))

w = sc.world or bpy.data.worlds.new("World")
sc.world = w
w.use_nodes = True
nt = w.node_tree
for n in list(nt.nodes):
    nt.nodes.remove(n)
out = nt.nodes.new("ShaderNodeOutputWorld")
bg = nt.nodes.new("ShaderNodeBackground")
tc = nt.nodes.new("ShaderNodeTexCoord")
sep = nt.nodes.new("ShaderNodeSeparateXYZ")
ramp = nt.nodes.new("ShaderNodeValToRGB")
nt.links.new(tc.outputs["Generated"], sep.inputs[0])
nt.links.new(sep.outputs["Z"], ramp.inputs["Fac"])
nt.links.new(ramp.outputs["Color"], bg.inputs["Color"])
nt.links.new(bg.outputs[0], out.inputs["Surface"])
el = ramp.color_ramp.elements
el[0].position, el[0].color = 0.0, (0.16, 0.14, 0.12, 1)      # ziemia
el[1].position, el[1].color = 0.5, (0.30, 0.45, 0.75, 1)      # niebo
e = el.new(0.04); e.color = (0.85, 0.85, 0.82, 1)               # jasny horyzont
bg.inputs["Strength"].default_value = 1.0
try:
    sc.view_settings.view_transform = "AgX"
except TypeError:
    pass
print("oswietlenie: slonce + niebo z gradientem")
