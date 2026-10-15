# Cialo v2: jednolita organiczna siatka (modyfikator Skin + Subsurf), uformowana glowa, strefy ubran,
# plynne wagi skory liczone od odleglosci do kosci. Wymaga ob_lib, ob_p2, ch_lib.


def skin_mesh(nazwa, C, verts, edges, radii, poziom=1, root=0, gladko=False):
    me = bpy.data.meshes.new(nazwa)
    me.from_pydata([tuple(v) for v in verts], edges, [])
    o = bpy.data.objects.new(nazwa, me)
    C.objects.link(o)
    md = o.modifiers.new("Skin", "SKIN")
    md.use_smooth_shade = True
    md.branch_smoothing = 0.6
    if len(me.skin_vertices) == 0:
        me.skin_vertices.new()
    sv = me.skin_vertices[0].data
    for i, r in enumerate(radii):
        sv[i].radius = (r, r) if not isinstance(r, tuple) else r
        sv[i].use_root = (i == root)
    ss = o.modifiers.new("Sub", "SUBSURF")
    ss.levels = poziom
    apply_mods(o)
    for p in o.data.polygons:
        p.use_smooth = gladko
    return o


def tuba(nazwa, C, r_dol, r_gora, z_dol, z_gora, tile, glebokosc=0.8, seg=16):
    """Otwarta rura (bez denek) — poly plaszcza, spodnica."""
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=False, segments=seg, radius1=r_dol, radius2=r_gora, depth=z_gora - z_dol)
    bmesh.ops.translate(bm, vec=(0, 0, (z_dol + z_gora) / 2), verts=bm.verts)
    for v in bm.verts:
        v.co.y *= glebokosc
    bm.normal_update()
    _uv_box(bm, tile)
    o = _finish(nazwa, bm, C, atlas_mat())
    o.modifiers.new("Sol", "SOLIDIFY").thickness = 0.006
    apply_mods(o)
    for p in o.data.polygons:
        p.use_smooth = False
    return o


def odl_odcinek(P, a, b):
    ab = b - a
    t = np.clip(((P - a) @ ab) / max(ab @ ab, 1e-9), 0, 1)
    return np.linalg.norm(P - (a + t[:, None] * ab), axis=1)


def wagi(o, arm, tylko=None, top=3, ostrosc=5.0):
    """Plynne wagi: 1/odleglosc^ostrosc do odcinkow kosci, 3 najblizsze."""
    P = np.array([v.co[:] for v in o.data.vertices])
    kosci = [b for b in arm.data.bones if (tylko is None or b.name in tylko) and not b.name.endswith("Toes")]
    D = np.stack([odl_odcinek(P, np.array(b.head_local[:]), np.array(b.tail_local[:])) for b in kosci], 1)
    W = 1.0 / (D + 0.012) ** ostrosc
    idx = np.argsort(-W, axis=1)[:, :top]
    for b in kosci:
        if b.name not in o.vertex_groups:
            o.vertex_groups.new(name=b.name)
    for i in range(len(P)):
        ws = W[i, idx[i]]; ws = ws / ws.sum()
        for k, w in zip(idx[i], ws):
            if w > 0.02:
                o.vertex_groups[kosci[k].name].add([i], float(w), "REPLACE")
    return D, kosci


def uv_strefy(o, tile_twarzy):
    """Kazda sciana dostaje UV w kaflu zwroconym przez tile_twarzy(srodek, normalna) (rzut pudelkowy w ramach strefy)."""
    me = o.data
    bm = bmesh.new(); bm.from_mesh(me)
    uv = bm.loops.layers.uv.verify()
    grupy = {}
    for f in bm.faces:
        grupy.setdefault(tile_twarzy(f.calc_center_median(), f.normal), []).append(f)
    for tile, faces in grupy.items():
        idx = TILES[tile][0]; tx, ty = idx % N, idx // N
        vs = [l.vert.co for f in faces for l in f.loops]
        mn = Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs)))
        mx = Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs)))
        rg = mx - mn; rg = Vector((max(rg.x, 1e-4), max(rg.y, 1e-4), max(rg.z, 1e-4)))
        for f in faces:
            ax = max(range(3), key=lambda k: abs(f.normal[k]))
            a, b = [(1, 2), (0, 2), (0, 1)][ax]
            for l in f.loops:
                p = l.vert.co
                u = 0.08 + 0.84 * (p[a] - mn[a]) / rg[a]; v = 0.08 + 0.84 * (p[b] - mn[b]) / rg[b]
                l[uv].uv = ((tx + u) / N, (ty + v) / N)
    bm.to_mesh(me); bm.free()
    me.materials.clear(); me.materials.append(atlas_mat())


