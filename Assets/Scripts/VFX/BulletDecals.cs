using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Pooled bullet-hole quads aligned to the hit normal (the oldest is reused when all are taken).
    /// Each lives <see cref="Life"/> s and fades over the last <see cref="FadeTime"/> s through a property block;
    /// tint follows the scene light so a hole is not brighter than the wall at night.
    /// </summary>
    public sealed class BulletDecals
    {
        public const float Life = 25f;
        public const float FadeTime = 3f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        readonly Transform[] t;
        readonly MeshRenderer[] r;
        readonly float[] born;
        readonly Color[] tint;
        readonly bool[] fading;
        readonly MaterialPropertyBlock block = new();
        int next;

        public BulletDecals(Transform parent, Mesh quadXZ, Material material, int count)
        {
            t = new Transform[count];
            r = new MeshRenderer[count];
            born = new float[count];
            tint = new Color[count];
            fading = new bool[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("BulletHole");
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = quadXZ;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = material;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                go.SetActive(false);
                t[i] = go.transform;
                r[i] = mr;
            }
        }

        public void Place(Vector3 point, Vector3 normal, float size, Color light)
        {
            int i = next;
            next = (next + 1) % t.Length;
            var rot = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            // Lifted off the surface against z-fighting; a little more for far-away shots would need the camera.
            t[i].SetPositionAndRotation(point + normal * 0.006f, rot);
            float s = size * Random.Range(0.85f, 1.15f);
            t[i].localScale = new Vector3(s, 1f, s);
            born[i] = Time.time;
            tint[i] = new Color(light.r, light.g, light.b, 1f);
            fading[i] = false;
            Apply(i, 1f);
            if (!t[i].gameObject.activeSelf) t[i].gameObject.SetActive(true);
        }

        public void Tick()
        {
            float now = Time.time;
            for (int i = 0; i < t.Length; i++)
            {
                if (!t[i].gameObject.activeSelf) continue;
                float age = now - born[i];
                if (age >= Life)
                {
                    t[i].gameObject.SetActive(false);
                    continue;
                }
                if (age > Life - FadeTime)
                {
                    fading[i] = true;
                    Apply(i, (Life - age) / FadeTime);
                }
            }
        }

        void Apply(int i, float alpha)
        {
            Color c = tint[i];
            c.a = alpha;
            block.SetColor(BaseColorId, c);
            r[i].SetPropertyBlock(block);
        }
    }
}
