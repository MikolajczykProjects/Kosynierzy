// Rola D (obiekty). Skrypt EDYTORA, nie logika gry.
// Sklada prefaby OB_* z modeli FBX (Blender) w Assets/_Obiekty/Modele:
// materialy URP, collidery, punkty, Animatory z klipami, swiatla i efekty FX_*.
// Menu: Obiekty > Zbuduj wszystko. Mozna odpalac wiele razy (nadpisuje).
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public class ObiektyImport : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith("Assets/_Obiekty/Modele")) return;
        var m = (ModelImporter)assetImporter;
        m.globalScale = 1f;
        m.useFileScale = true;
        m.importCameras = false;
        m.importLights = false;
        m.importAnimation = false;
        m.animationType = ModelImporterAnimationType.None;
        m.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        m.materialLocation = ModelImporterMaterialLocation.InPrefab;
    }

    // T_*_N = normal mapa, T_*_MS = metal (R) + gladkosc (A) — dane liniowe, nie kolor
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/_Obiekty/Materialy")) return;
        var t = (TextureImporter)assetImporter;
        string n = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        t.maxTextureSize = 2048;
        if (n.EndsWith("_N")) t.textureType = TextureImporterType.NormalMap;
        else if (n.EndsWith("_MS")) { t.sRGBTexture = false; t.alphaSource = TextureImporterAlphaSource.FromInput; }
    }
}

public static partial class BudujObiekty
{
    const string R = "Assets/_Obiekty";
    const string MOD = R + "/Modele";
    const string MAT = R + "/Materialy";
    const string ANI = R + "/Animacje";
    const string FXD = R + "/Efekty";
    const string PRE = R + "/Prefaby";

    // materialy swiecace (nazwa materialu z Blendera -> kolor emisji HDR)
    static readonly Dictionary<string, Color> Emisja = new Dictionary<string, Color>
    {
        { "M_Lampka_Czerwona", new Color(1f, 0.05f, 0.02f) * 4f },
        { "M_Klosz",           new Color(1f, 0.78f, 0.42f) * 3f },
        { "M_Reflektor",       new Color(1f, 0.9f, 0.65f) * 3f },
        { "M_Lampa_Ciemnia",   new Color(1f, 0.06f, 0.03f) * 3f },
        { "M_Lampa_Radio",     new Color(1f, 0.7f, 0.3f) * 1.5f },
    };

    [MenuItem("Obiekty/Zbuduj wszystko")]
    public static void ZbudujWszystko()
    {
        foreach (var d in new[] { MAT, ANI, FXD, PRE })
            if (!AssetDatabase.IsValidFolder(d)) AssetDatabase.CreateFolder(R, Path.GetFileName(d));

        Efekty();
        Priorytet1();
        Priorytet2();
        Priorytet3();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Obiekty: prefaby zbudowane w " + PRE);
    }

