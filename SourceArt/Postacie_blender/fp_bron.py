# Rece FPP dla dowolnej broni. Obie dlonie sterowane niezaleznie (IK ramion), bron to osobna kosc.
# Uklad kamery: (0,0,0), patrzy w -Y, gora +Z, prawa = -X. Wymaga ob_lib, ob_p2, ob_v2, ch_lib, ch_cialo2, bronie.
L1, L2 = 0.29, 0.26
S_R0, S_L0 = Vector((-0.2, 0.1, -0.36)), Vector((0.24, 0.08, -0.4))
POLE_R0, POLE_L0 = Vector((-0.5, -0.2, -0.7)), Vector((0.5, -0.2, -0.7))
I4 = Matrix.Identity(4)


def M4(loc=(0, 0, 0), rot=(0, 0, 0)):
    return T4(loc) @ R(*rot).to_4x4()


def ik2(S, T, pole):
    d = T - S
    dl = min(d.length, L1 + L2 - 1e-4)
    n = d.normalized()
    x = (L1 * L1 - L2 * L2 + dl * dl) / (2 * dl)
    h = math.sqrt(max(L1 * L1 - x * x, 0))
    p = pole - S
    p = (p - n * p.dot(n)).normalized()
    return S + n * x + p * h, S + n * dl


def ramka(a, r=Vector((-1, 0, 0))):
    a = a.normalized()
    f = a.cross(r).normalized()
    r = f.cross(a).normalized() * -1 if False else r
    return a, f, r


# ------------------------------------------------------------------ dlonie (Skin + Subsurf)
def _dlon(C, nazwa, V, E, Rr):
    o = skin_mesh(nazwa, C, V, E, Rr, poziom=2, root=0)
    bm = bmesh.new(); bm.from_mesh(o.data); _uv_box(bm, "skora_cialo"); bm.to_mesh(o.data); bm.free()
    o.data.materials.clear(); o.data.materials.append(atlas_mat())
    for p in o.data.polygons:
        p.use_smooth = True
    return o


class _G:
    def __init__(self):
        self.V, self.E, self.R = [], [], []

    def add(self, p, r, par=None):
        self.V.append(Vector(p)); self.R.append(r)
        if par is not None:
            self.E.append((par, len(self.V) - 1))
        return len(self.V) - 1


def prawa_chwyt(gc, a, trig=None):
    """Chwyt pistoletowy / rekojesc: dlon po prawej, palce owiniete z przodu, kciuk po lewej."""
    a, f, r = ramka(a)
    g = _G()
    w = g.add(gc - a * 0.055 - f * 0.05, 0.024)
    palm = g.add(gc + r * 0.022 - f * 0.03 - a * 0.008, 0.024, w)
    offs = (-0.005, -0.027, -0.049) if trig is not None else (0.017, -0.005, -0.027, -0.049)
    for o in offs:
        b0 = g.add(gc + a * o + r * 0.022 + f * 0.018, 0.0095, palm)
        b1 = g.add(gc + a * o + r * 0.006 + f * 0.037, 0.009, b0)
        g.add(gc + a * o - r * 0.017 + f * 0.031, 0.0082, b1)
    if trig is not None:
        i0 = g.add(trig + r * 0.02 - f * 0.035, 0.0095, palm)
        i1 = g.add(trig + r * 0.016 - f * 0.01, 0.0088, i0)
        g.add(trig + r * 0.007 + f * 0.006, 0.0082, i1)
    t0 = g.add(gc + a * 0.03 - f * 0.03 - r * 0.012, 0.012, palm)
    t1 = g.add(gc + a * 0.04 - r * 0.022 + f * 0.012, 0.0098, t0)
    g.add(gc + a * 0.042 - r * 0.022 + f * 0.037, 0.0088, t1)
    return g, g.V[0]


def lewa_podparcie(gc, a):
    a, f, r = ramka(a)
    g = _G()
    w = g.add(gc - r * 0.055 - f * 0.035 - a * 0.068, 0.024)
    palm = g.add(gc - r * 0.04 + f * 0.005 - a * 0.012, 0.024, w)
    for o in (-0.012, -0.032, -0.052):
        b0 = g.add(gc + a * o - r * 0.03 + f * 0.036, 0.0095, palm)
        b1 = g.add(gc + a * o - r * 0.005 + f * 0.052, 0.009, b0)
        g.add(gc + a * o + r * 0.02 + f * 0.047, 0.0082, b1)
    t0 = g.add(gc - r * 0.035 + a * 0.012, 0.012, palm)
    t1 = g.add(gc - r * 0.026 + f * 0.03 + a * 0.026, 0.0098, t0)
    g.add(gc - r * 0.024 + f * 0.055 + a * 0.028, 0.0088, t1)
    return g, g.V[0]


