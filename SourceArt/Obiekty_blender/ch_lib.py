# Postacie (rola C) — szkielet Humanoid (nazwy kosci wg Unity), cialo low poly z ubraniami,
# pozy liczone w przestrzeni armatury i animacje. Wymaga ob_lib.py (+ ob_p2.py: rod).
# Konwencja: przod postaci = -Y, lewa strona postaci = +X, gora = +Z. T-poza w spoczynku.
import os
import numpy as np
from mathutils import Euler, Quaternion

FPS = 30
DIR_POST = os.path.join(os.path.dirname(ROOT), "_Postacie")
DIR_POST_MOD = os.path.join(DIR_POST, "Modele")
os.makedirs(DIR_POST_MOD, exist_ok=True)
os.makedirs(os.path.join(DIR_POST, "Materialy"), exist_ok=True)


def R(x=0, y=0, z=0):
    """Obrot (stopnie) wokol osi armatury, kolejnosc XYZ, jako Matrix 3x3."""
    return Euler((math.radians(x), math.radians(y), math.radians(z)), "XYZ").to_matrix()


def T4(v):
    return Matrix.Translation(Vector(v))


# ============================================================ szkielet
def kosci_czlowieka(s=1.0, kobieta=False):
    sh = 0.155 if kobieta else 0.17
    hx = 0.09 if kobieta else 0.10
    B = {}

    def add(n, h, t, p):
        B[n] = (Vector(h) * s, Vector(t) * s, p)
    add("Hips", (0, 0, 0.95), (0, 0, 1.05), None)
    add("Spine", (0, 0, 1.05), (0, 0, 1.22), "Hips")
    add("Chest", (0, 0, 1.22), (0, 0, 1.45), "Spine")
    add("Neck", (0, 0, 1.47), (0, 0, 1.58), "Chest")
    add("Head", (0, 0, 1.58), (0, 0, 1.8), "Neck")
    for side, sx in (("Left", 1), ("Right", -1)):
        add(side + "Shoulder", (sx * 0.03, 0, 1.43), (sx * sh, 0, 1.43), "Chest")
        add(side + "UpperArm", (sx * (sh + 0.01), 0, 1.43), (sx * (sh + 0.29), 0, 1.43), side + "Shoulder")
        add(side + "LowerArm", (sx * (sh + 0.29), 0, 1.43), (sx * (sh + 0.54), 0, 1.43), side + "UpperArm")
        add(side + "Hand", (sx * (sh + 0.54), 0, 1.43), (sx * (sh + 0.63), 0, 1.43), side + "LowerArm")
        add(side + "UpperLeg", (sx * hx, 0, 0.92), (sx * hx, 0, 0.5), "Hips")
        add(side + "LowerLeg", (sx * hx, 0, 0.5), (sx * hx, 0, 0.09), side + "UpperLeg")
        add(side + "Foot", (sx * hx, 0, 0.09), (sx * hx, -0.12, 0.025), side + "LowerLeg")
        add(side + "Toes", (sx * hx, -0.12, 0.025), (sx * hx, -0.19, 0.025), side + "Foot")
    return B


def zrob_armature(name, coll, B):
    ad = bpy.data.armatures.new(name)
    arm = bpy.data.objects.new(name, ad)
    coll.objects.link(arm)
    bpy.context.view_layer.update()
    prev = bpy.context.view_layer.objects.active
    bpy.context.view_layer.objects.active = arm
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    arm.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    for n, (h, t, p) in B.items():
        eb = ad.edit_bones.new(n)
        eb.head, eb.tail = h, t
        eb.roll = 0.0
        if p:
            eb.parent = ad.edit_bones[p]
            eb.use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")
    arm.select_set(False)
    if prev:
        bpy.context.view_layer.objects.active = prev
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"
    return arm


def kolejnosc(arm):
    out = []

    def rek(b):
        out.append(b)
        for c in b.children:
            rek(c)
    for b in arm.data.bones:
        if b.parent is None:
            rek(b)
    return out


# ============================================================ pozy
def ustaw_poze(arm, posed):
    """posed: nazwa -> docelowa macierz 4x4 kosci w przestrzeni armatury. Brakujace koscie jada z rodzicem."""
    full = {}
    for b in kolejnosc(arm):
        if b.name in posed:
            full[b.name] = posed[b.name]
        elif b.parent:
            full[b.name] = full[b.parent.name] @ (b.parent.matrix_local.inverted() @ b.matrix_local)
        else:
            full[b.name] = b.matrix_local.copy()
    for b in kolejnosc(arm):
        pb = arm.pose.bones[b.name]
        if b.parent:
            rest_rel = b.parent.matrix_local.inverted() @ b.matrix_local
            basis = (full[b.parent.name] @ rest_rel).inverted() @ full[b.name]
        else:
            basis = b.matrix_local.inverted() @ full[b.name]
        pb.matrix_basis = basis
    return full


