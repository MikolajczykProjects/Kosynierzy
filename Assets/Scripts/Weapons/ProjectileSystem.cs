using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>Everything needed to fire one bullet.</summary>
    public struct ShotInfo
    {
        public Vector3 Origin;
        public Vector3 Direction;
        /// <summary>Where the tracer appears to start (gun muzzle); bullets travel from Origin (camera).</summary>
        public Vector3 VisualOrigin;
        public float Speed;
        public float Damage;
        public float GravityScale;
        public float MaxDistance;
        public float Impulse;
        public Transform Owner;
        public bool FromPlayer;
        public Color TracerColor;
    }

    /// <summary>Result of a bullet hitting something; consumed by HUD and feedback systems.</summary>
    public struct HitResult
    {
        public Vector3 Point, Normal, Direction;
        public Health Health;
        public bool Killed, Headshot, FromPlayer;
        public float Damage;
        public Transform Owner;
    }

    /// <summary>
    /// Fast simulated bullets for the player and enemies: each frame a bullet moves along its velocity
    /// (with a little drop) and the swept segment is raycast, so nothing is skipped at high speed.
    /// Pooled tracers; hits damage <see cref="Health"/> (x <see cref="Hitbox"/> multiplier) and spawn VFX.
    /// Created on first use; no scene setup needed.
    /// </summary>
    public class ProjectileSystem : MonoBehaviour
    {
        class Bullet
        {
            public ShotInfo Shot;
            public Vector3 Position, Velocity, VisualOffset;
            public float Travelled;
            public LineRenderer Tracer;
        }

        const float TracerLength = 4f;
        const float VisualConvergeDistance = 12f;

        static ProjectileSystem instance;
        readonly List<Bullet> active = new();
        readonly Stack<Bullet> pool = new();
        readonly RaycastHit[] hits = new RaycastHit[16];
        int hitMask = ~0;

        /// <summary>Raised for every bullet that hits something (also misses on walls: Health == null).</summary>
        public static event Action<HitResult> AnyHit;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
            AnyHit = null;
        }

        static ProjectileSystem Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("[ProjectileSystem]");
                    instance = go.AddComponent<ProjectileSystem>();
                }
                return instance;
            }
        }

        public static void Fire(in ShotInfo shot) => Instance.Spawn(shot);

        void Spawn(in ShotInfo shot)
        {
            var b = pool.Count > 0 ? pool.Pop() : new Bullet();
            b.Shot = shot;
            b.Position = shot.Origin;
            b.Velocity = shot.Direction.normalized * shot.Speed;
            b.VisualOffset = shot.VisualOrigin - shot.Origin;
            b.Travelled = 0f;
            if (b.Tracer == null) b.Tracer = CreateTracer();
            b.Tracer.startColor = shot.TracerColor;
            b.Tracer.endColor = new Color(shot.TracerColor.r, shot.TracerColor.g, shot.TracerColor.b, 0f);
            b.Tracer.SetPosition(0, shot.VisualOrigin);
            b.Tracer.SetPosition(1, shot.VisualOrigin);
            b.Tracer.enabled = true;
            active.Add(b);
            // First step right away so point-blank shots land this frame.
            if (Step(b, Time.deltaTime)) Release(active.Count - 1);
        }

        LineRenderer CreateTracer()
        {
            var go = new GameObject("Tracer");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            VfxLibrary.ConfigureTracer(lr);
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = active.Count - 1; i >= 0; i--)
                if (Step(active[i], dt)) Release(i);
        }

        /// <summary>Moves one bullet; returns true when it is finished.</summary>
        bool Step(Bullet b, float dt)
        {
            if (dt <= 0f) return false;
            Vector3 start = b.Position;
            b.Velocity += Physics.gravity * (b.Shot.GravityScale * dt);
            Vector3 delta = b.Velocity * dt;
            float dist = delta.magnitude;
            Vector3 dir = delta / dist;

            if (TryHit(b, start, dir, dist, out RaycastHit hit))
            {
                Resolve(b, hit, dir);
                return true;
            }

            b.Position = start + delta;
            if (!b.Shot.FromPlayer) VfxLibrary.BulletNearMiss(start, b.Position);
            b.Travelled += dist;
            UpdateTracer(b);
            return b.Travelled >= b.Shot.MaxDistance;
        }

        bool TryHit(Bullet b, Vector3 start, Vector3 dir, float dist, out RaycastHit best)
        {
            best = default;
            int count = Physics.RaycastNonAlloc(start, dir, hits, dist, hitMask, QueryTriggerInteraction.Ignore);
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var h = hits[i];
                if (b.Shot.Owner != null && h.collider.transform.IsChildOf(b.Shot.Owner)) continue;
                if (h.distance < bestDist) { bestDist = h.distance; best = h; }
            }
            return bestDist < float.MaxValue;
        }

        void Resolve(Bullet b, RaycastHit hit, Vector3 dir)
        {
            var result = new HitResult
            {
                Point = hit.point, Normal = hit.normal, Direction = dir,
                FromPlayer = b.Shot.FromPlayer, Owner = b.Shot.Owner,
            };

            var health = hit.collider.GetComponentInParent<Health>();
            if (health != null && !health.IsDead)
            {
                var hitbox = hit.collider.GetComponent<Hitbox>();
                float dmg = b.Shot.Damage * (hitbox != null ? hitbox.Multiplier : 1f);
                health.TakeDamage(dmg, hit.point, -dir);
                result.Health = health;
                result.Damage = dmg;
                result.Headshot = hitbox != null && hitbox.IsHead;
                result.Killed = health.IsDead;
            }

            if (hit.rigidbody != null && !hit.rigidbody.isKinematic)
                hit.rigidbody.AddForceAtPosition(dir * b.Shot.Impulse, hit.point, ForceMode.Impulse);

            VfxLibrary.PlayImpact(hit.point, hit.normal, health != null ? ImpactKind.Flesh : ImpactKind.Ground);
            if (health == null) SfxBank.PlayAt(SfxBank.Sfx.BulletImpact, hit.point);

            // Final tracer frame reaches the hit point.
            b.Position = hit.point;
            UpdateTracer(b);
            AnyHit?.Invoke(result);
        }

        void UpdateTracer(Bullet b)
        {
            float converge = Mathf.Clamp01(1f - b.Travelled / VisualConvergeDistance);
            Vector3 head = b.Position + b.VisualOffset * converge;
            Vector3 back = b.Velocity.normalized * Mathf.Min(TracerLength, b.Travelled + 0.5f);
            b.Tracer.SetPosition(0, head);
            b.Tracer.SetPosition(1, head - back);
        }

        void Release(int index)
        {
            var b = active[index];
            active.RemoveAt(index);
            b.Tracer.enabled = false;
            pool.Push(b);
        }
    }
}