def lewa_lozysko(P):
    """Lewa dlon pod lozyskiem karabinu: palce w gore po lewym boku, kciuk po prawym."""
    X, Y, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))
    g = _G()
    w = g.add(P + X * 0.03 + Y * 0.07 - Z * 0.07, 0.025)
    palm = g.add(P - Z * 0.032 + X * 0.008, 0.026, w)
    for o in (-0.03, -0.01, 0.01, 0.03):
        b0 = g.add(P + Y * o + X * 0.022 - Z * 0.03, 0.0095, palm)
        b1 = g.add(P + Y * o + X * 0.032 - Z * 0.004, 0.009, b0)
        g.add(P + Y * o + X * 0.026 + Z * 0.02, 0.0082, b1)
    t0 = g.add(P - X * 0.02 - Z * 0.028 + Y * 0.02, 0.012, palm)
    t1 = g.add(P - X * 0.03 - Z * 0.004 - Y * 0.005, 0.0098, t0)
    g.add(P - X * 0.027 + Z * 0.018 - Y * 0.02, 0.0088, t1)
    return g, g.V[0]


def lewa_luzna(P):
    """Swobodna lewa dlon (noze) — trzymana nisko, poza kadrem."""
    X, Y, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))
    g = _G()
    w = g.add(P, 0.025)
    palm = g.add(P - Y * 0.05, 0.026, w)
    for o in (-0.02, 0.0, 0.02, 0.035):
        b0 = g.add(P - Y * 0.08 + X * o, 0.0095, palm)
        g.add(P - Y * 0.11 + X * o - Z * 0.02, 0.0085, b0)
    g.add(P - Y * 0.06 - X * 0.03 + Z * 0.01, 0.011, palm)
    return g, g.V[0]


