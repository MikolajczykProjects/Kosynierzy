using UnityEngine;
using UnityEngine.Rendering;

namespace Kosynierzy
{
    public enum ImpactKind { Ground, Flesh }

    /// <summary>
    /// Code-built, pooled Shuriken effects so gameplay needs no prefabs: muzzle flash, bullet impacts
    /// (ground / flesh), enemy death burst, tracer material. Layered per effect (core flash, sparks,
    /// dust/smoke, low-poly debris) with fast-out + drag motion and fading size/alpha curves.
    /// Square particles on purpose — they match the low-poly look.
    /// Put one in the scene (menu Kosynierzy/Add VFX Library) so the particle shader is kept in builds;
    /// without it one is created on first use (fine in the editor).
    /// </summary>
    public class VfxLibrary : MonoBehaviour
    {
        [Tooltip("Universal Render Pipeline/Particles/Unlit — referenced here so builds include it.")]
        [SerializeField] Shader particleShader;
        [SerializeField] int poolSize = 10;

        static VfxLibrary instance;
        static Material tracerMaterial;

        Material addSoft, alphaSoft, addSquare, alphaSquare, debrisMat;
        Mesh cubeMesh;
        Pool muzzle, impactGround, impactFlesh, impactBlood, death;

        class Pool
        {
            public Transform[] Roots;
            public ParticleSystem[][] Systems;
            int next;

            public void Play(Vector3 pos, Quaternion rot)
            {
                int i = next;
                next = (next + 1) % Roots.Length;
                Roots[i].SetPositionAndRotation(pos, rot);
                foreach (var ps in Systems[i])
                {
                    ps.Clear(false);
                    ps.Play(false);
                }
            }
        }

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

        public static Material TracerMaterial
        {
            get
            {
                if (tracerMaterial == null) tracerMaterial = Instance.MakeMaterial(true, null);
                return tracerMaterial;
            }
        }

        public static void PlayMuzzleFlash(Vector3 position, Quaternion rotation) => Instance.muzzle.Play(position, rotation);

        public static void PlayImpact(Vector3 point, Vector3 normal, ImpactKind kind)
        {
            var rot = Quaternion.LookRotation(normal);
            var lib = Instance;
            var pool = kind != ImpactKind.Flesh ? lib.impactGround
                : GameSettings.MatureContent ? lib.impactBlood : lib.impactFlesh;
            pool.Play(point + normal * 0.02f, rot);
        }

        public static void PlayDeath(Vector3 groundPosition) => Instance.death.Play(groundPosition, Quaternion.identity);

        void Awake()
        {
            if (instance == null) instance = this;
            if (instance == this) Init();
        }

        bool initialized;

        void Init()
        {
            if (initialized) return;
            initialized = true;
            if (particleShader == null) particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

            var soft = MakeSoftCircle(64);
            addSoft = MakeMaterial(true, soft);
            alphaSoft = MakeMaterial(false, soft);
            addSquare = MakeMaterial(true, null);
            alphaSquare = MakeMaterial(false, null);
            debrisMat = MakeMaterial(false, null);

            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tmp);

            muzzle = BuildPool("Muzzle", BuildMuzzle);
            impactGround = BuildPool("ImpactGround", BuildImpactGround);
            impactFlesh = BuildPool("ImpactFlesh", BuildImpactFlesh);
            impactBlood = BuildPool("ImpactBlood", BuildImpactBlood);
            death = BuildPool("Death", BuildDeath);
        }

        // ---------------------------------------------------------------- effects

        void BuildMuzzle(Transform root)
        {
            var core = Make(root, "Core", addSoft);
            Main(core, 0.04f, 0.06f, 0f, 0f, 0.35f, 0.5f, new Color(1f, 0.9f, 0.6f));
            Burst(core, 1);
            Fade(core, new Color(1f, 0.85f, 0.5f), new Color(1f, 0.45f, 0.1f), 1f);
            GrowShrink(core, 1f, 0.4f);

            var flame = Make(root, "Flame", addSoft);
            Main(flame, 0.04f, 0.08f, 3f, 9f, 0.07f, 0.16f, Color.white);
            Burst(flame, 7);
            Cone(flame, 12f, 0.01f);
            Fade(flame, new Color(1f, 0.95f, 0.7f), new Color(1f, 0.4f, 0.05f), 1f);
            Drag(flame, 8f);

            var sparks = Make(root, "Sparks", addSquare, stretch: 0.05f);
            Main(sparks, 0.08f, 0.18f, 8f, 15f, 0.012f, 0.025f, new Color(1f, 0.75f, 0.3f));
            Burst(sparks, 5);
            Cone(sparks, 22f, 0.01f);
            var m = sparks.main; m.gravityModifier = 0.5f;

            var smoke = Make(root, "Smoke", alphaSoft);
            Main(smoke, 0.5f, 0.9f, 0.3f, 1.4f, 0.12f, 0.22f, new Color(0.7f, 0.68f, 0.64f, 0.3f));
            Burst(smoke, 3);
            Cone(smoke, 18f, 0.02f);
            Fade(smoke, new Color(0.75f, 0.72f, 0.68f), new Color(0.5f, 0.5f, 0.52f), 0.3f);
            GrowShrink(smoke, 0.6f, 3f);
            Drag(smoke, 2.5f);
            m = smoke.main; m.gravityModifier = -0.04f;
        }

