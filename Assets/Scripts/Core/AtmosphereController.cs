using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kosynierzy
{
    /// <summary>One lighting look at a given hour; AtmosphereController blends between these.</summary>
    [Serializable]
    public class LightingPreset
    {
        public string name = "Preset";
        [Range(0f, 24f)] public float hour = 12f;

        [Header("Sun / moon (the scene's directional light)")]
        [Range(-10f, 90f)] public float sunElevation = 30f;
        [Tooltip("Compass direction the light comes FROM, degrees clockwise from +Z (north).")]
        [Range(0f, 360f)] public float sunAzimuth = 230f;
        public Color sunColor = Color.white;
        [Min(0f)] public float sunIntensity = 1f;
        [Range(0f, 1f)] public float shadowStrength = 0.85f;

        [Header("Ambient (Trilight gradient)")]
        [ColorUsage(false, true)] public Color ambientSky = new(0.45f, 0.5f, 0.6f);
        [ColorUsage(false, true)] public Color ambientEquator = new(0.45f, 0.4f, 0.35f);
        [ColorUsage(false, true)] public Color ambientGround = new(0.18f, 0.15f, 0.12f);
        [Range(0f, 1f)] public float reflectionIntensity = 1f;

        [Header("Fog (linear)")]
        public Color fogColor = new(0.6f, 0.6f, 0.62f);
        [Min(0f)] public float fogStart = 25f;
        [Min(0f)] public float fogEnd = 180f;

        [Header("Procedural skybox")]
        public Color skyTint = new(0.5f, 0.5f, 0.5f);
        public Color skyGround = new(0.37f, 0.35f, 0.34f);
        [Range(0f, 8f)] public float skyExposure = 1.2f;
        [Range(0f, 5f)] public float atmosphereThickness = 1f;
        [Range(0f, 1f)] public float sunSize = 0.04f;

        [Header("Post-processing (per time of day)")]
        public float postExposure = 0.2f;
        public Color colorFilter = Color.white;
        [Range(0f, 1f)] public float vignette = 0.25f;
        [Range(-100f, 100f)] public float whiteBalanceTemperature = 5f;

        public LightingPreset Clone() => (LightingPreset)MemberwiseClone();

        /// <summary>Writes the blend of a and b into target (no allocation).</summary>
        public static void Lerp(LightingPreset a, LightingPreset b, float t, LightingPreset target)
        {
            target.hour = Mathf.Lerp(a.hour, b.hour, t);
            target.sunElevation = Mathf.Lerp(a.sunElevation, b.sunElevation, t);
            target.sunAzimuth = Mathf.LerpAngle(a.sunAzimuth, b.sunAzimuth, t);
            target.sunColor = Color.Lerp(a.sunColor, b.sunColor, t);
            target.sunIntensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t);
            target.shadowStrength = Mathf.Lerp(a.shadowStrength, b.shadowStrength, t);
            target.ambientSky = Color.Lerp(a.ambientSky, b.ambientSky, t);
            target.ambientEquator = Color.Lerp(a.ambientEquator, b.ambientEquator, t);
            target.ambientGround = Color.Lerp(a.ambientGround, b.ambientGround, t);
            target.reflectionIntensity = Mathf.Lerp(a.reflectionIntensity, b.reflectionIntensity, t);
            target.fogColor = Color.Lerp(a.fogColor, b.fogColor, t);
            target.fogStart = Mathf.Lerp(a.fogStart, b.fogStart, t);
            target.fogEnd = Mathf.Lerp(a.fogEnd, b.fogEnd, t);
            target.skyTint = Color.Lerp(a.skyTint, b.skyTint, t);
            target.skyGround = Color.Lerp(a.skyGround, b.skyGround, t);
            target.skyExposure = Mathf.Lerp(a.skyExposure, b.skyExposure, t);
            target.atmosphereThickness = Mathf.Lerp(a.atmosphereThickness, b.atmosphereThickness, t);
            target.sunSize = Mathf.Lerp(a.sunSize, b.sunSize, t);
            target.postExposure = Mathf.Lerp(a.postExposure, b.postExposure, t);
            target.colorFilter = Color.Lerp(a.colorFilter, b.colorFilter, t);
            target.vignette = Mathf.Lerp(a.vignette, b.vignette, t);
            target.whiteBalanceTemperature = Mathf.Lerp(a.whiteBalanceTemperature, b.whiteBalanceTemperature, t);
        }
    }

    /// <summary>
    /// Scene lighting and mood driven by mission steps, not real time: SetTime(hour) blends the sun, ambient, fog,
    /// procedural skybox and the Global Volume between presets (15:30 afternoon, 19:00 sunset, 19:30 night).
    /// NightChanged fires when the time crosses lampsOnFromHour (street lamps). Tweak values here and use the
    /// context menu "Apply" (or just edit — live update in edit mode). Created by Kosynierzy > Setup Atmosphere.
    /// </summary>
    [DisallowMultipleComponent]
    public class AtmosphereController : MonoBehaviour
    {
        static readonly int SunSizeId = Shader.PropertyToID("_SunSize");
        static readonly int SkyTintId = Shader.PropertyToID("_SkyTint");
        static readonly int GroundColorId = Shader.PropertyToID("_GroundColor");
        static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        static readonly int AtmosphereThicknessId = Shader.PropertyToID("_AtmosphereThickness");

        /// <summary>The active controller (null when the scene has none).</summary>
        public static AtmosphereController Current { get; private set; }
        /// <summary>True from lampsOnFromHour on.</summary>
        public static bool IsNight { get; private set; }
        /// <summary>Raised when the time crosses lampsOnFromHour in either direction.</summary>
        public static event Action<bool> NightChanged;

        [Header("Time of day")]
        [Tooltip("Hour shown in the editor and applied on Start.")]
        [SerializeField, Range(0f, 24f)] float hour = 15.5f;
        [Tooltip("Street lamps switch on (NightChanged(true)) from this hour.")]
        [SerializeField, Range(0f, 24f)] float lampsOnFromHour = 19f;
        [SerializeField] List<LightingPreset> presets = DefaultPresets();

        [Header("References (filled by the setup menu)")]
        [SerializeField] Light sun;
        [SerializeField] Volume volume;
        [SerializeField] Material skybox;
        [SerializeField] bool fogEnabled = true;

        [Header("Post-processing (constant)")]
        [SerializeField, Range(-100f, 100f)] float contrast = 10f;
        [SerializeField, Range(-100f, 100f)] float saturation = -10f;
        [SerializeField, Min(0f)] float bloomThreshold = 1f;
        [SerializeField, Min(0f)] float bloomIntensity = 0.6f;
        [SerializeField, Range(0f, 1f)] float bloomScatter = 0.7f;
        [SerializeField, Range(0f, 1f)] float filmGrain = 0.15f;
        [SerializeField] Color splitShadows = new(0.42f, 0.5f, 0.56f);
        [SerializeField] Color splitHighlights = new(0.62f, 0.52f, 0.42f);
        [SerializeField, Range(-100f, 100f)] float splitBalance = 0f;

        [Header("Editor")]
        [Tooltip("Re-apply on every inspector change in edit mode.")]
        [SerializeField] bool liveUpdate = true;

        readonly LightingPreset scratch = new();
        float currentHour, blendFrom, blendTo, blendDuration, blendElapsed;
        bool blending;
        float nextEnvironmentUpdate;
        Material runtimeSkybox;

        public float CurrentHour => currentHour;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Current = null;
            IsNight = false;
            NightChanged = null;
        }

        void Awake() => currentHour = hour;

        void OnEnable()
        {
            if (Application.isPlaying) Current = this;
        }

        void OnDisable()
        {
            if (Current == this) Current = null;
        }

        void Start()
        {
            if (!Application.isPlaying) return;
            // Work on copies at runtime so Play mode never rewrites the skybox material asset.
            if (skybox != null)
            {
                runtimeSkybox = new Material(skybox) { name = skybox.name + " (runtime)" };
            }
            currentHour = hour;
            ApplyHour(currentHour, markDirty: false);
            UpdateEnvironment(force: true);
        }

        void OnDestroy()
        {
            if (runtimeSkybox != null) Destroy(runtimeSkybox);
        }

        void Update()
        {
            if (!Application.isPlaying || !blending) return;
            blendElapsed += Time.deltaTime;
            float t = blendDuration <= 0f ? 1f : Mathf.Clamp01(blendElapsed / blendDuration);
            currentHour = Mathf.Lerp(blendFrom, blendTo, Mathf.SmoothStep(0f, 1f, t));
            ApplyHour(currentHour, markDirty: false);
            if (t >= 1f)
            {
                blending = false;
                UpdateEnvironment(force: true);
            }
            else UpdateEnvironment(force: false);
        }

        /// <summary>Moves the clock to <paramref name="newHour"/> (e.g. 15.5 = 15:30), blending over real game seconds.</summary>
        public void SetTime(float newHour, float blendSeconds = 2f)
        {
            newHour = Mathf.Repeat(newHour, 24f);
            hour = newHour;
            if (!Application.isPlaying || blendSeconds <= 0f)
            {
                blending = false;
                currentHour = newHour;
                ApplyHour(currentHour, markDirty: false);
                UpdateEnvironment(force: true);
                return;
            }
            blendFrom = currentHour;
            blendTo = newHour;
            blendDuration = blendSeconds;
            blendElapsed = 0f;
            blending = true;
        }

        /// <summary>One-argument version for UnityEvents (quest step onStarted/onCompleted): blends over 2 s.</summary>
        public void SetTimeOfDay(float newHour) => SetTime(newHour);

        /// <summary>Static shortcut for code: does nothing when the scene has no controller.</summary>
        public static void SetTimeGlobal(float newHour, float blendSeconds = 2f)
        {
            if (Current != null) Current.SetTime(newHour, blendSeconds);
        }

        /// <summary>Applies the inspector hour now (context menu, setup menu).</summary>
        [ContextMenu("Apply")]
        public void Apply()
        {
            currentHour = hour;
            ApplyHour(hour, markDirty: true);
            UpdateEnvironment(force: true);
        }

        /// <summary>Called by the setup menu; follow with Apply().</summary>
        public void Configure(Light sunLight, Volume globalVolume, Material skyboxMaterial, float startHour)
        {
            sun = sunLight;
            volume = globalVolume;
            skybox = skyboxMaterial;
            hour = Mathf.Repeat(startHour, 24f);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!liveUpdate) return;
            UnityEditor.EditorApplication.delayCall -= DelayedEditorApply;
            UnityEditor.EditorApplication.delayCall += DelayedEditorApply;
        }

        void DelayedEditorApply()
        {
            if (this == null || !isActiveAndEnabled) return;
            if (Application.isPlaying)
            {
                if (!blending) { currentHour = hour; ApplyHour(currentHour, markDirty: false); }
                return;
            }
            currentHour = hour;
            ApplyHour(hour, markDirty: false);
            UpdateEnvironment(force: true);
        }
