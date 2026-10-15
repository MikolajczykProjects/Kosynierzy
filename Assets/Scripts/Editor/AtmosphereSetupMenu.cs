using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kosynierzy.EditorTools
{
    /// <summary>
    /// Kosynierzy > Setup Atmosphere: directional sun, Trilight ambient, linear fog, procedural skybox and the
    /// Global Volume with its own profile (Assets/Settings/Atmosphere_Dusk.asset; the template profile is untouched),
    /// all driven by an AtmosphereController set to 15:30. Idempotent: re-running keeps tweaked values.
    /// </summary>
    static class AtmosphereSetupMenu
    {
        const string ProfilePath = "Assets/Settings/Atmosphere_Dusk.asset";
        const string SkyboxPath = "Assets/Materials/Skybox_TimeOfDay.mat";
        const float StartHour = 15.5f;

        [MenuItem("Kosynierzy/Setup Atmosphere (Low-Poly Dusk)")]
        static void SetupAtmosphere()
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Setup Atmosphere");
            int group = Undo.GetCurrentGroup();

            var profile = LoadOrCreateProfile();
            var skybox = LoadOrCreateSkybox();
            var sun = FindOrCreateSun();
            var volume = FindOrCreateVolume(profile);
            EnablePostProcessingOnMainCamera(volume);
            WarnIfNoHdr();

            var controller = Object.FindAnyObjectByType<AtmosphereController>();
            if (controller == null)
            {
                var go = new GameObject("Atmosphere");
                Undo.RegisterCreatedObjectUndo(go, "Create Atmosphere");
                controller = go.AddComponent<AtmosphereController>();
            }
            Undo.RecordObject(controller, "Setup Atmosphere");
            controller.Configure(sun, volume, skybox, StartHour);
            controller.Apply();
            EditorUtility.SetDirty(controller);

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = controller.gameObject;
            Debug.Log("Kosynierzy: atmosphere ready (15:30). Tweak presets on the 'Atmosphere' object; quest steps call " +
                      "AtmosphereController.SetTimeOfDay(19) / (19.5). If the ambient looks stale, use Window > Rendering > " +
                      "Lighting > Generate Lighting. Save the scene (Ctrl+S).");
        }

        static VolumeProfile LoadOrCreateProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile != null) return profile;
            EnsureFolder("Assets/Settings");
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
            return profile; // components are added (as sub-assets) by AtmosphereController.Apply
        }

        static Material LoadOrCreateSkybox()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
            if (mat != null) return mat;
            var shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                Debug.LogWarning("Kosynierzy: shader Skybox/Procedural not found; skybox left unchanged.");
                return null;
            }
            EnsureFolder("Assets/Materials");
            mat = new Material(shader) { name = "Skybox_TimeOfDay" };
            if (mat.HasProperty("_SunDisk")) mat.SetFloat("_SunDisk", 2f); // high quality sun disk
            if (mat.HasProperty("_SunSizeConvergence")) mat.SetFloat("_SunSizeConvergence", 4f);
            AssetDatabase.CreateAsset(mat, SkyboxPath);
            return mat;
        }

        static Light FindOrCreateSun()
        {
            if (RenderSettings.sun != null) return RenderSettings.sun;
            foreach (var light in Object.FindObjectsByType<Light>())
                if (light.type == LightType.Directional) return light;

            var go = new GameObject("Directional Light");
            Undo.RegisterCreatedObjectUndo(go, "Create Sun");
            var sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            return sun;
        }

        static Volume FindOrCreateVolume(VolumeProfile profile)
        {
            Volume volume = null;
            var named = GameObject.Find("Global Volume");
            if (named != null) volume = named.GetComponent<Volume>();
            if (volume == null)
            {
                foreach (var v in Object.FindObjectsByType<Volume>())
                    if (v.isGlobal) { volume = v; break; }
            }
            if (volume == null)
            {
                var go = new GameObject("Global Volume");
                Undo.RegisterCreatedObjectUndo(go, "Create Global Volume");
                volume = go.AddComponent<Volume>();
            }

            Undo.RecordObject(volume, "Assign Atmosphere Profile");
            volume.isGlobal = true;
            volume.weight = 1f;
            volume.sharedProfile = profile;
            EditorUtility.SetDirty(volume);
            return volume;
        }

        static void EnablePostProcessingOnMainCamera(Volume volume)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("Kosynierzy: no Main Camera in the scene; enable Post Processing on the player camera by hand.");
                return;
            }
            var data = cam.GetComponent<UniversalAdditionalCameraData>();
            if (data == null) data = Undo.AddComponent<UniversalAdditionalCameraData>(cam.gameObject);
            Undo.RecordObject(data, "Enable Post-Processing");
            data.renderPostProcessing = true;
            int layerBit = 1 << volume.gameObject.layer;
            if ((data.volumeLayerMask.value & layerBit) == 0) data.volumeLayerMask = data.volumeLayerMask.value | layerBit;
            EditorUtility.SetDirty(data);
        }

        static void WarnIfNoHdr()
        {
            var urp = GameGraphicsSettings.ActivePipelineAsset;
            if (urp == null)
                Debug.LogWarning("Kosynierzy: the active render pipeline is not URP; post-processing will not show.");
            else if (!urp.supportsHDR)
                Debug.LogWarning($"Kosynierzy: HDR is off on {urp.name}; ACES tonemapping and bloom need it (URP asset > Quality > HDR).");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
