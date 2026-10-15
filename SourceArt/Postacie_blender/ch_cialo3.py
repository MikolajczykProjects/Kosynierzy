# Postacie v3: WLASNA siatka (Skin + fasety), przekroje ciala zmierzone na bazach referencyjnych (BAZA_*_T),
# spodnice/poly z wagami biodra + oba uda. Nadpisuje zloz_postac z ch_baza. Wymaga ob_lib, ob_p2, ch_lib, ch_cialo2, ch_baza.


def przekroj(P, os_, poz, szer=0.012, maska=None):
    """Polowy szerokosci przekroju siatki w plaszczyznie prostopadlej do osi os_ ('x'/'z') w poz."""
    k = {"x": 0, "z": 2}[os_]
    m = np.abs(P[:, k] - poz) < szer
    if maska is not None:
        m &= maska
    s = P[m]
    if len(s) < 3:
        return None
    if os_ == "z":
        return (s[:, 0].max() - s[:, 0].min()) / 2, (s[:, 1].max() - s[:, 1].min()) / 2, s[:, 0].mean(), s[:, 1].mean()
    return (s[:, 2].max() - s[:, 2].min()) / 2, (s[:, 1].max() - s[:, 1].min()) / 2, s[:, 2].mean(), s[:, 1].mean()


