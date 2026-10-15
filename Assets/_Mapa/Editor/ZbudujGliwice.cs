using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Buduje całą mapę Gliwic z FBX-ów (Modele/) i danych (Dane/paleta.json, Dane/manifest.json)
/// jednym kliknięciem: Tools -> Zbuduj Gliwice.
/// </summary>
public static class ZbudujGliwice
{
    const string ROOT = "Assets/_Mapa";
    const string MODELE = ROOT + "/Modele";
    const string DANE = ROOT + "/Dane";
    const string MATERIALY = ROOT + "/Materialy";
    const string USTAWIENIA = ROOT + "/Ustawienia";
    const string TEKSTURY = ROOT + "/Tekstury";

    static readonly string[] GRUPY = { "Teren", "Ulice", "Budynki", "Natura", "Wnetrza", "Granice", "Punkty", "Swiatla" };

    // Materiały z emisja_noca, do podłączenia do MapaSwiatlo.
    static List<Material> _matyEmisjaNocna = new List<Material>();

    [MenuItem("Tools/Zbuduj Gliwice")]
    public static void Zbuduj()
    {
        var log = new List<string>();
        _matyEmisjaNocna.Clear();

        Dictionary<string, object> paleta = WczytajJson(Path.Combine(DANE, "paleta.json"));
        Dictionary<string, object> manifest = WczytajJson(Path.Combine(DANE, "manifest.json"));
        Dictionary<string, object> tekstury = WczytajJson(Path.Combine(DANE, "tekstury.json")) ?? new Dictionary<string, object>();
        Dictionary<string, object> akty = WczytajJson(Path.Combine(DANE, "akty.json")) ?? new Dictionary<string, object>();
        if (paleta == null) { Debug.LogError("ZbudujGliwice: brak Dane/paleta.json"); return; }
        if (manifest == null) { Debug.LogError("ZbudujGliwice: brak Dane/manifest.json"); return; }

        // a) tekstury + materiały
        int liczbaTekstur = PrzygotujImportTekstur(tekstury);
        log.Add($"Tekstury przeimportowane: {liczbaTekstur}");
        var materialy = ZbudujMaterialy(paleta, tekstury);
        log.Add($"Materiały: {materialy.Count}");

        // b) import FBX
        int liczbaFbx = PrzygotujImportFbx();
        log.Add($"FBX przeimportowane: {liczbaFbx}");

        // c) złożenie hierarchii
        GameObject root = GameObject.Find("Mapa_Gliwice");
        if (root != null) UnityEngine.Object.DestroyImmediate(root);
        root = new GameObject("Mapa_Gliwice");

        var grupy = new Dictionary<string, Transform>();
        foreach (var g in GRUPY)
        {
            var t = new GameObject(g).transform;
            t.SetParent(root.transform, false);
            grupy[g] = t;
        }

        int zlozone = 0, bezKompasu = 0;

        if (manifest.TryGetValue("warstwy", out var warstwyObj))
            foreach (var w in (List<object>)warstwyObj)
            {
                var d = (Dictionary<string, object>)w;
                string fbx = (string)d["fbx"];
                string grupa = (string)d["grupa"];
                if (ZlozObiekt(fbx, grupa, grupy, Vector3.zero, 0f, out bool mialKompas, out GameObject _)) zlozone++;
                if (!mialKompas) bezKompasu++;
            }

        // obiekty z akty.json: przypisanie do strefy wnętrz i oznaczenie AktZnacznik
        var aktyObiekty = akty.TryGetValue("obiekty", out var aO) ? (Dictionary<string, object>)aO : new Dictionary<string, object>();
        Transform strefaWnetrz = null;

        if (manifest.TryGetValue("obiekty", out var obiektyObj))
            foreach (var o in (List<object>)obiektyObj)
            {
                var d = (Dictionary<string, object>)o;
                string fbx = (string)d["fbx"];
                string grupa = (string)d["grupa"];
                Vector3 poz = WczytajWektor(d["poz"]);
                float kat = Convert.ToSingle(d["kat"]);
                if (ZlozObiekt(fbx, grupa, grupy, poz, kat, out bool mialKompas, out GameObject instancja)) zlozone++;
                if (!mialKompas) bezKompasu++;

                if (instancja != null && aktyObiekty.TryGetValue(fbx, out var infoObj))
                {
                    var info = (Dictionary<string, object>)infoObj;
                    var aktyLista = ((List<object>)info["akty"]).Select(x => Convert.ToInt32(x)).ToArray();
                    string rola = info.TryGetValue("rola", out var r) ? (string)r : null;
                    string prefiks = "AKT" + string.Join("-", aktyLista);

                    if (grupa == "Wnetrza")
                    {
                        if (strefaWnetrz == null)
                        {
                            var go = new GameObject("STREFA_WNETRZ");
                            go.transform.SetParent(root.transform, false);
                            strefaWnetrz = go.transform;
                        }
                        instancja.transform.SetParent(strefaWnetrz, true);
                    }

                    instancja.name = $"{prefiks}_{fbx}";
                    var zn = instancja.AddComponent<AktZnacznik>();
                    zn.akty = aktyLista;
                    zn.rola = rola;
                }
            }
        log.Add($"Obiekty złożone: {zlozone} (bez markerów Kompas: {bezKompasu})");

        // d) kolizje i statyczność
        int kolidery = ZastosujKolizjeIStatyke(root.transform, grupy["Punkty"]);
        log.Add($"Kolidery dodane: {kolidery}");

        // e) punkty i światła
        int liczbaPunktow = 0, liczbaSwiatel = 0;
        var aktyPunkty = akty.TryGetValue("punkty", out var aP) ? (Dictionary<string, object>)aP : new Dictionary<string, object>();
        if (manifest.TryGetValue("punkty", out var punktyObj))
        {
            foreach (var p in (List<object>)punktyObj)
            {
                var d = (Dictionary<string, object>)p;
                string n = (string)d["n"];
                Vector3 pozMapa = WczytajWektor(d["p"]);
                float yaw = Convert.ToSingle(d["yaw"]);
                var go = new GameObject(n);
                go.transform.SetParent(grupy["Punkty"], false);
                go.transform.position = MapaDoUnity(pozMapa);
                go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

                if (aktyPunkty.TryGetValue(n, out var aktyObjP))
                {
                    var zn = go.AddComponent<AktZnacznik>();
                    zn.akty = ((List<object>)aktyObjP).Select(x => Convert.ToInt32(x)).ToArray();
                }
                liczbaPunktow++;
            }
            grupy["Punkty"].gameObject.AddComponent<PunktyGizmos>();
        }

        if (manifest.TryGetValue("swiatla", out var swiatlaObj))
        {
            foreach (var s in (List<object>)swiatlaObj)
            {
                var d = (Dictionary<string, object>)s;
                Vector3 pozMapa = WczytajWektor(d["p"]);
                string kolorHex = (string)d["kolor"];
                float zasieg = Convert.ToSingle(d["zasieg"]);
                float moc = Convert.ToSingle(d["moc"]);
                bool noc = d.TryGetValue("noc", out var nocObj) && Convert.ToBoolean(nocObj);

                var go = new GameObject("Lampa");
                go.transform.SetParent(grupy["Swiatla"], false);
                go.transform.position = MapaDoUnity(pozMapa);
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = HexDoColor(kolorHex);
                light.range = zasieg;
                light.intensity = moc;
                var lampa = go.AddComponent<MapaLampa>();
                lampa.noc = noc;
                liczbaSwiatel++;
            }
        }
        log.Add($"Punkty: {liczbaPunktow}, światła: {liczbaSwiatel}");

        // f) atmosfera
        MapaSwiatlo mapaSwiatlo = ZbudujAtmosfere(root.transform);
        log.Add("Atmosfera zbudowana (słońce, skybox, ambient, mgła, Volume).");

        // g) walidacja
        var (sprawdzone, bezPodloza) = ZwalidujPodloze(grupy["Punkty"]);
        log.Add($"Walidacja punktów: {sprawdzone} sprawdzonych, {bezPodloza} bez podłoża (patrz Warning).");

        int liczbaObiektow = root.GetComponentsInChildren<Transform>(true).Length;
        int liczbaKolideryTotal = root.GetComponentsInChildren<Collider>(true).Length;
        Debug.Log($"ZbudujGliwice: GOTOWE. Obiekty: {liczbaObiektow}, kolidery: {liczbaKolideryTotal}, punkty: {liczbaPunktow}, światła: {liczbaSwiatel}.\n" + string.Join("\n", log));

        Directory.CreateDirectory(ROOT);
        string prefabPath = ROOT + "/Mapa_Gliwice.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // ---------- a) tekstury + materiały ----------

    static int PrzygotujImportTekstur(Dictionary<string, object> tekstury)
    {
        if (!Directory.Exists(TEKSTURY)) return 0;
        var nazwyTex = new HashSet<string>();
        foreach (var kv in tekstury)
        {
            var d = (Dictionary<string, object>)kv.Value;
            nazwyTex.Add((string)d["tex"]);
        }
        int n = 0;
        foreach (var tex in nazwyTex)
        {
            n += UstawImporterTekstury($"{TEKSTURY}/T_{tex}_A.png", false);
            n += UstawImporterTekstury($"{TEKSTURY}/T_{tex}_N.png", true);
        }
        AssetDatabase.SaveAssets();
        return n;
    }

    static int UstawImporterTekstury(string sciezka, bool normalMap)
    {
        if (!File.Exists(sciezka)) return 0;
        var importer = AssetImporter.GetAtPath(sciezka) as TextureImporter;
        if (importer == null) return 0;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.anisoLevel = 4;
        importer.mipmapEnabled = true;
        importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !normalMap;
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        return 1;
    }

    static Dictionary<string, Material> ZbudujMaterialy(Dictionary<string, object> paleta, Dictionary<string, object> tekstury)
    {
        Directory.CreateDirectory(MATERIALY);
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var wynik = new Dictionary<string, Material>();

        foreach (var kv in paleta)
        {
            string nazwa = kv.Key;
            var d = (Dictionary<string, object>)kv.Value;
            string sciezka = $"{MATERIALY}/M_{nazwa}.mat";

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(sciezka);
            bool nowy = mat == null;
            if (nowy) mat = new Material(shader);
            else mat.shader = shader;

            Color kolor = HexDoColor((string)d["kolor"]);
            float smooth = d.TryGetValue("smooth", out var sm) ? Convert.ToSingle(sm) : 0.12f;
            float metal = d.TryGetValue("metal", out var mt) ? Convert.ToSingle(mt) : 0f;

            mat.SetColor("_BaseColor", kolor);
            mat.SetFloat("_Smoothness", smooth);
            mat.SetFloat("_Metallic", metal);

            if (tekstury.TryGetValue(nazwa, out var texObj))
            {
                var td = (Dictionary<string, object>)texObj;
                string texNazwa = (string)td["tex"];
                float kafel = Convert.ToSingle(td["kafel"]);
                var texA = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEKSTURY}/T_{texNazwa}_A.png");
                var texN = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEKSTURY}/T_{texNazwa}_N.png");
                if (texA != null)
                {
                    mat.SetTexture("_BaseMap", texA);
                    mat.SetTextureScale("_BaseMap", new Vector2(1f / kafel, 1f / kafel));
                }
                if (texN != null)
                {
                    mat.SetTexture("_BumpMap", texN);
                    mat.SetTextureScale("_BumpMap", new Vector2(1f / kafel, 1f / kafel));
                    mat.SetFloat("_BumpScale", 0.8f);
                    mat.EnableKeyword("_NORMALMAP");
                }
            }

            bool alfa = d.TryGetValue("alfa", out var al) && Convert.ToBoolean(al);
            if (alfa) UstawPrzezroczystosc(mat);

            if (d.TryGetValue("emisja", out var emObj))
            {
                Color emisja = HexDoColor((string)emObj);
                bool emisjaNoca = d.TryGetValue("emisja_noca", out var en) && Convert.ToBoolean(en);
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                mat.SetColor("_EmissionColor", emisjaNoca ? Color.black : emisja);
                if (emisjaNoca) _matyEmisjaNocna.Add(mat);
            }

            if (nowy)
            {
                AssetDatabase.CreateAsset(mat, sciezka);
            }
            else
            {
                EditorUtility.SetDirty(mat);
            }
            wynik[nazwa] = mat;
        }
        AssetDatabase.SaveAssets();
        return wynik;
    }

    static void UstawPrzezroczystosc(Material mat)
    {
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3000;
    }

    // ---------- b) import FBX ----------

    static int PrzygotujImportFbx()
    {
        if (!Directory.Exists(MODELE)) return 0;
        var pliki = Directory.GetFiles(MODELE, "*.fbx", SearchOption.TopDirectoryOnly);
        int n = 0;
        foreach (var plikAbs in pliki)
        {
            string norm = plikAbs.Replace('\\', '/');
            string sciezka = norm.StartsWith("Assets/") ? norm : "Assets" + norm.Substring(Application.dataPath.Length);
            var importer = AssetImporter.GetAtPath(sciezka) as ModelImporter;
            if (importer == null) continue;

            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.isReadable = false;

            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Everywhere);
            importer.SaveAndReimport();
            n++;
        }
        return n;
    }

    // ---------- c) złożenie obiektów + korekta osi Kompas ----------

    static bool ZlozObiekt(string fbxNazwa, string grupa, Dictionary<string, Transform> grupy, Vector3 pozMapa, float katDeg, out bool mialKompas, out GameObject obiektWynik)
    {
        mialKompas = false;
        obiektWynik = null;
        string sciezka = $"{MODELE}/{fbxNazwa}.fbx";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(sciezka);
        if (prefab == null)
        {
            Debug.LogWarning($"ZbudujGliwice: nie znaleziono {sciezka}");
            return false;
        }
        if (!grupy.TryGetValue(grupa, out var grupaTransform))
        {
            Debug.LogWarning($"ZbudujGliwice: nieznana grupa '{grupa}' dla {fbxNazwa}");
            return false;
        }

        GameObject instancja = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instancja.name = fbxNazwa;
        instancja.transform.SetParent(null, false);
        instancja.transform.position = Vector3.zero;
        instancja.transform.rotation = Quaternion.identity;
        instancja.transform.localScale = Vector3.one;

        Transform k0 = ZnajdzRekurencyjnie(instancja.transform, "Kompas_0");
        Transform kE = ZnajdzRekurencyjnie(instancja.transform, "Kompas_E");
        Transform kN = ZnajdzRekurencyjnie(instancja.transform, "Kompas_N");
        Transform kU = ZnajdzRekurencyjnie(instancja.transform, "Kompas_U");

        if (k0 != null && kE != null && kN != null && kU != null)
        {
            mialKompas = true;
            Vector3 p0 = instancja.transform.InverseTransformPoint(k0.position);
            Vector3 pE = instancja.transform.InverseTransformPoint(kE.position);
            Vector3 pN = instancja.transform.InverseTransformPoint(kN.position);
            Vector3 pU = instancja.transform.InverseTransformPoint(kU.position);

            Vector3 wektorE = pE - p0;
            Vector3 wektorN = pN - p0;
            Vector3 wektorU = pU - p0;

            float skala = wektorE.magnitude;
            if (skala < 1e-4f) skala = 100f;
            if (Mathf.Abs(skala - 100f) > 1f)
                Debug.LogWarning($"ZbudujGliwice: {fbxNazwa} - skala kompasu {skala:F1} (oczekiwano 100).");
            float korektaSkali = 100f / skala;

            Vector3 E = wektorE.normalized;
            Vector3 N = wektorN.normalized;
            Vector3 U = wektorU.normalized;

            bool lustro = Vector3.Dot(Vector3.Cross(U, N), E) < 0f;
            Quaternion R = Quaternion.Inverse(Quaternion.LookRotation(N, U));

            instancja.transform.localRotation = R;
            instancja.transform.localScale = new Vector3((lustro ? -1f : 1f) * korektaSkali, korektaSkali, korektaSkali);

            Vector3 kompas0Po = instancja.transform.TransformPoint(p0);
            instancja.transform.position -= kompas0Po;

            UnityEngine.Object.DestroyImmediate(k0.gameObject);
            if (kE != null) UnityEngine.Object.DestroyImmediate(kE.gameObject);
            if (kN != null) UnityEngine.Object.DestroyImmediate(kN.gameObject);
            if (kU != null) UnityEngine.Object.DestroyImmediate(kU.gameObject);
        }
        else
        {
            Debug.LogWarning($"ZbudujGliwice: {fbxNazwa} nie ma markerów Kompas_*, pozycja bez korekty osi.");
        }

        // finalne umieszczenie landmarka/warstwy na mapie
        Vector3 pozUnity = MapaDoUnity(pozMapa);
        instancja.transform.position += pozUnity;
        instancja.transform.rotation = Quaternion.Euler(0f, -katDeg, 0f) * instancja.transform.rotation;

        instancja.transform.SetParent(grupaTransform, true);
        PrefabUtility.UnpackPrefabInstance(instancja, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        obiektWynik = instancja;
        return true;
    }

    static Transform ZnajdzRekurencyjnie(Transform root, string nazwa)
    {
        if (root.name == nazwa) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var w = ZnajdzRekurencyjnie(root.GetChild(i), nazwa);
            if (w != null) return w;
        }
        return null;
    }

    // ---------- d) kolizje i statyczność ----------

    static int ZastosujKolizjeIStatyke(Transform root, Transform punktyGrupa)
    {
        int liczbaKolideryw = 0;
        var filtry = root.GetComponentsInChildren<MeshFilter>(true);
        foreach (var mf in filtry)
        {
            var go = mf.gameObject;
            string n = go.name;

            if (n.EndsWith("_COL"))
            {
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null) UnityEngine.Object.DestroyImmediate(mr);
                liczbaKolideryw++;
            }
            else if (n.EndsWith("_NOCOL") || n.StartsWith("Woda") || n.StartsWith("Krzak"))
            {
                // bez kolidera
            }
            else if (n.StartsWith("Drzewo"))
            {
                var cc = go.AddComponent<CapsuleCollider>();
                cc.radius = 0.35f;
                cc.height = 3f;
                Vector3 centrumSwiata = go.transform.position + Vector3.up * 1.5f;
                Vector3 lossy = go.transform.lossyScale;
                Vector3 centrumLokalnie = go.transform.InverseTransformPoint(centrumSwiata);
                cc.center = centrumLokalnie;
                liczbaKolideryw++;
            }
            else
            {
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                liczbaKolideryw++;
            }
        }

        const StaticEditorFlags FLAGI =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.ContributeGI |
            StaticEditorFlags.NavigationStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OffMeshLinkGeneration |
            StaticEditorFlags.ReflectionProbeStatic;

        var wszystkie = root.GetComponentsInChildren<Transform>(true);
        foreach (var t in wszystkie)
        {
            if (t == punktyGrupa || JestPotomkiem(t, punktyGrupa)) continue;
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, FLAGI);
        }
        return liczbaKolideryw;
    }

    static bool JestPotomkiem(Transform t, Transform mozliwyRodzic)
    {
        var p = t.parent;
        while (p != null)
        {
            if (p == mozliwyRodzic) return true;
            p = p.parent;
        }
        return false;
    }

    // ---------- f) atmosfera ----------

    static MapaSwiatlo ZbudujAtmosfere(Transform root)
    {
        var slonceGo = new GameObject("Slonce");
        slonceGo.transform.SetParent(root, false);
        var slonce = slonceGo.AddComponent<Light>();
        slonce.type = LightType.Directional;
        slonce.color = new Color(1f, 0.96f, 0.88f);
        slonce.intensity = 1.25f;
        slonce.shadows = LightShadows.Soft;
        slonceGo.transform.rotation = Quaternion.Euler(55f, 200f, 0f);

        Directory.CreateDirectory(MATERIALY);
        string skyboxPath = $"{MATERIALY}/Skybox_Gliwice.mat";
        Material skybox = AssetDatabase.LoadAssetAtPath<Material>(skyboxPath);
        if (skybox == null)
        {
            skybox = new Material(Shader.Find("Skybox/Procedural"));
            AssetDatabase.CreateAsset(skybox, skyboxPath);
        }
        RenderSettings.skybox = skybox;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.65f, 0.85f);
        RenderSettings.ambientEquatorColor = new Color(0.6f, 0.58f, 0.5f);
        RenderSettings.ambientGroundColor = new Color(0.35f, 0.3f, 0.22f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 150f;
        RenderSettings.fogEndDistance = 900f;
        RenderSettings.fogColor = new Color(0.72f, 0.68f, 0.6f);

        Directory.CreateDirectory(USTAWIENIA);
        string volumePath = $"{USTAWIENIA}/MapaVolume.asset";
        VolumeProfile profil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(volumePath);
        if (profil == null)
        {
            profil = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profil, volumePath);
        }

        var tonemapping = ZdobadzOverride<Tonemapping>(profil);
        tonemapping.mode.overrideState = true;
        tonemapping.mode.value = TonemappingMode.ACES;

        var bloom = ZdobadzOverride<Bloom>(profil);
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 0.6f;

        var kolor = ZdobadzOverride<ColorAdjustments>(profil);
        kolor.postExposure.overrideState = true;
        kolor.postExposure.value = 0.1f;
        kolor.contrast.overrideState = true;
        kolor.contrast.value = 12f;
        kolor.saturation.overrideState = true;
        kolor.saturation.value = -18f;
        kolor.colorFilter.overrideState = true;
        kolor.colorFilter.value = new Color(1f, 0.96f, 0.9f);

        var winieta = ZdobadzOverride<Vignette>(profil);
        winieta.intensity.overrideState = true;
        winieta.intensity.value = 0.25f;

        var ziarno = ZdobadzOverride<FilmGrain>(profil);
        ziarno.intensity.overrideState = true;
        ziarno.intensity.value = 0.25f;

        EditorUtility.SetDirty(profil);
        AssetDatabase.SaveAssets();

        var volumeGo = new GameObject("Global Volume");
        volumeGo.transform.SetParent(root, false);
        var volume = volumeGo.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.weight = 1f;
        volume.profile = profil;

        var mapaSwiatlo = root.gameObject.AddComponent<MapaSwiatlo>();
        mapaSwiatlo.slonce = slonce;
        mapaSwiatlo.materialyEmisjaNocna = new List<Material>(_matyEmisjaNocna);

        return mapaSwiatlo;
    }

    static T ZdobadzOverride<T>(VolumeProfile profil) where T : VolumeComponent
    {
        if (!profil.TryGet(out T skladnik))
            skladnik = profil.Add<T>(true);
        return skladnik;
    }

    // ---------- g) walidacja ----------

    static (int sprawdzone, int bezPodloza) ZwalidujPodloze(Transform punktyGrupa)
    {
        Physics.SyncTransforms();
        int sprawdzone = 0, bezPodloza = 0;
        for (int i = 0; i < punktyGrupa.childCount; i++)
        {
            var t = punktyGrupa.GetChild(i);
            sprawdzone++;
            Vector3 start = t.position + Vector3.up * 50f;
            if (!Physics.Raycast(start, Vector3.down, 100f))
            {
                Debug.LogWarning($"ZbudujGliwice: punkt '{t.name}' bez podłoża pod nim.");
                bezPodloza++;
            }
        }
        return (sprawdzone, bezPodloza);
    }

    // ---------- pomocnicze ----------

    static Vector3 MapaDoUnity(Vector3 blender) => new Vector3(blender.x, blender.z, blender.y);

    static Vector3 WczytajWektor(object tablica)
    {
        var lista = (List<object>)tablica;
        return new Vector3(Convert.ToSingle(lista[0]), Convert.ToSingle(lista[1]), Convert.ToSingle(lista[2]));
    }

    static Color HexDoColor(string hex)
    {
        hex = hex.TrimStart('#');
        float r = Convert.ToInt32(hex.Substring(0, 2), 16) / 255f;
        float g = Convert.ToInt32(hex.Substring(2, 2), 16) / 255f;
        float b = Convert.ToInt32(hex.Substring(4, 2), 16) / 255f;
        return new Color(r, g, b, 1f);
    }

    static Dictionary<string, object> WczytajJson(string wzgledemAssets)
    {
        string pelna = Path.Combine(Application.dataPath, "..", wzgledemAssets);
        if (!File.Exists(pelna)) return null;
        string tekst = File.ReadAllText(pelna);
        return MiniJson.Parsuj(tekst) as Dictionary<string, object>;
    }
}