def rozwiaz(arm, poza):
    """poza: {'a': bezwzgledne, 'w': w osiach swiata po rodzicu, 'r': lokalne, 'hips': przesuniecie}.
    Zwraca (slownik kosc -> Quaternion obrotu bezwzglednego, hips)."""
    a, w, r = poza.get("a", {}), poza.get("w", {}), poza.get("r", {})
    absR = {}
    for b in kolejnosc(arm):
        n = b.name
        par = absR.get(b.parent.name) if b.parent else None
        if n in a:
            Rb = R(*a[n])
        elif par is not None and n in w:
            Rb = R(*w[n]) @ par
        elif par is not None and n in r:
            Rb = par @ R(*r[n])
        else:
            Rb = par.copy() if par is not None else R()
        absR[n] = Rb
    return {n: m.to_quaternion() for n, m in absR.items()}, Vector(poza.get("hips", (0, 0, 0)))


def zastosuj(arm, absQ, hips):
    posed = {}
    for b in kolejnosc(arm):
        n = b.name
        if b.parent:
            rest_rel = b.parent.matrix_local.inverted() @ b.matrix_local
            head = (posed[b.parent.name] @ rest_rel).translation
        else:
            head = b.matrix_local.translation + hips
        posed[n] = T4(head) @ absQ[n].to_matrix().to_4x4() @ b.matrix_local.to_3x3().to_4x4()
    return ustaw_poze(arm, posed)


def poza_czlowieka(arm, a=None, w=None, r=None, hips=(0, 0, 0)):
    q, h = rozwiaz(arm, {"a": a or {}, "w": w or {}, "r": r or {}, "hips": hips})
    return zastosuj(arm, q, h)


_ostatnie_q = {}


def klucz(arm, frame):
    for pb in arm.pose.bones:
        loc, q, sc = pb.matrix_basis.decompose()
        prev = _ostatnie_q.get((arm.name, pb.name))
        if prev is not None and prev.dot(q) < 0:
            q.negate()
        _ostatnie_q[(arm.name, pb.name)] = q.copy()
        pb.location, pb.rotation_quaternion, pb.scale = loc, q, sc
        pb.keyframe_insert("location", frame=frame)
        pb.keyframe_insert("rotation_quaternion", frame=frame)
        pb.keyframe_insert("scale", frame=frame)


def nowa_akcja(arm, nazwa):
    old = bpy.data.actions.get(nazwa)
    if old:
        bpy.data.actions.remove(old)
    act = bpy.data.actions.new(nazwa)
    act.use_fake_user = True
    if arm.animation_data is None:
        arm.animation_data_create()
    arm.animation_data.action = act
    _ostatnie_q.clear()
    return act


def sm(t):
    return t * t * (3 - 2 * t)


def klip(arm, nazwa, klucze_, petla=False, gladko=True):
    """klucze_: lista (czas_s, poza). Kazda poza rozwiazana do pelnych obrotow, potem slerp; pieczemy kazda klatke."""
    nowa_akcja(arm, nazwa)
    rozw = [(t, rozwiaz(arm, p)) for t, p in klucze_]
    t_end = rozw[-1][0]
    n = int(round(t_end * FPS))
    for f in range(n + 1):
        t = f / FPS
        i = 0
        while i < len(rozw) - 2 and rozw[i + 1][0] <= t:
            i += 1
        (t0, (q0, h0)), (t1, (q1, h1)) = rozw[i], rozw[i + 1]
        u = 0 if t1 == t0 else min(1, max(0, (t - t0) / (t1 - t0)))
        u = sm(u) if gladko else u
        q = {k: q0[k].slerp(q1[k], u) for k in q0}
        zastosuj(arm, q, h0.lerp(h1, u))
        klucz(arm, f + 1)
    act = arm.animation_data.action
    act.frame_range = (1, n + 1)
    return act


# ============================================================ cialo
def _grupa(o, kosc):
    vg = o.vertex_groups.new(name=kosc)
    vg.add(list(range(len(o.data.vertices))), 1.0, "REPLACE")
    return o