def cialo_v3(C, B, baza, K):
    """Szkielet Skin wzdluz kosci; promienie z przekrojow bazy; tulow splaszczony wg glebokosci bazy."""
    P = np.array([v.co[:] for v in baza.data.vertices])
    H = P[:, 2].max()
    V, E, Rr = [], [], []

    def add(p, r, par=None):
        V.append(Vector(p)); Rr.append(max(r, 0.012))
        if par is not None:
            E.append((par, len(V) - 1))
        return len(V) - 1
    tul = np.abs(P[:, 0]) < 0.24
    zs = {"miednica": 0.52, "talia": 0.6, "klatka": 0.7, "piersi": 0.76, "barki": 0.8}
    prof = {}
    for n, f in zs.items():
        pr = przekroj(P, "z", f * H, 0.015 * H, tul & (np.abs(P[:, 0]) < (0.2 if f > 0.62 else 0.22)))
        prof[n] = pr if pr else (0.15, 0.1, 0, 0)
    root = add((0, 0, zs["miednica"] * H), max(prof["miednica"][0], 0.16) * 0.97)
    ta = add((0, 0, zs["talia"] * H), prof["talia"][0] * 0.95, root)
    kl = add((0, 0, zs["klatka"] * H), prof["klatka"][0] * (0.98 if K else 1.04), ta)
    br = add((0, 0, zs["piersi"] * H), prof["piersi"][0] * (0.98 if K else 1.06), kl)
    hz = B["Neck"][0].z
    sz = add((0, 0, hz - 0.01), 0.062 * H / 1.8, br)
    add((0, 0, hz + 0.08 * H / 1.8), 0.052 * H / 1.8, sz)
    for side, sx in (("Left", 1), ("Right", -1)):
        s0 = B[side + "UpperArm"][0]; e0 = B[side + "LowerArm"][0]; w0 = B[side + "Hand"][0]; f0 = B[side + "Hand"][1]
        reka = (P[:, 0] * sx > 0) & (np.abs(P[:, 2] - s0.z) < 0.12)

        def r_x(x, d=0.045):
            pr = przekroj(P, "x", x, 0.012, reka)
            return max(pr[0], pr[1]) if pr else d
        k_ = H / 1.8
        # v4 (Otton): rekaw jako rowna, lekko zwezajaca sie tuba (bez "balona" na bicepsie i patyka w nadgarstku)
        rk = 0.9 if K else 1.0
        bark = add((s0.x - sx * 0.02, 0, s0.z - 0.005), 0.06 * k_ * rk, br)
        bic = add(s0 + (e0 - s0) * 0.45, 0.05 * k_ * rk, bark)
        lok = add(e0, 0.046 * k_ * rk, bic)
        prz = add(e0 + (w0 - e0) * 0.4, 0.043 * k_ * rk, lok)
        nad = add(w0 - (w0 - e0) * 0.06, 0.038 * k_ * rk, prz)
        dl = add(w0 + (f0 - w0) * 0.4, 0.042 * k_ * rk, nad)
        add(f0 + (f0 - w0) * 0.1, 0.034 * k_ * rk, dl)
        add(w0 + (f0 - w0) * 0.3 + Vector((0, -0.045, -0.006)) * k_, 0.017 * k_ * rk, nad)
        hip = B[side + "UpperLeg"][0]; kn = B[side + "LowerLeg"][0]; an = B[side + "Foot"][0]; to = B[side + "Toes"][1]
        noga = (P[:, 0] * sx > 0.01)

        def r_z(z, d=0.06):
            pr = przekroj(P, "z", z, 0.012, noga)
            return max(pr[0], pr[1]) if pr else d
        # v4 (Otton): pelniejsze nogi (nogawki), wieksze buty
        k_ = H / 1.8
        if K:   # gole nogi pod spodnica: smuklej, ale nie patyk
            bi = add(hip + Vector((0, 0, -0.03)), 0.088 * k_, root)
            ud = add(hip + (kn - hip) * 0.45, 0.074 * k_, bi)
            ko = add(kn, 0.054 * k_, ud)
            ly = add(kn + (an - kn) * 0.35 + Vector((0, 0.012, 0)), 0.056 * k_, ko)
            ks = add(an + Vector((0, 0, 0.02)), 0.038 * k_, ly)
        else:   # spodnie: prosta nogawka
            bi = add(hip + Vector((0, 0, -0.03)), 0.1 * k_, root)
            ud = add(hip + (kn - hip) * 0.45, 0.088 * k_, bi)
            ko = add(kn, 0.07 * k_, ud)
            ly = add(kn + (an - kn) * 0.35 + Vector((0, 0.012, 0)), 0.066 * k_, ko)
            ks = add(an + Vector((0, 0, 0.03)), 0.056 * k_, ly)
        add(Vector((to.x, to.y + 0.01, 0.034 * k_)), 0.042 * k_, ks)
        add(Vector((an.x, an.y + 0.045, 0.042 * k_)), 0.042 * k_, ks)
    o = skin_mesh("cialo", C, V, E, Rr, poziom=1, root=root)
    # glebokosc tulowia jak w bazie (interpolacja po wysokosci)
    tab = sorted((f * H, prof[n][1] / max(prof[n][0], 1e-3), prof[n][3]) for n, f in zs.items())
    zz = [t[0] for t in tab]
    for v in o.data.vertices:
        if abs(v.co.x) < 0.24 * H / 1.8 and zz[0] - 0.08 < v.co.z < zz[-1] + 0.06:
            ratio = float(np.interp(v.co.z, zz, [t[1] for t in tab]))
            v.co.y *= min(max(ratio, 0.55), 1.0)
        if v.co.z < 0.0:
            v.co.z = 0.0
    if K:   # delikatny biust (mniejszy niz w bazie)
        zb = zs["piersi"] * H - 0.03
        for v in o.data.vertices:
            if v.co.y < 0 and abs(v.co.z - zb) < 0.07 and abs(v.co.x) < 0.14:
                f = (1 - abs(v.co.z - zb) / 0.07) * max(0, 1 - abs(abs(v.co.x) - 0.065) / 0.06)
                v.co.y -= 0.018 * f
    for p in o.data.polygons:
        p.use_smooth = False
    return o


def wagi_spodnicy(o, arm, idx, H, z_gora, z_dol):
    """Spodnica/poly: u gory biodra, nizej coraz wiecej wplywu obu ud (strona wg x)."""
    for n in ("Hips", "LeftUpperLeg", "RightUpperLeg"):
        if n not in o.vertex_groups:
            o.vertex_groups.new(name=n)
    for i in idx:
        v = o.data.vertices[i]
        for g in list(v.groups):
            o.vertex_groups[g.group].remove([i])
        t = min(1, max(0, (z_gora - v.co.z) / max(z_gora - z_dol, 1e-3)))
        side = max(-1, min(1, v.co.x / 0.12))
        nog = 0.7 * t
        o.vertex_groups["Hips"].add([i], 1 - nog, "REPLACE")
        o.vertex_groups["LeftUpperLeg"].add([i], nog * (0.5 + 0.5 * side), "REPLACE")
        o.vertex_groups["RightUpperLeg"].add([i], nog * (0.5 - 0.5 * side), "REPLACE")