# ------------------------------------------------------------------ budowa rigu
def zbuduj_rig(spec):
    C = new_collection(spec["nazwa"])
    cz, pk = spec["czesci"](C)
    spec["pk"] = pk
    W0 = spec["W0"]
    gR, wr = spec["prawa"](pk)
    gL, wl = spec["lewa"](pk)
    Wr0 = W0 @ wr
    Wl0 = (W0 @ wl) if spec.get("lewa_na_broni", True) else wl
    S_R, S_L = spec.get("S_R", S_R0), spec.get("S_L", S_L0)
    Er0, _ = ik2(S_R, Wr0, POLE_R0)
    El0, _ = ik2(S_L, Wl0, POLE_L0)
    q = W0.to_3x3()
    B = {"Root": (Vector(), Vector((0, 0.05, 0)), None),
         "Ramie_P": (S_R, Er0, "Root"), "Przedramie_P": (Er0, Wr0, "Ramie_P"),
         "Dlon_P": (Wr0, Wr0 + q @ Vector((0, -0.08, 0.03)), "Przedramie_P"),
         "Ramie_L": (S_L, El0, "Root"), "Przedramie_L": (El0, Wl0, "Ramie_L"),
         "Dlon_L": (Wl0, Wl0 + Vector((0, -0.08, 0.02)), "Przedramie_L"),
         "Bron": (W0 @ Vector(), W0 @ Vector((0, 0, 0.04)), "Root"),
         "Luska": (W0 @ pk.get("Wyrzut", Vector()), W0 @ (pk.get("Wyrzut", Vector()) + Vector((0, 0, 0.02))), "Root")}
    for n, (o, piv) in cz.items():
        if n != "Bron":
            B[n] = (W0 @ piv, W0 @ (piv + Vector((0, 0, 0.03))), "Bron")
    for n in ("Lufa", "Wyrzut"):
        if n in pk:
            B[n] = (W0 @ pk[n], W0 @ (pk[n] + Vector((0, -0.03, 0))), "Bron")
    for n, (reka, zrodlo) in spec.get("trzymane", {}).items():
        piv = cz[zrodlo][1]
        A = spec.get("A_trzymane", {}).get(n)
        if A is not None:
            piv = A(spec).inverted() @ piv
        B[n] = (W0 @ piv, W0 @ (piv + Vector((0, 0, 0.03))), "Dlon_" + reka)
    arm = zrob_armature(spec["nazwa"] + "_Rig", C, B)
    parts = []
    bpy.context.view_layer.update()
    for n, (o, piv) in cz.items():
        o.data.transform(W0 @ o.matrix_world); o.matrix_world = I4; parts.append((o, n))
    for n, (reka, zrodlo) in spec.get("trzymane", {}).items():
        me = cz[zrodlo][0].data.copy()
        A = spec.get("A_trzymane", {}).get(n)
        if A is not None:
            me.transform(W0 @ A(spec).inverted() @ W0.inverted())
        ob = bpy.data.objects.new(n, me); C.objects.link(ob); parts.append((ob, n))
    for g, kosc, mat_ in ((gR, "Dlon_P", W0), (gL, "Dlon_L", W0 if spec.get("lewa_na_broni", True) else I4)):
        V = [mat_ @ v for v in g.V]
        parts.append((_dlon(C, "dlon_" + kosc, V, g.E, g.R), kosc))
    for S, E, W, sfx in ((S_R, Er0, Wr0, "P"), (S_L, El0, Wl0, "L")):
        d = (W - E).normalized()
        parts.append((rod("rekaw_g", S, E, 0.06, "plaszcz_brazowy", C, seg=12), "Ramie_" + sfx))
        parts.append((rod("rekaw_d", E, W - d * 0.03, 0.052, "plaszcz_brazowy", C, seg=12), "Przedramie_" + sfx))
        parts.append((rod("mankiet", W - d * 0.045, W - d * 0.01, 0.045, "koszula", C, seg=12), "Przedramie_" + sfx))
        parts.append((rod("nadgarstek", W - d * 0.02, W + d * 0.03, 0.03, "skora_cialo", C, seg=10), "Przedramie_" + sfx))
    if spec.get("luska"):
        dl, r_ = spec["luska"]
        lu = join([cyl("luska", r_, dl, (0, 0, 0), "mosiadz", C, seg=10), cyl("rant", r_ * 1.07, 0.0015, (0, 0, -dl / 2), "mosiadz", C, seg=10)], "luska")
        lu.data.transform(T4(W0 @ pk["Wyrzut"]) @ (q @ R(0, 90, 0)).to_4x4())
        parts.append((lu, "Luska"))
    objs = []
    for o, k in parts:
        _grupa(o, k); objs.append(o)
    bpy.context.view_layer.update()
    with bpy.context.temp_override(active_object=objs[0], selected_editable_objects=objs, object=objs[0]):
        bpy.ops.object.join()
    mesh = objs[0]
    mesh.name = mesh.data.name = spec["nazwa"] + "_Mesh"
    mesh.parent = arm
    mesh.modifiers.new("Armature", "ARMATURE").object = arm
    spec.update(Wr0=Wr0, Wl0=Wl0, S_R=S_R, S_L=S_L)
    arm.data.display_type = "STICK"
    return arm


# ------------------------------------------------------------------ stan -> poza
def blend(A, B_, u):
    u = min(1.0, max(0.0, u))
    la, qa, sa = A.decompose(); lb, qb, sb = B_.decompose()
    return Matrix.LocRotScale(la.lerp(lb, u), qa.slerp(qb, u), sa.lerp(sb, u))


def poza(arm, spec, st):
    W0 = spec["W0"]; W0i = W0.inverted()
    Wp = st["Wp"]
    Gw = Wp @ W0i
    Gr = st.get("Gr", Gw)
    Gl = st.get("Gl", Gw if spec.get("lewa_na_broni", True) else I4)
    b = arm.data.bones
    ml = {x.name: x.matrix_local for x in b}
    posed = {}
    for side, G, S, pole in (("P", Gr, spec["S_R"], POLE_R0), ("L", Gl, spec["S_L"], POLE_L0)):
        tgt = G @ b["Przedramie_" + side].tail_local
        E, Wt = ik2(S, tgt, pole)
        for n, a0, a1 in (("Ramie_" + side, S, E), ("Przedramie_" + side, E, Wt)):
            bb = b[n]
            q = (bb.tail_local - bb.head_local).normalized().rotation_difference((a1 - a0).normalized())
            posed[n] = T4(a0) @ q.to_matrix().to_4x4() @ ml[n].to_3x3().to_4x4()
    posed["Dlon_P"] = Gr @ ml["Dlon_P"]
    posed["Dlon_L"] = Gl @ ml["Dlon_L"]
    posed["Bron"] = Gw @ ml["Bron"]
    for x in b:
        n = x.name
        if x.parent and x.parent.name == "Bron":
            D = st.get("czesci", {}).get(n, _ukryj(spec["pk"]["gc"]) if n in spec.get("ukryte", ()) else I4)
            posed[n] = Wp @ D @ W0i @ ml[n]
        elif x.parent and x.parent.name in ("Dlon_P", "Dlon_L") and n not in posed:
            G = Gr if x.parent.name == "Dlon_P" else Gl
            s = max(st.get("trzymane", {}).get(n, 0.0), 0.001)
            c = x.head_local
            posed[n] = G @ T4(c) @ Matrix.Diagonal((s, s, s, 1)) @ T4(-c) @ ml[n]
    posed["Luska"] = st.get("luska", _ukryj(b["Luska"].head_local)) @ ml["Luska"]
    ustaw_poze(arm, posed)