        void BuildImpactGround(Transform root)
        {
            var flash = Make(root, "Flash", addSoft);
            Main(flash, 0.05f, 0.07f, 0f, 0f, 0.22f, 0.3f, new Color(1f, 0.8f, 0.5f));
            Burst(flash, 1);
            Fade(flash, new Color(1f, 0.85f, 0.55f), new Color(1f, 0.5f, 0.2f), 1f);

            var sparks = Make(root, "Sparks", addSquare, stretch: 0.04f);
            Main(sparks, 0.12f, 0.3f, 5f, 12f, 0.018f, 0.035f, new Color(1f, 0.72f, 0.3f));
            Burst(sparks, 7);
            Cone(sparks, 50f, 0.02f);
            var m = sparks.main; m.gravityModifier = 1f;
            Drag(sparks, 2f);

            var dust = Make(root, "Dust", alphaSoft);
            Main(dust, 0.45f, 0.9f, 1.5f, 4.5f, 0.12f, 0.28f, new Color(0.56f, 0.5f, 0.42f, 0.55f));
            Burst(dust, 9);
            Cone(dust, 35f, 0.04f);
            Fade(dust, new Color(0.6f, 0.54f, 0.45f), new Color(0.45f, 0.42f, 0.38f), 0.55f);
            GrowShrink(dust, 0.7f, 2.4f);
            Drag(dust, 5f);
            m = dust.main; m.gravityModifier = 0.12f;

            var chips = MakeDebris(root, "Chips", new Color(0.33f, 0.3f, 0.26f));
            Main(chips, 0.5f, 0.9f, 2f, 5.5f, 0.03f, 0.07f, Color.white);
            Burst(chips, 5);
            Cone(chips, 40f, 0.02f);
            m = chips.main; m.gravityModifier = 2.5f;
        }

        void BuildImpactFlesh(Transform root)
        {
            // Team rule: no blood or gore. A hit on a person is a puff of dust and cloth fibres.
            var puff = Make(root, "ClothPuff", alphaSoft);
            Main(puff, 0.3f, 0.6f, 1f, 3f, 0.1f, 0.22f, new Color(0.55f, 0.57f, 0.5f, 0.7f));
            Burst(puff, 7);
            Cone(puff, 30f, 0.03f);
            Fade(puff, new Color(0.6f, 0.62f, 0.55f), new Color(0.45f, 0.46f, 0.42f), 0.7f);
            GrowShrink(puff, 0.8f, 2.2f);
            Drag(puff, 6f);
            var m = puff.main; m.gravityModifier = 0.1f;

            var fibres = Make(root, "Fibres", alphaSquare, stretch: 0.03f);
            Main(fibres, 0.3f, 0.6f, 2f, 5f, 0.015f, 0.03f, new Color(0.38f, 0.42f, 0.33f, 1f));
            Burst(fibres, 6);
            Cone(fibres, 35f, 0.02f);
            m = fibres.main; m.gravityModifier = 1.2f;
        }

        // Only with GameSettings.MatureContent ("Krew (16+)").
        void BuildImpactBlood(Transform root)
        {
            var mist = Make(root, "Mist", alphaSoft);
            Main(mist, 0.3f, 0.6f, 1f, 3.5f, 0.12f, 0.26f, new Color(0.45f, 0.03f, 0.03f, 0.85f));
            Burst(mist, 9);
            Cone(mist, 30f, 0.03f);
            Fade(mist, new Color(0.55f, 0.05f, 0.04f), new Color(0.25f, 0.02f, 0.02f), 0.85f);
            GrowShrink(mist, 0.8f, 2.4f);
            Drag(mist, 6f);
            var m = mist.main; m.gravityModifier = 0.3f;

            var drops = Make(root, "Drops", alphaSquare, stretch: 0.035f);
            Main(drops, 0.35f, 0.7f, 2.5f, 6f, 0.02f, 0.045f, new Color(0.38f, 0.02f, 0.02f, 1f));
            Burst(drops, 14);
            Cone(drops, 35f, 0.02f);
            m = drops.main; m.gravityModifier = 1.8f;

            var chunks = MakeDebris(root, "Chunks", new Color(0.32f, 0.02f, 0.02f));
            Main(chunks, 0.5f, 0.9f, 2f, 4f, 0.025f, 0.05f, Color.white);
            Burst(chunks, 4);
            Cone(chunks, 40f, 0.02f);
            m = chunks.main; m.gravityModifier = 2.2f;
        }

