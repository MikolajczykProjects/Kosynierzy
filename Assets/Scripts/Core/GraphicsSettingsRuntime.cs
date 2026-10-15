using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Kosynierzy
{
    /// <summary>
    /// Graphics options from the settings menu (quality level, display, VSync, frame cap, render scale, shadows,
    /// anti-aliasing, post-processing, fog). Persisted in PlayerPrefs and applied at startup and on every scene load.
    /// Named GameGraphicsSettings to avoid a clash with UnityEngine.Rendering.GraphicsSettings.
    /// In the Editor the URP asset and quality values it touches are restored when Play mode ends,
    /// so testing the menu never dirties Assets/Settings.
    /// </summary>
    public static class GameGraphicsSettings
    {
        public enum ShadowSetting { Off, Low, High }
        public enum AntiAliasingSetting { Off, Fxaa, Smaa, Msaa4x }

        /// <summary>Frame cap options; 0 = unlimited.</summary>
        public static readonly int[] FrameCaps = { 30, 60, 120, 144, 0 };

        const string Prefix = "kosynierzy.gfx.";

        public static int QualityLevel;
        public static int ResolutionWidth;       // 0 = keep what the game launched with
        public static int ResolutionHeight;
        public static FullScreenMode ScreenMode = FullScreenMode.FullScreenWindow;
        public static bool VSync = true;
        public static int FrameCap;               // 0 = unlimited
        public static float RenderScale = 1f;     // 0.5 – 1.5
        public static ShadowSetting Shadows = ShadowSetting.High;
        public static AntiAliasingSetting AntiAliasing = AntiAliasingSetting.Smaa;
        public static bool PostProcessing = true;
        public static bool Fog = true;

        /// <summary>Raised after Apply().</summary>
        public static event Action Changed;

        static bool sceneFog = true;              // whether the current scene is authored with fog

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Changed = null;
            sceneFog = true;
#if UNITY_EDITOR
            snapshots.Clear();
            vSyncSnapshots.Clear();
            originalQuality = -1;
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            sceneFog = RenderSettings.fog;
            Load();
            Apply();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) sceneFog = RenderSettings.fog;
            ApplyCameras();
            ApplyFog();
        }

        public static void Load()
        {
            QualityLevel = PlayerPrefs.GetInt(Prefix + "quality", QualitySettings.GetQualityLevel());
            ResolutionWidth = PlayerPrefs.GetInt(Prefix + "resW", 0);
            ResolutionHeight = PlayerPrefs.GetInt(Prefix + "resH", 0);
            ScreenMode = (FullScreenMode)PlayerPrefs.GetInt(Prefix + "screenMode", (int)Screen.fullScreenMode);
            VSync = PlayerPrefs.GetInt(Prefix + "vsync", 1) == 1;
            FrameCap = PlayerPrefs.GetInt(Prefix + "frameCap", 0);
            RenderScale = PlayerPrefs.GetFloat(Prefix + "renderScale", 1f);
            Shadows = (ShadowSetting)PlayerPrefs.GetInt(Prefix + "shadows", (int)ShadowSetting.High);
            AntiAliasing = (AntiAliasingSetting)PlayerPrefs.GetInt(Prefix + "aa", (int)AntiAliasingSetting.Smaa);
            PostProcessing = PlayerPrefs.GetInt(Prefix + "post", 1) == 1;
            Fog = PlayerPrefs.GetInt(Prefix + "fog", 1) == 1;
        }

        public static void Save()
        {
            PlayerPrefs.SetInt(Prefix + "quality", QualityLevel);
            PlayerPrefs.SetInt(Prefix + "resW", ResolutionWidth);
            PlayerPrefs.SetInt(Prefix + "resH", ResolutionHeight);
            PlayerPrefs.SetInt(Prefix + "screenMode", (int)ScreenMode);
            PlayerPrefs.SetInt(Prefix + "vsync", VSync ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "frameCap", FrameCap);
            PlayerPrefs.SetFloat(Prefix + "renderScale", RenderScale);
            PlayerPrefs.SetInt(Prefix + "shadows", (int)Shadows);
            PlayerPrefs.SetInt(Prefix + "aa", (int)AntiAliasing);
            PlayerPrefs.SetInt(Prefix + "post", PostProcessing ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "fog", Fog ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>Applies every option. Cheap: quality level and resolution change only when they differ.</summary>
        public static void Apply()
        {
            ApplyQuality();
            ApplyDisplay();
#if UNITY_EDITOR
            RememberVSync();
#endif
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = FrameCap > 0 ? FrameCap : -1;
            ApplyPipelineAsset();
            ApplyCameras();
            ApplyFog();
            Changed?.Invoke();
        }

        /// <summary>Called by AtmosphereController when it switches the scene fog on or off.</summary>
        public static void SetSceneFog(bool enabled)
        {
            sceneFog = enabled;
            ApplyFog();
        }

        /// <summary>Re-applies camera options, e.g. after a new camera was spawned.</summary>
        public static void ApplyCameras()
        {
            foreach (var cam in Camera.allCameras) ApplyToCamera(cam);
        }

        public static void ApplyToCamera(Camera cam)
        {
            if (cam == null || !cam.TryGetComponent<UniversalAdditionalCameraData>(out var data)) return;
            if (data.renderType != CameraRenderType.Base) return;
            data.renderPostProcessing = PostProcessing;
            data.renderShadows = Shadows != ShadowSetting.Off;
            data.antialiasing = AntiAliasing switch
            {
                AntiAliasingSetting.Fxaa => AntialiasingMode.FastApproximateAntialiasing,
                AntiAliasingSetting.Smaa => AntialiasingMode.SubpixelMorphologicalAntiAliasing,
                _ => AntialiasingMode.None,
            };
            data.antialiasingQuality = AntialiasingQuality.High;
            cam.allowMSAA = true;
        }

        static void ApplyFog() => RenderSettings.fog = sceneFog && Fog;

        static void ApplyQuality()
        {
            int count = QualitySettings.names.Length;
            if (count == 0) return;
            QualityLevel = Mathf.Clamp(QualityLevel, 0, count - 1);
            if (QualitySettings.GetQualityLevel() == QualityLevel) return;
#if UNITY_EDITOR
            if (originalQuality < 0) originalQuality = QualitySettings.GetQualityLevel();
#endif
            QualitySettings.SetQualityLevel(QualityLevel, true);
        }

        /// <summary>Resolution and window mode. Skipped in the Editor (the Game view owns its size).</summary>
        public static void ApplyDisplay()
        {
            if (Application.isEditor) return;
            int w = ResolutionWidth > 0 ? ResolutionWidth : Screen.width;
            int h = ResolutionHeight > 0 ? ResolutionHeight : Screen.height;
            if (w != Screen.width || h != Screen.height || ScreenMode != Screen.fullScreenMode)
                Screen.SetResolution(w, h, ScreenMode);
        }

        /// <summary>The URP asset of the active quality level (Assets/Settings/PC_RPAsset or Mobile_RPAsset).</summary>
        public static UniversalRenderPipelineAsset ActivePipelineAsset
        {
            get
            {
                var rp = QualitySettings.renderPipeline != null ? QualitySettings.renderPipeline : GraphicsSettings.defaultRenderPipeline;
                return rp as UniversalRenderPipelineAsset;
            }
        }

        static void ApplyPipelineAsset()
        {
            var urp = ActivePipelineAsset;
            if (urp == null) return;
#if UNITY_EDITOR
            RememberAsset(urp);
#endif
            urp.renderScale = Mathf.Clamp(RenderScale, 0.5f, 1.5f);
            urp.msaaSampleCount = AntiAliasing == AntiAliasingSetting.Msaa4x ? 4 : 1;

            // Off is handled per camera (renderShadows); Low/High pick the asset's shadow budget.
            if (Shadows == ShadowSetting.Low)
            {
                urp.shadowDistance = 40f;
                urp.mainLightShadowmapResolution = 1024;
                urp.additionalLightsShadowmapResolution = 512;
                urp.shadowCascadeCount = 1;
                SetNonPublic(urp, "supportsSoftShadows", false);
                SetNonPublic(urp, "supportsAdditionalLightShadows", false);
            }
            else if (Shadows == ShadowSetting.High)
            {
                urp.shadowDistance = 100f;
                urp.mainLightShadowmapResolution = 2048;
                urp.additionalLightsShadowmapResolution = 2048;
                urp.shadowCascadeCount = 4;
                SetNonPublic(urp, "supportsSoftShadows", true);
                SetNonPublic(urp, "supportsAdditionalLightShadows", true);
            }
        }

        /// <summary>Some URP asset toggles have internal setters; set them by reflection and skip quietly if that fails.</summary>
        static void SetNonPublic(object target, string property, object value)
        {
            try
            {
                var prop = target.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var setter = prop?.GetSetMethod(true);
                setter?.Invoke(target, new[] { value });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Kosynierzy: could not set URP '{property}' ({e.Message}); skipped.");
            }
        }

        static object GetProperty(object target, string property)
        {
            try
            {
                var prop = target.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return prop?.GetValue(target);
            }
            catch
            {
                return null;
            }
        }

#if UNITY_EDITOR
        // ---- Editor only: put project assets back the way they were when Play mode ends. ----

        sealed class AssetSnapshot
        {
            public float renderScale, shadowDistance;
            public int msaa, mainShadowRes, additionalShadowRes, cascades;
            public object softShadows, additionalShadows;
        }

        static readonly Dictionary<UniversalRenderPipelineAsset, AssetSnapshot> snapshots = new();
        static readonly Dictionary<int, int> vSyncSnapshots = new();
        static int originalQuality = -1;

        static void RememberAsset(UniversalRenderPipelineAsset urp)
        {
            if (snapshots.ContainsKey(urp)) return;
            snapshots[urp] = new AssetSnapshot
            {
                renderScale = urp.renderScale,
                shadowDistance = urp.shadowDistance,
                msaa = urp.msaaSampleCount,
                mainShadowRes = urp.mainLightShadowmapResolution,
                additionalShadowRes = urp.additionalLightsShadowmapResolution,
                cascades = urp.shadowCascadeCount,
                softShadows = GetProperty(urp, "supportsSoftShadows"),
                additionalShadows = GetProperty(urp, "supportsAdditionalLightShadows"),
            };
        }

        static void RememberVSync()
        {
            int level = QualitySettings.GetQualityLevel();
            if (!vSyncSnapshots.ContainsKey(level)) vSyncSnapshots[level] = QualitySettings.vSyncCount;
        }

        static void OnPlayModeChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state != UnityEditor.PlayModeStateChange.ExitingPlayMode) return;

            foreach (var pair in snapshots)
            {
                var urp = pair.Key;
                if (urp == null) continue;
                var s = pair.Value;
                urp.renderScale = s.renderScale;
                urp.shadowDistance = s.shadowDistance;
                urp.msaaSampleCount = s.msaa;
                urp.mainLightShadowmapResolution = s.mainShadowRes;
                urp.additionalLightsShadowmapResolution = s.additionalShadowRes;
                urp.shadowCascadeCount = s.cascades;
                if (s.softShadows != null) SetNonPublic(urp, "supportsSoftShadows", s.softShadows);
                if (s.additionalShadows != null) SetNonPublic(urp, "supportsAdditionalLightShadows", s.additionalShadows);
            }
            snapshots.Clear();

            foreach (var pair in vSyncSnapshots)
            {
                QualitySettings.SetQualityLevel(pair.Key, false);
                QualitySettings.vSyncCount = pair.Value;
            }
            vSyncSnapshots.Clear();

            if (originalQuality >= 0) QualitySettings.SetQualityLevel(originalQuality, false);
            originalQuality = -1;
        }
#endif
    }
}