def wlosy(C, s, fryzura, tile, glowa_z):
    P = []
    if fryzura == "lyse" or tile is None:
        return P
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=14, v_segments=9, radius=0.112 * s)
    bmesh.ops.scale(bm, vec=(0.95, 1.0, 1.05), verts=bm.verts)
    zc = glowa_z + 0.012 * s
    usun = [v for v in bm.verts if (v.co.y < -0.02 * s and v.co.z < 0.03 * s) or v.co.z < -0.06 * s]
    bmesh.ops.delete(bm, geom=usun, context="VERTS")
    bmesh.ops.translate(bm, vec=(0, 0.01 * s, zc), verts=bm.verts)
    bm.normal_update()
    _uv_box(bm, tile)
    o = _finish("wlosy", bm, C, atlas_mat())
    for p in o.data.polygons:
        p.use_smooth = True
    P.append(o)
    if fryzura == "kok":
        P.append(sphere("kok", 0.055 * s, (0, 0.105 * s, glowa_z + 0.04 * s), tile, C, seg=10, rings=6))
    if fryzura == "dlugie":
        P.append(box("dlugie", (0.2 * s, 0.07 * s, 0.26 * s), (0, 0.075 * s, glowa_z - 0.09 * s), tile, C, bevel=0.03 * s, seg=2))
    return P


def czapka(C, s, rodzaj, tile, gz):
    P = []
    if rodzaj is None:
        return P
    top = gz + 0.1 * s
    if rodzaj == "kapelusz":
        P.append(cyl("rondo", 0.165 * s, 0.012 * s, (0, 0, top - 0.025 * s), tile, C, seg=20))
        P.append(cyl("glowka", 0.1 * s, 0.11 * s, (0, 0, top + 0.03 * s), tile, C, seg=16, r2=0.085 * s, bevel=0.01 * s))
        P.append(cyl("tasma", 0.102 * s, 0.022 * s, (0, 0, top - 0.01 * s), "plaszcz_czarny", C, seg=16))
    elif rodzaj == "furazerka":
        P.append(box("furazerka", (0.13 * s, 0.25 * s, 0.07 * s), (0.01 * s, 0, top), tile, C, rot=(0, 8, 0), bevel=0.02 * s, seg=2))
    elif rodzaj in ("oficerska", "kolejarz"):
        P.append(cyl("otok", 0.105 * s, 0.05 * s, (0, 0, top - 0.02 * s), tile if rodzaj == "kolejarz" else "plaszcz_czarny", C, seg=16))
        P.append(cyl("denko", 0.13 * s if rodzaj == "oficerska" else 0.115 * s, 0.04 * s, (0, 0.01 * s, top + 0.025 * s), tile, C, seg=16, r2=0.11 * s, bevel=0.01 * s))
        P.append(box("daszek", (0.15 * s, 0.07 * s, 0.01 * s), (0, -0.115 * s, top - 0.04 * s), "lakier_czarny", C, rot=(-20, 0, 0), bevel=0.004))
    elif rodzaj == "czako":
        P.append(cyl("czako", 0.1 * s, 0.17 * s, (0, 0, top + 0.04 * s), tile, C, seg=16, r2=0.096 * s, bevel=0.008 * s))
        P.append(box("daszek", (0.15 * s, 0.07 * s, 0.01 * s), (0, -0.115 * s, top - 0.04 * s), "lakier_czarny", C, rot=(-20, 0, 0)))
    elif rodzaj == "kaszkiet":
        P.append(sphere("kaszkiet", 0.12 * s, (0, 0.0, top - 0.005 * s), tile, C, seg=14, rings=6, scale=(1, 1.1, 0.35)))
        P.append(box("daszek", (0.15 * s, 0.07 * s, 0.012 * s), (0, -0.13 * s, top - 0.025 * s), tile, C, rot=(-12, 0, 0), bevel=0.01 * s))
    return P


