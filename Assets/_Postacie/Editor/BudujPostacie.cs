// Rola C (postacie) — skrypt EDYTORA. Sklada prefaby CH_* (Humanoid) z AC_Czlowiek oraz FP_Rece (Generic) z AC_FP.
// Menu: Postacie > Zbuduj wszystko. Bazy modeli: Sketchfab CC BY — "Simple Low Poly Character" (PIXELOKAY),
// "Low Poly Female Base Mesh" (mesh-base). Podac w napisach koncowych.
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

public class PostacieImport : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith("Assets/_Postacie/Modele")) return;
        var m = (ModelImporter)assetImporter;
        string n = Path.GetFileNameWithoutExtension(assetPath);
        m.globalScale = 1f; m.useFileScale = true;
        m.importCameras = false; m.importLights = false;
        m.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        if (n.StartsWith("FP_"))
        {
            m.animationType = ModelImporterAnimationType.Generic;
            m.importAnimation = true;
        }
        else
        {
            m.animationType = ModelImporterAnimationType.Human;
            m.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            m.importAnimation = n == "CH_Animacje";
        }
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/_Postacie/Materialy")) return;
        var t = (TextureImporter)assetImporter;
        string n = Path.GetFileNameWithoutExtension(assetPath);
        t.maxTextureSize = 2048;
        if (n.EndsWith("_N")) t.textureType = TextureImporterType.NormalMap;
        else if (n.EndsWith("_MS")) { t.sRGBTexture = false; t.alphaSource = TextureImporterAlphaSource.FromInput; }
    }
}

public static class BudujPostacie
{
    const string R = "Assets/_Postacie";
    const string MOD = R + "/Modele";
    const string MAT = R + "/Materialy";
    const string PRE = R + "/Prefaby";
    const string ANI = R + "/Animacje";

    static readonly string[] Petle = { "Idle", "Chod", "Bieg", "Kucanie_Idle", "Kucanie_Chod", "Celowanie", "Rozmowa",
        "RecePodniesione", "Siedzi", "Lezy", "Spadanie" };

    // bron FPP: pliki FP_*.fbx w Modele (kazdy to osobny model recow+bron, bez wspolnego FP_Rece)
    static readonly string[] Noze = { "Bagnet", "Noz" };

