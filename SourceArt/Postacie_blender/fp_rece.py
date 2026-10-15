# Rece FPP (przedramiona w rekawach plaszcza Kruka) + Vis wbudowany w rig. Wszystko w ukladzie kamery:
# kamera w (0,0,0), patrzy w -Y, gora +Z, prawa reka = -X. Wymaga ob_lib, ob_p2, ob_v2 (build_vis), ch_lib.
S16, C16 = math.sin(math.radians(16)), math.cos(math.radians(16))
GC = Vector((0, 0.012, -0.012))            # srodek chwytu (uklad pistoletu)
OS = Vector((0, -S16, C16))                # os chwytu w gore
WR = GC + Vector((0, 0.05, -0.055))        # nadgarstek prawy (uklad pistoletu)
WL = Vector((0.055, 0.035, -0.08))         # nadgarstek lewy przy podpieraniu
PORT = Vector((-0.016, -0.025, 0.066))     # okno wyrzutowe
PIV_K = Vector((0, 0.037, 0.062))          # os kurka
S_R, S_L = Vector((-0.2, 0.1, -0.36)), Vector((0.24, 0.08, -0.4))
L1, L2 = 0.29, 0.26
POLE_R, POLE_L = Vector((-0.5, -0.2, -0.7)), Vector((0.5, -0.2, -0.7))


def M4(loc=(0, 0, 0), rot=(0, 0, 0)):
    return T4(loc) @ R(*rot).to_4x4()


W_P0 = M4((-0.11, -0.40, -0.125), (3, 0, 7))
W_ADS = M4((0, -0.36, -0.072))
W_TILT = M4((-0.03, -0.32, -0.04), (-40, 38, 6))
W_RACK = M4((-0.085, -0.40, -0.16), (-8, 25, 12))
BELT = Vector((0.16, -0.08, -0.72))


def ik(S, T, pole):
    d = T - S
    dl = min(d.length, L1 + L2 - 1e-4)
    n = d.normalized()
    x = (L1 * L1 - L2 * L2 + dl * dl) / (2 * dl)
    h = math.sqrt(max(L1 * L1 - x * x, 0))
    p = pole - S
    p = (p - n * p.dot(n)).normalized()
    return S + n * x + p * h, S + n * dl


def dlon_skin(C, strona):
    """Organiczna dlon (Skin + Subsurf) owinieta wokol chwytu, w ukladzie pistoletu."""
    V, E, Rr = [], [], []

    def add(p, r, par=None):
        V.append(Vector(p)); Rr.append(r)
        if par is not None:
            E.append((par, len(V) - 1))
        return len(V) - 1
    if strona == "P":
        w = add(WR, 0.024)
        palm = add(GC + Vector((-0.022, 0.03, -0.008)), 0.024, w)
        for o in (-0.005, -0.027, -0.049):
            b0 = add(GC + OS * o + Vector((-0.022, -0.018, 0)), 0.0095, palm)
            b1 = add(GC + OS * o + Vector((-0.006, -0.037, 0)), 0.009, b0)
            add(GC + OS * o + Vector((0.017, -0.031, 0)), 0.0082, b1)
        i0 = add(Vector((-0.02, 0.0, 0.018)), 0.0095, palm)
        i1 = add(Vector((-0.016, -0.028, 0.018)), 0.0088, i0)
        add(Vector((-0.007, -0.045, 0.015)), 0.0082, i1)
        t0 = add(Vector((0.012, 0.03, 0.02)), 0.012, palm)
        t1 = add(Vector((0.022, 0.0, 0.028)), 0.0098, t0)
        add(Vector((0.022, -0.025, 0.03)), 0.0088, t1)
    else:
        w = add(WL, 0.024)
        palm = add(GC + Vector((0.04, -0.005, -0.022)), 0.024, w)
        for o in (-0.012, -0.032, -0.052):
            b0 = add(GC + OS * o + Vector((0.03, -0.036, 0)), 0.0095, palm)
            b1 = add(GC + OS * o + Vector((0.005, -0.052, 0)), 0.009, b0)
            add(GC + OS * o + Vector((-0.02, -0.047, 0)), 0.0082, b1)
        t0 = add(GC + Vector((0.035, 0.0, 0.012)), 0.012, palm)
        t1 = add(Vector((0.026, -0.03, 0.026)), 0.0098, t0)
        add(Vector((0.024, -0.055, 0.028)), 0.0088, t1)
    o = skin_mesh("dlon_" + strona, C, V, E, Rr, poziom=2, root=0)
    bm = bmesh.new(); bm.from_mesh(o.data); _uv_box(bm, "skora_cialo"); bm.to_mesh(o.data); bm.free()
    o.data.materials.clear(); o.data.materials.append(atlas_mat())
    return o