def cialo(C, B, st, s):
    """Zwraca liste (obiekt, kosc). Wszystko w T-pozie, w przestrzeni armatury."""
    K = st.get("plec") == "K"
    skin = "skora_cialo"
    tul = st.get("tulow", "garnitur")
    rek = st.get("rekawy", tul)
    spo = st.get("spodnie", "garnitur")
    but = st.get("buty", "skora_czarna")
    pl, plk = st.get("plaszcz"), st.get("plaszcz_kol", "plaszcz_brazowy")
    if pl:
        tul_vis, rek = plk, plk
    else:
        tul_vis = tul
    out = []

    def add(objs, kosc):
        for o in (objs if isinstance(objs, list) else [objs]):
            out.append((o, kosc))
    hz = lambda n: B[n][0].z
    # miednica / spodnica / pola plaszcza
    w = 0.165 * s if not K else 0.17 * s
    m = cyl("miednica", w, 0.2 * s, (0, 0, 0.95 * s), spo if not K else st.get("spodnica", "sukienka_niebieska"), C, seg=12)
    m.data.transform(Matrix.Diagonal((1, 0.68, 1, 1)) @ Matrix.Translation((0, 0, 0)))
    add(m, "Hips")
    if st.get("pas"):
        add(torus("pas", w + 0.005 * s, 0.018 * s, (0, 0, 1.02 * s), st["pas"], C, seg=16, rseg=4), "Hips")
        for o, k in out[-1:]:
            o.data.transform(Matrix.Translation((0, 0, 0)))
    if K:
        sp = cyl("spodnica", 0.29 * s, 0.5 * s, (0, 0, 0.75 * s), st.get("spodnica", "sukienka_niebieska"), C, seg=16, r2=0.17 * s)
        sp.data.transform(Matrix.Translation((0, 0, 0)))
        add(sp, "Hips")
    if pl:
        dl = 0.5 if pl == "dlugi" else 0.22
        pole = cyl("pola", 0.26 * s, dl * s, (0, 0, (1.0 - dl / 2) * s), plk, C, seg=16, r2=0.2 * s)
        add(pole, "Hips")
    # tulow
    br = cyl("brzuch", 0.165 * s, 0.22 * s, (0, 0, 1.135 * s), tul_vis, C, seg=12, r2=0.17 * s)
    br.data.transform(Matrix.Translation((0, 0, -1.135 * s)) )
    br.data.transform(Matrix.Diagonal((1, 0.68 if not pl else 0.75, 1, 1)))
    br.data.transform(Matrix.Translation((0, 0, 1.135 * s)))
    add(br, "Spine")
    kl = cyl("klatka", 0.17 * s, 0.24 * s, (0, 0, 1.33 * s), tul_vis, C, seg=12, r2=(0.19 if not K else 0.17) * s)
    kl.data.transform(Matrix.Translation((0, 0, -1.33 * s)))
    kl.data.transform(Matrix.Diagonal((1, 0.64 if not pl else 0.7, 1, 1)))
    kl.data.transform(Matrix.Translation((0, 0, 1.33 * s)))
    add(kl, "Chest")
    sh = abs(B["LeftUpperArm"][0].x)
    bark = cyl("barki", 0.065 * s, 2 * sh, (0, 0, 1.42 * s), tul_vis, C, rot=(0, 90, 0), seg=10)
    add(bark, "Chest")
    if not pl and st.get("kamizelka"):
        kz = cyl("kamizelka", 0.172 * s, 0.34 * s, (0, 0, 1.22 * s), st["kamizelka"], C, seg=12, r2=0.18 * s)
        kz.data.transform(Matrix.Translation((0, 0, -1.22 * s)))
        kz.data.transform(Matrix.Diagonal((1, 0.66, 1, 1)))
        kz.data.transform(Matrix.Translation((0, 0, 1.22 * s)))
        add(kz, "Chest")
    if st.get("krawat"):
        add(box("krawat", (0.045 * s, 0.01 * s, 0.24 * s), (0, -0.125 * s, 1.3 * s), st["krawat"], C, bevel=0.005), "Chest")
    if st.get("mucha"):
        add(box("mucha", (0.08 * s, 0.015 * s, 0.03 * s), (0, -0.115 * s, 1.43 * s), st["mucha"], C, bevel=0.006), "Chest")
    if pl or st.get("kolnierz"):
        add(torus("kolnierz", 0.075 * s, 0.025 * s, (0, 0.005 * s, 1.45 * s), plk if pl else st["kolnierz"], C, seg=12, rseg=5), "Chest")
    if pl:
        for k in range(3):
            add(sphere("guzik", 0.012 * s, (0.04 * s, -0.135 * s, (1.3 - k * 0.12) * s), "skora_czarna", C, seg=6, rings=3), "Chest")
    if st.get("okrycie"):   # Honiok: przykryty plaszczem (z szacunkiem, bez makabry)
        ok = box("okrycie", (0.6 * s, 0.05 * s, 1.45 * s), (0, -0.16 * s, 1.15 * s), st["okrycie"], C, bevel=0.03 * s, seg=2)
        add(ok, "Hips")
    # szyja + glowa
    add(cyl("szyja", 0.05 * s, 0.16 * s, (0, 0, 1.52 * s), skin, C, seg=10), "Neck")
    gz = 1.7 * s
    g = sphere("glowa", 0.105 * s, (0, 0, gz), skin, C, seg=16, rings=10, scale=(0.9, 1.0, 1.12))
    add(g, "Head")
    for sx in (-1, 1):
        add(sphere("oko", 0.012 * s, (sx * 0.035 * s, -0.093 * s, gz + 0.015 * s), "oczy", C, seg=8, rings=4), "Head")
        add(box("brew", (0.035 * s, 0.01 * s, 0.008 * s), (sx * 0.035 * s, -0.098 * s, gz + 0.04 * s), st.get("wlosy", "wlosy_ciemne") or "wlosy_ciemne", C), "Head")
        add(sphere("ucho", 0.022 * s, (sx * 0.095 * s, 0.0, gz), skin, C, seg=8, rings=5, scale=(0.5, 1, 1.3)), "Head")
    add(cyl("nos", 0.016 * s, 0.035 * s, (0, -0.11 * s, gz - 0.005 * s), skin, C, rot=(90, 0, 0), seg=6, r2=0.008 * s), "Head")
    add(box("usta", (0.04 * s, 0.008 * s, 0.008 * s), (0, -0.096 * s, gz - 0.045 * s), "usta", C), "Head")
    if st.get("wasy"):
        add(box("wasy", (0.06 * s, 0.012 * s, 0.014 * s), (0, -0.1 * s, gz - 0.03 * s), st["wasy"], C, bevel=0.004), "Head")
    if st.get("okulary"):
        for sx in (-1, 1):
            add(torus("szklo", 0.02 * s, 0.003 * s, (sx * 0.035 * s, -0.103 * s, gz + 0.015 * s), "zeliwo", C, rot=(90, 0, 0), seg=10, rseg=3), "Head")
        add(box("mostek", (0.03 * s, 0.004 * s, 0.004 * s), (0, -0.103 * s, gz + 0.018 * s), "zeliwo", C), "Head")
    add(wlosy(C, s, st.get("fryzura", "krotkie"), st.get("wlosy", "wlosy_ciemne"), gz), "Head")
    add(czapka(C, s, st.get("czapka"), st.get("czapka_kol", "garnitur"), gz), "Head")
    # rece
    for side, sx in (("Left", 1), ("Right", -1)):
        h0 = B[side + "UpperArm"][0]; e0 = B[side + "LowerArm"][0]; w0 = B[side + "Hand"][0]; f0 = B[side + "Hand"][1]
        ry = 90 if sx > 0 else -90
        ln = (e0 - h0).length
        add(cyl("ramie", 0.058 * s, ln, ((h0.x + e0.x) / 2, 0, h0.z), rek, C, rot=(0, ry, 0), seg=10, r2=0.048 * s), side + "UpperArm")
        ln2 = (w0 - e0).length
        add(cyl("przedramie", 0.048 * s, ln2, ((e0.x + w0.x) / 2, 0, e0.z), rek, C, rot=(0, ry, 0), seg=10, r2=0.04 * s), side + "LowerArm")
        mank = st.get("mankiet", "koszula" if not K else None)
        if mank and not pl:
            add(cyl("mankiet", 0.041 * s, 0.03 * s, (w0.x - sx * 0.012 * s, 0, w0.z), mank, C, rot=(0, 90, 0), seg=10), side + "LowerArm")
        dl = box("dlon", (0.085 * s, 0.08 * s, 0.03 * s), ((w0.x + f0.x) / 2, -0.005 * s, w0.z), skin, C, bevel=0.01 * s, seg=2)
        kc = box("kciuk", (0.05 * s, 0.022 * s, 0.022 * s), (w0.x + sx * 0.035 * s, -0.05 * s, w0.z), skin, C, rot=(0, 0, sx * -30), bevel=0.006)
        add([dl, kc], side + "Hand")
        # nogi
        hip = B[side + "UpperLeg"][0]; kn = B[side + "LowerLeg"][0]; an = B[side + "Foot"][0]
        nogi = st.get("nogi", spo if not K else "skora_cialo")
        add(cyl("udo", 0.065 * s, (hip - kn).length, (hip.x, 0, (hip.z + kn.z) / 2), nogi, C, seg=10, r2=0.09 * s), side + "UpperLeg")
        lyd = but if st.get("buty_wysokie") else nogi
        add(cyl("lydka", 0.047 * s, (kn - an).length, (kn.x, 0, (kn.z + an.z) / 2), lyd, C, seg=10, r2=0.064 * s), side + "LowerLeg")
        if st.get("buty_wysokie"):
            add(torus("cholewa", 0.066 * s, 0.01 * s, (kn.x, 0, kn.z - 0.06 * s), but, C, seg=10, rseg=4), side + "LowerLeg")
        add(box("but", (0.1 * s, 0.26 * s, 0.085 * s), (an.x, -0.05 * s, 0.043 * s), but, C, bevel=0.025 * s, seg=2), side + "Foot")
    return out


