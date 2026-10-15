using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Kosynierzy
{
    /// <summary>
    /// Townsperson. Strolls between crowd points (miasto_tlum_*, preferring nearby ones) or around its start.
    /// Panic ("sianie paniki"): a body in view or the player's gunshot within 30 m → scream (bark_cywil_panika if the
    /// dialogue file has it), run to the nearest Schupo / SD and raise their alarm at that spot, then flee.
    /// Shots fired by SD only make it flee. It calms down ~10 s after the last scare.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class CivilianController : NpcBrain
    {
        enum Mode { Wander, Reporting, Fleeing }

        static readonly float[] FleeAngles = { 0f, 35f, -35f, 70f, -70f, 110f, -110f };

        [Tooltip("Stroll stops; empty = random walks around the start (Wander Radius).")]
        [SerializeField] Transform[] wanderPoints = new Transform[0];
        [SerializeField] float wanderRadius = 8f;
        [SerializeField] Vector2 walkSpeedRange = new(1.1f, 1.5f);
        [SerializeField] Vector2 waitTime = new(2f, 6f);
        [SerializeField] float runSpeed = 4.5f;
        [Tooltip("Gunshots closer than this scare the civilian.")]
        [SerializeField] float fleeHearing = 30f;
        [Tooltip("How far each flee leg goes.")]
        [SerializeField] float fleeDistance = 15f;
        [Tooltip("Seconds after the last scare before calming down.")]
        [SerializeField] float calmDownAfter = 10f;
        [Tooltip("Runs to a Schupo / SD within this distance to raise the alarm; farther away it only flees.")]
        [SerializeField] float reportSearchRadius = 60f;
        [SerializeField] float bodySightRange = 12f;
        [SerializeField] float bodySightRangeDark = 5f;

        readonly List<Vector3> points = new();
        Mode mode;
        float walkSpeed, calmAt, reportStart;
        bool reportIsAlarm;
        Vector3 scareFrom, fleeTarget;
        NpcBrain reportTo;

        public bool IsFleeing => mode == Mode.Fleeing;
        public bool IsPanicking => mode != Mode.Wander;
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
            // Never hears farther than the shot carries: a Nagant (12 m) does not scare a street 25 m away.
            float hearing = Mathf.Min(radius, fleeHearing);
            if ((position - transform.position).sqrMagnitude > hearing * hearing) return;
            bool soldierFired = source != null && source.GetComponentInParent<EnemyController>() != null;
            if (soldierFired) Flee(position);
            else Panic(position, true);
        }

        void Update()
        {
            if (IsDown) return;

            var body = SpotBody(bodySightRange, bodySightRangeDark, 140f);
            if (body != null) Panic(body.Position, false);

            switch (mode)
            {
                case Mode.Wander:
                    Roam(points, true, wanderRadius, walkSpeed, waitTime);
                    break;
                case Mode.Reporting:
                    UpdateReport();
                    break;
                case Mode.Fleeing:
                    if (Time.time >= calmAt)
                    {
                        mode = Mode.Wander;
                        Halt();
                        ResetRoam();
                        return;
                    }
                    if (Arrived(fleeTarget, 1.5f)) fleeTarget = PickFleePoint();
                    MoveTo(fleeTarget, runSpeed);
                    break;
            }
        }

        /// <summary>
        /// Scream and run to the nearest Schupo / SD. <paramref name="shots"/> = the player's gunshot: they get a full alarm;
        /// a body only sends them to search there (a corpse nobody can pin on the player must not turn the whole district hostile).
        /// </summary>
        void Panic(Vector3 position, bool shots)
        {
            calmAt = Time.time + calmDownAfter;
            if (mode == Mode.Reporting)
            {
                if (shots) reportIsAlarm = true; // a shot upgrades a body report in progress
                return;
            }
            reportIsAlarm = shots;
            scareFrom = position;
            Bark("bark_cywil_panika");
            reportTo = NearestAuthority();
            if (reportTo == null)
            {
                Flee(position);
                return;
            }
            mode = Mode.Reporting;
            reportStart = Time.time;
        }

        void Flee(Vector3 from)
        {
            scareFrom = from;
            calmAt = Time.time + calmDownAfter;
            if (mode == Mode.Fleeing || mode == Mode.Reporting) return;
            mode = Mode.Fleeing;
            fleeTarget = PickFleePoint();
        }

        void UpdateReport()
        {
            if (reportTo == null || reportTo.IsDown || !reportTo.isActiveAndEnabled)
            {
                reportTo = NearestAuthority();
                if (reportTo == null)
                {
                    mode = Mode.Wander;
                    Flee(scareFrom);
                    return;
                }
            }

            Vector3 target = reportTo.transform.position;
            if (FlatDistanceTo(target) <= 2.5f || Time.time - reportStart > 25f)
            {
                if (reportTo is EnemyController soldier)
                {
                    if (reportIsAlarm) soldier.RaiseAlarm(scareFrom, true);
                    else soldier.Investigate(scareFrom);
                }
                else if (reportTo is PoliceController police) police.ReportFrom(scareFrom, reportIsAlarm);
                reportTo = null;
                mode = Mode.Wander;
                Flee(scareFrom);
                return;
            }
            MoveTo(target, runSpeed);
        }

        NpcBrain NearestAuthority()
        {
            NpcBrain best = null;
            float bestSqr = reportSearchRadius * reportSearchRadius;
            foreach (var e in EnemyController.All)
            {
                if (e.IsDown) continue;
                float sqr = (e.transform.position - transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = e;
                }
            }
            foreach (var p in FindObjectsByType<PoliceController>(FindObjectsInactive.Exclude))
            {
                if (p.IsDown || !p.isActiveAndEnabled) continue;
                float sqr = (p.transform.position - transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = p;
                }
            }
            return best;
        }

        Vector3 PickFleePoint()
        {
            Vector3 away = transform.position - scareFrom;
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