def zbuduj_fp():
    build_vis()                                  # aktualny Vis (z ob_v2)
    C = new_collection("FP_Rece")
    Wr0 = W_P0 @ WR
    Wl0 = W_P0 @ WL
    Er0, _ = ik(S_R, Wr0, POLE_R)
    El0, _ = ik(S_L, Wl0, POLE_L)
    q = W_P0.to_3x3()
    B = {"Root": (Vector((0, 0, 0)), Vector((0, 0.05, 0)), None),
         "Ramie_P": (S_R, Er0, "Root"), "Przedramie_P": (Er0, Wr0, "Ramie_P"),
         "Dlon_P": (Wr0, Wr0 + q @ Vector((0, -0.08, 0.03)), "Przedramie_P"),
         "Bron": (W_P0 @ Vector((0, 0, 0)), W_P0 @ Vector((0, 0, 0.04)), "Dlon_P"),
         "Zamek": (W_P0 @ Vector((0, 0.0, 0.06)), W_P0 @ Vector((0, 0.0, 0.09)), "Bron"),
         "Kurek": (W_P0 @ PIV_K, W_P0 @ (PIV_K + Vector((0, 0, 0.02))), "Bron"),
         "Spust": (W_P0 @ Vector((0, -0.04, 0.016)), W_P0 @ Vector((0, -0.04, 0.03)), "Bron"),
         "Magazynek": (W_P0 @ GC, W_P0 @ (GC + OS * 0.04), "Bron"),
         "Lufa": (W_P0 @ Vector((0, -0.172, 0.065)), W_P0 @ Vector((0, -0.2, 0.065)), "Bron"),
         "Wyrzut": (W_P0 @ PORT, W_P0 @ (PORT + Vector((-0.02, 0, 0))), "Bron"),
         "Ramie_L": (S_L, El0, "Root"), "Przedramie_L": (El0, Wl0, "Ramie_L"),
         "Dlon_L": (Wl0, Wl0 + q @ Vector((-0.03, -0.075, 0.01)), "Przedramie_L"),
         "Magazynek_Nowy": (W_P0 @ GC, W_P0 @ (GC + OS * 0.04), "Dlon_L"),
         "Luska": (W_P0 @ PORT, W_P0 @ (PORT + Vector((0, 0, 0.02))), "Root")}
    arm = zrob_armature("FP_Rig", C, B)
    parts = []

    def kopia(nazwa, kosc):
        src = bpy.data.collections["OB_Pistolet_Vis"].all_objects
        o = next(x for x in src if x.name.split(".")[0] == nazwa)
        me = o.data.copy()
        me.transform(W_P0 @ o.matrix_world)
        ob = bpy.data.objects.new(nazwa + "_fp", me)
        C.objects.link(ob)
        parts.append((ob, kosc))
    bpy.context.view_layer.update()
    for n, k in (("Vis", "Bron"), ("Zamek", "Zamek"), ("Kurek", "Kurek"), ("Spust", "Spust"), ("Magazynek", "Magazynek"),
                 ("Magazynek", "Magazynek_Nowy")):
        kopia(n, k)
    # dlonie (modelowane w ukladzie pistoletu, potem przeniesione W_P0)
    skin = "skora_cialo"
    rp = [dlon_skin(C, "P")]
    lp = [dlon_skin(C, "L")]
    for o in rp + lp:
        o.data.transform(W_P0)
    parts += [(o, "Dlon_P") for o in rp] + [(o, "Dlon_L") for o in lp]
    # rekawy plaszcza, mankiety koszuli, nadgarstki
    for S, E, W, sfx in ((S_R, Er0, Wr0, "P"), (S_L, El0, Wl0, "L")):
        parts.append((rod("rekaw_g", S, E, 0.06, "plaszcz_brazowy", C, seg=12), "Ramie_" + sfx))
        d = (W - E).normalized()
        parts.append((rod("rekaw_d", E, W - d * 0.03, 0.052, "plaszcz_brazowy", C, seg=12), "Przedramie_" + sfx))
        parts.append((rod("mankiet", W - d * 0.045, W - d * 0.01, 0.045, "koszula", C, seg=12), "Przedramie_" + sfx))
        parts.append((rod("nadgarstek", W - d * 0.02, W + d * 0.03, 0.03, skin, C, seg=10), "Przedramie_" + sfx))
    lu = [cyl("luska", 0.0046, 0.019, (0, 0, 0), "mosiadz", C, seg=10), cyl("rant", 0.0049, 0.0015, (0, 0, -0.009), "mosiadz", C, seg=10)]
    lus = join(lu, "luska")
    lus.data.transform(T4(W_P0 @ PORT) @ (q @ R(0, 90, 0)).to_4x4())
    parts.append((lus, "Luska"))
    objs = []
    for o, k in parts:
        _grupa(o, k); objs.append(o)
    bpy.context.view_layer.update()
    with bpy.context.temp_override(active_object=objs[0], selected_editable_objects=objs, object=objs[0]):
        bpy.ops.object.join()
    mesh = objs[0]
    mesh.name = mesh.data.name = "FP_Rece_Mesh"
    mesh.parent = arm
    mesh.modifiers.new("Armature", "ARMATURE").object = arm
    return arm