def _ukryj(c):
    return T4(c) @ Matrix.Diagonal((0.001, 0.001, 0.001, 1)) @ T4(-c)


def wyrzut_luski(spec, t, t0, Wp_t0, v=Vector((-1.3, 0.35, 1.1))):
    c = spec["W0"] @ spec["pk"]["Wyrzut"]
    if t < t0 or t > t0 + 0.45:
        return _ukryj(c)
    dt = t - t0
    start = Wp_t0 @ spec["pk"]["Wyrzut"]
    pos = start + v * dt + Vector((0, 0, -9.81)) * (0.5 * dt * dt)
    return T4(pos) @ R(dt * 1900, dt * 700, 0).to_4x4() @ (Wp_t0 @ spec["W0"].inverted()).to_3x3().to_4x4() @ T4(-c)


def kanal(keys, t, krok=False):
    i = 0
    while i < len(keys) - 1 and keys[i + 1][0] <= t:
        i += 1
    if krok or i == len(keys) - 1:
        return keys[i][1], keys[i][1], 0.0
    (t0, v0), (t1, v1) = keys[i], keys[i + 1]
    return v0, v1, sm((t - t0) / (t1 - t0))


def num(keys, t, krok=False):
    a, b_, u = kanal(keys, t, krok)
    return a + (b_ - a) * u


def mat(keys, t):
    a, b_, u = kanal(keys, t)
    return blend(a, b_, u)


def G_rel(Wp, spec, A):
    """Operator dloni trzymanej wzgledem broni (A w ukladzie broni, dziala na dlon w spoczynku)."""
    return Wp @ A @ spec["W0"].inverted()


def G_swiat(spec, cel, rot, wrist0):
    return T4(cel) @ R(*rot).to_4x4() @ T4(-wrist0)


def piecz_fp(arm, spec, nazwa, czas, fn):
    nowa_akcja(arm, nazwa)
    n = int(round(czas * FPS))
    for f in range(n + 1):
        poza(arm, spec, fn(f / FPS))
        klucz(arm, f + 1)
    arm.animation_data.action.frame_range = (1, n + 1)


def kick(t, a=0.035, b=0.25):
    return sm(t / a) if t < a else 1 - sm(min(1, (t - a) / (b - a)))


# ------------------------------------------------------------------ klipy wspolne
def klipy_ruch(arm, spec, ads=True):
    W0 = spec["W0"]
    piecz_fp(arm, spec, spec["nazwa"] + "_Idle", 3.0, lambda t: {"Wp": T4((0.003 * math.sin(2 * math.pi * t / 3), 0, 0.004 * math.sin(4 * math.pi * t / 3))) @ W0 @ R(0.6 * math.sin(2 * math.pi * t / 3), 0, 0).to_4x4()})
    piecz_fp(arm, spec, spec["nazwa"] + "_Chod", 1.0, lambda t: {"Wp": T4((0.012 * math.sin(2 * math.pi * t), 0, -0.01 * abs(math.sin(2 * math.pi * t)))) @ W0 @ R(0, 1.5 * math.sin(2 * math.pi * t), 0).to_4x4()})
    if ads:
        Wa = spec["Wads"]
        piecz_fp(arm, spec, spec["nazwa"] + "_Celowanie", 2.0, lambda t: {"Wp": T4((0.001 * math.sin(math.pi * t), 0, 0.001 * math.sin(2 * math.pi * t))) @ Wa})