def zloz_postac(nazwa, st):
    """Tworzy kolekcje z armatura + jednym skinowanym meshem (wagi sztywne 1.0 na kosc)."""
    C = new_collection(nazwa)
    K = st.get("plec") == "K"
    s = 0.94 if K else 1.0
    s *= st.get("skala", 1.0)
    B = kosci_czlowieka(s, K)
    arm = zrob_armature(nazwa + "_Rig", C, B)
    czesci = cialo(C, B, st, s)
    objs = []
    for o, k in czesci:
        _grupa(o, k)
        objs.append(o)
    bpy.context.view_layer.update()
    with bpy.context.temp_override(active_object=objs[0], selected_editable_objects=objs, object=objs[0]):
        bpy.ops.object.join()
    mesh = objs[0]
    mesh.name = nazwa + "_Cialo"
    mesh.data.name = nazwa + "_Cialo"
    mesh.data.materials.clear()
    mesh.data.materials.append(postac_mat())
    mesh.parent = arm
    md = mesh.modifiers.new("Armature", "ARMATURE")
    md.object = arm
    arm["plec"] = "K" if K else "M"
    return C, arm, mesh


def postac_mat():
    """Kopia materialu atlasu pod nazwa M_Postacie_Atlas (osobny folder w Unity)."""
    m = bpy.data.materials.get("M_Postacie_Atlas")
    if m:
        return m
    src = atlas_mat()
    m = src.copy()
    m.name = "M_Postacie_Atlas"
    return m


