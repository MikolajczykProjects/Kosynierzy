using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kosynierzy
{
    /// <summary>
    /// Full-screen feedback on top of the scene's Global Volume: one runtime Volume (own profile) per effect,
    /// blended by weight on unscaled time, so pulses never freeze in hit-stop or pause.
    ///   KillPulse   — chromatic aberration + lens punch + saturation / exposure pop (0.3 s)
    ///   DamagePulse — red vignette + desaturate (0.75 s)
    ///   ShotPulse   — tiny bloom / exposure kick (0.1 s)
    ///   NearMissPulse — aberration flick + dark vignette when a bullet passes the head
    ///   LowHealth(bool) — beating red vignette, desaturated
    ///   Slowmo(bool) — desaturated, darker edges; also switches on by itself while Time.timeScale stays low
    /// Distortion (aberration, lens) scales with GameSettings.CameraShake: 0 in the menu = none.
    /// Created on first call; nothing to place in the scene.
    /// </summary>
    public class ScreenFx : MonoBehaviour
    {
        class Fx
        {
            public Volume Volume;
            public VolumeProfile Profile;
            public float Start = -100f, Duration = 0.3f, Peak;
            public float Level;     // toggled effects: 0..1 blend toward the target
        }

        static ScreenFx instance;
        static bool quitting;

        Fx kill, damage, shot, near, low, slow;
        ChromaticAberration killCa, damageCa, nearCa, slowCa;
        LensDistortion killLens, nearLens;
        ColorAdjustments killColor, shotColor;
        Bloom shotBloom;

        bool lowHealth, slowForced;
        float slowAutoTime;
        Volume baseVolume;
        float nextBaseSearch;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
            quitting = false;
            Application.quitting -= OnQuit;
            Application.quitting += OnQuit;
        }

        static void OnQuit() => quitting = true;

        static ScreenFx Instance
        {
            get
            {
                if (instance == null && !quitting)
                    instance = new GameObject("[ScreenFx]").AddComponent<ScreenFx>();
                return instance;
            }
        }

        static float Distortion => Mathf.Clamp01(GameSettings.CameraShake);

        // ------------------------------------------------------------------ API

        /// <summary>Kill confirm: aberration + barrel punch + colour pop.</summary>
        public static void KillPulse(float strength = 1f)
        {
            var s = Instance;
            if (s == null) return;
            float d = Distortion;
            Set(s.killCa.intensity, 0.9f * d);
            Set(s.killLens.intensity, -0.28f * d);
            s.FindBase(out float baseExposure, out _);
            Set(s.killColor.postExposure, baseExposure + 0.35f);
            Fire(s.kill, 0.3f, strength);
        }

        /// <summary>The player is hit: red vignette and drained colour.</summary>
        public static void DamagePulse(float strength = 1f)
        {
            var s = Instance;
            if (s == null) return;
            Set(s.damageCa.intensity, 0.55f * Distortion);
            Fire(s.damage, 0.75f, strength);
        }

        /// <summary>The player's own shot: a tiny bloom / exposure kick.</summary>
        public static void ShotPulse(float strength = 1f)
        {
            var s = Instance;
            if (s == null) return;
            s.FindBase(out float baseExposure, out float baseBloom);
            Set(s.shotColor.postExposure, baseExposure + 0.22f);
            Set(s.shotBloom.intensity, baseBloom + 1.1f);
            Fire(s.shot, 0.1f, strength);
        }

        /// <summary>A bullet just missed the head (0..1 = how close).</summary>
        public static void NearMissPulse(float strength = 1f)
        {
            var s = Instance;
            if (s == null) return;
            float d = Distortion;
            Set(s.nearCa.intensity, 0.75f * d);
            Set(s.nearLens.intensity, -0.14f * d);
            Fire(s.near, 0.28f, Mathf.Clamp01(strength));
        }

        public static void LowHealth(bool on)
        {
            if (instance == null && !on) return; // no need to create anything just to switch it off
            var s = Instance;
            if (s != null) s.lowHealth = on;
        }

        public static void Slowmo(bool on)
        {
            if (instance == null && !on) return;
            var s = Instance;
            if (s != null) s.slowForced = on;
        }

        static void Fire(Fx fx, float duration, float strength)
        {
            float now = Time.unscaledTime;
            // A new pulse never lowers one that is still bright.
            float current = Envelope(fx, now);
            fx.Start = now;
            fx.Duration = duration;
            fx.Peak = Mathf.Max(Mathf.Clamp01(strength), current);
        }

        // ------------------------------------------------------------------ set-up

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            int layer = FindBaseLayer();

            kill = Create("Kill", 220f, layer, p =>
            {
                killCa = p.Add<ChromaticAberration>(false);
                Set(killCa.intensity, 0.9f);
                killLens = p.Add<LensDistortion>(false);
                Set(killLens.intensity, -0.28f);
                killColor = p.Add<ColorAdjustments>(false);
                Set(killColor.saturation, 35f);
                Set(killColor.contrast, 22f);
                Set(killColor.postExposure, 0.5f);
            });

            damage = Create("Damage", 230f, layer, p =>
            {
                var vig = p.Add<Vignette>(false);
                Set(vig.color, new Color(0.6f, 0.02f, 0.02f));
                Set(vig.intensity, 0.52f);
                Set(vig.smoothness, 0.6f);
                var col = p.Add<ColorAdjustments>(false);
                Set(col.saturation, -75f);
                Set(col.contrast, 15f);
                damageCa = p.Add<ChromaticAberration>(false);
                Set(damageCa.intensity, 0.55f);
            });

            shot = Create("Shot", 200f, layer, p =>
            {
                shotColor = p.Add<ColorAdjustments>(false);
                Set(shotColor.postExposure, 0.4f);
                shotBloom = p.Add<Bloom>(false);
                Set(shotBloom.intensity, 1.5f);
            });

            near = Create("NearMiss", 215f, layer, p =>
            {
                nearCa = p.Add<ChromaticAberration>(false);
                Set(nearCa.intensity, 0.75f);
                nearLens = p.Add<LensDistortion>(false);
                Set(nearLens.intensity, -0.14f);
                var vig = p.Add<Vignette>(false);
                Set(vig.color, Color.black);
                Set(vig.intensity, 0.42f);
                Set(vig.smoothness, 0.5f);
            });

            low = Create("LowHealth", 225f, layer, p =>
            {
                var vig = p.Add<Vignette>(false);
                Set(vig.color, new Color(0.38f, 0f, 0f));
                Set(vig.intensity, 0.45f);
                Set(vig.smoothness, 0.8f);
                var col = p.Add<ColorAdjustments>(false);
                Set(col.saturation, -45f);
            });

            slow = Create("Slowmo", 210f, layer, p =>
            {
                var col = p.Add<ColorAdjustments>(false);
                Set(col.saturation, -50f);
                Set(col.contrast, 18f);
                Set(col.colorFilter, new Color(0.9f, 0.95f, 1f));
                var vig = p.Add<Vignette>(false);
                Set(vig.color, Color.black);
                Set(vig.intensity, 0.45f);
                Set(vig.smoothness, 0.5f);
                slowCa = p.Add<ChromaticAberration>(false);
                Set(slowCa.intensity, 0.25f);
            });
        }

        Fx Create(string name, float priority, int layer, System.Action<VolumeProfile> build)
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "ScreenFx_" + name;
            build(profile);

            var go = new GameObject(name);
            go.SetActive(false);
            go.layer = layer;
            go.transform.SetParent(transform, false);
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = priority;
            vol.weight = 0f;
            vol.sharedProfile = profile;
            go.SetActive(true);
            return new Fx { Volume = vol, Profile = profile };
        }

        // Our volumes must sit on a layer the camera's Volume Mask sees: copy the scene's global volume.
        int FindBaseLayer()
        {
            FindBaseVolume();
            return baseVolume != null ? baseVolume.gameObject.layer : 0;
        }

        void FindBaseVolume()
        {
            baseVolume = null;
            float best = float.MinValue;
            foreach (var v in Object.FindObjectsByType<Volume>())
            {
                if (v == null || !v.isGlobal || v.transform.IsChildOf(transform)) continue;
                if (v.priority > best)
                {
                    best = v.priority;
                    baseVolume = v;
                }
            }
        }

        /// <summary>The scene's current exposure and bloom, so kicks are relative to the time-of-day grade.</summary>
        void FindBase(out float exposure, out float bloom)
        {
            exposure = 0f;
            bloom = 0f;
            if (baseVolume == null && Time.unscaledTime >= nextBaseSearch)
            {
                nextBaseSearch = Time.unscaledTime + 3f;
                FindBaseVolume();
            }
            if (baseVolume == null) return;
            var profile = baseVolume.HasInstantiatedProfile() ? baseVolume.profile : baseVolume.sharedProfile;
            if (profile == null) return;
            if (profile.TryGet<ColorAdjustments>(out var ca) && ca.postExposure.overrideState) exposure = ca.postExposure.value;
            if (profile.TryGet<Bloom>(out var b) && b.intensity.overrideState) bloom = b.intensity.value;
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
            foreach (var fx in new[] { kill, damage, shot, near, low, slow })
                if (fx != null && fx.Profile != null) Destroy(fx.Profile);
        }

        // ------------------------------------------------------------------ per frame

        void Update()
        {
            if (kill == null) return;
            float now = Time.unscaledTime;
            float dt = Time.unscaledDeltaTime;

            kill.Volume.weight = Envelope(kill, now);
            damage.Volume.weight = Envelope(damage, now);
            shot.Volume.weight = Envelope(shot, now) * 0.85f;
            near.Volume.weight = Envelope(near, now);

            // Low health: a lub-dub heartbeat on top of a steady tint.
            low.Level = Mathf.MoveTowards(low.Level, lowHealth ? 1f : 0f, dt * (lowHealth ? 2.5f : 1.2f));
            if (low.Level > 0f)
            {
                float ph = (now * 1.15f) % 1f;
                float beat = Mathf.Exp(-ph * 10f) + (ph > 0.2f ? 0.6f * Mathf.Exp(-(ph - 0.2f) * 10f) : 0f);
                low.Volume.weight = low.Level * (0.6f + 0.4f * Mathf.Clamp01(beat));
            }
            else low.Volume.weight = 0f;

            // Slow motion: forced, or Time.timeScale held low (not a hit-stop blip, not the pause menu).
            bool slowTime = !GameState.IsPaused && Time.timeScale > 0.001f && Time.timeScale < 0.65f;
            slowAutoTime = slowTime ? slowAutoTime + dt : 0f;
            bool slowOn = slowForced || slowAutoTime > 0.12f;
            slow.Level = Mathf.MoveTowards(slow.Level, slowOn ? 1f : 0f, dt * (slowOn ? 5f : 2.2f));
            if (slow.Level > 0f) Set(slowCa.intensity, 0.25f * Distortion);
            slow.Volume.weight = slow.Level * slow.Level * (3f - 2f * slow.Level);
        }

        // Instant attack (35 ms), then ease-out decay.
        static float Envelope(Fx fx, float now)
        {
            float age = now - fx.Start;
            if (age < 0f || age >= fx.Duration) return 0f;
            const float attack = 0.035f;
            if (age < attack) return fx.Peak * Mathf.Lerp(0.5f, 1f, age / attack);
            float k = 1f - (age - attack) / Mathf.Max(0.001f, fx.Duration - attack);
            return fx.Peak * k * k;
        }

        static void Set<T>(VolumeParameter<T> parameter, T value)
        {
            parameter.overrideState = true;
            parameter.value = value;
        }
    }
}
