# Postacie z gotowych baz Sketchfab (CC BY): meska "Simple Low Poly Character" (PIXELOKAY),
# damska "Low Poly Female Base Mesh" (mesh-base). Bazy ustawiane w T-poze, szkielet Humanoid wg pomiarow,
# ubrania jako strefy atlasu + dodatki z ch_cialo2. Wymaga ob_lib, ob_p2, ch_lib, ch_cialo2.
BAZY_PLIK = os.path.join(os.path.dirname(DIR_POST), "blender", "bazy.blend")


def _sm01(x):
    x = np.clip(x, 0, 1)
    return x * x * (3 - 2 * x)


def przygotuj_baze(nazwa, plec):
    """Zwraca obiekt bazy w T-pozie (przod -Y, srodek tulowia y=0) + slownik pomiarow."""
    o = bpy.data.objects[nazwa]
    me = o.data
    P = np.array([v.co[:] for v in me.vertices])
    H = P[:, 2].max()
    sl = P[np.abs(P[:, 2] - 0.62 * H) < 0.03]
    P[:, 1] -= (sl[:, 1].min() + sl[:, 1].max()) / 2           # tulow na y=0
    if plec == "K":                                            # mniejszy biust
        band = (P[:, 2] > 0.68 * H) & (P[:, 2] < 0.79 * H) & (np.abs(P[:, 0]) < 0.16)
        yb = -0.075
        f = _sm01(1 - np.abs(P[:, 2] - 0.735 * H) / (0.055 * H))
        m = band & (P[:, 1] < yb)
        P[m, 1] = yb + (P[m, 1] - yb) * (1 - 0.5 * f[m])
    sx_ = 0.165 * H / 1.8 if plec == "M" else 0.15 * H / 1.68
    S = {1: np.array([sx_, 0.0, 0.815 * H]), -1: np.array([-sx_, 0.0, 0.815 * H])}
    pom = {"H": H}
    for sx in (1, -1):
        s0 = S[sx]
        side = P[:, 0] * sx > 0.05
        tip = P[side][np.argmax(P[side][:, 0] * sx)]
        d = tip - s0; L = np.linalg.norm(d); d /= L
        tgt = Vector((sx, 0, 0))
        q = Vector(d).rotation_difference(tgt)
        rel = P - s0
        along = rel @ d
        # wagi jak przy skinningu: blizej osi reki niz osi tulowia (wzgledem ich grubosci) -> obrot calej reki z dlonia
        d_a = odl_odcinek(P, s0, s0 + d * (L + 0.12))
        d_t = odl_odcinek(P, np.array([0, 0, 0.45 * H]), np.array([0, 0, 0.83 * H]))
        rt, ra = 0.13 * H / 1.8, 0.055 * H / 1.8
        w = _sm01((d_t / rt - d_a / ra) / 1.0 + 0.5) * _sm01((along + 0.04) / 0.08) * side
        for i in np.nonzero(w > 0)[0]:
            qi = Quaternion().slerp(q, float(w[i]))
            P[i] = s0 + np.array(qi @ Vector(rel[i]))
        pom[sx] = (s0, L)
    for i, v in enumerate(me.vertices):
        v.co = P[i]
    me.update()
    # nogi: srodki w przekrojach
    for sx in (1, -1):
        for key, z in (("biodro", 0.5), ("kolano", 0.28), ("kostka", 0.05)):
            s = P[(np.abs(P[:, 2] - z * H) < 0.02 * H) & (P[:, 0] * sx > 0.01)]
            pom[(key, sx)] = np.array([s[:, 0].mean(), 0.0, z * H])
    gl = P[P[:, 2] > 0.88 * H]
    pom["glowa_y"] = (gl[:, 1].min() + gl[:, 1].max()) / 2
    pom["klatka_y"] = P[(np.abs(P[:, 2] - 0.75 * H) < 0.02 * H) & (np.abs(P[:, 0]) < 0.05)][:, 1].min()
    bio = P[np.abs(P[:, 2] - 0.5 * H) < 0.02 * H]
    pom["biodra_r"] = (bio[:, 0].max() - bio[:, 0].min()) / 2
    return o, pom