# ============================================================ animacje czlowieka
L, P_ = "Left", "Right"


def nogi_stoj():
    return {"LeftFoot": (0, 0, 0), "RightFoot": (0, 0, 0)}


def poza_stoj(oddech=0.0):
    a = {"LeftUpperArm": (0, 78, 0), "RightUpperArm": (0, -78, 0),
         "LeftLowerArm": (-8, 78, 0), "RightLowerArm": (-8, -78, 0),
         "LeftHand": (-8, 80, 0), "RightHand": (-8, -80, 0),
         "Chest": (1 + oddech, 0, 0), "Neck": (0, 0, 0), "Head": (-1, 0, 0)}
    a.update(nogi_stoj())
    return {"a": a}


def poza_krok(faza, skala=1.0, bieg=False):
    """faza 0..1 cyklu. Lewa noga w przod przy 0."""
    c = math.cos(2 * math.pi * faza); s_ = math.sin(2 * math.pi * faza)
    zam = (40 if bieg else 24) * skala
    udoL = -zam * c - (8 if bieg else 3)
    udoP = zam * c - (8 if bieg else 3)
    kolano = lambda x: max(0.0, x)
    kolL = 8 + (70 if bieg else 38) * kolano(-s_) * skala
    kolP = 8 + (70 if bieg else 38) * kolano(s_) * skala
    rek = (35 if bieg else 18) * skala
    lok = 80 if bieg else 12
    lean = 12 if bieg else 2
    a = {"Hips": (0, 0, 4 * c), "Spine": (lean * 0.5, 0, 4 * c), "Chest": (lean, 0, -3 * c), "Neck": (-lean * 0.6, 0, 0), "Head": (-lean * 0.4, 0, 0),
         "LeftUpperLeg": (udoL, 0, 0), "RightUpperLeg": (udoP, 0, 0),
         "LeftLowerLeg": (udoL + kolL, 0, 0), "RightLowerLeg": (udoP + kolP, 0, 0),
         "LeftFoot": (max(0, udoL + kolL - 10) * 0.3, 0, 0), "RightFoot": (max(0, udoP + kolP - 10) * 0.3, 0, 0),
         "LeftUpperArm": (rek * c, 76, 0), "RightUpperArm": (-rek * c, -76, 0),
         "LeftLowerArm": (rek * c - lok, 76, 0), "RightLowerArm": (-rek * c - lok, -76, 0),
         "LeftHand": (rek * c - lok, 80, 0), "RightHand": (-rek * c - lok, -80, 0)}
    bob = (0.035 if bieg else 0.022) * abs(math.sin(2 * math.pi * faza)) - (0.06 if bieg else 0.012)
    return {"a": a, "hips": (0, 0, bob)}