/// <summary>Minimalny parser JSON (bez zależności), zwraca Dictionary/List/string/double/bool/null.</summary>
static class MiniJson
{
    public static object Parsuj(string tekst)
    {
        int i = 0;
        return ParsujWartosc(tekst, ref i);
    }

    static object ParsujWartosc(string s, ref int i)
    {
        PrzeskoczBiale(s, ref i);
        char c = s[i];
        if (c == '{') return ParsujObiekt(s, ref i);
        if (c == '[') return ParsujTablice(s, ref i);
        if (c == '"') return ParsujString(s, ref i);
        if (c == 't') { i += 4; return true; }
        if (c == 'f') { i += 5; return false; }
        if (c == 'n') { i += 4; return null; }
        return ParsujLiczbe(s, ref i);
    }

    static Dictionary<string, object> ParsujObiekt(string s, ref int i)
    {
        var wynik = new Dictionary<string, object>();
        i++; // {
        PrzeskoczBiale(s, ref i);
        if (s[i] == '}') { i++; return wynik; }
        while (true)
        {
            PrzeskoczBiale(s, ref i);
            string klucz = ParsujString(s, ref i);
            PrzeskoczBiale(s, ref i);
            i++; // :
            object wartosc = ParsujWartosc(s, ref i);
            wynik[klucz] = wartosc;
            PrzeskoczBiale(s, ref i);
            if (s[i] == ',') { i++; continue; }
            if (s[i] == '}') { i++; break; }
        }
        return wynik;
    }