def cialo_skin(C, B, s, K):
    """Organiczny korpus z konczynami (T-poza)."""
    V, E, Rr = [], [], []

    def add(p, r, parent=None):
        V.append(Vector(p) * s); Rr.append(r * s if not isinstance(r, tuple) else (r[0] * s, r[1] * s))
        if parent is not None:
            E.append((parent, len(V) - 1))
        return len(V) - 1
    bio = 1.06 if K else 1.0         # kobiety: szersze biodra
    pel = add((0, 0, 0.93), 0.15 * bio)
    tal = add((0, 0, 1.07), 0.125 if K else 0.13, pel)
    kl = add((0, 0, 1.26), 0.16 if not K else 0.15, tal)
    kg = add((0, 0, 1.39), 0.175 if not K else 0.14, kl)
    sz = add((0, 0, 1.47), 0.068, kg)
    add((0, 0, 1.58), 0.058, sz)
    sh = abs(B["LeftUpperArm"][0].x) / s
    for sx in (1, -1):
        bark = add((sx * (sh - 0.01), 0, 1.415), 0.062, kg)
        lok = add((sx * (sh + 0.29), 0, 1.43), 0.042, bark)
        mid = add((sx * (sh + 0.13), 0, 1.425), 0.052 if not K else 0.045, None)
        E.remove((bark, lok)); E.append((bark, mid)); E.append((mid, lok))
        nad = add((sx * (sh + 0.54), 0, 1.43), 0.03, lok)
        pr = add((sx * (sh + 0.36), 0, 1.43), 0.045 if not K else 0.04, None)
        E.remove((lok, nad)); E.append((lok, pr)); E.append((pr, nad))
        dl = add((sx * (sh + 0.6), -0.005, 1.43), 0.04, nad)
        add((sx * (sh + 0.67), -0.005, 1.425), 0.032, dl)
        add((sx * (sh + 0.58), -0.045, 1.42), 0.014, nad)
        hx = (0.09 if K else 0.10)
        bio_ = add((sx * hx, 0, 0.88), 0.085 * bio, pel)
        ud = add((sx * hx, -0.01, 0.72), 0.078, bio_)
        kol = add((sx * hx, 0, 0.5), 0.052, ud)
        lyd = add((sx * hx, 0.012, 0.36), 0.058, kol)
        kost = add((sx * hx, 0, 0.1), 0.036, lyd)
        add((sx * hx, -0.14, 0.035), (0.042, 0.03), kost)
        add((sx * hx, 0.035, 0.045), 0.035, kost)
    o = skin_mesh("cialo", C, V, E, Rr, poziom=1, root=pel)
    # splaszczenie tulowia (glebokosc) + ksztalt stopy
    for v in o.data.vertices:
        if abs(v.co.x) < 0.22 * s and 0.82 * s < v.co.z < 1.46 * s:
            v.co.y *= 0.72
            if K and 1.2 * s < v.co.z < 1.38 * s and v.co.y < 0:
                v.co.y -= 0.025 * s * math.sin((v.co.z / s - 1.2) / 0.18 * math.pi) * max(0, 1 - abs(abs(v.co.x) - 0.075 * s) / (0.06 * s))
        if v.co.z < 0.07 * s:
            v.co.z = max(v.co.z, 0.0)
    return o