# ---- pistolety (zamek suwliwy albo kolano Lugera)
def D_rack(spec, u):
    pk = spec["pk"]
    if spec.get("kolano"):
        return {"Kolano": T4(spec["piv_kolano"]) @ R(-70 * u, 0, 0).to_4x4() @ T4(-spec["piv_kolano"])}
    d = {"Zamek": T4((0, spec.get("skok", 0.04) * u, 0))}
    if "piv_kurek" in spec:
        d["Kurek"] = T4(spec["piv_kurek"]) @ R(-12 * min(1, u * 2), 0, 0).to_4x4() @ T4(-spec["piv_kurek"])
    return d


def D_mag(spec, d, s):
    pk = spec["pk"]; gc, a = pk["gc"], pk["os"]
    return T4(-a * d) @ T4(gc) @ Matrix.Diagonal((max(s, 0.001),) * 3 + (1,)) @ T4(-gc)


def strzal_pistolet(spec, Wbase, sila, t):
    k = kick(t)
    gc = spec["pk"]["gc"]
    Wp = Wbase @ T4(gc) @ R(-10 * k * sila, 0, 0).to_4x4() @ T4(-gc) @ T4((0, 0.03 * k * sila, 0.006 * k))
    cz = D_rack(spec, kick(t, 0.035, 0.12) * (0.9 if not spec.get("kolano") else 1.0))
    if "piv_kurek" in spec:
        ang = 45 * (sm(min(1, t / 0.012)) if t < 0.012 else 1 - sm(min(1, (t - 0.012) / 0.038)))
        cz["Kurek"] = T4(spec["piv_kurek"]) @ R(ang, 0, 0).to_4x4() @ T4(-spec["piv_kurek"])
    cz["Spust"] = T4((0, 0.005 * kick(t, 0.015, 0.15), 0))
    cz["Magazynek"] = D_mag(spec, 0, 1)
    v = Vector((-0.6, 0.4, 1.6)) if spec.get("kolano") else Vector((-1.3, 0.35, 1.1))
    return {"Wp": Wp, "czesci": cz, "luska": wyrzut_luski(spec, t, 0.03, Wbase, v)}


def klipy_pistolet(arm, spec):
    klipy_ruch(arm, spec)
    n = spec["nazwa"]
    piecz_fp(arm, spec, n + "_Strzal", 0.5, lambda t: strzal_pistolet(spec, spec["W0"], 1.0, t))
    piecz_fp(arm, spec, n + "_Strzal_Cel", 0.5, lambda t: strzal_pistolet(spec, spec["Wads"], 0.6, t))
    W0, Wt, Wr = spec["W0"], spec["Wtilt"], spec["Wrack"]
    gc, a = spec["pk"]["gc"], spec["pk"]["os"]
    belt_w = spec["Wl0"]
    belt = G_swiat(spec, Vector((0.16, -0.08, -0.72)), (40, 0, 20), belt_w)
    A_rack = spec["A_rack"]
    Wp_k = [(0, W0), (0.22, Wt), (1.4, Wt), (1.55, Wr), (1.78, Wr), (2.05, W0), (2.2, W0)]
    Gl_k = [(0, ("A", I4)), (0.22, ("A", T4((0.03, 0, -0.035)))), (0.62, ("W", belt)), (0.72, ("W", belt)),
            (1.05, ("A", T4(-a * 0.1))), (1.15, ("A", T4(-a * 0.08))), (1.35, ("A", I4)), (1.42, ("A", I4)),
            (1.55, ("A", A_rack(0))), (1.72, ("A", A_rack(1))), (1.78, ("A", A_rack(1.4))), (2.05, ("A", I4)), (2.2, ("A", I4))]
    rack_k = [(0, 0.0), (1.55, 0.0), (1.72, 1.0), (1.755, 0.0), (2.2, 0.0)]
    mag_k = [(0, 0.0), (0.25, 0.0), (0.3, 0.015), (0.55, 0.35), (1.36, 0.0), (2.2, 0.0)]

    def fn(t):
        Wp = mat(Wp_k, t)
        a0, a1, u = kanal(Gl_k, t)
        g0 = G_rel(Wp, spec, a0[1]) if a0[0] == "A" else a0[1]
        g1 = G_rel(Wp, spec, a1[1]) if a1[0] == "A" else a1[1]
        cz = D_rack(spec, num(rack_k, t))
        md = 0.0 if t > 1.36 else num(mag_k, t)
        cz["Magazynek"] = D_mag(spec, md, num([(0, 1.0), (0.56, 0.0), (1.36, 1.0)], t, True))
        return {"Wp": Wp, "Gl": blend(g0, g1, u), "czesci": cz,
                "trzymane": {"Magazynek_Nowy": num([(0, 0.0), (0.66, 1.0), (1.36, 0.0)], t, True)}}
    piecz_fp(arm, spec, n + "_Przeladowanie", 2.2, fn)