def zloz_postac(nazwa, st):
    zaladuj_bazy()
    K = st.get("plec") == "K"
    baza = bpy.data.objects["BAZA_K_T" if K else "BAZA_M_T"]
    pom = pomiary(baza)
    H = pom["H"]; s = H / 1.8
    C = new_collection(nazwa)
    B = kosci_z_pomiarow(pom, "K" if K else "M")
    arm = zrob_armature(nazwa + "_Rig", C, B)
    body = cialo_v3(C, B, baza, K)
    body.name = nazwa + "_Cialo"
    tul = st.get("tulow", "garnitur")
    pl, plk = st.get("plaszcz"), st.get("plaszcz_kol", "plaszcz_brazowy")
    tul_vis = plk if pl else (st.get("kamizelka") or tul)
    rek = plk if pl else st.get("rekawy", tul)
    spo = st.get("spodnie", "garnitur")
    but = st.get("buty", "skora_czarna")
    nogi = st.get("nogi", spo if not K else "skora_cialo")
    wl = st.get("wlosy") or "wlosy_ciemne"
    sh_x = abs(B["LeftUpperArm"][0].x); wrist_x = abs(B["LeftHand"][0].x)

    def strefa(c, n):
        z = c.z / H; ax = abs(c.x)
        if z > 0.83 and ax < 0.11:
            return "skora_cialo"
        if ax > wrist_x - 0.005:
            return "skora_cialo"
        if ax > sh_x + 0.02 and z > 0.7:
            if ax > wrist_x - 0.04 and not pl and st.get("mankiet", "koszula"):
                return st.get("mankiet", "koszula")
            return rek
        if z < 0.055:
            return but
        if z < 0.25 and st.get("buty_wysokie"):
            return but
        if K and not pl and 0.3 < z < 0.57:
            return st.get("spodnica", "sukienka_niebieska")
        if z < 0.47:
            return nogi
        if z < 0.56 and not K:
            return spo
        if K and z < 0.56:
            return st.get("spodnica", "sukienka_niebieska")
        return tul_vis
    uv_strefy(body, strefa)
    for p in body.data.polygons:
        p.use_smooth = False
    wagi(body, arm)
    gz = B["Head"][0].z + 0.105 * s
    acc = [glowa_mesh(C, s, gz)]
    for sx in (-1, 1):
        acc.append(sphere("bialko", 0.0125 * s, (sx * 0.034 * s, -0.083 * s, gz + 0.012 * s), "koszula", C, seg=8, rings=5))
        acc.append(sphere("teczowka", 0.0065 * s, (sx * 0.034 * s, -0.094 * s, gz + 0.012 * s), "oczy", C, seg=6, rings=4))
        acc.append(box("brew", (0.032 * s, 0.01 * s, 0.007 * s), (sx * 0.035 * s, -0.092 * s, gz + 0.033 * s), wl, C, rot=(0, sx * -8, 0), bevel=0.003))
        acc.append(sphere("ucho", 0.024 * s, (sx * 0.09 * s, 0.005 * s, gz - 0.002 * s), "skora_cialo", C, seg=8, rings=5, scale=(0.4, 0.85, 1.25)))
    nos = bmesh.new()
    for p in ((0, -0.088, 0.03), (-0.016, -0.088, -0.022), (0.016, -0.088, -0.022), (0, -0.118, -0.018), (0, -0.1, -0.03)):
        nos.verts.new(Vector(p) * s + Vector((0, 0, gz)))
    nos.verts.ensure_lookup_table(); vv = nos.verts
    for f in ((0, 3, 1), (0, 2, 3), (1, 3, 4), (3, 2, 4)):
        nos.faces.new([vv[i] for i in f])
    nos.normal_update(); _uv_box(nos, "skora_cialo")
    acc.append(_finish("nos", nos, C, atlas_mat()))
    acc.append(box("usta", (0.042 * s, 0.01 * s, 0.009 * s), (0, -0.09 * s, gz - 0.05 * s), "usta", C, bevel=0.003))
    if st.get("wasy"):
        acc.append(box("wasy", (0.055 * s, 0.012 * s, 0.012 * s), (0, -0.097 * s, gz - 0.035 * s), st["wasy"], C, bevel=0.004))
    if st.get("okulary"):
        for sx in (-1, 1):
            acc.append(torus("szklo", 0.019 * s, 0.0025 * s, (sx * 0.034 * s, -0.102 * s, gz + 0.012 * s), "zeliwo", C, rot=(90, 0, 0), seg=10, rseg=3))
        acc.append(box("mostek", (0.03 * s, 0.004 * s, 0.004 * s), (0, -0.102 * s, gz + 0.016 * s), "zeliwo", C))
    acc += wlosy(C, s, st.get("fryzura", "krotkie"), wl, gz)
    acc += czapka(C, s, st.get("czapka"), st.get("czapka_kol", "garnitur"), gz)
    for o in acc:
        for p in o.data.polygons:
            p.use_smooth = False
        _grupa(o, "Head")
    dod, spod = [], []
    if pl:
        dl = 0.5 if pl == "dlugi" else 0.24
        top = 0.58 * H
        rx, ry = promien_ciala(body, top, H)
        t_ = tuba("pola", C, rx + 0.12, rx + 0.012, top - dl * s, top, plk, (ry + 0.025) / (rx + 0.012))
        spod.append((t_, top, top - dl * s))
        dod.append(torus("kolnierz", 0.072 * s, 0.022 * s, (0, 0.0, B["Neck"][0].z + 0.01), plk, C, seg=12, rseg=5))
    elif st.get("kolnierz"):
        dod.append(torus("kolnierz", 0.064 * s, 0.014 * s, (0, 0.0, B["Neck"][0].z + 0.01), st["kolnierz"], C, seg=12, rseg=4))
    if K and not pl:
        rx, ry = promien_ciala(body, 0.57 * H, H)
        t_ = tuba("spodnica", C, rx + 0.17, rx + 0.01, 0.31 * H, 0.57 * H, st.get("spodnica", "sukienka_niebieska"), (ry + 0.015) / (rx + 0.01))
        spod.append((t_, 0.57 * H, 0.31 * H))
    if st.get("pas"):
        rx, ry = promien_ciala(body, 0.56 * H, H)
        p_ = torus("pas", rx + 0.008, 0.016 * s, (0, 0, 0.56 * H), st["pas"], C, seg=16, rseg=4)
        for v in p_.data.vertices:
            v.co.y *= (ry + 0.008) / (rx + 0.008)
        dod.append(p_)
        dod.append(box("klamra", (0.05 * s, 0.01 * s, 0.035 * s), (0, -(ry + 0.012), 0.56 * H), "stal", C, bevel=0.004))
    ky = promien_ciala(body, 0.73 * H, H)[1]
    if st.get("krawat"):
        dod.append(box("krawat", (0.04 * s, 0.01 * s, 0.22 * s), (0, -ky - 0.004, 0.735 * H), st["krawat"], C, bevel=0.005))
    if st.get("mucha"):
        dod.append(box("mucha", (0.075 * s, 0.015 * s, 0.028 * s), (0, -ky + 0.01, 0.8 * H), st["mucha"], C, bevel=0.006))
    if st.get("okrycie"):
        dod.append(box("okrycie", (0.62 * s, 0.05 * s, 1.5 * s), (0, -ky - 0.05, 0.62 * H), st["okrycie"], C, bevel=0.03 * s, seg=2))
    for o in dod:
        for p in o.data.polygons:
            p.use_smooth = False
        wagi(o, arm)
    for o, zg, zd in spod:
        for p in o.data.polygons:
            p.use_smooth = False
        wagi_spodnicy(o, arm, range(len(o.data.vertices)), H, zg, zd)
    with bpy.context.temp_override(active_object=body, selected_editable_objects=[body] + acc + dod + [x[0] for x in spod], object=body):
        bpy.ops.object.join()
    body.data.materials.clear(); body.data.materials.append(postac_mat())
    body.parent = arm
    body.modifiers.new("Armature", "ARMATURE").object = arm
    arm.data.display_type = "STICK"
    arm.hide_set(True)
    arm["plec"] = "K" if K else "M"
    return C, arm, body