def glowa_mesh(C, s, gz):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=12, v_segments=9, radius=0.105 * s)
    for v in bm.verts:
        x, y, z = v.co / s
        x *= 0.86; z *= 1.12
        if z < -0.01:                                   # zuchwa i broda
            t = min(1, (-z - 0.01) / 0.1)
            x *= 1 - 0.32 * t
            if y < 0:
                y *= 1 - 0.15 * t
        if y > 0.02:                                    # tyl glowy
            y *= 1.06
        if y < -0.06 and 0.0 < z < 0.04 and abs(x) > 0.015:   # oczodoly
            y += 0.01 * (1 - abs(abs(x) - 0.035) / 0.03)
        v.co = Vector((x, y, z)) * s + Vector((0, 0, gz))
    bm.normal_update()
    _uv_box(bm, "skora_cialo")
    g = _finish("glowa", bm, C, atlas_mat())
    for p in g.data.polygons:
        p.use_smooth = False
    return g


def cialo2(C, B, st, s, arm):
    K = st.get("plec") == "K"
    tul = st.get("tulow", "garnitur")
    pl, plk = st.get("plaszcz"), st.get("plaszcz_kol", "plaszcz_brazowy")
    tul_vis = plk if pl else tul
    rek = plk if pl else st.get("rekawy", tul)
    spo = st.get("spodnie", "garnitur")
    but = st.get("buty", "skora_czarna")
    nogi = st.get("nogi", spo if not K else "skora_cialo")
    gz = 1.68 * s
    body = cialo_skin(C, B, s, K)
    D, kosci = wagi(body, arm)
    names = [b.name for b in kosci]
    near = {i: names[int(np.argmin(D[i]))] for i in range(len(body.data.vertices))}
    P = body.data.vertices

    def strefa(c, n):
        z = c.z / s
        ax = abs(c.x) / s
        if z > 1.475 and ax < 0.075:
            return "skora_cialo"
        if ax > abs(B["LeftHand"][0].x) / s - 0.01:
            return "skora_cialo"
        if ax > abs(B["LeftUpperArm"][0].x) / s + 0.02 and z > 1.3:
            if ax > abs(B["LeftHand"][0].x) / s - 0.05 and not pl:
                return st.get("mankiet", "koszula") or rek
            return rek
        if z < 0.115:
            return but
        if z < 0.42 and st.get("buty_wysokie"):
            return but
        if z < 0.86:
            return nogi if (K or z < 0.82) else spo
        if z < 0.98 and not K:
            return spo
        return tul_vis
    uv_strefy(body, strefa)
    czesci = [body]
    # glowa + twarz
    g = glowa_mesh(C, s, gz)
    acc = [g]
    for sx in (-1, 1):
        acc.append(sphere("bialko", 0.0125 * s, (sx * 0.034 * s, -0.083 * s, gz + 0.012 * s), "koszula", C, seg=10, rings=6))
        acc.append(sphere("teczowka", 0.0065 * s, (sx * 0.034 * s, -0.094 * s, gz + 0.012 * s), "oczy", C, seg=8, rings=4))
        acc.append(box("brew", (0.032 * s, 0.01 * s, 0.007 * s), (sx * 0.035 * s, -0.092 * s, gz + 0.033 * s), st.get("wlosy") or "wlosy_ciemne", C, rot=(0, sx * -8, 0), bevel=0.003))
        acc.append(sphere("ucho", 0.024 * s, (sx * 0.09 * s, 0.005 * s, gz - 0.002 * s), "skora_cialo", C, seg=10, rings=6, scale=(0.4, 0.85, 1.25)))
    nos = bmesh.new()
    for p in ((0, -0.088, 0.03), (-0.016, -0.088, -0.022), (0.016, -0.088, -0.022), (0, -0.118, -0.018), (0, -0.1, -0.03)):
        nos.verts.new(Vector(p) * s + Vector((0, 0, gz)))
    nos.verts.ensure_lookup_table()
    vv = nos.verts
    for f in ((0, 3, 1), (0, 2, 3), (1, 3, 4), (3, 2, 4)):
        nos.faces.new([vv[i] for i in f])
    nos.normal_update(); _uv_box(nos, "skora_cialo")
    acc.append(_finish("nos", nos, C, atlas_mat()))
    acc.append(box("usta", (0.042 * s, 0.01 * s, 0.009 * s), (0, -0.088 * s, gz - 0.05 * s), "usta", C, bevel=0.003))
    if st.get("wasy"):
        acc.append(box("wasy", (0.055 * s, 0.012 * s, 0.012 * s), (0, -0.095 * s, gz - 0.035 * s), st["wasy"], C, bevel=0.004))
    if st.get("okulary"):
        for sx in (-1, 1):
            acc.append(torus("szklo", 0.019 * s, 0.0025 * s, (sx * 0.034 * s, -0.1 * s, gz + 0.012 * s), "zeliwo", C, rot=(90, 0, 0), seg=12, rseg=3))
        acc.append(box("mostek", (0.03 * s, 0.004 * s, 0.004 * s), (0, -0.1 * s, gz + 0.016 * s), "zeliwo", C))
    for o in wlosy(C, s, st.get("fryzura", "krotkie"), st.get("wlosy", "wlosy_ciemne"), gz):
        for p in o.data.polygons:
            p.use_smooth = False
        acc.append(o)
    acc += czapka(C, s, st.get("czapka"), st.get("czapka_kol", "garnitur"), gz)
    for o in acc:
        _grupa(o, "Head")
    czesci += acc
    # dodatki tulowia (plynne wagi)
    dod = []
    if pl:
        dl = 0.5 if pl == "dlugi" else 0.24
        pole = tuba("pola", C, 0.25 * s, 0.185 * s, (0.98 - dl) * s, 1.0 * s, plk)
        dod.append(pole)
        dod.append(torus("kolnierz", 0.07 * s, 0.024 * s, (0, 0.005 * s, 1.45 * s), plk, C, seg=16, rseg=6))
        for k in range(3):
            dod.append(sphere("guzik", 0.011 * s, (0.035 * s, -0.122 * s, (1.3 - k * 0.12) * s), "skora_czarna", C, seg=6, rings=3))
    elif st.get("kolnierz"):
        dod.append(torus("kolnierz", 0.06 * s, 0.016 * s, (0, 0.0, 1.47 * s), st["kolnierz"], C, seg=14, rseg=5))
    if K:
        sp = tuba("spodnica", C, 0.27 * s, 0.16 * s, 0.5 * s, 0.99 * s, st.get("spodnica", "sukienka_niebieska"), 0.82)
        dod.append(sp)
    if st.get("pas"):
        p_ = torus("pas", 0.15 * s, 0.018 * s, (0, 0, 1.0 * s), st["pas"], C, seg=20, rseg=4)
        for v in p_.data.vertices:
            v.co.y *= 0.74
        dod.append(p_)
        dod.append(box("klamra", (0.05 * s, 0.01 * s, 0.04 * s), (0, -0.113 * s, 1.0 * s), "stal", C, bevel=0.004))
    if st.get("kamizelka") and not pl:
        kz = cyl("kamizelka", 0.163 * s, 0.4 * s, (0, 0, 1.2 * s), st["kamizelka"], C, seg=20, r2=0.168 * s)
        for v in kz.data.vertices:
            v.co.y *= 0.74
        dod.append(kz)
    if st.get("krawat"):
        dod.append(box("krawat", (0.042 * s, 0.01 * s, 0.24 * s), (0, -0.122 * s, 1.31 * s), st["krawat"], C, bevel=0.005))
    if st.get("mucha"):
        dod.append(box("mucha", (0.08 * s, 0.015 * s, 0.03 * s), (0, -0.105 * s, 1.44 * s), st["mucha"], C, bevel=0.006))
    if st.get("okrycie"):
        ok = box("okrycie", (0.62 * s, 0.05 * s, 1.5 * s), (0, -0.15 * s, 1.12 * s), st["okrycie"], C, bevel=0.03 * s, seg=2)
        md = ok.modifiers.new("Sub", "SUBSURF"); md.levels = 1; apply_mods(ok)
        dod.append(ok)
    if st.get("buty_wysokie"):
        for sx in (-1, 1):
            hx = (0.09 if K else 0.10) * s
            dod.append(torus("cholewa", 0.058 * s, 0.008 * s, (sx * hx, 0.005 * s, 0.42 * s), but, C, seg=12, rseg=4))
    for o in dod:
        wagi(o, arm)
    czesci += dod
    with bpy.context.temp_override(active_object=body, selected_editable_objects=czesci, object=body):
        bpy.ops.object.join()
    return body