# ---- karabiny (zamek obrotowo-suwliwy, ladowanie z lodki)
def D_zamek(spec, obrot, cofniecie):
    p = spec["pk"]; piv = Vector((0, 0, 0.078))
    return T4((0, cofniecie, 0)) @ T4(piv) @ R(0, obrot, 0).to_4x4() @ T4(-piv)


def K_galka(spec):
    """Prawa dlon z chwytu na galce zamka (wzgledem broni)."""
    p = spec["pk"]
    palm = p["gc"] + Vector((-0.022, 0.03, -0.008))
    return T4(p["galka"] + Vector((-0.01, 0.02, -0.025))) @ R(0, 0, 0).to_4x4() @ T4(-palm)


def A_lodka(spec):
    """Prawa dlon trzymajaca lodke nad komora (wzgledem broni)."""
    gc = spec["pk"]["gc"]
    lod = spec["pk"].get("lodka_poz", Vector((0, -0.07, 0.11)))
    palm = gc + Vector((-0.022, 0.03, -0.008))
    return T4(lod + Vector((-0.01, 0.0, 0.05))) @ R(0, -60, 0).to_4x4() @ T4(-palm)


def cykl_zamka(spec, t, t0):
    """Repetowanie: (Gr_rel A, D zamka) w chwili t; t0 = poczatek (0,9 s)."""
    u = t - t0
    obr = num([(0, 0.0), (0.25, 0.0), (0.35, 90.0), (0.6, 90.0), (0.7, 0.0), (1.0, 0.0)], u)
    cof = num([(0, 0.0), (0.35, 0.0), (0.45, 0.09), (0.5, 0.09), (0.6, 0.0), (1.0, 0.0)], u)
    D = D_zamek(spec, obr, cof)
    na = num([(0, 0.0), (0.15, 1.0), (0.7, 1.0), (0.9, 0.0), (1.0, 0.0)], u)
    return D, na


def klipy_karabin(arm, spec):
    klipy_ruch(arm, spec)
    n = spec["nazwa"]
    gc = spec["pk"]["gc"]

    def strzal(Wbase, sila):
        def fn(t):
            k = kick(t, 0.04, 0.3)
            Wp = Wbase @ T4(gc) @ R(-6 * k * sila, 0, 0).to_4x4() @ T4(-gc) @ T4((0, 0.05 * k * sila, 0.01 * k))
            D, na = cykl_zamka(spec, t, 0.3)
            A = blend(I4, D @ K_galka(spec), na)
            st = {"Wp": Wp, "czesci": {"Zamek": D, "Spust": T4((0, 0.004 * kick(t, 0.02, 0.2), 0)), "Lodka": _ukryj(spec["pk"]["gc"])},
                  "Gr": G_rel(Wp, spec, A), "luska": wyrzut_luski(spec, t, 0.75, Wbase)}
            return st
        return fn
    piecz_fp(arm, spec, n + "_Strzal", 1.3, strzal(spec["W0"], 1.0))
    piecz_fp(arm, spec, n + "_Strzal_Cel", 1.3, strzal(spec["Wads"], 0.7))
    W0, Wt = spec["W0"], spec["Wtilt"]
    Wp_k = [(0, W0), (0.3, Wt), (2.9, Wt), (3.2, W0), (3.4, W0)]
    lod = spec["pk"].get("lodka_poz", Vector((0, -0.07, 0.11)))
    kiesz = G_swiat(spec, Vector((-0.3, -0.05, -0.65)), (30, 0, -20), spec["Wr0"])

    def fn(t):
        Wp = mat(Wp_k, t)
        obr = num([(0, 0.0), (0.45, 0.0), (0.55, 90.0), (2.4, 90.0), (2.5, 0.0), (3.4, 0.0)], t)
        cof = num([(0, 0.0), (0.55, 0.0), (0.7, 0.09), (2.25, 0.09), (2.4, 0.0), (3.4, 0.0)], t)
        D = D_zamek(spec, obr, cof)
        # prawa dlon: galka -> kieszen -> lodka -> galka -> chwyt
        A_lod = A_lodka(spec)
        A_g = D @ K_galka(spec)
        keys = [(0, ("A", I4)), (0.3, ("A", I4)), (0.45, ("A", A_g)), (0.72, ("A", A_g)), (1.05, ("W", kiesz)),
                (1.2, ("W", kiesz)), (1.55, ("A", A_lod)), (1.65, ("A", A_lod)),
                (1.95, ("A", T4((0, 0, -0.03)) @ A_lod)), (2.05, ("A", T4((0.05, 0.05, 0.04)) @ A_lod)),
                (2.2, ("A", A_g)), (2.55, ("A", A_g)), (2.9, ("A", I4)), (3.4, ("A", I4))]
        a0, a1, u = kanal(keys, t)
        g0 = G_rel(Wp, spec, a0[1]) if a0[0] == "A" else a0[1]
        g1 = G_rel(Wp, spec, a1[1]) if a1[0] == "A" else a1[1]
        zejscie = num([(0, 0.0), (1.65, 0.0), (1.95, 0.03), (3.4, 0.03)], t)
        widac = num([(0, 0.0), (1.55, 1.0), (2.0, 0.0)], t, True)
        c = lod
        Dl = T4((0, 0, -zejscie)) @ T4(c) @ Matrix.Diagonal((max(widac, 0.001),) * 3 + (1,)) @ T4(-c)
        return {"Wp": Wp, "Gr": blend(g0, g1, u), "czesci": {"Zamek": D, "Lodka": Dl},
                "trzymane": {"Lodka_Reka": num([(0, 0.0), (1.15, 1.0), (1.55, 0.0)], t, True)},
                "luska": wyrzut_luski(spec, t, 0.68, Wt)}
    piecz_fp(arm, spec, n + "_Przeladowanie", 3.4, fn)