#endif

        /// <summary>Blended preset for an hour: before the first preset = first, after the last = last.</summary>
        public LightingPreset Evaluate(float atHour)
        {
            if (presets == null || presets.Count == 0) presets = DefaultPresets();
            presets.Sort((a, b) => a.hour.CompareTo(b.hour));
            if (atHour < 5f) atHour += 24f; // small hours count as the night after the last preset
            if (atHour <= presets[0].hour) { LightingPreset.Lerp(presets[0], presets[0], 0f, scratch); return scratch; }
            for (int i = 0; i < presets.Count - 1; i++)
            {
                var a = presets[i];
                var b = presets[i + 1];
                if (atHour <= b.hour)
                {
                    float span = Mathf.Max(0.0001f, b.hour - a.hour);
                    LightingPreset.Lerp(a, b, (atHour - a.hour) / span, scratch);
                    return scratch;
                }
            }
            var last = presets[presets.Count - 1];
            LightingPreset.Lerp(last, last, 0f, scratch);
            return scratch;
        }

        void ApplyHour(float atHour, bool markDirty)
        {
            var p = Evaluate(atHour);
            ApplySun(p, markDirty);
            ApplyRenderSettings(p);
            ApplySky(p, markDirty);
            ApplyVolume(p);
            UpdateNight(atHour);
#if UNITY_EDITOR
            if (markDirty && !Application.isPlaying)
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        }

        void UpdateNight(float atHour)
        {
            bool night = atHour >= lampsOnFromHour || atHour < 5f;
            if (!Application.isPlaying)
            {
                IsNight = night;
                return;
            }
            if (night == IsNight) return;
            IsNight = night;
            NightChanged?.Invoke(night);
        }

        void ApplySun(LightingPreset p, bool recordUndo)
        {
            if (sun == null) sun = FindSun();
            if (sun == null) return;
#if UNITY_EDITOR
            if (recordUndo && !Application.isPlaying)
                UnityEditor.Undo.RecordObjects(new UnityEngine.Object[] { sun, sun.transform }, "Apply Atmosphere");
#endif
            sun.type = LightType.Directional;
            // The light shines away from where the sun stands, hence +180 on the azimuth.
            sun.transform.rotation = Quaternion.Euler(p.sunElevation, p.sunAzimuth + 180f, 0f);
            sun.color = p.sunColor;
            sun.intensity = p.sunIntensity;
            sun.useColorTemperature = false;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = p.shadowStrength;
            RenderSettings.sun = sun;
        }

        void ApplyRenderSettings(LightingPreset p)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = p.ambientSky;
            RenderSettings.ambientEquatorColor = p.ambientEquator;
            RenderSettings.ambientGroundColor = p.ambientGround;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = p.reflectionIntensity;

            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = p.fogColor;
            RenderSettings.fogStartDistance = p.fogStart;
            RenderSettings.fogEndDistance = Mathf.Max(p.fogStart + 1f, p.fogEnd);
            if (Application.isPlaying) GameGraphicsSettings.SetSceneFog(fogEnabled); // respects the player's fog toggle
            else RenderSettings.fog = fogEnabled;
        }

        void ApplySky(LightingPreset p, bool recordUndo)
        {
            var mat = Application.isPlaying && runtimeSkybox != null ? runtimeSkybox : skybox;
            if (mat == null) return;
#if UNITY_EDITOR
            if (recordUndo && !Application.isPlaying) UnityEditor.Undo.RecordObject(mat, "Apply Atmosphere");
#endif
            if (mat.HasProperty(SkyTintId)) mat.SetColor(SkyTintId, p.skyTint);
            if (mat.HasProperty(GroundColorId)) mat.SetColor(GroundColorId, p.skyGround);
            if (mat.HasProperty(ExposureId)) mat.SetFloat(ExposureId, p.skyExposure);
            if (mat.HasProperty(AtmosphereThicknessId)) mat.SetFloat(AtmosphereThicknessId, p.atmosphereThickness);
            if (mat.HasProperty(SunSizeId)) mat.SetFloat(SunSizeId, p.sunSize);
            RenderSettings.skybox = mat;
#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(mat);
#endif
        }

        void ApplyVolume(LightingPreset p)
        {
            if (volume == null) return;
            // Edit mode writes the asset; Play mode uses volume.profile, which clones it, so the asset stays clean.
            var profile = Application.isPlaying ? volume.profile : volume.sharedProfile;
            if (profile == null) return;

            var tone = GetOrAdd<Tonemapping>(profile);
            Set(tone.mode, TonemappingMode.ACES);

            var color = GetOrAdd<ColorAdjustments>(profile);
            Set(color.postExposure, p.postExposure);
            Set(color.contrast, contrast);
            Set(color.saturation, saturation);
            Set(color.colorFilter, p.colorFilter);

            var bloom = GetOrAdd<Bloom>(profile);
            Set(bloom.threshold, bloomThreshold);
            Set(bloom.intensity, bloomIntensity);
            Set(bloom.scatter, bloomScatter);

            var vig = GetOrAdd<Vignette>(profile);
            Set(vig.intensity, p.vignette);
            Set(vig.smoothness, 0.4f);
            Set(vig.color, Color.black);

            var wb = GetOrAdd<WhiteBalance>(profile);
            Set(wb.temperature, p.whiteBalanceTemperature);

            var grain = GetOrAdd<FilmGrain>(profile);
            Set(grain.type, FilmGrainLookup.Thin1);
            Set(grain.intensity, filmGrain);
            Set(grain.response, 0.8f);

            var split = GetOrAdd<SplitToning>(profile);
            Set(split.shadows, splitShadows);
            Set(split.highlights, splitHighlights);
            Set(split.balance, splitBalance);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                foreach (var c in profile.components) UnityEditor.EditorUtility.SetDirty(c);
                UnityEditor.EditorUtility.SetDirty(profile);
            }