def poza_kucanie(faza=None):
    a = {"Spine": (22, 0, 0), "Chest": (30, 0, 0), "Neck": (-15, 0, 0), "Head": (-25, 0, 0),
         "LeftUpperArm": (-25, 70, 0), "RightUpperArm": (-25, -70, 0),
         "LeftLowerArm": (-70, 70, 0), "RightLowerArm": (-70, -70, 0),
         "LeftHand": (-70, 80, 0), "RightHand": (-70, -80, 0)}
    if faza is None:
        a.update({"LeftUpperLeg": (-62, 0, -6), "RightUpperLeg": (-58, 0, 6),
                  "LeftLowerLeg": (44, 0, 0), "RightLowerLeg": (46, 0, 0), "LeftFoot": (0, 0, 0), "RightFoot": (0, 0, 0)})
        return {"a": a, "hips": (0, 0.05, -0.36)}
    c = math.cos(2 * math.pi * faza); s_ = math.sin(2 * math.pi * faza)
    a.update({"LeftUpperLeg": (-60 - 18 * c, 0, -6), "RightUpperLeg": (-60 + 18 * c, 0, 6),
              "LeftLowerLeg": (44 + 18 * max(0, -s_), 0, 0), "RightLowerLeg": (44 + 18 * max(0, s_), 0, 0),
              "LeftFoot": (0, 0, 0), "RightFoot": (0, 0, 0)})
    return {"a": a, "hips": (0, 0.05, -0.36 + 0.015 * abs(s_))}


def poza_celowanie(odrzut=0.0):
    a = {"Chest": (2, 0, -12), "Neck": (0, 0, 8), "Head": (-2, 0, 6),
         "RightUpperArm": (-odrzut, 0, 84), "RightLowerArm": (-odrzut * 1.6, 0, 88), "RightHand": (-odrzut * 2, 90, 88),
         "LeftUpperArm": (8, 25, -55), "LeftLowerArm": (-odrzut, 20, -118), "LeftHand": (-odrzut, -60, -120)}
    a.update(nogi_stoj())
    a.update({"LeftUpperLeg": (-6, 0, 0), "RightUpperLeg": (6, 0, 0), "LeftLowerLeg": (-2, 0, 0), "RightLowerLeg": (8, 0, 0)})
    return {"a": a}


def poza_rece_gora(drz=0.0):
    a = {"LeftUpperArm": (0, -70 + drz, 0), "RightUpperArm": (0, 70 - drz, 0),
         "LeftLowerArm": (0, -95, 0), "RightLowerArm": (0, 95, 0),
         "LeftHand": (0, -95, 0), "RightHand": (0, 95, 0), "Head": (6, 0, 0), "Chest": (-2, 0, 0)}
    a.update(nogi_stoj())
    return {"a": a}


def poza_siedzi(oddech=0.0):
    a = {"Spine": (-4, 0, 0), "Chest": (-2 + oddech, 0, 0),
         "LeftUpperLeg": (-90, 0, -4), "RightUpperLeg": (-90, 0, 4), "LeftLowerLeg": (0, 0, 0), "RightLowerLeg": (0, 0, 0),
         "LeftFoot": (0, 0, 0), "RightFoot": (0, 0, 0),
         "LeftUpperArm": (-25, 75, 0), "RightUpperArm": (-25, -75, 0),
         "LeftLowerArm": (-75, 80, 0), "RightLowerArm": (-75, -80, 0),
         "LeftHand": (-80, 85, 0), "RightHand": (-80, -85, 0)}
    return {"a": a, "hips": (0, 0.06, -0.44)}


def poza_lezy(oddech=0.0):
    # na plecach: cale cialo obrocone Rx(-90), glowa w +Y
    a = {"Hips": (-90, 0, 0), "Spine": (-90, 0, 0), "Chest": (-90 + oddech, 0, 0), "Neck": (-90, 0, 0), "Head": (-90, 0, 18)}
    r = {"LeftUpperArm": (0, 72, 0), "RightUpperArm": (0, -72, 0), "LeftLowerArm": (0, 5, 0), "RightLowerArm": (0, -5, 0),
         "LeftUpperLeg": (0, 0, -4), "RightUpperLeg": (0, 0, 6)}
    return {"a": a, "r": r, "hips": (0, 0.0, -0.83)}


