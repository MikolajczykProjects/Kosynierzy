using UnityEngine;
using UnityEngine.AI;

namespace Kosynierzy
{
    /// <summary>
    /// Schupo (German police). Never shoots. Strolls around its post; when its "?" meter fills (same senses as SD)
    /// or it hears the player's gunshot, it barks "bark_schupo", runs to the nearest SD and puts it into Alarm with
    /// the player's last known position, then calms down for a while.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class PoliceController : WatcherBrain
    {
        enum Mode { Patrol, Watching, Reporting, Cooldown }

        [Header("Patrol")]
        [Tooltip("Random walks within this radius of the post; 0 = stand at the post.")]
        [SerializeField] float wanderRadius = 6f;
        [SerializeField] Vector2 waitTime = new(2f, 5f);
        [SerializeField] float walkSpeed = 1.4f;
        [SerializeField] float runSpeed = 4.2f;

        [Header("Report")]
        [Tooltip("Only SD within this distance are considered for the report.")]
        [SerializeField] float reportSearchRadius = 120f;
        [Tooltip("Distance at which the report is given.")]
        [SerializeField] float reportDistance = 2.5f;
        [Tooltip("Gives the alarm anyway after running this long.")]
        [SerializeField] float reportTimeout = 20f;
        [Tooltip("Seconds of calm after a report before watching again.")]
        [SerializeField] float cooldown = 12f;

        Mode mode;
        float modeStart, lastStimulus;
        EnemyController reportTo;

        public bool IsReporting => mode == Mode.Reporting;
        public override bool IsAlerted => mode == Mode.Reporting;
        public override bool ShowSuspicion => mode == Mode.Watching || (mode == Mode.Patrol && Suspicion > 0.02f);
        protected override CharacterType DefaultType => CharacterType.Police;

        void Update()
        {
            if (IsDown) return;
            UpdateSenses(mode == Mode.Reporting);
            switch (mode)
            {
                case Mode.Patrol:
                    UpdateSuspicion(1f);
                    if (PlayerVisible && Suspicion > 0.1f)
                    {
                        SetMode(Mode.Watching);
                        lastStimulus = Time.time;
                        break;
                    }
                    Roam(null, false, wanderRadius, walkSpeed, waitTime);
                    break;

                case Mode.Watching:
                    UpdateSuspicion(1f);
                    if (PlayerVisible) lastStimulus = Time.time;
                    Halt();
                    FaceTowards(LastKnownPlayerPosition, 240f);
                    if (Suspicion >= 1f) StartReport(LastKnownPlayerPosition);
                    else if (Time.time - lastStimulus > 4f)
                    {
                        SetMode(Mode.Patrol);
                        ResetRoam();
                    }
                    break;

                case Mode.Reporting:
                    UpdateReport();
                    break;

                case Mode.Cooldown:
                    Suspicion = Mathf.Max(0f, Suspicion - 0.5f * Time.deltaTime);
                    Roam(null, false, wanderRadius, walkSpeed, waitTime);
                    if (Time.time - modeStart > cooldown) SetMode(Mode.Patrol);
                    break;
            }
        }

        void SetMode(Mode next)
        {
            mode = next;
            modeStart = Time.time;
        }

        void StartReport(Vector3 lastKnown)
        {
            if (mode == Mode.Reporting || mode == Mode.Cooldown) return;
            if (!PlayerVisible) LastKnownPlayerPosition = lastKnown;
            Suspicion = 1f;
            reportTo = NearestSoldier();
            Bark("bark_schupo");
            if (reportTo == null)
            {
                SetMode(Mode.Cooldown);
                ResetRoam();
                return;
            }
            SetMode(Mode.Reporting);
        }

        void UpdateReport()
        {
            if (reportTo == null || reportTo.IsDown || !reportTo.isActiveAndEnabled)
            {
                reportTo = NearestSoldier();
                if (reportTo == null)
                {
                    SetMode(Mode.Cooldown);
                    ResetRoam();
                    return;
                }
            }

            Vector3 soldier = reportTo.transform.position;
            if (FlatDistanceTo(soldier) <= reportDistance || Time.time - modeStart > reportTimeout)
            {
                reportTo.RaiseAlarm(LastKnownPlayerPosition, true);
                reportTo = null;
                Halt();
                SetMode(Mode.Cooldown);
                ResetRoam();
                return;
            }
            MoveTo(soldier, runSpeed);
        }

        EnemyController NearestSoldier()
        {
            EnemyController best = null;
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
            return best;
        }

        protected override void OnHeard(Vector3 position, NoiseKind kind, Transform source)
        {
            if (!PlayerTarget.IsPlayerSource(source)) return;
            if (kind == NoiseKind.Gunshot)
            {
                StartReport(position);
                return;
            }
            if (mode != Mode.Patrol && mode != Mode.Watching) return;
            Suspicion = Mathf.Min(0.9f, Suspicion + (kind == NoiseKind.Footstep ? 0.06f : 0.15f));
            if (!PlayerVisible) LastKnownPlayerPosition = position;
            lastStimulus = Time.time;
            if (mode == Mode.Patrol) SetMode(Mode.Watching);
        }

        public override void NotifyDown()
        {
            Suspicion = 0f;
            reportTo = null;
        }
    }
}
