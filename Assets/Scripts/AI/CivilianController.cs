using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Kosynierzy
{
    /// <summary>
    /// Townsperson. Strolls between crowd points (miasto_tlum_*, preferring nearby ones) or around its start;
    /// a gunshot within 30 m makes it run away from the sound, and it calms down ~10 s after the last shot.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class CivilianController : NpcBrain
    {
        static readonly float[] FleeAngles = { 0f, 35f, -35f, 70f, -70f, 110f, -110f };

        [Tooltip("Stroll stops; empty = random walks around the start (Wander Radius).")]
        [SerializeField] Transform[] wanderPoints = new Transform[0];
        [SerializeField] float wanderRadius = 8f;
        [SerializeField] Vector2 walkSpeedRange = new(1.1f, 1.5f);
        [SerializeField] Vector2 waitTime = new(2f, 6f);
        [SerializeField] float runSpeed = 4.5f;
        [Tooltip("Gunshots closer than this make the civilian flee.")]
        [SerializeField] float fleeHearing = 30f;
        [Tooltip("How far each flee leg goes.")]
        [SerializeField] float fleeDistance = 15f;
        [Tooltip("Seconds after the last gunshot before calming down.")]
        [SerializeField] float calmDownAfter = 10f;

        readonly List<Vector3> points = new();
        float walkSpeed, fleeUntil;
        Vector3 fleeFrom, fleeTarget;

        public bool IsFleeing { get; private set; }
        protected override CharacterType DefaultType => CharacterType.Civilian;

        protected override void Awake()
        {
            base.Awake();
            walkSpeed = Random.Range(walkSpeedRange.x, walkSpeedRange.y);
            foreach (var t in wanderPoints)
                if (t != null) points.Add(t.position);
        }

        void OnEnable() => Noise.Emitted += OnNoise;
        void OnDisable() => Noise.Emitted -= OnNoise;

        /// <summary>Stroll between these positions (random order, nearby ones first).</summary>
        public void SetWanderPoints(IList<Vector3> positions)
        {
            points.Clear();
            if (positions != null) points.AddRange(positions);
            ResetRoamIndex();
            ResetRoam();
        }

        void OnNoise(Vector3 position, float radius, NoiseKind kind, Transform source)
        {
            if (kind != NoiseKind.Gunshot || IsDown) return;
            if ((position - transform.position).sqrMagnitude > fleeHearing * fleeHearing) return;
            fleeFrom = position;
            fleeUntil = Time.time + calmDownAfter;
            if (IsFleeing) return;
            IsFleeing = true;
            fleeTarget = PickFleePoint();
        }

        void Update()
        {
            if (IsDown) return;
            if (IsFleeing)
            {
                if (Time.time >= fleeUntil)
                {
                    IsFleeing = false;
                    Halt();
                    ResetRoam();
                    return;
                }
                if (Arrived(fleeTarget, 1.5f)) fleeTarget = PickFleePoint();
                MoveTo(fleeTarget, runSpeed);
                return;
            }
            Roam(points, true, wanderRadius, walkSpeed, waitTime);
        }

        Vector3 PickFleePoint()
        {
            Vector3 away = transform.position - fleeFrom;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f)
            {
                Vector2 r = Random.insideUnitCircle.normalized;
                away = new Vector3(r.x, 0f, r.y);
            }
            away.Normalize();
            foreach (float angle in FleeAngles)
            {
                Vector3 candidate = transform.position + Quaternion.Euler(0f, angle, 0f) * away * fleeDistance;
                if (NavMesh.SamplePosition(candidate, out var hit, 4f, NavMesh.AllAreas)) return hit.position;
            }
            return RandomNavPoint(transform.position, fleeDistance);
        }
    }
}