#endif
        }

        static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var existing)) return existing;
            var added = profile.Add<T>(false);
#if UNITY_EDITOR
            // Components of a profile asset must be sub-assets, or they are lost on reload.
            if (!Application.isPlaying && UnityEditor.EditorUtility.IsPersistent(profile))
                UnityEditor.AssetDatabase.AddObjectToAsset(added, profile);
#endif
            return added;
        }

        static void Set<T>(VolumeParameter<T> parameter, T value)
        {
            parameter.overrideState = true;
            parameter.value = value;
        }

        void UpdateEnvironment(bool force)
        {
            // Refreshes the ambient probe and the skybox reflection; throttled while blending.
            if (!force && Time.unscaledTime < nextEnvironmentUpdate) return;
            nextEnvironmentUpdate = Time.unscaledTime + 0.25f;
            DynamicGI.UpdateEnvironment();
        }

        static Light FindSun()
        {
            if (RenderSettings.sun != null) return RenderSettings.sun;
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light>())
                if (light.type == LightType.Directional) return light;
            return null;
        }

        /// <summary>15:30 afternoon, 19:00 sunset, 19:30 night (moonlight).</summary>
        public static List<LightingPreset> DefaultPresets() => new()
        {
            new LightingPreset
            {
                name = "Popołudnie 15:30", hour = 15.5f,
                sunElevation = 38f, sunAzimuth = 235f, sunColor = new Color(1f, 0.93f, 0.8f), sunIntensity = 1.4f, shadowStrength = 0.85f,
                ambientSky = new Color(0.48f, 0.55f, 0.66f), ambientEquator = new Color(0.5f, 0.46f, 0.4f), ambientGround = new Color(0.2f, 0.17f, 0.13f),
                reflectionIntensity = 1f,
                fogColor = new Color(0.66f, 0.68f, 0.7f), fogStart = 30f, fogEnd = 220f,
                skyTint = new Color(0.5f, 0.5f, 0.52f), skyGround = new Color(0.37f, 0.35f, 0.34f), skyExposure = 1.2f, atmosphereThickness = 1f, sunSize = 0.04f,
                postExposure = 0.2f, colorFilter = new Color(1f, 0.98f, 0.94f), vignette = 0.18f, whiteBalanceTemperature = 4f,
            },
            new LightingPreset
            {
                name = "Zachód 19:00", hour = 19f,
                sunElevation = 5f, sunAzimuth = 285f, sunColor = new Color(1f, 0.62f, 0.35f), sunIntensity = 1.3f, shadowStrength = 0.8f,
                ambientSky = new Color(0.34f, 0.36f, 0.46f), ambientEquator = new Color(0.55f, 0.36f, 0.24f), ambientGround = new Color(0.15f, 0.1f, 0.07f),
                reflectionIntensity = 0.8f,
                fogColor = new Color(0.62f, 0.45f, 0.34f), fogStart = 25f, fogEnd = 180f,
                skyTint = new Color(0.56f, 0.48f, 0.46f), skyGround = new Color(0.3f, 0.23f, 0.19f), skyExposure = 1.1f, atmosphereThickness = 1.5f, sunSize = 0.06f,
                postExposure = 0.25f, colorFilter = new Color(1f, 0.93f, 0.84f), vignette = 0.3f, whiteBalanceTemperature = 12f,
            },
            new LightingPreset
            {
                name = "Noc 19:30", hour = 19.5f,
                sunElevation = 40f, sunAzimuth = 200f, sunColor = new Color(0.32f, 0.4f, 0.65f), sunIntensity = 0.1f, shadowStrength = 0.5f,
                ambientSky = new Color(0.1f, 0.13f, 0.22f), ambientEquator = new Color(0.07f, 0.08f, 0.13f), ambientGround = new Color(0.03f, 0.03f, 0.04f),
                reflectionIntensity = 0.3f,
                fogColor = new Color(0.06f, 0.08f, 0.13f), fogStart = 15f, fogEnd = 120f,
                skyTint = new Color(0.25f, 0.3f, 0.45f), skyGround = new Color(0.05f, 0.05f, 0.07f), skyExposure = 0.12f, atmosphereThickness = 0.8f, sunSize = 0.025f,
                postExposure = 0.1f, colorFilter = new Color(0.85f, 0.9f, 1f), vignette = 0.38f, whiteBalanceTemperature = -15f,
            },
        };
    }
}