# ------------------------------------------------------------------ stan -> poza
def _blend(A, B_, u):
    la, qa, sa = A.decompose(); lb, qb, sb = B_.decompose()
    return Matrix.LocRotScale(la.lerp(lb, u), qa.slerp(qb, u), sa.lerp(sb, u))


def G_lewa(spec, Wp):
    kind, val = spec
    if kind == "A":                         # wzgledem pistoletu
        return Wp @ val @ W_P0.inverted()
    return val                              # bezwzgledny operator


def A_mag(d):
    return T4(-OS * d)


P0_L = GC + Vector((0.042, -0.005, -0.02))


def A_rack(y, z=0.0):
    return T4(Vector((0.004, 0.03 + y, 0.095 + z))) @ R(0, -90, 0).to_4x4() @ T4(-P0_L)


def G_belt():
    Wl0 = W_P0 @ WL
    return T4(BELT) @ R(40, 0, 20).to_4x4() @ T4(-Wl0)


def poza_fp(arm, st):
    Wp = st["Wp"]
    Gp = Wp @ W_P0.inverted()
    Gl = st["Gl"]
    bones = arm.data.bones
    ml = {b.name: b.matrix_local for b in bones}
    posed = {}
    for side, G, S, pole in (("P", Gp, S_R, POLE_R), ("L", Gl, S_L, POLE_L)):
        w0 = bones["Przedramie_" + side].tail_local
        tgt = G @ w0
        E, Wt = ik(S, tgt, pole)
        for n, a0, a1, b0 in (("Ramie_" + side, S, E, bones["Ramie_" + side]), ("Przedramie_" + side, E, Wt, bones["Przedramie_" + side])):
            rest_dir = (b0.tail_local - b0.head_local).normalized()
            q = rest_dir.rotation_difference((a1 - a0).normalized())
            posed[n] = T4(a0) @ q.to_matrix().to_4x4() @ ml[n].to_3x3().to_4x4()
    posed["Dlon_P"] = Gp @ ml["Dlon_P"]
    posed["Bron"] = Gp @ ml["Bron"]
    for n in ("Lufa", "Wyrzut"):
        posed[n] = Gp @ ml[n]
    inv = W_P0.inverted()
    posed["Zamek"] = Wp @ T4((0, st["zamek"], 0)) @ inv @ ml["Zamek"]
    posed["Kurek"] = Wp @ T4(PIV_K) @ R(st["kurek"], 0, 0).to_4x4() @ T4(-PIV_K) @ inv @ ml["Kurek"]
    posed["Spust"] = Wp @ T4((0, st["spust"], 0)) @ inv @ ml["Spust"]
    s = max(st["mag_s"], 0.001)
    posed["Magazynek"] = Wp @ T4(-OS * st["mag_d"]) @ T4(GC) @ Matrix.Diagonal((s, s, s, 1)) @ T4(-GC) @ inv @ ml["Magazynek"]
    posed["Dlon_L"] = Gl @ ml["Dlon_L"]
    ns = max(st["nmag_s"], 0.001)
    cw = W_P0 @ GC
    posed["Magazynek_Nowy"] = Gl @ T4(cw) @ Matrix.Diagonal((ns, ns, ns, 1)) @ T4(-cw) @ ml["Magazynek_Nowy"]
    posed["Luska"] = st["luska"] @ ml["Luska"]
    ustaw_poze(arm, posed)