    static List<object> ParsujTablice(string s, ref int i)
    {
        var wynik = new List<object>();
        i++; // [
        PrzeskoczBiale(s, ref i);
        if (s[i] == ']') { i++; return wynik; }
        while (true)
        {
            object wartosc = ParsujWartosc(s, ref i);
            wynik.Add(wartosc);
            PrzeskoczBiale(s, ref i);
            if (s[i] == ',') { i++; continue; }
            if (s[i] == ']') { i++; break; }
        }
        return wynik;
    }

    static string ParsujString(string s, ref int i)
    {
        i++; // "
        var sb = new System.Text.StringBuilder();
        while (s[i] != '"')
        {
            if (s[i] == '\\')
            {
                i++;
                char esc = s[i];
                switch (esc)
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'u':
                        string hex = s.Substring(i + 1, 4);
                        sb.Append((char)Convert.ToInt32(hex, 16));
                        i += 4;
                        break;
                    default: sb.Append(esc); break;
                }
            }
            else sb.Append(s[i]);
            i++;
        }
        i++; // "
        return sb.ToString();
    }

    static double ParsujLiczbe(string s, ref int i)
    {
        int start = i;
        while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
        return double.Parse(s.Substring(start, i - start), System.Globalization.CultureInfo.InvariantCulture);
    }

    static void PrzeskoczBiale(string s, ref int i)
    {
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
    }
}