        void BuildDeath(Transform root)
        {
            var dust = Make(root, "DustRing", alphaSoft);
            Main(dust, 0.7f, 1.3f, 1.8f, 4f, 0.3f, 0.6f, new Color(0.55f, 0.5f, 0.42f, 0.5f));
            Burst(dust, 18);
            var shape = dust.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.4f;
            shape.rotation = new Vector3(90f, 0f, 0f);
            Fade(dust, new Color(0.6f, 0.55f, 0.46f), new Color(0.45f, 0.42f, 0.38f), 0.5f);
            GrowShrink(dust, 0.6f, 2.2f);
            Drag(dust, 4f);

            var bits = MakeDebris(root, "Bits", new Color(0.36f, 0.4f, 0.3f));
            Main(bits, 0.7f, 1.1f, 2f, 4.5f, 0.05f, 0.1f, Color.white);
            Burst(bits, 7);
            Cone(bits, 60f, 0.2f);
            bits.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var m = bits.main; m.gravityModifier = 2f;
        }

        // ---------------------------------------------------------------- builders

        Pool BuildPool(string name, System.Action<Transform> build)
        {
            var pool = new Pool { Roots = new Transform[poolSize], Systems = new ParticleSystem[poolSize][] };
            var parent = new GameObject(name).transform;
            parent.SetParent(transform, false);
            for (int i = 0; i < poolSize; i++)
            {
                var root = new GameObject($"{name}_{i}").transform;
                root.SetParent(parent, false);
                build(root);
                pool.Roots[i] = root;
                pool.Systems[i] = root.GetComponentsInChildren<ParticleSystem>();
            }
            return pool;
        }

        ParticleSystem Make(Transform root, string name, Material mat, float stretch = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.enabled = false;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (stretch > 0f)
            {
                r.renderMode = ParticleSystemRenderMode.Stretch;
                r.velocityScale = stretch;
                r.lengthScale = 1f;
            }
            return ps;
        }

        ParticleSystem MakeDebris(Transform root, string name, Color color)
        {
            var mat = new Material(debrisMat);
            mat.SetColor("_BaseColor", color);
            var ps = Make(root, name, mat);
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = cubeMesh;
            var main = ps.main;
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = true;
            rot.x = new ParticleSystem.MinMaxCurve(-8f, 8f);
            rot.y = new ParticleSystem.MinMaxCurve(-8f, 8f);
            rot.z = new ParticleSystem.MinMaxCurve(-8f, 8f);
            // Chips shrink away instead of popping out.
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.7f, 1f), new Keyframe(1f, 0f)));
            return ps;
        }

        static void Main(ParticleSystem ps, float lifeMin, float lifeMax, float speedMin, float speedMax,
                         float sizeMin, float sizeMax, Color color)
        {
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = color;
            main.duration = Mathf.Max(0.05f, lifeMax);
        }

        static void Burst(ParticleSystem ps, int count)
        {
            var emission = ps.emission;
            emission.enabled = true;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
        }

        static void Cone(ParticleSystem ps, float angle, float radius)
        {
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = angle;
            shape.radius = radius;
        }

        static void Fade(ParticleSystem ps, Color from, Color to, float startAlpha)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
                new[] { new GradientAlphaKey(startAlpha, 0f), new GradientAlphaKey(startAlpha * 0.6f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
        }

        static void GrowShrink(ParticleSystem ps, float start, float end)
        {
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            // Ease-out growth: most of the change happens early.
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, start, 0f, (end - start) * 3f), new Keyframe(1f, end, 0f, 0f)));
        }

        static void Drag(ParticleSystem ps, float drag)
        {
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.drag = drag;
            limit.multiplyDragByParticleSize = false;
            limit.multiplyDragByParticleVelocity = false;
        }

        // ---------------------------------------------------------------- materials

        Material MakeMaterial(bool additive, Texture tex)
        {
            var shader = particleShader != null ? particleShader : Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var m = new Material(shader);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            if (tex != null) m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            return m;
        }

        static Texture2D MakeSoftCircle(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }
    }
}