    [MenuItem("Obiekty/Scena testowa")]
    public static void ScenaTestowa()
    {
        var sc = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var kostka = GameObject.CreatePrimitive(PrimitiveType.Cube);
        kostka.name = "Skala_Kostka_1m"; kostka.transform.position = new Vector3(-3f, 0.5f, 0f);
        var kaps = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        kaps.name = "Skala_Czlowiek_1_8m"; kaps.transform.position = new Vector3(-4.5f, 0.9f, 0f);
        kaps.transform.localScale = new Vector3(1f, 0.9f, 1f);
        var podloga = GameObject.CreatePrimitive(PrimitiveType.Plane);
        podloga.name = "Podloga"; podloga.transform.localScale = new Vector3(12f, 1f, 4f);
        podloga.transform.position = new Vector3(50f, 0f, 5f);
        float x = 0f;
        foreach (var g in AssetDatabase.FindAssets("t:Prefab", new[] { PRE }))
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g));
            var i = (GameObject)PrefabUtility.InstantiatePrefab(p);
            var b = Granice(i.transform, i.transform);
            x += Mathf.Max(1.5f, b.size.x * 0.5f + 0.75f);
            i.transform.position = new Vector3(x, 0f, 0f);
            x += Mathf.Max(1.5f, b.size.x * 0.5f + 0.75f);
        }
        EditorSceneManager.SaveScene(sc, R + "/Test_Obiekty.unity");
    }

    // ------------------------------------------------------------------ materialy
    static Material Mat(string nazwa)
    {
        string p = $"{MAT}/{nazwa}.mat";
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, p); }
        m.shader = sh;
        string t = $"{MAT}/T_{nazwa.Substring(2)}";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(t + ".png");
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Smoothness", 0.1f);
        m.SetFloat("_Metallic", 0f);
        // metal + gladkosc z mapy (chrom blyszczy, papier matowy); bez mapy zostaje 0,1
        var ms = AssetDatabase.LoadAssetAtPath<Texture2D>(t + "_MS.png");
        m.SetTexture("_MetallicGlossMap", ms);
        if (ms)
        {
            m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.SetFloat("_Smoothness", 1f);              // mnoznik alfy mapy
            m.SetFloat("_SmoothnessTextureChannel", 0f); // alfa mapy metalu
        }
        else m.DisableKeyword("_METALLICSPECGLOSSMAP");
        var nrm = AssetDatabase.LoadAssetAtPath<Texture2D>(t + "_N.png");
        m.SetTexture("_BumpMap", nrm);
        if (nrm) { m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 1f); }
        else m.DisableKeyword("_NORMALMAP");
        if (Emisja.TryGetValue(nazwa, out var e))
        {
            float mx = Mathf.Max(e.r, Mathf.Max(e.g, e.b));
            m.SetColor("_BaseColor", e / mx);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", e);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static string CzystaNazwa(string n)
    {
        int k = n.IndexOf(' '); if (k >= 0) n = n.Substring(0, k);
        k = n.IndexOf('.'); if (k >= 0) n = n.Substring(0, k);
        return n;
    }

    static void PodmienMaterialy(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != null) mats[i] = Mat(CzystaNazwa(mats[i].name));
            r.sharedMaterials = mats;
            r.shadowCastingMode = ShadowCastingMode.On;
        }
    }

    // ------------------------------------------------------------------ model / prefab
    static GameObject Model(string nazwa, string plik = null)
    {
        var m = AssetDatabase.LoadAssetAtPath<GameObject>($"{MOD}/{plik ?? nazwa}.fbx");
        if (m == null) { Debug.LogWarning($"Obiekty: brak modelu {MOD}/{plik ?? nazwa}.fbx — pomijam {nazwa}"); return null; }
        var go = Object.Instantiate(m);
        go.name = nazwa;
        if (go.transform.rotation != Quaternion.identity)
            Debug.LogWarning($"{nazwa}: root FBX ma rotacje {go.transform.eulerAngles} — sprawdz orientacje");
        go.transform.position = Vector3.zero;
        go.transform.localScale = Vector3.one;
        PodmienMaterialy(go);
        return go;
    }

    static void Zapisz(GameObject go)
    {
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        PrefabUtility.SaveAsPrefabAsset(go, $"{PRE}/{go.name}.prefab");
        Object.DestroyImmediate(go);
    }

    static Transform Znajdz(Transform t, string n)
    {
        if (t.name == n) return t;
        foreach (Transform c in t) { var r = Znajdz(c, n); if (r) return r; }
        return null;
    }

    static Transform Punkt(GameObject go, string nazwa, Vector3 poz, Transform rodzic = null)
    {
        var t = Znajdz(go.transform, nazwa);
        if (t == null) { t = new GameObject(nazwa).transform; t.SetParent(rodzic ? rodzic : go.transform, false); t.localPosition = poz; }
        return t;
    }

    static Bounds Granice(Transform w, Transform zakres)
    {
        var b = new Bounds(); bool init = false;
        foreach (var mf in zakres.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            var mb = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = w.InverseTransformPoint(mf.transform.TransformPoint(c));
                if (!init) { b = new Bounds(p, Vector3.zero); init = true; } else b.Encapsulate(p);
            }
        }
        return b;
    }

    static BoxCollider Kolizja(GameObject cel, Transform zakres = null, bool trigger = false)
    {
        var b = Granice(cel.transform, zakres ? zakres : cel.transform);
        var bc = cel.AddComponent<BoxCollider>();
        bc.center = b.center; bc.size = b.size; bc.isTrigger = trigger;
        return bc;
    }

    static Light Swiatlo(Transform rodzic, string nazwa, LightType typ, Color kolor, float zasieg, float moc,
                         Vector3 poz, Vector3 obrot, float kat = 60f)
    {
        var go = new GameObject(nazwa);
        go.transform.SetParent(rodzic, false);
        go.transform.localPosition = poz;
        go.transform.localEulerAngles = obrot;
        var l = go.AddComponent<Light>();
        l.type = typ; l.color = kolor; l.range = zasieg; l.intensity = moc;
        if (typ == LightType.Spot) { l.spotAngle = kat; l.innerSpotAngle = kat * 0.6f; }
        l.shadows = LightShadows.None;
        return l;
    }

    static GameObject Wstaw(string fx, Transform rodzic, string nazwa = null)
    {
        var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{FXD}/{fx}.prefab");
        var g = (GameObject)PrefabUtility.InstantiatePrefab(p);
        g.transform.SetParent(rodzic, false);
        g.transform.localPosition = Vector3.zero;
        if (nazwa != null) g.name = nazwa;
        return g;
    }

    // ------------------------------------------------------------------ animacje
    struct K
    {
        public string s; public System.Type t; public string p; public AnimationCurve c;
        public K(string s, System.Type t, string p, AnimationCurve c) { this.s = s; this.t = t; this.p = p; this.c = c; }
    }

    // krzywa z par (czas, wartosc); liniowa=true -> stale tempo (np. obrot w petli)
    static AnimationCurve Kr(bool liniowa, params float[] tv)
    {
        var c = new AnimationCurve();
        for (int i = 0; i < tv.Length; i += 2) c.AddKey(new Keyframe(tv[i], tv[i + 1]));
        for (int i = 0; i < c.length; i++)
        {
            if (liniowa)
            {
                AnimationUtility.SetKeyLeftTangentMode(c, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(c, i, AnimationUtility.TangentMode.Linear);
            }
            else
            {
                AnimationUtility.SetKeyLeftTangentMode(c, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(c, i, AnimationUtility.TangentMode.ClampedAuto);
            }
        }
        return c;
    }

    static AnimationCurve Stala(float v, float czas = 0.1f) => Kr(true, 0f, v, czas, v);
    static AnimationCurve Schodki(params float[] tv)
    {
        var c = new AnimationCurve();
        for (int i = 0; i < tv.Length; i += 2) c.AddKey(new Keyframe(tv[i], tv[i + 1]));
        for (int i = 0; i < c.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(c, i, AnimationUtility.TangentMode.Constant);
            AnimationUtility.SetKeyRightTangentMode(c, i, AnimationUtility.TangentMode.Constant);
        }
        return c;
    }

    static K[] Obrot(string sciezka, AnimationCurve x, AnimationCurve y, AnimationCurve z) => new[]
    {
        new K(sciezka, typeof(Transform), "localEulerAnglesRaw.x", x),
        new K(sciezka, typeof(Transform), "localEulerAnglesRaw.y", y),
        new K(sciezka, typeof(Transform), "localEulerAnglesRaw.z", z),
    };

    static K[] Pozycja(string sciezka, AnimationCurve x, AnimationCurve y, AnimationCurve z) => new[]
    {
        new K(sciezka, typeof(Transform), "m_LocalPosition.x", x),
        new K(sciezka, typeof(Transform), "m_LocalPosition.y", y),
        new K(sciezka, typeof(Transform), "m_LocalPosition.z", z),
    };

    static K[] Emisja4(string sciezka, AnimationCurve r, AnimationCurve g, AnimationCurve b) => new[]
    {
        new K(sciezka, typeof(MeshRenderer), "material._EmissionColor.r", r),
        new K(sciezka, typeof(MeshRenderer), "material._EmissionColor.g", g),
        new K(sciezka, typeof(MeshRenderer), "material._EmissionColor.b", b),
    };

    static K Wlacz(string sciezka, System.Type typ, AnimationCurve c) => new K(sciezka, typ, "m_Enabled", c);

    static AnimationClip Klip(string nazwa, bool petla, params K[][] grupy)
    {
        string p = $"{ANI}/{nazwa}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(p);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, p); }
        else clip.ClearCurves();
        foreach (var g in grupy)
            foreach (var k in g)
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(k.s, k.t, k.p), k.c);
        var st = AnimationUtility.GetAnimationClipSettings(clip);
        st.loopTime = petla;
        AnimationUtility.SetAnimationClipSettings(clip, st);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    static K[] J(params K[] k) => k;

    static AnimatorController Kontroler(string nazwa)
    {
        string p = $"{ANI}/{nazwa}.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(p) != null) AssetDatabase.DeleteAsset(p);
        return AnimatorController.CreateAnimatorControllerAtPath(p);
    }

    static AnimatorState Stan(AnimatorStateMachine sm, string nazwa, Motion m, Vector2 poz)
    {
        var s = sm.AddState(nazwa, new Vector3(poz.x, poz.y, 0));
        s.motion = m; s.writeDefaultValues = false;
        return s;
    }

    static AnimatorStateTransition Do(AnimatorState z, AnimatorState d, float czas = 0f, bool poKoncu = false)
    {
        var t = z.AddTransition(d);
        t.hasExitTime = poKoncu; t.exitTime = 1f;
        t.duration = czas; t.hasFixedDuration = true;
        return t;
    }

    static AnimatorControllerLayer NowaWarstwa(AnimatorController ac, string nazwa)
    {
        ac.AddLayer(nazwa);
        var ls = ac.layers;
        ls[ls.Length - 1].defaultWeight = 1f;
        ac.layers = ls;
        return ac.layers[ls.Length - 1];
    }

    static void DodajAnimator(GameObject go, AnimatorController ac)
    {
        var a = go.GetComponent<Animator>();
        if (a == null) a = go.AddComponent<Animator>();
        a.runtimeAnimatorController = ac;
        a.applyRootMotion = false;
        a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    // Bool: stan WYL (domyslny) <-> WL, opcjonalnie przejscia przez klip
    static void BoolWlWyl(AnimatorStateMachine sm, string param, AnimationClip wyl, AnimationClip wl, bool startWl)
    {
        var sWyl = Stan(sm, param + "_Nie", wyl, new Vector2(250, 0));
        var sWl = Stan(sm, param + "_Tak", wl, new Vector2(250, 120));
        sm.defaultState = startWl ? sWl : sWyl;
        Do(sWyl, sWl).AddCondition(AnimatorConditionMode.If, 0, param);
        Do(sWl, sWyl).AddCondition(AnimatorConditionMode.IfNot, 0, param);
    }

    // ------------------------------------------------------------------ efekty
    static Texture2D Kolko()
    {
        string p = $"{FXD}/T_FX_Kolko.png";
        if (!File.Exists(p))
        {
            var t = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32, 32)) / 32f;
                    float a = Mathf.Clamp01(1f - d); a *= a;
                    t.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            File.WriteAllBytes(p, t.EncodeToPNG());
            AssetDatabase.ImportAsset(p);
            var ti = (TextureImporter)AssetImporter.GetAtPath(p);
            ti.alphaIsTransparency = true; ti.mipmapEnabled = false;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }

    static Material FxMat(string nazwa, bool addytywny)
    {
        string p = $"{FXD}/{nazwa}.mat";
        var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, p); }
        m.shader = sh;
        m.SetTexture("_BaseMap", Kolko());
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", addytywny ? 2f : 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)(addytywny ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(m);
        return m;
    }

    static ParticleSystem Czastki(GameObject go, Material mat, bool petla, float czas,
        Vector2 zycie, Vector2 predkosc, Vector2 rozmiar, Color kolor, int wybuch, float naSekunde,
        float katStozka, float grawitacja, bool zanik = true, float wzrost = 1f)
    {
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = czas; main.loop = petla; main.playOnAwake = petla;
        main.startLifetime = new ParticleSystem.MinMaxCurve(zycie.x, zycie.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(predkosc.x, predkosc.y);
        main.startSize = new ParticleSystem.MinMaxCurve(rozmiar.x, rozmiar.y);
        main.startColor = kolor;
        main.gravityModifier = grawitacja;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.None;
        main.maxParticles = 200;
        var em = ps.emission;
        em.rateOverTime = naSekunde;
        em.SetBursts(wybuch > 0 ? new[] { new ParticleSystem.Burst(0f, (short)wybuch) } : new ParticleSystem.Burst[0]);
        var sh = ps.shape;
        sh.enabled = katStozka >= 0f;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = Mathf.Max(0f, katStozka); sh.radius = 0.01f;
        if (zanik)
        {
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
        }
        if (wzrost != 1f)
        {
            var sol = ps.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f / wzrost, 1f, 1f));
        }
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        return ps;
    }

    static void ZapiszFx(GameObject go)
    {
        PrefabUtility.SaveAsPrefabAsset(go, $"{FXD}/{go.name}.prefab");
        Object.DestroyImmediate(go);
    }

    static void Efekty()
    {
        var add = FxMat("M_FX_Blysk", true);
        var dym = FxMat("M_FX_Dym", false);

        // FX_BlyskLufy: zolto-bialy blysk 0,05 s + szary dymek (leci w +Z)
        var bl = new GameObject("FX_BlyskLufy");
        Czastki(bl, add, false, 0.1f, new Vector2(0.05f, 0.05f), Vector2.zero, new Vector2(0.14f, 0.2f),
                new Color(1f, 0.9f, 0.6f), 2, 0f, -1f, 0f, true);
        var d = new GameObject("Dym"); d.transform.SetParent(bl.transform, false);
        Czastki(d, dym, false, 0.6f, new Vector2(0.4f, 0.7f), new Vector2(0.2f, 0.6f), new Vector2(0.04f, 0.07f),
                new Color(0.6f, 0.6f, 0.6f, 0.45f), 6, 0f, 12f, -0.02f, true, 4f);
        ZapiszFx(bl);

        // FX_Iskra: iskry ze zwarcia kabla, 0,5 s
        var isk = new GameObject("FX_Iskra");
        var ps = Czastki(isk, add, false, 0.5f, new Vector2(0.2f, 0.5f), new Vector2(1.2f, 3f), new Vector2(0.015f, 0.03f),
                         new Color(1f, 0.75f, 0.3f), 30, 0f, 70f, 1f, true);
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.02f;
        var rr = isk.GetComponent<ParticleSystemRenderer>();
        rr.renderMode = ParticleSystemRenderMode.Stretch; rr.velocityScale = 0.04f; rr.lengthScale = 1f;
        ZapiszFx(isk);

        // FX_Dym: dym z komina, petla (Play On Awake ON), leci w gore
        var dm = new GameObject("FX_Dym");
        dm.transform.localEulerAngles = new Vector3(-90f, 0f, 0f);
        Czastki(dm, dym, true, 5f, new Vector2(3f, 4.5f), new Vector2(0.8f, 1.4f), new Vector2(0.4f, 0.7f),
                new Color(0.35f, 0.35f, 0.35f, 0.6f), 0, 8f, 12f, -0.05f, true, 4f);
        ZapiszFx(dm);

        // FX_FlashAparatu: bialy blysk
        var fl = new GameObject("FX_FlashAparatu");
        Czastki(fl, add, false, 0.1f, new Vector2(0.08f, 0.08f), Vector2.zero, new Vector2(0.6f, 0.6f),
                Color.white, 1, 0f, -1f, 0f, true);
        ZapiszFx(fl);

        // FX_Luska: mosiezna luska wylatujaca w prawo-gore (leci w +Z obiektu)
        var lu = new GameObject("FX_Luska");
        Czastki(lu, FxMat("M_FX_Luska", false), false, 0.8f, new Vector2(0.7f, 0.9f), new Vector2(1.5f, 2.5f),
                new Vector2(0.012f, 0.014f), new Color(0.85f, 0.65f, 0.25f), 1, 0f, 15f, 1f, false);
        ZapiszFx(lu);
    }

    // ================================================================== PRIORYTET 1
    static void Priorytet1()
    {
        Drzwi();
        KartaHistorii();
        PistoletVis();
        Lacznica();
    }

    static void Drzwi()
    {
        var go = Model("OB_Drzwi"); if (!go) return;
        var skrzydlo = Znajdz(go.transform, "Skrzydlo");
        Kolizja(skrzydlo.gameObject);
        Punkt(go, "PunktInterakcji", new Vector3(-0.5f, 0, 0.8f));

        const string S = "Skrzydlo";
        var zero = Stala(0f);
        var zamk = Klip("Drzwi_Zamkniete", false, Obrot(S, zero, zero, zero));
        var otw = Klip("Drzwi_Otwieranie", false, Obrot(S, Stala(0, .4f), Kr(false, 0, 0, .4f, 100), Stala(0, .4f)));
        var zam = Klip("Drzwi_Zamykanie", false, Obrot(S, Stala(0, .4f), Kr(false, 0, 100, .4f, 0), Stala(0, .4f)));
        var auto = Klip("Drzwi_OtworzIZamknij", false,
            Obrot(S, Stala(0, 1.8f), Kr(false, 0, 0, .4f, 100, 1.4f, 100, 1.8f, 0), Stala(0, 1.8f)));

        var ac = Kontroler("AC_Drzwi");
        ac.AddParameter("Otwarte", AnimatorControllerParameterType.Bool);
        ac.AddParameter("Otworz", AnimatorControllerParameterType.Trigger);
        var sm = ac.layers[0].stateMachine;
        var sZ = Stan(sm, "Zamkniete", zamk, new Vector2(250, 0));
        var sO = Stan(sm, "Otwarte", otw, new Vector2(500, 0));
        var sZm = Stan(sm, "Zamykanie", zam, new Vector2(500, 120));
        var sA = Stan(sm, "OtworzIZamknij", auto, new Vector2(250, 120));
        sm.defaultState = sZ;
        Do(sZ, sO).AddCondition(AnimatorConditionMode.If, 0, "Otwarte");
        Do(sO, sZm).AddCondition(AnimatorConditionMode.IfNot, 0, "Otwarte");
        Do(sZm, sO).AddCondition(AnimatorConditionMode.If, 0, "Otwarte");
        Do(sZm, sZ, 0f, true);
        Do(sZ, sA).AddCondition(AnimatorConditionMode.If, 0, "Otworz");
        Do(sA, sZ, 0f, true);
        DodajAnimator(go, ac);
        Zapisz(go);
    }

    static void KartaHistorii()
    {
        var go = Model("OB_Karta_Historii"); if (!go) return;
        var karta = Znajdz(go.transform, "Karta");
        Kolizja(go, null, true).size = new Vector3(0.5f, 0.6f, 0.5f);
        go.GetComponent<BoxCollider>().center = new Vector3(0, 1f, 0);
        Swiatlo(go.transform, "Poswiata", LightType.Point, new Color(1f, 0.85f, 0.45f), 2f, 0.6f, new Vector3(0, 1f, 0), Vector3.zero);
        var idle = Klip("Karta_Idle", true,
            Obrot("Karta", Stala(0, 4f), Kr(true, 0, 0, 4f, 360), Kr(false, 0, -4, 2f, 4, 4f, -4)),
            Pozycja("Karta", Stala(0, 4f), Kr(false, 0, 1f, 1f, 1.08f, 2f, 1f, 3f, 1.08f, 4f, 1f), Stala(0, 4f)));
        var ac = Kontroler("AC_Karta");
        Stan(ac.layers[0].stateMachine, "Idle", idle, new Vector2(250, 0));
        DodajAnimator(go, ac);
        _ = karta;
        Zapisz(go);
    }

    static void PistoletVis()
    {
        var go = Model("OB_Pistolet_Vis"); if (!go) return;
        var lufa = Punkt(go, "Lufa", new Vector3(0, 0.065f, 0.172f));
        Punkt(go, "Uchwyt", new Vector3(0, -0.012f, -0.012f));
        var wyrzut = Punkt(go, "Wyrzut", new Vector3(0.016f, 0.066f, 0.025f));
        Kolizja(go);
        Wstaw("FX_BlyskLufy", lufa);
        var lu = Wstaw("FX_Luska", wyrzut);
        lu.transform.localEulerAngles = new Vector3(-30f, 90f, 0f);   // w prawo i lekko w gore

        // Czesci: Zamek, Kurek (pivot w osi), Spust, Magazynek. Tyl pistoletu = -Z.
        Vector3 wDol = new Vector3(0f, -0.961f, -0.276f);   // kierunek wysuwania magazynka (os chwytu w dol)
        var zero = Stala(0f);
        var gotowy = Klip("Vis_Gotowy", false,
            Pozycja("Zamek", zero, zero, zero), Obrot("Kurek", zero, zero, zero),
            Pozycja("Spust", zero, zero, zero), Pozycja("Magazynek", zero, zero, zero),
            J(new K("Magazynek", typeof(Transform), "m_LocalScale.x", Stala(1)),
              new K("Magazynek", typeof(Transform), "m_LocalScale.y", Stala(1)),
              new K("Magazynek", typeof(Transform), "m_LocalScale.z", Stala(1))));
        // strzal 0,16 s: kurek uderza, zamek odskakuje 3,5 cm i wraca, spust cofa sie
        var strzal = Klip("Vis_Strzal", false,
            Pozycja("Zamek", Stala(0, .16f), Stala(0, .16f), Kr(false, 0, 0, .035f, -0.035f, .14f, 0, .16f, 0)),
            Obrot("Kurek", Kr(false, 0, 0, .012f, 45, .05f, 0, .16f, 0), Stala(0, .16f), Stala(0, .16f)),
            Pozycja("Spust", Stala(0, .16f), Stala(0, .16f), Kr(false, 0, 0, .015f, -0.005f, .15f, 0)));
        // przeladowanie 1,8 s: magazynek wypada, nowy wchodzi, zamek przeciagniety
        float[] d = { 0, 0, .15f, .01f, .45f, .25f, .46f, .12f, .8f, .12f, 1.2f, 0, 1.8f, 0 };
        var dy = new float[d.Length]; var dz = new float[d.Length];
        for (int i = 0; i < d.Length; i += 2) { dy[i] = dz[i] = d[i]; dy[i + 1] = wDol.y * d[i + 1]; dz[i + 1] = wDol.z * d[i + 1]; }
        var skala = Schodki(0, 1, .45f, 0, .8f, 1, 1.8f, 1);
        var przel = Klip("Vis_Przeladowanie", false,
            Pozycja("Magazynek", Stala(0, 1.8f), Kr(false, dy), Kr(false, dz)),
            J(new K("Magazynek", typeof(Transform), "m_LocalScale.x", skala),
              new K("Magazynek", typeof(Transform), "m_LocalScale.y", skala),
              new K("Magazynek", typeof(Transform), "m_LocalScale.z", skala)),
            Pozycja("Zamek", Stala(0, 1.8f), Stala(0, 1.8f), Kr(false, 0, 0, 1.35f, 0, 1.55f, -0.045f, 1.65f, -0.045f, 1.75f, 0, 1.8f, 0)),
            Obrot("Kurek", Kr(false, 0, 0, 1.35f, 0, 1.55f, -12, 1.75f, 0, 1.8f, 0), Stala(0, 1.8f), Stala(0, 1.8f)));

        var ac = Kontroler("AC_Vis");
        ac.AddParameter("Strzal", AnimatorControllerParameterType.Trigger);
        ac.AddParameter("Przeladuj", AnimatorControllerParameterType.Trigger);
        var sm = ac.layers[0].stateMachine;
        var sG = Stan(sm, "Gotowy", gotowy, new Vector2(250, 0));
        var sS = Stan(sm, "Strzal", strzal, new Vector2(500, -60));
        var sP = Stan(sm, "Przeladowanie", przel, new Vector2(500, 80));
        sm.defaultState = sG;
        var tS = sm.AddAnyStateTransition(sS);
        tS.AddCondition(AnimatorConditionMode.If, 0, "Strzal");
        tS.duration = 0f; tS.canTransitionToSelf = true;
        Do(sG, sP).AddCondition(AnimatorConditionMode.If, 0, "Przeladuj");
        Do(sS, sP).AddCondition(AnimatorConditionMode.If, 0, "Przeladuj");
        Do(sS, sG, 0f, true);
        Do(sP, sG, 0f, true);
        DodajAnimator(go, ac);
        Zapisz(go);
    }

    static void Lacznica()
    {
        var go = Model("OB_Lacznica"); if (!go) return;
        Kolizja(go, Znajdz(go.transform, "Korpus"));
        Punkt(go, "PunktInterakcji", new Vector3(0, 0, 0.9f));

        // kable: dzieci Kabel_N -> Gora / Dol, wspolny AC_Kabel z triggerem Przetnij
        var gGora = Klip("Kabel_Przetnij", false,
            Obrot("Gora", Kr(false, 0, 0, .15f, -34, .3f, -20, .5f, -25), Kr(false, 0, 0, .5f, 4), Stala(0, .5f)),
            Obrot("Dol", Kr(false, 0, 0, .25f, 138, .4f, 110, .6f, 118), Kr(false, 0, 0, .6f, -6), Kr(false, 0, 0, .6f, 8)));
        var gCaly = Klip("Kabel_Caly", false, Obrot("Gora", Stala(0), Stala(0), Stala(0)), Obrot("Dol", Stala(0), Stala(0), Stala(0)));
        var acK = Kontroler("AC_Kabel");
        acK.AddParameter("Przetnij", AnimatorControllerParameterType.Trigger);
        var smK = acK.layers[0].stateMachine;
        var sC = Stan(smK, "Caly", gCaly, new Vector2(250, 0));
        var sP = Stan(smK, "Przeciety", gGora, new Vector2(250, 120));
        smK.defaultState = sC;
        Do(sC, sP).AddCondition(AnimatorConditionMode.If, 0, "Przetnij");

        for (int i = 1; i <= 3; i++)
        {
            var k = Znajdz(go.transform, $"Kabel_{i}");
            Znajdz(k, $"Kabel_{i}_Gora").name = "Gora";
            Znajdz(k, $"Kabel_{i}_Dol").name = "Dol";
            DodajAnimator(k.gameObject, acK);
            var stary = Znajdz(go.transform, $"FX_Iskra_{i}");
            var fx = Wstaw("FX_Iskra", go.transform, $"FX_Iskra_{i}");
            fx.transform.localPosition = stary.localPosition;
            Object.DestroyImmediate(stary.gameObject);
        }

        // lampka kontrolna: bool Zasilanie (domyslnie ON)
        var lampka = Znajdz(go.transform, "Lampka");
        Swiatlo(go.transform, "Lampka_Swiatlo", LightType.Point, new Color(1f, 0.15f, 0.05f), 1.5f, 0.8f,
                lampka.localPosition + new Vector3(0, 0, 0.06f), Vector3.zero);
        var e = Emisja["M_Lampka_Czerwona"];
        var wl = Klip("Lacznica_Zasilanie_Wl", false, Emisja4("Lampka", Stala(e.r), Stala(e.g), Stala(e.b)),
                      J(Wlacz("Lampka_Swiatlo", typeof(Light), Stala(1))));
        var wy = Klip("Lacznica_Zasilanie_Wyl", false, Emisja4("Lampka", Stala(0), Stala(0), Stala(0)),
                      J(Wlacz("Lampka_Swiatlo", typeof(Light), Stala(0))));
        var ac = Kontroler("AC_Lacznica");
        ac.AddParameter(new AnimatorControllerParameter { name = "Zasilanie", type = AnimatorControllerParameterType.Bool, defaultBool = true });
        BoolWlWyl(ac.layers[0].stateMachine, "Zasilanie", wy, wl, true);
        DodajAnimator(go, ac);
        Zapisz(go);
    }
}
#endif