# ---- noze
def klipy_noz(arm, spec):
    klipy_ruch(arm, spec, ads=False)
    n = spec["nazwa"]; W0 = spec["W0"]
    ciecie = [(0, W0), (0.14, M4((-0.24, -0.3, -0.04), (40, -30, -60))), (0.3, M4((0.14, -0.48, -0.26), (-10, 40, 50))),
              (0.42, M4((0.18, -0.4, -0.3), (-20, 45, 60))), (0.7, W0)]
    piecz_fp(arm, spec, n + "_Atak", 0.7, lambda t: {"Wp": mat(ciecie, t)})
    pchn = [(0, W0), (0.15, M4((-0.13, -0.24, -0.2), (5, 0, -5))), (0.27, M4((-0.06, -0.62, -0.12), (-5, 0, 0))),
            (0.36, M4((-0.06, -0.6, -0.12), (-5, 0, 0))), (0.6, W0)]
    piecz_fp(arm, spec, n + "_Pchniecie", 0.6, lambda t: {"Wp": mat(pchn, t)})


# ------------------------------------------------------------------ specyfikacje broni
def _vis_czesci(C):
    build_vis()
    src = {o.name.split(".")[0]: o for o in bpy.data.collections["OB_Pistolet_Vis"].all_objects}
    bpy.context.view_layer.update()
    cz = {}
    for n in ("Vis", "Zamek", "Kurek", "Spust", "Magazynek"):
        o = src[n]; me = o.data.copy(); me.transform(o.matrix_world)
        ob = bpy.data.objects.new(n + "_fp", me); C.objects.link(ob)
        cz["Bron" if n == "Vis" else n] = (ob, Vector((0, 0.037, 0.062)) if n == "Kurek" else Vector())
    a = Vector((0, -math.sin(math.radians(16)), math.cos(math.radians(16))))
    pk = {"Lufa": Vector((0, -0.172, 0.065)), "Wyrzut": Vector((-0.016, -0.025, 0.066)), "gc": Vector((0, 0.012, -0.012)),
          "os": a, "trig": Vector((0, -0.04, 0.016))}
    return cz, pk


def _A_rack_zamek(pk, wys=0.095):
    gc = pk["gc"]
    p0 = gc + Vector((0.042, -0.005, -0.02))
    return lambda u: T4(Vector((0.004, 0.03 + 0.045 * min(u, 1) + 0.03 * max(0, u - 1), wys + 0.02 * max(0, u - 1)))) @ R(0, -90, 0).to_4x4() @ T4(-p0)