def luska_op(t, t0, Wp_strzal):
    """Operator dla luski: wyrzut w prawo-gore-tyl, obrot, potem ukryta."""
    p0 = W_P0 @ PORT
    if t < t0 or t > t0 + 0.42:
        return T4(p0) @ Matrix.Diagonal((0.001, 0.001, 0.001, 1)) @ T4(-p0)
    dt = t - t0
    start = Wp_strzal @ PORT
    v = Vector((-1.3, 0.35, 1.1)); gr = Vector((0, 0, -9.81))
    pos = start + v * dt + gr * (0.5 * dt * dt)
    rot = R(dt * 1900, dt * 700, 0).to_4x4()
    return T4(pos) @ rot @ (Wp_strzal @ W_P0.inverted()).to_3x3().to_4x4() @ T4(-p0)


def baza(Wp, t=0.0):
    return {"Wp": Wp, "Gl": Wp @ W_P0.inverted(), "zamek": 0.0, "kurek": 0.0, "spust": 0.0,
            "mag_d": 0.0, "mag_s": 1.0, "nmag_s": 0.0, "luska": luska_op(-1, 0, Wp)}


def piecz(arm, nazwa, czas, fn):
    nowa_akcja(arm, nazwa)
    n = int(round(czas * FPS))
    for f in range(n + 1):
        poza_fp(arm, fn(f / FPS))
        klucz(arm, f + 1)
    arm.animation_data.action.frame_range = (1, n + 1)


def kick(t, a=0.035, b=0.25):
    if t < a:
        return sm(t / a)
    return 1 - sm(min(1, (t - a) / (b - a)))


def strzal_fn(Wbase, sila):
    def fn(t):
        k = kick(t)
        Wp = Wbase @ T4(GC) @ R(-10 * k * sila, 0, 0).to_4x4() @ T4(-GC) @ T4((0, 0.03 * k * sila, 0.006 * k))
        st = baza(Wp)
        st["zamek"] = 0.035 * kick(t, 0.035, 0.12)
        st["kurek"] = 45 * (sm(min(1, t / 0.012)) if t < 0.012 else 1 - sm(min(1, (t - 0.012) / 0.038)))
        st["spust"] = 0.005 * kick(t, 0.015, 0.15)
        st["luska"] = luska_op(t, 0.03, Wbase)
        return st
    return fn


