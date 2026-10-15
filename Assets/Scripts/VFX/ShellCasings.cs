using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Pooled brass casings with a tiny hand-written simulation (no Rigidbody / Collider, so bullets, NPC vision
    /// and the player's capsule never touch them): thrown right/up with spin, gravity, raycast bounces that lose
    /// energy, then they settle LYING on their side, roll a few centimetres, rest ~4 s and shrink away.
    /// Driven by VfxLibrary.Update; no allocations per shot.
    /// </summary>
    public sealed class ShellCasings
    {
        struct Casing
        {
            public Transform T;
            public bool Active, Resting;
            public Vector3 Pos, Vel, SpinAxis, Size, RollDir, Normal;
            public Quaternion Rot;
            public float Spin, Age, Life, Radius, RollSpeed, SettleT;
            public int Bounces;
        }

        const int Mask = ~(1 << 2); // everything except "Ignore Raycast"
        readonly Casing[] items;
        readonly RaycastHit[] hits = new RaycastHit[8];
        int next;

        /// <summary>Raised on a hard bounce (position, impact speed) — hook a tinkle sound here if wanted.</summary>
        public System.Action<Vector3, float> Bounced;

        public ShellCasings(Transform parent, Mesh mesh, Material material, int count)
        {
            items = new Casing[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Casing");
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = material;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                go.SetActive(false);
                items[i].T = go.transform;
            }
        }

        /// <param name="muzzle">Muzzle point; the casing leaves the ejection port behind it.</param>
        /// <param name="scale">1 = pistol (9 mm), ≥ 1.3 = long rifle casing.</param>
        public void Eject(Vector3 muzzle, Quaternion rot, Vector3 inheritVelocity, float scale)
        {
            ref var c = ref items[next];
            next = (next + 1) % items.Length;

            bool rifle = scale >= 1.3f;
            // Slightly larger than life so they read at FPP distance.
            c.Size = rifle ? new Vector3(0.0155f, 0.07f, 0.0155f) : new Vector3(0.0135f, 0.026f, 0.0135f);
            c.Radius = c.Size.x * 0.5f;

            Vector3 fwd = rot * Vector3.forward, right = rot * Vector3.right, up = rot * Vector3.up;
            c.Pos = muzzle - fwd * (rifle ? 0.24f : 0.11f) + right * 0.02f + up * 0.015f;
            c.Vel = inheritVelocity
                  + right * Random.Range(1.7f, 2.6f)
                  + up * Random.Range(1.4f, 2.3f)
                  - fwd * Random.Range(0.1f, 0.6f);
            // Casing axis (mesh Y) along the barrel, then it tumbles end over end.
            c.Rot = rot * Quaternion.Euler(90f, 0f, 0f);
            c.SpinAxis = (up * Random.Range(0.6f, 1f) + right * Random.Range(-0.5f, 0.5f) + fwd * Random.Range(-0.2f, 0.2f)).normalized;
            c.Spin = Random.Range(1100f, 2100f) * (Random.value < 0.5f ? -1f : 1f);
            c.Age = 0f;
            c.Life = Random.Range(4f, 4.8f);
            c.Resting = false;
            c.Bounces = 0;
            c.RollSpeed = 0f;
            c.Active = true;
            c.T.localScale = c.Size;
            c.T.SetPositionAndRotation(c.Pos, c.Rot);
            c.T.gameObject.SetActive(true);
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            for (int i = 0; i < items.Length; i++)
            {
                ref var c = ref items[i];
                if (!c.Active) continue;

                c.Age += dt;
                if (c.Age >= c.Life)
                {
                    c.Active = false;
                    c.T.gameObject.SetActive(false);
                    continue;
                }

                if (!c.Resting) Fly(ref c, dt);
                else Rest(ref c, dt);

                c.T.SetPositionAndRotation(c.Pos, c.Rot);
                float fade = Mathf.Clamp01((c.Life - c.Age) / 0.35f);
                if (fade < 1f) c.T.localScale = c.Size * fade;
            }
        }

        void Fly(ref Casing c, float dt)
        {
            c.Vel += Physics.gravity * dt;
            c.Vel *= 1f - Mathf.Min(1f, 0.35f * dt);
            Vector3 step = c.Vel * dt;
            float dist = step.magnitude;

            if (dist > 1e-6f && Cast(c.Pos, step / dist, dist + c.Radius, out RaycastHit hit))
            {
                Vector3 n = hit.normal;
                c.Pos = hit.point + n * c.Radius;
                float vn = Vector3.Dot(c.Vel, n);
                Vector3 vt = c.Vel - vn * n;
                if (vn < -0.6f) Bounced?.Invoke(c.Pos, -vn);
                c.Vel = vt * 0.6f - n * (vn * Random.Range(0.22f, 0.38f));
                c.Spin *= 0.5f;
                c.SpinAxis = Vector3.Cross(n, Random.onUnitSphere).normalized;
                if (c.SpinAxis.sqrMagnitude < 0.5f) c.SpinAxis = Vector3.up;
                c.Bounces++;

                if (n.y > 0.55f && (c.Vel.magnitude < 0.75f || c.Bounces >= 5)) Settle(ref c, n, vt);
            }
            else c.Pos += step;

            c.Rot = Quaternion.AngleAxis(c.Spin * dt, c.SpinAxis) * c.Rot;
        }

        void Settle(ref Casing c, Vector3 n, Vector3 slide)
        {
            c.Resting = true;
            c.SettleT = 0f;
            c.Normal = n;
            // Lie down: the casing axis goes flat on the surface, keeping its heading.
            Vector3 axis = c.Rot * Vector3.up;
            Vector3 along = Vector3.ProjectOnPlane(axis, n);
            if (along.sqrMagnitude < 1e-4f) along = Vector3.ProjectOnPlane(Random.onUnitSphere, n);
            along.Normalize();
            c.Rot = Quaternion.FromToRotation(c.Rot * Vector3.up, along) * c.Rot;
            // A cylinder only rolls across its axis.
            c.RollDir = Vector3.Cross(n, along).normalized;
            float s = Vector3.Dot(slide, c.RollDir);
            if (s < 0f) { c.RollDir = -c.RollDir; s = -s; }
            c.RollSpeed = Mathf.Min(0.45f, s * 0.6f + Random.Range(0.04f, 0.14f));
        }

        void Rest(ref Casing c, float dt)
        {
            if (c.RollSpeed <= 0.002f) return;
            c.Pos += c.RollDir * (c.RollSpeed * dt);
            float deg = c.RollSpeed * dt / Mathf.Max(0.001f, c.Radius) * Mathf.Rad2Deg;
            c.Rot = Quaternion.AngleAxis(deg, Vector3.Cross(c.Normal, c.RollDir)) * c.Rot;
            c.RollSpeed *= Mathf.Exp(-4.5f * dt);
        }

        bool Cast(Vector3 origin, Vector3 dir, float dist, out RaycastHit best)
        {
            best = default;
            int count = Physics.RaycastNonAlloc(origin, dir, hits, dist, Mask, QueryTriggerInteraction.Ignore);
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var h = hits[i];
                if (h.collider is CharacterController) continue; // the player's capsule
                if (h.distance < bestDist)
                {
                    bestDist = h.distance;
                    best = h;
                }
            }
            return bestDist < float.MaxValue;
        }
    }
}