def zrob_klipy_czlowieka(arm):
    akcje = []
    akcje.append(klip(arm, "Idle", [(0, poza_stoj(0)), (1.0, poza_stoj(2.0)), (2.0, poza_stoj(0))]))
    kr = [(f / 8, poza_krok(f / 8)) for f in range(9)]
    akcje.append(klip(arm, "Chod", [(t * 1.0, p) for t, p in kr], gladko=False))
    akcje.append(klip(arm, "Bieg", [(t * 0.66, poza_krok(t, bieg=True)) for t in [f / 8 for f in range(9)]], gladko=False))
    akcje.append(klip(arm, "Kucanie_Idle", [(0, poza_kucanie()), (1.5, poza_kucanie()), (3.0, poza_kucanie())]))
    akcje.append(klip(arm, "Kucanie_Chod", [(t * 1.2, poza_kucanie(t)) for t in [f / 8 for f in range(9)]], gladko=False))
    akcje.append(klip(arm, "Celowanie", [(0, poza_celowanie()), (1.0, poza_celowanie(0.6)), (2.0, poza_celowanie())]))
    akcje.append(klip(arm, "Strzal", [(0, poza_celowanie()), (0.05, poza_celowanie(9)), (0.25, poza_celowanie())]))
    st = poza_stoj()
    trafiony1 = {"a": dict(st["a"], Spine=(-14, 0, 6), Chest=(-20, 0, 10), Head=(-15, 0, 0),
                           LeftUpperArm=(30, 40, 0), RightUpperArm=(30, -40, 0)), "hips": (0, 0.1, 0)}
    trafiony2 = {"a": dict(poza_kucanie()["a"], Spine=(-5, 0, 10), Chest=(-15, 0, 10), Head=(-10, 0, 0),
                           LeftUpperArm=(10, 50, 0), RightUpperArm=(10, -50, 0)), "hips": (0, 0.25, -0.4)}
    trafiony3 = {"a": {"Hips": (-60, 0, 5), "Spine": (-65, 0, 5), "Chest": (-70, 0, 5), "Neck": (-70, 0, 10), "Head": (-70, 0, 10),
                       "LeftUpperLeg": (-75, 0, 0), "RightUpperLeg": (-60, 0, 0), "LeftLowerLeg": (-20, 0, 0), "RightLowerLeg": (-10, 0, 0),
                       "LeftUpperArm": (-60, 50, 0), "RightUpperArm": (-60, -50, 0)}, "hips": (0, 0.55, -0.62)}
    lz = poza_lezy()
    lz["hips"] = (0, 0.7, -0.83)
    akcje.append(klip(arm, "Trafiony", [(0, st), (0.15, trafiony1), (0.5, trafiony2), (0.9, trafiony3), (1.3, lz), (1.6, lz)]))
    rozm = []
    for i, t in enumerate([0, 0.6, 1.2, 1.8, 2.4]):
        p = poza_stoj()
        if i % 2:
            p["a"].update({"RightUpperArm": (-25, -60, 10), "RightLowerArm": (-75, -50, 20), "RightHand": (-75, -10, 20), "Head": (4, 0, 6)})
        else:
            p["a"].update({"LeftUpperArm": (-20, 60, -10), "LeftLowerArm": (-70, 45, -25), "LeftHand": (-70, 10, -25), "Head": (-3, 0, -6)})
        rozm.append((t, p))
    rozm.append((3.0, poza_stoj()))
    akcje.append(klip(arm, "Rozmowa", rozm))
    akcje.append(klip(arm, "RecePodniesione", [(0, poza_rece_gora()), (0.5, poza_rece_gora(4)), (1.0, poza_rece_gora())]))
    akcje.append(klip(arm, "Siedzi", [(0, poza_siedzi()), (1.5, poza_siedzi(2)), (3.0, poza_siedzi())]))
    lz0 = poza_lezy(); lz1 = poza_lezy(1.5)
    akcje.append(klip(arm, "Lezy", [(0, lz0), (2.0, lz1), (4.0, lz0)]))
    spad = {"a": dict(poza_stoj()["a"], LeftUpperArm=(0, 30, 0), RightUpperArm=(0, -30, 0), LeftLowerArm=(0, 20, 0), RightLowerArm=(0, -20, 0),
                      LeftUpperLeg=(-20, 0, 0), RightUpperLeg=(-5, 0, 0), LeftLowerLeg=(10, 0, 0), RightLowerLeg=(25, 0, 0))}
    akcje.append(klip(arm, "Spadanie", [(0, spad), (0.5, spad)]))
    arm.animation_data.action = None
    for pb in arm.pose.bones:
        pb.matrix_basis = Matrix.Identity(4)
    return akcje


def eksport_postaci(kolekcja, plik, animacje=False):
    coll = bpy.data.collections[kolekcja]
    for lc in bpy.context.view_layer.layer_collection.children:
        if lc.name == kolekcja:
            lc.exclude = False
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action="DESELECT")
    arm = None
    for o in coll.all_objects:
        o.hide_set(False)
        o.select_set(True)
        if o.type == "ARMATURE":
            arm = o
    if arm:
        arm.animation_data_create()
        arm.animation_data.action = None
    path = os.path.join(DIR_POST_MOD, plik + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"ARMATURE", "MESH", "EMPTY"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z", axis_up="Y", bake_space_transform=False,
        use_mesh_modifiers=False, mesh_smooth_type="FACE", add_leaf_bones=False,
        primary_bone_axis="Y", secondary_bone_axis="X", armature_nodetype="NULL",
        use_armature_deform_only=False,
        bake_anim=animacje, bake_anim_use_all_actions=animacje, bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0, bake_anim_step=1.0,
        path_mode="STRIP")
    return path