def _kanal(keys, t, krok=False):
    i = 0
    while i < len(keys) - 1 and keys[i + 1][0] <= t:
        i += 1
    if krok or i == len(keys) - 1:
        return keys[i][1], keys[i][1], 0.0
    (t0, v0), (t1, v1) = keys[i], keys[i + 1]
    return v0, v1, sm((t - t0) / (t1 - t0))


def przeladowanie_fn():
    Wp_k = [(0, W_P0), (0.22, W_TILT), (1.4, W_TILT), (1.55, W_RACK), (1.78, W_RACK), (2.05, W_P0), (2.2, W_P0)]
    I = Matrix.Identity(4)
    Gl_k = [(0, ("A", I)), (0.22, ("A", T4((0.03, 0.0, -0.035)))), (0.62, ("W", G_belt())), (0.72, ("W", G_belt())),
            (1.05, ("A", A_mag(0.1))), (1.15, ("A", A_mag(0.08))), (1.35, ("A", A_mag(0.0))), (1.42, ("A", I)),
            (1.55, ("A", A_rack(0.0))), (1.72, ("A", A_rack(0.045))), (1.78, ("A", A_rack(0.07, 0.02))),
            (2.05, ("A", I)), (2.2, ("A", I))]
    zam_k = [(0, 0.0), (1.55, 0.0), (1.72, 0.045), (1.755, 0.0), (2.2, 0.0)]
    kur_k = [(0, 0.0), (1.55, 0.0), (1.72, -12.0), (1.8, 0.0), (2.2, 0.0)]
    mag_k = [(0, 0.0), (0.25, 0.0), (0.3, 0.015), (0.55, 0.35), (1.36, 0.0), (2.2, 0.0)]
    mags_k = [(0, 1.0), (0.56, 0.0), (1.36, 1.0)]
    nmag_k = [(0, 0.0), (0.66, 1.0), (1.36, 0.0)]

    def fn(t):
        a, b, u = _kanal(Wp_k, t)
        Wp = _blend(a, b, u)
        st = baza(Wp)
        a, b, u = _kanal(Gl_k, t)
        st["Gl"] = _blend(G_lewa(a, Wp), G_lewa(b, Wp), u)
        for key, ks in (("zamek", zam_k), ("kurek", kur_k)):
            a, b, u = _kanal(ks, t)
            st[key] = a + (b - a) * u
        a, b, u = _kanal(mag_k, t)
        st["mag_d"] = a + (b - a) * (u if t > 0.3 else u)
        if t > 1.36:
            st["mag_d"] = 0.0
        st["mag_s"] = _kanal(mags_k, t, True)[0]
        st["nmag_s"] = _kanal(nmag_k, t, True)[0]
        return st
    return fn


def zrob_klipy_fp(arm):
    piecz(arm, "FP_Idle", 3.0, lambda t: baza(W_P0 @ T4((0.003 * math.sin(2 * math.pi * t / 3), 0, 0.004 * math.sin(4 * math.pi * t / 3)))
                                              @ R(0.6 * math.sin(2 * math.pi * t / 3), 0, 0).to_4x4()))
    piecz(arm, "FP_Chod", 1.0, lambda t: baza(T4((0.012 * math.sin(2 * math.pi * t), 0, -0.01 * abs(math.sin(2 * math.pi * t))))
                                              @ W_P0 @ R(0, 1.5 * math.sin(2 * math.pi * t), 0).to_4x4()))
    piecz(arm, "FP_Celowanie", 2.0, lambda t: baza(W_ADS @ T4((0.001 * math.sin(math.pi * t), 0, 0.001 * math.sin(2 * math.pi * t)))))
    piecz(arm, "FP_Strzal", 0.5, strzal_fn(W_P0, 1.0))
    piecz(arm, "FP_Strzal_Cel", 0.5, strzal_fn(W_ADS, 0.6))
    piecz(arm, "FP_Przeladowanie", 2.2, przeladowanie_fn())
    arm.animation_data.action = None
    for pb in arm.pose.bones:
        pb.matrix_basis = Matrix.Identity(4)