    [MenuItem("Postacie/Zbuduj wszystko")]
    public static void ZbudujWszystko()
    {
        foreach (var d in new[] { PRE, ANI })
            if (!AssetDatabase.IsValidFolder(d)) AssetDatabase.CreateFolder(R, Path.GetFileName(d));
        UstawKlipy($"{MOD}/CH_Animacje.fbx", true);
        var mat = Material();
        var ac = KontrolerCzlowiek();
        foreach (var g in AssetDatabase.FindAssets("CH_ t:Model", new[] { MOD }))
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            string n = Path.GetFileNameWithoutExtension(p);
            if (n == "CH_Animacje") continue;
            Postac(p, n, ac, mat);
        }
        foreach (var g in AssetDatabase.FindAssets("FP_ t:Model", new[] { MOD }))
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            string n = Path.GetFileNameWithoutExtension(p);   // np. "FP_Vis"
            string krotkaNazwa = n.StartsWith("FP_") ? n.Substring(3) : n; // "Vis"
            UstawKlipy(p, false);
            Bron(p, krotkaNazwa, mat);
        }
        KopiaFpRece();
        AssetDatabase.SaveAssets();
        Debug.Log("Postacie: prefaby w " + PRE);
    }

    // ------------------------------------------------------------ klipy
    static string Krotka(string take) { int i = take.LastIndexOf('|'); return i >= 0 ? take.Substring(i + 1) : take; }

    static void UstawKlipy(string sciezka, bool human)
    {
        var imp = AssetImporter.GetAtPath(sciezka) as ModelImporter;
        if (imp == null) { Debug.LogWarning("Brak " + sciezka); return; }
        var klipy = imp.defaultClipAnimations.Select(c =>
        {
            c.name = Krotka(c.takeName);
            // dla broni FPP klipy sa prefiksowane nazwa bron (np. "FP_Vis_Idle"), wiec petla po sufiksie
            bool petla = Petle.Contains(c.name) ||
                (!human && (c.name.EndsWith("_Idle") || c.name.EndsWith("_Chod") || c.name.EndsWith("_Celowanie")));
            c.loopTime = petla; c.loopPose = petla && human;
            if (human)
            {
                c.lockRootRotation = true; c.keepOriginalOrientation = true;
                c.lockRootHeightY = true; c.keepOriginalPositionY = true;
                c.lockRootPositionXZ = true; c.keepOriginalPositionXZ = true;
            }
            return c;
        }).ToArray();
        imp.clipAnimations = klipy;
        imp.SaveAndReimport();
    }

    static AnimationClip Klip(string sciezka, string nazwa)
    {
        var c = AssetDatabase.LoadAllAssetsAtPath(sciezka).OfType<AnimationClip>().FirstOrDefault(x => x.name == nazwa);
        if (c == null) Debug.LogWarning($"Brak klipu {nazwa} w {sciezka}");
        return c;
    }

    // ------------------------------------------------------------ material
    static Material Material()
    {
        string p = $"{MAT}/M_Postacie_Atlas.mat";
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, p); }
        m.shader = sh;
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{MAT}/T_Postacie_Atlas.png"));
        var ms = AssetDatabase.LoadAssetAtPath<Texture2D>($"{MAT}/T_Postacie_Atlas_MS.png");
        m.SetTexture("_MetallicGlossMap", ms);
        if (ms) { m.EnableKeyword("_METALLICSPECGLOSSMAP"); m.SetFloat("_Smoothness", 1f); }
        var n = AssetDatabase.LoadAssetAtPath<Texture2D>($"{MAT}/T_Postacie_Atlas_N.png");
        m.SetTexture("_BumpMap", n);
        if (n) m.EnableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(m);
        return m;
    }

    // ------------------------------------------------------------ AC_Czlowiek
    static AnimatorController KontrolerCzlowiek()
    {
        string a = $"{MOD}/CH_Animacje.fbx";
        string p = $"{R}/AC_Czlowiek.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(p) != null) AssetDatabase.DeleteAsset(p);
        var ac = AnimatorController.CreateAnimatorControllerAtPath(p);
        void P(string n, AnimatorControllerParameterType t, float f = 0, bool b = false) =>
            ac.AddParameter(new AnimatorControllerParameter { name = n, type = t, defaultFloat = f, defaultBool = b });
        P("Speed", AnimatorControllerParameterType.Float);
        P("MotionSpeed", AnimatorControllerParameterType.Float, 1f);
        P("Grounded", AnimatorControllerParameterType.Bool, 0, true);
        P("Jump", AnimatorControllerParameterType.Bool);
        P("FreeFall", AnimatorControllerParameterType.Bool);
        P("Kucanie", AnimatorControllerParameterType.Bool);
        P("Celuje", AnimatorControllerParameterType.Bool);
        P("Strzal", AnimatorControllerParameterType.Trigger);
        P("Trafiony", AnimatorControllerParameterType.Trigger);
        P("Rozmowa", AnimatorControllerParameterType.Bool);
        P("RecePodniesione", AnimatorControllerParameterType.Bool);
        P("Siedzi", AnimatorControllerParameterType.Bool);
        P("Lezy", AnimatorControllerParameterType.Bool);

        BlendTree Drzewo(string nazwa, params (string klip, float prog)[] el)
        {
            var bt = new BlendTree { name = nazwa, blendParameter = "Speed", blendType = BlendTreeType.Simple1D, useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(bt, ac);
            foreach (var (k, t) in el) bt.AddChild(Klip(a, k), t);
            return bt;
        }
        var ruch = Drzewo("Ruch", ("Idle", 0f), ("Chod", 1.5f), ("Bieg", 5.5f));
        var kuc = Drzewo("Kucanie", ("Kucanie_Idle", 0f), ("Kucanie_Chod", 2.0f));

        var sm = ac.layers[0].stateMachine;
        AnimatorState S(AnimatorStateMachine m_, string n, Motion mo, float x, float y)
        {
            var s = m_.AddState(n, new Vector3(x, y, 0)); s.motion = mo; return s;
        }
        AnimatorStateTransition T(AnimatorState z, AnimatorState d, float czas = 0.15f)
        {
            var t = z.AddTransition(d); t.hasExitTime = false; t.duration = czas; return t;
        }
        var sRuch = S(sm, "Ruch", ruch, 300, 0); sRuch.speedParameterActive = true; sRuch.speedParameter = "MotionSpeed";
        var sKuc = S(sm, "Kucanie", kuc, 300, 120);
        var sSpad = S(sm, "Spadanie", Klip(a, "Spadanie"), 550, -80);
        var sSiedzi = S(sm, "Siedzi", Klip(a, "Siedzi"), 550, 60);
        var sLezy = S(sm, "Lezy", Klip(a, "Lezy"), 550, 180);
        var sTraf = S(sm, "Trafiony", Klip(a, "Trafiony"), 550, 300);
        sm.defaultState = sRuch;
        T(sRuch, sKuc).AddCondition(AnimatorConditionMode.If, 0, "Kucanie");
        T(sKuc, sRuch).AddCondition(AnimatorConditionMode.IfNot, 0, "Kucanie");
        foreach (var z in new[] { sRuch, sKuc })
        {
            T(z, sSpad).AddCondition(AnimatorConditionMode.If, 0, "FreeFall");
            T(z, sSiedzi).AddCondition(AnimatorConditionMode.If, 0, "Siedzi");
            T(z, sLezy, 0.3f).AddCondition(AnimatorConditionMode.If, 0, "Lezy");
        }
        T(sSpad, sRuch).AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        T(sSiedzi, sRuch, 0.3f).AddCondition(AnimatorConditionMode.IfNot, 0, "Siedzi");
        T(sLezy, sRuch, 0.4f).AddCondition(AnimatorConditionMode.IfNot, 0, "Lezy");
        var tTraf = sm.AddAnyStateTransition(sTraf);   // trafiony: upada i zostaje na ziemi
        tTraf.AddCondition(AnimatorConditionMode.If, 0, "Trafiony"); tTraf.duration = 0.05f; tTraf.canTransitionToSelf = false;

        // warstwa gorna (rece/tulow): celowanie, strzal, rozmowa, rece w gorze
        var maska = new AvatarMask { name = "Maska_Gorna" };
        foreach (AvatarMaskBodyPart bp in System.Enum.GetValues(typeof(AvatarMaskBodyPart)))
            if (bp != AvatarMaskBodyPart.LastBodyPart)
                maska.SetHumanoidBodyPartActive(bp, bp == AvatarMaskBodyPart.Body || bp == AvatarMaskBodyPart.Head ||
                    bp == AvatarMaskBodyPart.LeftArm || bp == AvatarMaskBodyPart.RightArm ||
                    bp == AvatarMaskBodyPart.LeftFingers || bp == AvatarMaskBodyPart.RightFingers);
        AssetDatabase.AddObjectToAsset(maska, ac);
        ac.AddLayer("Gorna");
        var ls = ac.layers; ls[1].defaultWeight = 1f; ls[1].avatarMask = maska; ac.layers = ls;
        var sg = ac.layers[1].stateMachine;
        var gRuch = S(sg, "Ruch", ruch, 300, 0);          // ta sama animacja co nogi = brak nadpisania
        var gCel = S(sg, "Celowanie", Klip(a, "Celowanie"), 550, -80);
        var gStrz = S(sg, "Strzal", Klip(a, "Strzal"), 800, -80);
        var gRoz = S(sg, "Rozmowa", Klip(a, "Rozmowa"), 550, 60);
        var gRece = S(sg, "RecePodniesione", Klip(a, "RecePodniesione"), 550, 180);
        sg.defaultState = gRuch;
        T(gRuch, gCel).AddCondition(AnimatorConditionMode.If, 0, "Celuje");
        T(gCel, gRuch).AddCondition(AnimatorConditionMode.IfNot, 0, "Celuje");
        T(gCel, gStrz, 0.02f).AddCondition(AnimatorConditionMode.If, 0, "Strzal");
        T(gRuch, gStrz, 0.02f).AddCondition(AnimatorConditionMode.If, 0, "Strzal");
        var tw = gStrz.AddTransition(gCel); tw.hasExitTime = true; tw.exitTime = 1f; tw.duration = 0.05f;
        T(gRuch, gRoz, 0.3f).AddCondition(AnimatorConditionMode.If, 0, "Rozmowa");
        T(gRoz, gRuch, 0.3f).AddCondition(AnimatorConditionMode.IfNot, 0, "Rozmowa");
        T(gRuch, gRece, 0.3f).AddCondition(AnimatorConditionMode.If, 0, "RecePodniesione");
        T(gRece, gRuch, 0.3f).AddCondition(AnimatorConditionMode.IfNot, 0, "RecePodniesione");
        EditorUtility.SetDirty(ac);
        return ac;
    }

    static Transform Znajdz(Transform t, string n)
    {
        if (t.name == n) return t;
        foreach (Transform c in t) { var r = Znajdz(c, n); if (r) return r; }
        return null;
    }

    static void Podmien(GameObject go, Material mat)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
    }

    static void Postac(string sciezka, string nazwa, AnimatorController ac, Material mat)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(sciezka);
        var go = Object.Instantiate(model); go.name = nazwa;
        go.transform.position = Vector3.zero;
        Podmien(go, mat);
        var an = go.GetComponent<Animator>(); if (an == null) an = go.AddComponent<Animator>();
        an.avatar = AssetDatabase.LoadAllAssetsAtPath(sciezka).OfType<Avatar>().FirstOrDefault();
        an.runtimeAnimatorController = ac; an.applyRootMotion = false;
        var cc = go.AddComponent<CapsuleCollider>();
        cc.height = 1.8f; cc.radius = 0.25f; cc.center = new Vector3(0, 0.9f, 0);
        var reka = Znajdz(go.transform, "RightHand");
        if (reka)
        {
            var s = new GameObject("Bron_P").transform;   // gniazdo na OB_Pistolet_Vis (B moze poprawic offset)
            s.SetParent(reka, false);
        }
        PrefabUtility.SaveAsPrefabAsset(go, $"{PRE}/{nazwa}.prefab");
        Object.DestroyImmediate(go);
    }

    // ------------------------------------------------------------ rece FPP (osobny plik na bron, bron wbudowana w rece)
    static bool JestNozem(string f, string n) =>
        Noze.Contains(n) || AssetDatabase.LoadAllAssetsAtPath(f).OfType<AnimationClip>().Any(c => c.name == $"{n}_Atak");

    static AnimatorController KontrolerBroniPalnej(string f, string n)
    {
        string p = $"{ANI}/AC_FP_{n}.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(p) != null) AssetDatabase.DeleteAsset(p);
        var ac = AnimatorController.CreateAnimatorControllerAtPath(p);
        ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ac.AddParameter("Celuje", AnimatorControllerParameterType.Bool);
        ac.AddParameter("Strzal", AnimatorControllerParameterType.Trigger);
        ac.AddParameter("Przeladuj", AnimatorControllerParameterType.Trigger);
        var bt = new BlendTree { name = $"{n}_Ruch", blendParameter = "Speed", blendType = BlendTreeType.Simple1D, useAutomaticThresholds = false };
        AssetDatabase.AddObjectToAsset(bt, ac);
        bt.AddChild(Klip(f, $"{n}_Idle"), 0f); bt.AddChild(Klip(f, $"{n}_Chod"), 1.5f);
        var sm = ac.layers[0].stateMachine;
        var sR = sm.AddState("Ruch"); sR.motion = bt;
        var sC = sm.AddState("Celowanie"); sC.motion = Klip(f, $"{n}_Celowanie");
        var sS = sm.AddState("Strzal"); sS.motion = Klip(f, $"{n}_Strzal");
        var sSC = sm.AddState("Strzal_Cel"); sSC.motion = Klip(f, $"{n}_Strzal_Cel");
        var sP = sm.AddState("Przeladowanie"); sP.motion = Klip(f, $"{n}_Przeladowanie");
        sm.defaultState = sR;
        AnimatorStateTransition T(AnimatorState z, AnimatorState d, float c = 0.15f)
        { var t = z.AddTransition(d); t.hasExitTime = false; t.duration = c; return t; }
        T(sR, sC).AddCondition(AnimatorConditionMode.If, 0, "Celuje");
        T(sC, sR).AddCondition(AnimatorConditionMode.IfNot, 0, "Celuje");
        var a1 = sm.AddAnyStateTransition(sS); a1.duration = 0f; a1.canTransitionToSelf = true;
        a1.AddCondition(AnimatorConditionMode.If, 0, "Strzal"); a1.AddCondition(AnimatorConditionMode.IfNot, 0, "Celuje");
        var a2 = sm.AddAnyStateTransition(sSC); a2.duration = 0f; a2.canTransitionToSelf = true;
        a2.AddCondition(AnimatorConditionMode.If, 0, "Strzal"); a2.AddCondition(AnimatorConditionMode.If, 0, "Celuje");
        var a3 = sm.AddAnyStateTransition(sP); a3.duration = 0.1f; a3.canTransitionToSelf = false;
        a3.AddCondition(AnimatorConditionMode.If, 0, "Przeladuj");
        foreach (var (z, d) in new[] { (sS, sR), (sSC, sC), (sP, sR) })
        { var t = z.AddTransition(d); t.hasExitTime = true; t.exitTime = 1f; t.duration = 0.1f; }
        EditorUtility.SetDirty(ac);
        return ac;
    }

    static AnimatorController KontrolerNoza(string f, string n)
    {
        string p = $"{ANI}/AC_FP_{n}.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(p) != null) AssetDatabase.DeleteAsset(p);
        var ac = AnimatorController.CreateAnimatorControllerAtPath(p);
        ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ac.AddParameter("Atak", AnimatorControllerParameterType.Trigger);
        ac.AddParameter("Pchniecie", AnimatorControllerParameterType.Trigger);
        var bt = new BlendTree { name = $"{n}_Ruch", blendParameter = "Speed", blendType = BlendTreeType.Simple1D, useAutomaticThresholds = false };
        AssetDatabase.AddObjectToAsset(bt, ac);
        bt.AddChild(Klip(f, $"{n}_Idle"), 0f); bt.AddChild(Klip(f, $"{n}_Chod"), 1.5f);
        var sm = ac.layers[0].stateMachine;
        var sR = sm.AddState("Ruch"); sR.motion = bt;
        var sA = sm.AddState("Atak"); sA.motion = Klip(f, $"{n}_Atak");
        var sP = sm.AddState("Pchniecie"); sP.motion = Klip(f, $"{n}_Pchniecie");
        sm.defaultState = sR;
        var a1 = sm.AddAnyStateTransition(sA); a1.duration = 0f; a1.canTransitionToSelf = true;
        a1.AddCondition(AnimatorConditionMode.If, 0, "Atak");
        var a2 = sm.AddAnyStateTransition(sP); a2.duration = 0f; a2.canTransitionToSelf = true;
        a2.AddCondition(AnimatorConditionMode.If, 0, "Pchniecie");
        foreach (var (z, d) in new[] { (sA, sR), (sP, sR) })
        { var t = z.AddTransition(d); t.hasExitTime = true; t.exitTime = 1f; t.duration = 0.1f; }
        EditorUtility.SetDirty(ac);
        return ac;
    }

    static void Bron(string f, string n, Material mat)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(f);
        if (!model) { Debug.LogWarning("Brak " + f); return; }
        bool nozem = JestNozem(f, n);
        var ac = nozem ? KontrolerNoza(f, n) : KontrolerBroniPalnej(f, n);

        var go = Object.Instantiate(model); go.name = $"FP_{n}";
        Podmien(go, mat);
        var an = go.GetComponent<Animator>(); if (an == null) an = go.AddComponent<Animator>();
        an.runtimeAnimatorController = ac; an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>()) { r.shadowCastingMode = ShadowCastingMode.Off; r.updateWhenOffscreen = true; }
        if (!nozem)
        {
            var lufa = Znajdz(go.transform, "Lufa");
            var fx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Obiekty/Efekty/FX_BlyskLufy.prefab");
            if (!lufa) Debug.LogWarning($"FP_{n}: brak kosci 'Lufa'");
            else if (!fx) Debug.LogWarning($"FP_{n}: brak FX_BlyskLufy.prefab");
            else
            {
                var b = (GameObject)PrefabUtility.InstantiatePrefab(fx);
                b.transform.SetParent(lufa, false);
                b.transform.rotation = Quaternion.LookRotation(go.transform.forward);
            }
        }
        PrefabUtility.SaveAsPrefabAsset(go, $"{PRE}/FP_{n}.prefab");
        Object.DestroyImmediate(go);
    }

    // kompatybilnosc wstecz: stary kod moze szukac FP_Rece.prefab -> kopia domyslnego pistoletu (Vis)
    static void KopiaFpRece()
    {
        string wzor = $"{PRE}/FP_Vis.prefab";
        string cel = $"{PRE}/FP_Rece.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(wzor) == null)
        {
            Debug.LogWarning("Brak FP_Vis.prefab - nie mozna zrobic kopii zgodnosci FP_Rece.prefab");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(cel) != null) AssetDatabase.DeleteAsset(cel);
        AssetDatabase.CopyAsset(wzor, cel);
    }
}
#endif