def _A_rack_kolano(pk):
    gc = pk["gc"]
    p0 = gc + Vector((0.042, -0.005, -0.02))
    return lambda u: T4(Vector((0.0, 0.045 + 0.02 * u, 0.1 + 0.035 * u))) @ R(0, -90, 0).to_4x4() @ T4(-p0)


def specyfikacje():
    pist = dict(prawa=lambda pk: prawa_chwyt(pk["gc"], pk["os"], pk["trig"]),
                lewa=lambda pk: lewa_podparcie(pk["gc"], pk["os"]),
                trzymane={"Magazynek_Nowy": ("L", "Magazynek")}, luska=(0.019, 0.0046), typ="pistolet",
                W0=M4((-0.11, -0.40, -0.125), (3, 0, 7)), Wads=M4((0, -0.36, -0.072)),
                Wtilt=M4((-0.03, -0.32, -0.04), (-40, 38, 6)), Wrack=M4((-0.06, -0.5, -0.21), (-5, 28, 14)))
    S = []
    S.append(dict(pist, nazwa="FP_Vis", czesci=_vis_czesci, piv_kurek=Vector((0, 0.037, 0.062)), skok=0.045,
                  A_rack=None))
    S.append(dict(pist, nazwa="FP_PPK", czesci=lambda C: ppk(C), piv_kurek=Vector((0, 0.034, 0.055)), skok=0.035,
                  Wads=M4((0, -0.34, -0.062)), A_rack=None))
    S.append(dict(pist, nazwa="FP_Luger", czesci=lambda C: luger(C), kolano=True, piv_kolano=Vector((0, 0.055, 0.078)),
                  Wads=M4((0, -0.36, -0.08)), A_rack=None))
    kar = dict(prawa=lambda pk: prawa_chwyt(pk["gc"], pk["os"], pk["trig"]),
               lewa=lambda pk: lewa_lozysko(pk["lozysko"]), luska=(0.057, 0.006), typ="karabin",
               W0=M4((-0.1, -0.26, -0.17), (2, 0, 3)), Wtilt=M4((-0.05, -0.34, -0.2), (-6, -22, 8)),
               trzymane={"Lodka_Reka": ("P", "Lodka")}, A_trzymane={"Lodka_Reka": A_lodka}, ukryte=("Lodka",),
               S_R=Vector((-0.17, 0.1, -0.3)))
    S.append(dict(kar, nazwa="FP_Kar98k", czesci=lambda C: karabin(C, False), Wads=M4((0, -0.1, -0.092))))
    S.append(dict(kar, nazwa="FP_wz29", czesci=lambda C: karabin(C, True), Wads=M4((0, -0.1, -0.092))))
    noz_ = dict(prawa=lambda pk: prawa_chwyt(pk["gc"], pk["os"], None),
                lewa=lambda pk: lewa_luzna(Vector((0.26, -0.15, -0.62))), lewa_na_broni=False, typ="noz",
                W0=M4((-0.14, -0.38, -0.2), (12, 0, 8)))
    S.append(dict(noz_, nazwa="FP_Bagnet", czesci=lambda C: bagnet(C)))
    S.append(dict(noz_, nazwa="FP_Noz", czesci=lambda C: noz(C)))
    return S


def zbuduj_fp_wszystkie(tylko=None):
    wyniki = []
    for spec in specyfikacje():
        if tylko and spec["nazwa"] not in tylko:
            continue
        arm = zbuduj_rig(spec)
        pk = spec["pk"]
        if spec["typ"] == "pistolet":
            spec["A_rack"] = _A_rack_kolano(pk) if spec.get("kolano") else _A_rack_zamek(pk)
            klipy_pistolet(arm, spec)
        elif spec["typ"] == "karabin":
            klipy_karabin(arm, spec)
        else:
            klipy_noz(arm, spec)
        arm.animation_data.action = None
        for pb in arm.pose.bones:
            pb.matrix_basis = Matrix.Identity(4)
        wyniki.append(spec["nazwa"])
    return wyniki


def eksport_fp_wszystkie():
    import os
    out = []
    for spec in specyfikacje():
        n = spec["nazwa"]
        if n not in bpy.data.collections:
            continue
        akc = [a for a in bpy.data.actions if a.name.startswith(n + "_")]
        out.append(os.path.basename(eksport_postaci(n, n, animacje=True, akcje=akc)))
    return out
