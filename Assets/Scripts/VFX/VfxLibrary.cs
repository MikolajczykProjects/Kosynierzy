using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kosynierzy
{
    /// <summary>Melee = bayonet / knife on a surface: small sparks and dust, no bullet hole, no light.</summary>
    public enum ImpactKind { Ground, Flesh, Melee }

    /// <summary>
    /// Code-built gunplay effects — no prefabs, nothing to wire. One world-space particle system per LAYER
    /// (flash, glow, flame, sparks, smoke, dust, debris…) fed by Emit() with per-particle position / velocity /
    /// size / colour, so any number of shots can overlap and nothing allocates per shot.
    /// Every effect is layered (core flash → body → micro-detail → lingering smoke), moves fast-out with drag,
    /// fades through size / alpha curves and hue-shifts white → yellow → orange → ember; the world reacts
    /// (pooled flash lights, casings that bounce and lie down, bullet holes that fade after 25 s).
    /// Dust / smoke / debris colours follow the scene light (day, sunset, night) and the hit surface's colour.
    /// Team rules: flesh hits are a cloth / dust puff; blood only with GameSettings.MatureContent ("Krew 16+").
    /// Put one in the scene (menu Kosynierzy/Add VFX Library) so the particle shader is kept in builds;
    /// without it one is created on first use (fine in the editor).
    /// </summary>
    public class VfxLibrary : MonoBehaviour
    {
        [Tooltip("Universal Render Pipeline/Particles/Unlit — referenced here so builds include it.")]
        [SerializeField] Shader particleShader;
        [Tooltip("Universal Render Pipeline/Lit — brass casings. Optional; found by name when empty.")]
        [SerializeField] Shader litShader;
        [SerializeField] int maxDecals = 60;
        [SerializeField] int maxCasings = 24;
        [SerializeField] int maxLights = 8;

        static VfxLibrary instance;
        static Material tracerMaterial;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        const int WorldMask = ~(1 << 2); // everything except "Ignore Raycast"

        enum Blend { Additive, Alpha, Opaque }

        bool initialized;
        Transform fxRoot;
        readonly List<Object> owned = new();
        Texture2D tracerTex;

        ParticleSystem flash, glow, flame, sparks, sparkRing, embers, smoke, dust, bits, chips, ring, groundRing, streak;
        ShellCasings casings;
        BulletDecals decals;

        Light[] lights;
        float[] lightStart, lightDur, lightPeak;
        int nextLight;

        Color shade = Color.white;
        float nextShade;
        float lastWhizz = -10f;
        ParticleSystem.EmitParams ep, ep3;
        MaterialPropertyBlock probeBlock;

        static readonly Color DustBase = new(0.58f, 0.53f, 0.45f);
        static readonly Color Gold = new(1f, 0.82f, 0.4f);

        // ================================================================== public API

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
            tracerMaterial = null;
        }

        static VfxLibrary Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Object.FindAnyObjectByType<VfxLibrary>();
                    if (instance == null) instance = new GameObject("[VfxLibrary]").AddComponent<VfxLibrary>();
                    instance.Init();
                }
                return instance;
            }
        }

        /// <summary>Additive glow for tracer LineRenderers: white-hot core, warm halo, bright head (U = 0).</summary>
        public static Material TracerMaterial
        {
            get
            {
                if (tracerMaterial == null)
                {
                    var lib = Instance;
                    tracerMaterial = lib.MakeMaterial(Blend.Additive, lib.tracerTex, 2.6f);
                    tracerMaterial.name = "vfx_tracer";
                    lib.owned.Add(tracerMaterial);
                }
                return tracerMaterial;
            }
        }

        /// <summary>Sets material, width taper and texture mode of a tracer (head = position 0).</summary>
        public static void ConfigureTracer(LineRenderer lr)
        {
            lr.sharedMaterial = TracerMaterial;
            lr.textureMode = LineTextureMode.Stretch;
            lr.alignment = LineAlignment.View;
            lr.numCapVertices = 0;
            lr.widthCurve = new AnimationCurve(new Keyframe(0f, 0.085f), new Keyframe(0.1f, 0.055f), new Keyframe(1f, 0.016f));
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.generateLightingData = false;
        }

        public static void PlayMuzzleFlash(Vector3 position, Quaternion rotation) => Instance.Muzzle(position, rotation, 1f);

        /// <summary>scale 1 = pistol, 1.6 = rifle.</summary>
        public static void PlayMuzzleFlash(Vector3 position, Quaternion rotation, float scale) =>
            Instance.Muzzle(position, rotation, Mathf.Max(0.2f, scale));

        /// <summary>
        /// Brass casing thrown right/up from the ejection port behind <paramref name="muzzlePosition"/>
        /// (pass the same point and rotation as the muzzle flash). scale ≥ 1.3 = rifle casing.
        /// </summary>
        public static void PlayShellEject(Vector3 muzzlePosition, Quaternion rotation, Vector3 inheritVelocity, float scale = 1f) =>
            Instance.casings.Eject(muzzlePosition, rotation, inheritVelocity, scale);

        public static void PlayImpact(Vector3 point, Vector3 normal, ImpactKind kind) => Instance.Impact(point, normal, kind);

        /// <summary>Feet scuff the moment a character dies (the body hitting the ground is PlayBodyFall).</summary>
        public static void PlayDeath(Vector3 groundPosition) => Instance.Death(groundPosition);

        /// <summary>A body lands: dust rolling out along it, a ground dust ring, pebbles, a slow haze.</summary>
        public static void PlayBodyFall(Vector3 feet, Vector3 fallDirection, float length = 1.7f) =>
            Instance.BodyFall(feet, fallDirection, length);

        /// <summary>Kill confirm at the victim: gold flash, double shockwave ring, dust burst, rising embers, light.</summary>
        public static void PlayKillConfirm(Vector3 position) => Instance.KillConfirm(position);

        /// <summary>Headshot: white-hot double star, fast thin ring, spark ring facing the camera.</summary>
        public static void PlayHeadshot(Vector3 position) => Instance.Headshot(position);

        /// <summary>One call for a kill: confirm burst, headshot layer when needed, screen pulse.</summary>
        public static void PlayKill(Vector3 position, bool headshot)
        {
            var lib = Instance;
            lib.KillConfirm(position);
            if (headshot) lib.Headshot(position);
            ScreenFx.KillPulse(headshot ? 1f : 0.8f);
        }

        /// <summary>Bright glowing head on a flying bullet; call once per frame per bullet (optional).</summary>
        public static void TracerHead(Vector3 position, Color color)
        {
            var lib = Instance;
            lib.Emit(lib.glow, position, Vector3.zero, 0.11f, 0.035f, new Color(color.r, color.g, color.b, 0.9f));
        }

        /// <summary>
        /// Call with each frame's bullet segment for enemy bullets: when it passes within ~1.6 m of the camera
        /// it draws a streak past the head, plays the whizz, flicks the screen and adds a little trauma.
        /// </summary>
        public static void BulletNearMiss(Vector3 from, Vector3 to)
        {
            if (instance == null) return; // nothing to show before the first shot created the library
            instance.NearMiss(from, to);
        }

        // ================================================================== effects

        void Muzzle(Vector3 pos, Quaternion rot, float s)
        {
            Vector3 f = rot * Vector3.forward, rt = rot * Vector3.right, up = rot * Vector3.up;
            float roll = Random.Range(0f, 360f);

            // Core: two stars rolled against each other + a warm bloom halo (2–3 frames).
            Emit(flash, pos + f * (0.03f * s), Vector3.zero, Random.Range(0.2f, 0.28f) * s, 0.05f, new Color(1f, 0.93f, 0.75f), roll);
            Emit(flash, pos + f * (0.07f * s), Vector3.zero, Random.Range(0.11f, 0.16f) * s, 0.035f, Color.white, roll + 45f);
            Emit(glow, pos + f * (0.06f * s), f * 0.4f, 0.5f * s, 0.08f, new Color(1f, 0.55f, 0.2f, 0.45f));

            // Forward flame cone (stretched, hue shift in the layer's gradient).
            int n = Random.Range(4, 7);
            for (int i = 0; i < n; i++)
                Emit(flame, pos + f * (0.02f * s), Cone(f, 9f) * (Random.Range(7f, 15f) * s),
                     Random.Range(0.05f, 0.09f) * s, Random.Range(0.04f, 0.07f), Color.white);

            // Side petals.
            int petals = Random.Range(3, 5);
            float baseA = Random.Range(0f, 360f);
            for (int i = 0; i < petals; i++)
            {
                float a = (baseA + i * 360f / petals + Random.Range(-18f, 18f)) * Mathf.Deg2Rad;
                Vector3 d = (rt * Mathf.Cos(a) + up * Mathf.Sin(a) + f * Random.Range(0.25f, 0.6f)).normalized;
                Emit(flame, pos + f * (0.03f * s), d * (Random.Range(3.5f, 6.5f) * s),
                     Random.Range(0.04f, 0.065f) * s, Random.Range(0.035f, 0.055f), new Color(1f, 0.85f, 0.6f));
            }

            // Sparks.
            int sp = Random.Range(6, 11);
            for (int i = 0; i < sp; i++)
                Emit(sparks, pos + f * (0.04f * s), Cone(f, 16f) * Random.Range(9f, 18f),
                     Random.Range(0.012f, 0.022f), Random.Range(0.08f, 0.24f), Color.white);

            // Lingering smoke: drifts forward, slows, expands, rises.
            for (int i = 0; i < 3; i++)
                Emit(smoke, pos + f * (Random.Range(0.04f, 0.22f) * s),
                     f * (Random.Range(0.5f, 1.8f) * s) + up * 0.15f + Random.insideUnitSphere * 0.2f,
                     Random.Range(0.35f, 0.55f) * s, Random.Range(0.7f, 1.2f),
                     Lit(new Color(0.75f, 0.72f, 0.68f), Random.Range(0.18f, 0.28f)), Random.Range(-30f, 30f));
            // Thin wisp from the breech.
            Emit(smoke, pos - f * (0.1f * s) + up * 0.03f, up * 0.3f + rt * 0.12f, 0.22f * s, 1.1f,
                 Lit(new Color(0.7f, 0.7f, 0.7f), 0.13f), Random.Range(-30f, 30f));

            FlashLight(pos + f * 0.15f, new Color(1f, 0.68f, 0.35f), 4.5f * s, 7f * s, 0.06f);
        }

        void Impact(Vector3 p, Vector3 n, ImpactKind kind)
        {
            if (n.sqrMagnitude < 1e-4f) n = Vector3.up;
            n.Normalize();
            if (kind == ImpactKind.Flesh)
            {
                if (GameSettings.MatureContent) Blood(p, n);
                else Cloth(p, n);
                return;
            }
            Surface(p, n, kind == ImpactKind.Melee);
        }

        void Surface(Vector3 p, Vector3 n, bool melee)
        {
            bool found = Probe(p, n, out Color surf, out bool movable);
            Color dustCol = found ? Color.Lerp(DustBase, surf, 0.5f) : DustBase;
            Color chipCol = found ? surf * 0.85f : new Color(0.36f, 0.32f, 0.27f);

            // 1. Flash + glow.
            Emit(flash, p + n * 0.03f, Vector3.zero, Random.Range(0.12f, 0.18f) * (melee ? 0.6f : 1f), 0.045f,
                 new Color(1f, 0.85f, 0.6f), Random.Range(0f, 360f));
            if (!melee) Emit(glow, p + n * 0.05f, Vector3.zero, 0.32f, 0.07f, new Color(1f, 0.6f, 0.3f, 0.5f));

            // 2. Sparks that bounce.
            int ns = melee ? 5 : Random.Range(8, 13);
            for (int i = 0; i < ns; i++)
                Emit(sparks, p + n * 0.02f, Cone(n, 70f) * Random.Range(3f, 9f),
                     Random.Range(0.009f, 0.018f), Random.Range(0.15f, 0.45f), Color.white);

            // 3. Dust plume: a column off the surface + a skirt that hugs it.
            int nd = melee ? 2 : 5;
            for (int i = 0; i < nd; i++)
                Emit(dust, p + n * 0.04f, Cone(n, 28f) * Random.Range(1.5f, 4.5f),
                     Random.Range(0.3f, 0.55f), Random.Range(0.7f, 1.3f),
                     Lit(dustCol * Random.Range(0.85f, 1.15f), Random.Range(0.5f, 0.75f)), Random.Range(-25f, 25f));
            for (int i = 0; i < 3; i++)
            {
                Vector3 t = Vector3.ProjectOnPlane(Random.onUnitSphere, n).normalized;
                Emit(dust, p + n * 0.03f, (t + n * 0.3f) * Random.Range(1f, 2.2f),
                     Random.Range(0.25f, 0.4f), Random.Range(0.5f, 0.9f), Lit(dustCol, 0.45f), Random.Range(-25f, 25f));
            }

            // 4. Debris chunks in the surface colour, bouncing.
            int nc = melee ? 2 : Random.Range(4, 8);
            for (int i = 0; i < nc; i++)
                Emit3(chips, p + n * 0.03f, Cone(n, 55f) * Random.Range(2f, 5.5f),
                      Random.Range(0.015f, 0.04f), Random.Range(0.9f, 1.5f),
                      Lit(chipCol * Random.Range(0.75f, 1.1f), 1f), RandomEuler());

            // 5. Thin smoke wisp that outlives the dust.
            Emit(smoke, p + n * 0.08f, n * 0.25f + Vector3.up * 0.3f, Random.Range(0.3f, 0.45f), Random.Range(1.4f, 2.1f),
                 Lit(new Color(0.62f, 0.6f, 0.57f), 0.16f), Random.Range(-30f, 30f));

            if (melee) return;

            // 6. Tiny light + bullet hole.
            FlashLight(p + n * 0.15f, new Color(1f, 0.65f, 0.35f), 2.2f, 2.8f, 0.05f);
            if (found && !movable) decals.Place(p, n, Random.Range(0.07f, 0.09f), shade);
        }

        // Team rule: no gore by default — a hit on a person is cloth fibres and dust.
        void Cloth(Vector3 p, Vector3 n)
        {
            var cloth = new Color(0.52f, 0.55f, 0.47f);
            for (int i = 0; i < 6; i++)
                Emit(dust, p + n * 0.03f, Cone(n, 40f) * Random.Range(1f, 3.5f), Random.Range(0.28f, 0.42f),
                     Random.Range(0.45f, 0.85f), Lit(cloth * Random.Range(0.9f, 1.1f), Random.Range(0.5f, 0.7f)), Random.Range(-30f, 30f));
            for (int i = 0; i < 9; i++)
                Emit(bits, p + n * 0.02f, Cone(n, 50f) * Random.Range(2f, 5f), Random.Range(0.01f, 0.018f),
                     Random.Range(0.35f, 0.7f), Lit(new Color(0.34f, 0.38f, 0.3f), 1f));
            for (int i = 0; i < 3; i++)
                Emit3(chips, p + n * 0.02f, Cone(n, 45f) * Random.Range(1.5f, 3.5f), Random.Range(0.01f, 0.018f),
                      Random.Range(0.6f, 1f), Lit(new Color(0.3f, 0.33f, 0.26f), 1f), RandomEuler());
        }

        // Only with GameSettings.MatureContent ("Krew 16+").
        void Blood(Vector3 p, Vector3 n)
        {
            for (int i = 0; i < 7; i++)
                Emit(dust, p + n * 0.03f, Cone(n, 35f) * Random.Range(1f, 3.5f), Random.Range(0.25f, 0.45f),
                     Random.Range(0.4f, 0.8f), Lit(new Color(0.45f, 0.03f, 0.03f), Random.Range(0.7f, 0.9f)), Random.Range(-30f, 30f));
            for (int i = 0; i < 16; i++)
                Emit(bits, p + n * 0.02f, Cone(n, 40f) * Random.Range(2.5f, 6f), Random.Range(0.012f, 0.024f),
                     Random.Range(0.4f, 0.8f), Lit(new Color(0.36f, 0.02f, 0.02f), 1f));
            for (int i = 0; i < 3; i++)
                Emit3(chips, p + n * 0.02f, Cone(n, 40f) * Random.Range(2f, 4f), Random.Range(0.012f, 0.022f),
                      Random.Range(0.6f, 1f), Lit(new Color(0.3f, 0.02f, 0.02f), 1f), RandomEuler());
        }

        void Death(Vector3 ground)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 d = Flat(Random.Range(0f, Mathf.PI * 2f));
                Emit(dust, ground + Vector3.up * 0.1f + d * 0.15f, d * Random.Range(1.4f, 3f) + Vector3.up * Random.Range(0.1f, 0.5f),
                     Random.Range(0.35f, 0.55f), Random.Range(0.6f, 1f), Lit(DustBase, Random.Range(0.4f, 0.6f)), Random.Range(-30f, 30f));
            }
        }

        void BodyFall(Vector3 feet, Vector3 dir, float length)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up);
            dir = dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.forward;
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            Color dc = Probe(feet + Vector3.up * 0.05f, Vector3.up, out Color surf, out _) ? Color.Lerp(DustBase, surf, 0.45f) : DustBase;

            // Dust rolling out from under the whole body.
            for (int i = 0; i < 14; i++)
            {
                Vector3 b = feet + dir * (Random.Range(0.15f, 1f) * length) + Vector3.up * 0.06f;
                Vector3 o = side * (Random.value < 0.5f ? -1f : 1f) + dir * Random.Range(-0.3f, 0.5f);
                o.y = 0f;
                o.Normalize();
                Emit(dust, b, o * Random.Range(1.2f, 3.2f) + Vector3.up * Random.Range(0.15f, 0.7f),
                     Random.Range(0.4f, 0.75f), Random.Range(0.8f, 1.5f), Lit(dc * Random.Range(0.9f, 1.1f), Random.Range(0.45f, 0.65f)),
                     Random.Range(-30f, 30f));
            }

            // Flat dust ring on the ground.
            Vector3 centre = feet + dir * (length * 0.55f) + Vector3.up * 0.03f;
            Emit3(groundRing, centre, Vector3.zero, length * 1.9f, 0.55f, Lit(dc * 1.1f, 0.6f), new Vector3(0f, Random.Range(0f, 360f), 0f));

            // Pebbles hopping.
            for (int i = 0; i < 8; i++)
            {
                Vector3 o = Flat(Random.Range(0f, Mathf.PI * 2f));
                Emit3(chips, feet + dir * (Random.Range(0.2f, 1f) * length) + Vector3.up * 0.05f,
                      Vector3.up * Random.Range(1.2f, 2.6f) + o * Random.Range(0.4f, 1.4f),
                      Random.Range(0.012f, 0.03f), Random.Range(1f, 1.6f), Lit(dc * 0.7f, 1f), RandomEuler());
            }

            // Slow haze.
            for (int i = 0; i < 2; i++)
                Emit(smoke, centre + Vector3.up * 0.2f, Vector3.up * Random.Range(0.2f, 0.4f) + side * Random.Range(-0.2f, 0.2f),
                     Random.Range(0.6f, 0.9f), Random.Range(1.5f, 2.2f), Lit(dc, 0.15f), Random.Range(-30f, 30f));
        }

        void KillConfirm(Vector3 pos)
        {
            CamBasis(pos, out Vector3 toCam, out _, out _);
            Vector3 front = pos + toCam * 0.25f;

            Emit(flash, front, Vector3.zero, 0.6f, 0.07f, Gold, Random.Range(0f, 360f));
            Emit(glow, front, Vector3.zero, 1.1f, 0.14f, new Color(1f, 0.6f, 0.25f, 0.5f));
            // Double shockwave: a fast bright ring and a slower, wider, warmer one.
            Emit(ring, front, Vector3.zero, 1.6f, 0.24f, new Color(1f, 0.85f, 0.55f, 0.9f), Random.Range(0f, 360f));
            Emit(ring, front, Vector3.zero, 2.4f, 0.4f, new Color(1f, 0.55f, 0.2f, 0.45f), Random.Range(0f, 360f));

            for (int i = 0; i < 9; i++)
            {
                Vector3 d = Random.onUnitSphere;
                d.y = d.y * 0.35f + 0.15f;
                d.Normalize();
                Emit(dust, pos + d * 0.1f, d * Random.Range(2.5f, 4.5f), Random.Range(0.4f, 0.65f), Random.Range(0.5f, 0.85f),
                     Lit(new Color(0.72f, 0.67f, 0.58f), Random.Range(0.35f, 0.5f)), Random.Range(-30f, 30f));
            }
            for (int i = 0; i < 16; i++)
            {
                Vector3 d = (Random.onUnitSphere + Vector3.up * 0.6f).normalized;
                Emit(embers, pos + d * 0.1f, d * Random.Range(1.5f, 4.5f), Random.Range(0.018f, 0.04f), Random.Range(0.6f, 1.3f),
                     new Color(1f, Random.Range(0.6f, 0.85f), 0.35f));
            }
            FlashLight(front, new Color(1f, 0.75f, 0.4f), 4f, 4.5f, 0.14f);
        }

        void Headshot(Vector3 pos)
        {
            CamBasis(pos, out Vector3 toCam, out Vector3 right, out Vector3 up);
            Vector3 front = pos + toCam * 0.3f;
            float roll = Random.Range(0f, 360f);

            Emit(flash, front, Vector3.zero, 0.85f, 0.06f, Color.white, roll);
            Emit(flash, front, Vector3.zero, 0.5f, 0.09f, new Color(1f, 0.9f, 0.6f), roll + 45f);
            Emit(ring, front, Vector3.zero, 1.2f, 0.16f, Color.white, roll);

            const int count = 18;
            for (int i = 0; i < count; i++)
            {
                float a = (i + Random.Range(-0.3f, 0.3f)) / count * Mathf.PI * 2f;
                Vector3 d = right * Mathf.Cos(a) + up * Mathf.Sin(a);
                Emit(sparkRing, front + d * 0.05f, (d + toCam * Random.Range(-0.1f, 0.2f)) * Random.Range(7f, 11f),
                     Random.Range(0.018f, 0.03f), Random.Range(0.18f, 0.3f), Color.white);
            }
            FlashLight(front, new Color(1f, 0.95f, 0.85f), 5f, 4f, 0.08f);
        }

        void NearMiss(Vector3 a, Vector3 b)
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 c = cam.transform.position;
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 1e-6f) return;
            float t = Vector3.Dot(c - a, ab) / len2;
            if (t <= 0f || t >= 1f) return; // the pass is in another frame's segment
            Vector3 q = a + ab * t;
            float dist = Vector3.Distance(q, c);
            const float radius = 1.6f;
            if (dist > radius || dist < 0.12f) return; // < 0.12 m is a hit, not a near miss
            if (Time.unscaledTime - lastWhizz < 0.12f) return;
            lastWhizz = Time.unscaledTime;

            float close = 1f - dist / radius;
            Vector3 dir = ab / Mathf.Sqrt(len2);
            Emit(streak, q - dir * 2f, dir * 90f, 0.03f, 0.05f, new Color(1f, 0.85f, 0.6f, 0.9f));
            SfxBank.PlayAt(SfxBank.Sfx.BulletWhizz, q);
            ScreenFx.NearMissPulse(0.4f + 0.6f * close);
            CameraShake.AddTrauma(0.1f * close);
        }

        // ================================================================== helpers

        void Emit(ParticleSystem ps, Vector3 pos, Vector3 vel, float size, float life, Color col, float rot = 0f)
        {
            ep.position = pos;
            ep.velocity = vel;
            ep.startSize = size;
            ep.startLifetime = life;
            ep.startColor = col;
            ep.rotation = rot;
            ps.Emit(ep, 1);
        }

        void Emit3(ParticleSystem ps, Vector3 pos, Vector3 vel, float size, float life, Color col, Vector3 euler)
        {
            ep3.position = pos;
            ep3.velocity = vel;
            ep3.startSize = size;
            ep3.startLifetime = life;
            ep3.startColor = col;
            ep3.rotation3D = euler;
            ps.Emit(ep3, 1);
        }

        /// <summary>Unlit dust / smoke / debris take the scene light so they are not glowing at night.</summary>
        Color Lit(Color c, float alpha) => new(c.r * shade.r, c.g * shade.g, c.b * shade.b, alpha);

        static Vector3 Cone(Vector3 axis, float angle)
        {
            Vector3 perp = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.95f ? Vector3.up : Vector3.right).normalized;
            Vector3 tilted = Quaternion.AngleAxis(Random.Range(0f, angle), perp) * axis;
            return Quaternion.AngleAxis(Random.Range(0f, 360f), axis) * tilted;
        }

        static Vector3 Flat(float a) => new(Mathf.Cos(a), 0f, Mathf.Sin(a));

        static Vector3 RandomEuler() => new(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));

        static void CamBasis(Vector3 pos, out Vector3 toCam, out Vector3 right, out Vector3 up)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                toCam = Vector3.up;
                right = Vector3.right;
                up = Vector3.forward;
                return;
            }
            var ct = cam.transform;
            toCam = ct.position - pos;
            float d = toCam.magnitude;
            toCam = d > 1e-3f ? toCam / d : -ct.forward;
            right = ct.right;
            up = ct.up;
        }

        /// <summary>What was hit: its colour (material / property block) and whether it moves.</summary>
        bool Probe(Vector3 p, Vector3 n, out Color col, out bool movable)
        {
            col = DustBase;
            movable = false;
            if (!Physics.Raycast(p + n * 0.05f, -n, out RaycastHit hit, 0.15f, WorldMask, QueryTriggerInteraction.Ignore)) return false;
            if (hit.collider is CharacterController) return false;
            var rb = hit.rigidbody;
            movable = rb != null && !rb.isKinematic;

            if (hit.collider.TryGetComponent(out Renderer rend))
            {
                Color c = default;
                bool got = false;
                if (rend.HasPropertyBlock())
                {
                    probeBlock ??= new MaterialPropertyBlock();
                    rend.GetPropertyBlock(probeBlock);
                    if (probeBlock.HasColor(BaseColorId)) { c = probeBlock.GetColor(BaseColorId); got = true; }
                }
                if (!got)
                {
                    var m = rend.sharedMaterial;
                    if (m != null)
                    {
                        if (m.HasProperty(BaseColorId)) { c = m.GetColor(BaseColorId); got = true; }
                        else if (m.HasProperty(ColorId)) { c = m.GetColor(ColorId); got = true; }
                    }
                }
                // White usually means a textured material: keep the neutral dust.
                if (got && c.r + c.g + c.b < 2.7f) col = new Color(c.r, c.g, c.b, 1f);
            }
            return true;
        }

        void FlashLight(Vector3 pos, Color col, float peak, float range, float duration)
        {
            int i = nextLight;
            nextLight = (nextLight + 1) % lights.Length;
            var l = lights[i];
            l.transform.position = pos;
            l.color = col;
            l.range = range;
            l.intensity = peak;
            l.enabled = true;
            lightStart[i] = Time.time;
            lightDur[i] = duration;
            lightPeak[i] = peak;
        }

        void UpdateShade()
        {
            Color amb = RenderSettings.ambientMode == AmbientMode.Flat
                ? RenderSettings.ambientLight
                : (RenderSettings.ambientSkyColor + RenderSettings.ambientEquatorColor) * 0.5f;
            Color lit = amb;
            var sun = RenderSettings.sun;
            if (sun != null && sun.isActiveAndEnabled)
            {
                float elev = Mathf.Max(0.25f, -sun.transform.forward.y);
                lit += sun.color * (sun.intensity * 0.55f * elev);
            }
            shade = new Color(
                Mathf.Clamp01(lit.r * 0.85f + 0.18f),
                Mathf.Clamp01(lit.g * 0.85f + 0.18f),
                Mathf.Clamp01(lit.b * 0.85f + 0.2f), 1f);
        }

        // ================================================================== lifecycle

        void Awake()
        {
            if (instance == null) instance = this;
            if (instance == this) Init();
        }

        void Update()
        {
            if (!initialized) return;
            float now = Time.time;
            for (int i = 0; i < lights.Length; i++)
            {
                var l = lights[i];
                if (!l.enabled) continue;
                float k = (now - lightStart[i]) / lightDur[i];
                if (k >= 1f) l.enabled = false;
                else l.intensity = lightPeak[i] * (1f - k) * (1f - k);
            }
            if (casings != null) casings.Tick(Time.deltaTime);
            if (decals != null) decals.Tick();
            if (Time.unscaledTime >= nextShade)
            {
                nextShade = Time.unscaledTime + 0.5f;
                UpdateShade();
            }
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
            if (fxRoot != null) Destroy(fxRoot.gameObject);
            foreach (var o in owned)
                if (o != null) Destroy(o);
            owned.Clear();
        }

        void Init()
        {
            if (initialized) return;
            initialized = true;
            if (particleShader == null) particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

            // World-space systems are fed absolute positions: keep their root at the origin, unscaled.
            fxRoot = new GameObject("[VFX]").transform;

            // ---- textures & meshes
            var softTex = Own(VfxTextures.SoftCircle(64));
            var sparkTex = Own(VfxTextures.Spark(32));
            var starTex = Own(VfxTextures.Star(128));
            var smokeTex = Own(VfxTextures.SmokeAtlas(128));
            var ringTex = Own(VfxTextures.Ring(128));
            var holeTex = Own(VfxTextures.BulletHole(128));
            var shadeTex = Own(VfxTextures.ShadeStrip());
            tracerTex = Own(VfxTextures.TracerGlow(64, 32));
            var cubeMesh = Own(VfxMeshes.ShadedCube());
            var quadMesh = Own(VfxMeshes.QuadXZ());
            var cylMesh = Own(VfxMeshes.Cylinder(10));

            // ---- materials (HDR _BaseColor on the hot ones so bloom picks them up)
            var matFlash = Own(MakeMaterial(Blend.Additive, starTex, 3f));
            var matGlow = Own(MakeMaterial(Blend.Additive, softTex, 1.6f));
            var matFlame = Own(MakeMaterial(Blend.Additive, softTex, 2.4f));
            var matSpark = Own(MakeMaterial(Blend.Additive, sparkTex, 3.2f));
            var matEmber = Own(MakeMaterial(Blend.Additive, sparkTex, 2.6f));
            var matRing = Own(MakeMaterial(Blend.Additive, ringTex, 1.8f));
            var matStreak = Own(MakeMaterial(Blend.Additive, sparkTex, 2f));
            var matSmoke = Own(MakeMaterial(Blend.Alpha, smokeTex, 1f));
            var matBits = Own(MakeMaterial(Blend.Alpha, sparkTex, 1f));
            var matGroundRing = Own(MakeMaterial(Blend.Alpha, ringTex, 1f));
            var matChips = Own(MakeMaterial(Blend.Opaque, shadeTex, 1f));
            var matDecal = Own(MakeMaterial(Blend.Alpha, holeTex, 1f));
            matDecal.renderQueue = (int)RenderQueue.Transparent - 10; // under smoke and dust
            var matBrass = Own(MakeBrass());

            // ---- layers
            flash = Layer("Flash", matFlash, 64);
            ColorLife(flash, new[] { CK(1f, 1f, 1f, 0f), CK(1f, 0.8f, 0.45f, 0.5f), CK(1f, 0.45f, 0.15f, 1f) },
                      new[] { AK(1f, 0f), AK(0.8f, 0.4f), AK(0f, 1f) });
            SizeLife(flash, new Keyframe(0f, 0.85f), new Keyframe(0.3f, 1.05f), new Keyframe(1f, 1f));

            glow = Layer("Glow", matGlow, 192);
            ColorLife(glow, new[] { CK(1f, 1f, 1f, 0f), CK(1f, 0.85f, 0.7f, 1f) }, new[] { AK(1f, 0f), AK(0.5f, 0.35f), AK(0f, 1f) });
            SizeLife(glow, new Keyframe(0f, 0.8f), new Keyframe(1f, 1.3f));

            flame = Layer("Flame", matFlame, 128, drag: 10f, stretch: 0.022f);
            ColorLife(flame, new[] { CK(1f, 1f, 1f, 0f), CK(1f, 0.88f, 0.5f, 0.25f), CK(1f, 0.5f, 0.12f, 0.6f), CK(0.55f, 0.1f, 0.03f, 1f) },
                      new[] { AK(1f, 0f), AK(0.9f, 0.5f), AK(0f, 1f) });
            SizeLife(flame, new Keyframe(0f, 1f), new Keyframe(1f, 0.5f));

            sparks = Layer("Sparks", matSpark, 320, gravity: 0.9f, drag: 1.8f, stretch: 0.025f);
            Collide(sparks, 0.35f, 0.4f, 0.5f);
            ColorLife(sparks, SparkColors(), new[] { AK(1f, 0f), AK(1f, 0.6f), AK(0f, 1f) });
            SizeLife(sparks, new Keyframe(0f, 1f), new Keyframe(1f, 0.3f));

            sparkRing = Layer("SparkRing", matSpark, 96, gravity: 0.4f, drag: 6.5f, stretch: 0.05f);
            ColorLife(sparkRing, SparkColors(), new[] { AK(1f, 0f), AK(1f, 0.5f), AK(0f, 1f) });
            SizeLife(sparkRing, new Keyframe(0f, 1f), new Keyframe(1f, 0.3f));

            embers = Layer("Embers", matEmber, 160, gravity: -0.12f, drag: 1.6f);
            AddNoise(embers, 0.8f, 1.5f);
            ColorLife(embers, new[] { CK(1f, 0.9f, 0.6f, 0f), CK(1f, 0.55f, 0.18f, 0.5f), CK(0.7f, 0.18f, 0.05f, 1f) },
                      new[] { AK(0f, 0f), AK(1f, 0.08f), AK(0.9f, 0.6f), AK(0f, 1f) });
            SizeLife(embers, new Keyframe(0f, 1f), new Keyframe(0.7f, 0.8f), new Keyframe(1f, 0f));

            smoke = Layer("Smoke", matSmoke, 220, gravity: -0.03f, drag: 3f, sort: true);
            Sheet(smoke);
            Spin(smoke, 0.5f);
            AddNoise(smoke, 0.15f, 0.6f);
            ColorLife(smoke, new[] { CK(1f, 1f, 1f, 0f), CK(0.85f, 0.85f, 0.85f, 1f) },
                      new[] { AK(0f, 0f), AK(1f, 0.1f), AK(0.55f, 0.5f), AK(0f, 1f) });
            SizeEaseOut(smoke, 0.35f, 1f);

            dust = Layer("Dust", matSmoke, 320, gravity: 0.06f, drag: 6f, sort: true);
            Sheet(dust);
            Spin(dust, 0.7f);
            ColorLife(dust, new[] { CK(1f, 1f, 1f, 0f), CK(0.8f, 0.78f, 0.76f, 1f) },
                      new[] { AK(0f, 0f), AK(1f, 0.06f), AK(0.6f, 0.45f), AK(0f, 1f) });
            SizeExpoOut(dust, 0.3f, 1f);

            bits = Layer("Bits", matBits, 256, gravity: 1.3f, drag: 1.5f, stretch: 0.03f);
            Collide(bits, 0.15f, 0.7f, 0.5f);
            ColorLife(bits, new[] { CK(1f, 1f, 1f, 0f), CK(1f, 1f, 1f, 1f) }, new[] { AK(1f, 0f), AK(1f, 0.75f), AK(0f, 1f) });

            chips = Layer("Chips", matChips, 200, gravity: 2.4f, drag: 0.4f, mesh: cubeMesh);
            Collide(chips, 0.35f, 0.35f, 0.6f);
            RotateBySpeed(chips, 16f);
            SizeLife(chips, new Keyframe(0f, 1f), new Keyframe(0.8f, 1f), new Keyframe(1f, 0f));

            ring = Layer("Ring", matRing, 32);
            ColorLife(ring, new[] { CK(1f, 1f, 1f, 0f), CK(1f, 0.7f, 0.35f, 1f) }, new[] { AK(1f, 0f), AK(0.6f, 0.4f), AK(0f, 1f) });
            SizeExpoOut(ring, 0.12f, 1f);

            groundRing = Layer("GroundRing", matGroundRing, 16, mesh: quadMesh);
            ColorLife(groundRing, new[] { CK(1f, 1f, 1f, 0f), CK(0.9f, 0.9f, 0.9f, 1f) }, new[] { AK(0.9f, 0f), AK(0.5f, 0.4f), AK(0f, 1f) });
            SizeExpoOut(groundRing, 0.15f, 1f);

            streak = Layer("Streak", matStreak, 16, stretch: 0.025f);
            ColorLife(streak, new[] { CK(1f, 1f, 1f, 0f), CK(1f, 0.8f, 0.5f, 1f) }, new[] { AK(1f, 0f), AK(0f, 1f) });

            // ---- pooled world reactions
            casings = new ShellCasings(fxRoot, cylMesh, matBrass, Mathf.Clamp(maxCasings, 4, 30));
            decals = new BulletDecals(fxRoot, quadMesh, matDecal, Mathf.Clamp(maxDecals, 4, 40));

            int lc = Mathf.Clamp(maxLights, 2, 6);
            lights = new Light[lc];
            lightStart = new float[lc];
            lightDur = new float[lc];
            lightPeak = new float[lc];
            for (int i = 0; i < lc; i++)
            {
                var go = new GameObject("FlashLight");
                go.transform.SetParent(fxRoot, false);
                var l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.shadows = LightShadows.None;
                l.enabled = false;
                lights[i] = l;
                lightDur[i] = 0.05f;
            }

            UpdateShade();
        }

        T Own<T>(T o) where T : Object
        {
            owned.Add(o);
            return o;
        }

        // ================================================================== builders

        ParticleSystem Layer(string name, Material mat, int max, float gravity = 0f, float drag = 0f,
                             float stretch = 0f, Mesh mesh = null, bool sort = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(fxRoot, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.maxParticles = max;
            main.gravityModifier = gravity;
            main.startLifetime = 1f;
            main.startSpeed = 0f;
            main.startSize = 0.1f;
            main.startColor = Color.white;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.enabled = false;

            if (drag > 0f)
            {
                var limit = ps.limitVelocityOverLifetime;
                limit.enabled = true;
                limit.limit = 1000f;
                limit.dampen = 0f;
                limit.drag = drag;
                limit.multiplyDragByParticleSize = false;
                limit.multiplyDragByParticleVelocity = false;
            }

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            r.sortMode = sort ? ParticleSystemSortMode.Distance : ParticleSystemSortMode.None;
            if (mesh != null)
            {
                r.renderMode = ParticleSystemRenderMode.Mesh;
                r.mesh = mesh;
                r.alignment = ParticleSystemRenderSpace.World;
                main.startRotation3D = true;
            }
            else if (stretch > 0f)
            {
                r.renderMode = ParticleSystemRenderMode.Stretch;
                r.velocityScale = stretch;
                r.lengthScale = 1f;
            }
            else r.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Play();
            return ps;
        }

        static GradientColorKey CK(float r, float g, float b, float t) => new(new Color(r, g, b), t);
        static GradientAlphaKey AK(float a, float t) => new(a, t);

        static GradientColorKey[] SparkColors() =>
            new[] { CK(1f, 1f, 1f, 0f), CK(1f, 0.78f, 0.35f, 0.3f), CK(1f, 0.38f, 0.08f, 1f) };

        static void ColorLife(ParticleSystem ps, GradientColorKey[] colors, GradientAlphaKey[] alphas)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(colors, alphas);
            col.color = g;
        }

        static void SizeLife(ParticleSystem ps, params Keyframe[] keys)
        {
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(keys));
        }

        // Cubic ease-out without overshoot: most of the growth happens early.
        static void SizeEaseOut(ParticleSystem ps, float start, float end) =>
            SizeLife(ps, new Keyframe(0f, start, 0f, (end - start) * 3f), new Keyframe(1f, end, 0f, 0f));

        // Sharper: 80 % of the growth in the first 20 % of the life (shockwaves, dust bursts).
        static void SizeExpoOut(ParticleSystem ps, float start, float end)
        {
            float d = end - start;
            SizeLife(ps,
                new Keyframe(0f, start, 0f, d * 4.9f),
                new Keyframe(0.2f, start + d * 0.75f, d * 0.6f, d * 0.6f),
                new Keyframe(1f, end, 0f, 0f));
        }

        static void Collide(ParticleSystem ps, float bounce, float dampen, float radiusScale)
        {
            var c = ps.collision;
            c.enabled = true;
            c.type = ParticleSystemCollisionType.World;
            c.mode = ParticleSystemCollisionMode.Collision3D;
            c.quality = ParticleSystemCollisionQuality.High;
            c.bounce = bounce;
            c.dampen = dampen;
            c.lifetimeLoss = 0f;
            c.minKillSpeed = 0f;
            c.radiusScale = radiusScale;
            c.collidesWith = WorldMask;
            c.enableDynamicColliders = true;
            c.sendCollisionMessages = false;
        }

        static void RotateBySpeed(ParticleSystem ps, float maxRadPerSec)
        {
            var rbs = ps.rotationBySpeed;
            rbs.enabled = true;
            rbs.separateAxes = true;
            var lo = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, -1f));
            var hi = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));
            rbs.x = new ParticleSystem.MinMaxCurve(maxRadPerSec, lo, hi);
            rbs.y = new ParticleSystem.MinMaxCurve(maxRadPerSec, lo, hi);
            rbs.z = new ParticleSystem.MinMaxCurve(maxRadPerSec, lo, hi);
            rbs.range = new Vector2(0.3f, 5f); // resting chips stop turning
        }

        static void Spin(ParticleSystem ps, float radPerSec)
        {
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-radPerSec, radPerSec);
        }

        static void AddNoise(ParticleSystem ps, float strength, float frequency)
        {
            var nz = ps.noise;
            nz.enabled = true;
            nz.strength = strength;
            nz.frequency = frequency;
            nz.scrollSpeed = 0.5f;
            nz.damping = true;
            nz.octaveCount = 1;
            nz.quality = ParticleSystemNoiseQuality.Low;
        }

        // Random puff out of the 2×2 smoke atlas, constant per particle.
        static void Sheet(ParticleSystem ps)
        {
            var ts = ps.textureSheetAnimation;
            ts.enabled = true;
            ts.mode = ParticleSystemAnimationMode.Grid;
            ts.numTilesX = 2;
            ts.numTilesY = 2;
            ts.animation = ParticleSystemAnimationType.WholeSheet;
            ts.frameOverTime = new ParticleSystem.MinMaxCurve(0f, 0.999f);
            ts.cycleCount = 1;
        }

        // ================================================================== materials

        Material MakeMaterial(Blend blend, Texture tex, float intensity)
        {
            var shader = particleShader != null ? particleShader : Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var m = new Material(shader);
            if (blend == Blend.Opaque)
            {
                m.SetFloat("_Surface", 0f);
                m.SetOverrideTag("RenderType", "Opaque");
                m.SetFloat("_SrcBlend", (float)BlendMode.One);
                m.SetFloat("_DstBlend", (float)BlendMode.Zero);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
                m.SetFloat("_ZWrite", 1f);
                m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Geometry;
            }
            else
            {
                bool additive = blend == Blend.Additive;
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", additive ? 2f : 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                // Additive glows draw after alpha smoke so a puff never hides a flash behind it.
                m.renderQueue = (int)RenderQueue.Transparent + (additive ? 10 : 0);
            }
            m.SetFloat("_Cull", 0f); // two-sided: flat rings, decals, casings seen from any side
            if (tex != null) m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", new Color(intensity, intensity, intensity, 1f));
            return m;
        }

        Material MakeBrass()
        {
            var brass = new Color(0.86f, 0.63f, 0.27f);
            var lit = litShader != null ? litShader : Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                var fallback = MakeMaterial(Blend.Opaque, null, 1f);
                fallback.SetColor("_BaseColor", brass);
                return fallback;
            }
            var m = new Material(lit) { name = "vfx_brass" };
            m.SetColor("_BaseColor", brass);
            m.SetFloat("_Metallic", 0.45f);
            m.SetFloat("_Smoothness", 0.72f);
            return m;
        }
    }
}