def popraw_dlonie(o):
    """Prostuje zgiete w nadgarstku dlonie baz (A-poza) do osi przedramienia; nadgarstek wykrywany
    tam, gdzie przekroj reki nagle robi sie wysoki (zwisajaca dlon). Aktualizuje dlugosc reki w pomiarach."""
    pom = pomiary(o)
    P = np.array([v.co[:] for v in o.data.vertices])
    raw = eval(o["pom"])
    for sx in (1, -1):
        s0, L = pom[sx]
        z0 = s0[2]
        arm = P[(P[:, 0] * sx > abs(s0[0]) + 0.05) & (np.abs(P[:, 1]) < 0.15)]
        xs = np.arange(abs(s0[0]) + 0.1, np.abs(arm[:, 0]).max(), 0.01)
        grub = []
        for x0 in xs:
            sl = arm[np.abs(np.abs(arm[:, 0]) - x0) < 0.012]
            grub.append(sl[:, 2].max() - sl[:, 2].min() if len(sl) else 0)
        ref = np.median(grub[:max(3, len(grub) // 3)])
        wx = next((x0 for x0, g_ in zip(xs, grub) if g_ > 1.8 * ref), None)
        if wx is not None:
            wr = np.array([sx * (wx - 0.015), s0[1], z0])
            dx = (P[:, 0] - wr[0]) * sx
            m = (dx > -0.03) & (P[:, 2] > z0 - 0.4) & (P[:, 2] < z0 + 0.15) & (np.abs(P[:, 1]) < 0.15)
            dlon = m & (dx > 0.0)
            c = P[dlon].mean(0) - wr
            q = Vector(c).normalized().rotation_difference(Vector((sx, 0, 0)))
            for i in np.nonzero(m)[0]:
                w = float(_sm01((dx[i] + 0.03) / 0.04))
                P[i] = wr + np.array(Quaternion().slerp(q, w) @ Vector(P[i] - wr))
        reka = P[(P[:, 0] * sx > 0.2) & (np.abs(P[:, 2] - z0) < 0.12)]
        tip = reka[np.argmax(reka[:, 0] * sx)]
        raw[str(sx)] = [list(s0), float(abs(tip[0] - s0[0]))]
    for i, v in enumerate(o.data.vertices):
        v.co = P[i]
    o.data.update()
    o["pom"] = repr(raw)


def promien_ciala(o, z, H):
    P = np.array([v.co[:] for v in o.data.vertices])
    sl = P[(np.abs(P[:, 2] - z) < 0.02 * H) & (np.abs(P[:, 0]) < 0.3)]
    return max(abs(sl[:, 0]).max(), 0.08), max(abs(sl[:, 1]).max(), 0.06)


def kosci_z_pomiarow(pom, plec):
    H = pom["H"]
    B = {}

    def add(n, h, t, p):
        B[n] = (Vector(h), Vector(t), p)
    add("Hips", (0, 0, 0.53 * H), (0, 0, 0.585 * H), None)
    add("Spine", (0, 0, 0.585 * H), (0, 0, 0.68 * H), "Hips")
    add("Chest", (0, 0, 0.68 * H), (0, 0, 0.8 * H), "Spine")
    add("Neck", (0, pom["glowa_y"] * 0.5, 0.82 * H), (0, pom["glowa_y"], 0.875 * H), "Chest")
    add("Head", (0, pom["glowa_y"], 0.875 * H), (0, pom["glowa_y"], H), "Neck")
    for side, sx in (("Left", 1), ("Right", -1)):
        s0, L = pom[sx]
        z = s0[2]
        add(side + "Shoulder", (sx * 0.03, 0, z), (s0[0], 0, z), "Chest")
        add(side + "UpperArm", (s0[0], 0, z), (s0[0] + sx * 0.4 * L, 0, z), side + "Shoulder")
        add(side + "LowerArm", (s0[0] + sx * 0.4 * L, 0, z), (s0[0] + sx * 0.75 * L, 0, z), side + "UpperArm")
        add(side + "Hand", (s0[0] + sx * 0.75 * L, 0, z), (s0[0] + sx * 0.95 * L, 0, z), side + "LowerArm")
        b, k, a = pom[("biodro", sx)], pom[("kolano", sx)], pom[("kostka", sx)]
        add(side + "UpperLeg", (b[0], 0, b[2]), (k[0], 0, k[2]), "Hips")
        add(side + "LowerLeg", (k[0], 0, k[2]), (a[0], 0, a[2]), side + "UpperLeg")
        add(side + "Foot", (a[0], 0, a[2]), (a[0], -0.11 * H / 1.8, 0.015 * H), side + "LowerLeg")
        add(side + "Toes", (a[0], -0.11 * H / 1.8, 0.015 * H), (a[0], -0.17 * H / 1.8, 0.015 * H), side + "Foot")
    return B


def zaladuj_bazy():
    if "BAZA_M_T" in bpy.data.objects:
        return
    with bpy.data.libraries.load(BAZY_PLIK) as (src, dst):
        dst.objects = [n for n in src.objects if n in ("BAZA_M_T", "BAZA_K_T")]
    for o in dst.objects:
        if o.name not in bpy.context.scene.collection.objects:
            bpy.context.scene.collection.objects.link(o)
        o.hide_set(True); o.hide_render = True


def scal(root_name, nowa):
    root = bpy.data.objects[root_name]
    objs = [root] + list(root.children_recursive)
    meshe = [o for o in objs if o.type == "MESH"]
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    nowe = []
    for o in meshe:
        m = bpy.data.meshes.new_from_object(o.evaluated_get(dg)); m.transform(o.matrix_world)
        ob = bpy.data.objects.new(nowa + "_cz", m); bpy.context.scene.collection.objects.link(ob); nowe.append(ob)
    for o in objs:
        bpy.data.objects.remove(o, do_unlink=True)
    with bpy.context.temp_override(active_object=nowe[0], selected_editable_objects=nowe, object=nowe[0]):
        bpy.ops.object.join()
    nowe[0].name = nowa
    return nowe[0]


def przygotuj_wszystkie_bazy():
    """Raz: z zaimportowanych BAZA_M / BAZA_K robi BAZA_M_T / BAZA_K_T (T-poza) i zapisuje do bazy.blend."""
    for src, plec in (("BAZA_M", "M"), ("BAZA_K", "K")):
        o, pom = przygotuj_baze(src, plec)
        hair = [p.index for p in o.data.polygons if plec == "M" and p.material_index == 0]
        o.name = src + "_T"
        o["pom"] = repr({k if isinstance(k, str) else str(k): (v.tolist() if isinstance(v, np.ndarray) else
                         ([v[0].tolist(), v[1]] if isinstance(v, tuple) else v)) for k, v in pom.items()})
        o["wlosy_sciany"] = hair
    bpy.data.libraries.write(BAZY_PLIK, {bpy.data.objects["BAZA_M_T"], bpy.data.objects["BAZA_K_T"]}, fake_user=True)


def pomiary(o):
    raw = eval(o["pom"])
    pom = {"H": raw["H"], "glowa_y": raw["glowa_y"], "klatka_y": raw["klatka_y"], "biodra_r": raw["biodra_r"]}
    for sx in (1, -1):
        s0, L = raw[str(sx)]
        pom[sx] = (np.array(s0), L)
        for key in ("biodro", "kolano", "kostka"):
            pom[(key, sx)] = np.array(raw[str((key, sx))])
    return pom


def zloz_postac(nazwa, st):
    zaladuj_bazy()
    K = st.get("plec") == "K"
    baza = bpy.data.objects["BAZA_K_T" if K else "BAZA_M_T"]
    pom = pomiary(baza)
    H = pom["H"]; s = H / 1.8 if not K else H / (1.8 * 0.94) * 0.94
    C = new_collection(nazwa)
    B = kosci_z_pomiarow(pom, "K" if K else "M")
    arm = zrob_armature(nazwa + "_Rig", C, B)
    me = baza.data.copy(); me.name = nazwa + "_Cialo"
    body = bpy.data.objects.new(nazwa + "_Cialo", me); C.objects.link(body)
    body.hide_set(False)
    while me.uv_layers:
        me.uv_layers.remove(me.uv_layers[0])
    # strefy ubran
    tul = st.get("tulow", "garnitur")
    pl, plk = st.get("plaszcz"), st.get("plaszcz_kol", "plaszcz_brazowy")
    tul_vis = plk if pl else (st.get("kamizelka") or tul)
    rek = plk if pl else st.get("rekawy", tul)
    spo = st.get("spodnie", "garnitur")
    but = st.get("buty", "skora_czarna")
    nogi = st.get("nogi", spo if not K else "skora_cialo")
    wl = st.get("wlosy") or "wlosy_ciemne"
    sh_x = abs(pom[1][0][0]); wrist_x = sh_x + 0.75 * pom[1][1]
    hair = set(baza.get("wlosy_sciany", []))

    def strefa(i, c):
        z = c.z / H; ax = abs(c.x)
        if i in hair:
            return wl
        if z > 0.82 and ax < 0.11:
            return "skora_cialo"
        if ax > wrist_x - 0.01:
            return "skora_cialo"
        if ax > sh_x + 0.02 and z > 0.7:
            if ax > wrist_x - 0.05 and not pl and st.get("mankiet", "koszula"):
                return st.get("mankiet", "koszula")
            return rek
        if z < 0.06:
            return but
        if z < 0.24 and st.get("buty_wysokie"):
            return but
        if K and not pl and 0.31 < z < 0.57:
            return st.get("spodnica", "sukienka_niebieska")
        if z < 0.47:
            return nogi
        if z < 0.55 and not K:
            return spo
        if K and z < 0.55:
            return st.get("spodnica", "sukienka_niebieska")
        return tul_vis
    bm = bmesh.new(); bm.from_mesh(me)
    uv = bm.loops.layers.uv.verify()
    grupy = {}
    for f in bm.faces:
        grupy.setdefault(strefa(f.index, f.calc_center_median()), []).append(f)
    for tile, faces in grupy.items():
        idx = TILES[tile][0]; tx, ty = idx % N, idx // N
        vs = [l.vert.co for f in faces for l in f.loops]
        mn = Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs)))
        mx = Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs)))
        rg = mx - mn; rg = Vector((max(rg.x, 1e-4), max(rg.y, 1e-4), max(rg.z, 1e-4)))
        for f in faces:
            ax = max(range(3), key=lambda k: abs(f.normal[k])); a, b = [(1, 2), (0, 2), (0, 1)][ax]
            for l in f.loops:
                p = l.vert.co
                l[uv].uv = ((tx + 0.08 + 0.84 * (p[a] - mn[a]) / rg[a]) / N, (ty + 0.08 + 0.84 * (p[b] - mn[b]) / rg[b]) / N)
        for f in faces:
            f.smooth = False
    bm.to_mesh(me); bm.free()
    me.materials.clear(); me.materials.append(atlas_mat())
    wagi(body, arm)
    # dodatki glowy (sztywno do Head)
    gz = H - 0.118 * s
    gy = pom["glowa_y"]
    acc = []
    if K:
        acc += wlosy(C, s, st.get("fryzura", "krotkie"), wl, gz)
    acc += czapka(C, s, st.get("czapka"), st.get("czapka_kol", "garnitur"), gz + (0.01 if not K else 0))
    if st.get("wasy"):
        acc.append(box("wasy", (0.05 * s, 0.012 * s, 0.012 * s), (0, -0.098 * s, gz - 0.04 * s), st["wasy"], C, bevel=0.004))
    if st.get("okulary"):
        for sx in (-1, 1):
            acc.append(torus("szklo", 0.019 * s, 0.0025 * s, (sx * 0.034 * s, -0.104 * s, gz + 0.01 * s), "zeliwo", C, rot=(90, 0, 0), seg=12, rseg=3))
        acc.append(box("mostek", (0.03 * s, 0.004 * s, 0.004 * s), (0, -0.104 * s, gz + 0.014 * s), "zeliwo", C))
    for o in acc:
        o.data.transform(T4((0, gy, 0)))
        for p in o.data.polygons:
            p.use_smooth = False
        _grupa(o, "Head")
    # dodatki tulowia (plynne wagi)
    ky = pom["klatka_y"]; br = max(pom["biodra_r"], 0.15)
    dod = []
    if pl:
        dl = 0.5 if pl == "dlugi" else 0.24
        top = 0.58 * H
        rx, ry = promien_ciala(body, top, H)
        dod.append(tuba("pola", C, rx + 0.075, rx + 0.012, top - dl * s, top, plk, (ry + 0.02) / (rx + 0.012)))
        dod.append(torus("kolnierz", 0.075 * s, 0.024 * s, (0, gy * 0.4, 0.82 * H), plk, C, seg=12, rseg=5))
    elif st.get("kolnierz"):
        dod.append(torus("kolnierz", 0.065 * s, 0.015 * s, (0, gy * 0.4, 0.82 * H), st["kolnierz"], C, seg=12, rseg=4))
    if K and not pl:
        rx, ry = promien_ciala(body, 0.57 * H, H)
        dod.append(tuba("spodnica", C, rx + 0.17, rx + 0.008, 0.32 * H, 0.57 * H, st.get("spodnica", "sukienka_niebieska"), (ry + 0.012) / (rx + 0.008)))
    if st.get("pas"):
        p_ = torus("pas", br + 0.012, 0.016 * s, (0, 0, 0.56 * H), st["pas"], C, seg=16, rseg=4)
        for v in p_.data.vertices:
            v.co.y *= 0.72
        dod.append(p_)
        dod.append(box("klamra", (0.05 * s, 0.01 * s, 0.035 * s), (0, -(br * 0.72 + 0.012), 0.56 * H), "stal", C, bevel=0.004))
    if st.get("krawat"):
        dod.append(box("krawat", (0.04 * s, 0.01 * s, 0.22 * s), (0, ky - 0.006, 0.735 * H), st["krawat"], C, bevel=0.005))
    if st.get("mucha"):
        dod.append(box("mucha", (0.075 * s, 0.015 * s, 0.028 * s), (0, ky + 0.01, 0.8 * H), st["mucha"], C, bevel=0.006))
    if st.get("okrycie"):
        ok = box("okrycie", (0.62 * s, 0.05 * s, 1.5 * s), (0, ky - 0.04, 0.62 * H), st["okrycie"], C, bevel=0.03 * s, seg=2)
        dod.append(ok)
    for o in dod:
        for p in o.data.polygons:
            p.use_smooth = False
        wagi(o, arm)
    with bpy.context.temp_override(active_object=body, selected_editable_objects=[body] + acc + dod, object=body):
        bpy.ops.object.join()
    body.data.materials.clear(); body.data.materials.append(postac_mat())
    body.parent = arm
    body.modifiers.new("Armature", "ARMATURE").object = arm
    arm.data.display_type = "STICK"
    arm.hide_set(True)
    arm["plec"] = "K" if K else "M"
    return C, arm, body
