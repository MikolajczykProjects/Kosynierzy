using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// NPC that watches for the player (SD, Schupo): a sight cone with line of sight from the eyes (1.6 m) to the
    /// player's camera (triggers, itself and other people ignored), day / night / lamp-light sight ranges, hearing
    /// through Noise.Emitted, and the "?" suspicion meter (0..1) that SuspicionIndicator draws above the head.
    /// The meter fills faster when the player is close, sprints or stands in a ForbiddenZone (3x).
    /// </summary>
    public abstract class WatcherBrain : NpcBrain
    {
        const float PerceptionInterval = 0.15f;

        [Header("Sight")]
        [SerializeField] float eyeHeight = 1.6f;
        [Tooltip("Full width of the view cone in degrees (before an alarm).")]
        [SerializeField] float viewAngle = 90f;
        [SerializeField] float daySightRange = 18f;
        [Tooltip("After dark while the player stands in lamp light.")]
        [SerializeField] float nightSightRange = 10f;
        [Tooltip("After dark while the player is outside lamp light.")]
        [SerializeField] float darkSightRange = 6f;
        [Tooltip("Once alarmed: sight range multiplier; the cone also widens to 240° (tracking a known target).")]
        [SerializeField] float alertedRangeMultiplier = 1.5f;

        [Header("Suspicion '?' meter (fill per second)")]
        [Tooltip("Fill speed with the player at the edge of sight range.")]
        [SerializeField] float fillFar = 0.35f;
        [Tooltip("Fill speed with the player right in front.")]
        [SerializeField] float fillNear = 1.6f;
        [SerializeField] float sprintMultiplier = 1.8f;
        [SerializeField] float crouchMultiplier = 0.6f;
        [SerializeField] float forbiddenZoneMultiplier = 3f;
        [Tooltip("Meter drop per second while the player is out of sight.")]
        [SerializeField] float decayPerSecond = 0.12f;

        readonly RaycastHit[] hits = new RaycastHit[16];
        float perceptionTimer;

        /// <summary>The "?" meter, 0..1 (1 = alarm / report).</summary>
        public float Suspicion { get; protected set; }
        public bool PlayerVisible { get; private set; }
        public float PlayerDistance { get; private set; } = float.MaxValue;
        public Vector3 LastKnownPlayerPosition { get; protected set; }
        public float LastSeenTime { get; private set; } = -999f;
        /// <summary>Red "!" above the head.</summary>
        public abstract bool IsAlerted { get; }
        /// <summary>Yellow "?" with the meter above the head.</summary>
        public abstract bool ShowSuspicion { get; }

        protected Vector3 Eye => transform.position + Vector3.up * eyeHeight;

        protected override void Awake()
        {
            base.Awake();
            perceptionTimer = Random.Range(0f, PerceptionInterval); // spread raycasts across frames
            LastKnownPlayerPosition = transform.position;
        }

        protected virtual void OnEnable() => Noise.Emitted += HandleNoise;

        protected virtual void OnDisable()
        {
            Noise.Emitted -= HandleNoise;
            PlayerVisible = false;
        }

        void HandleNoise(Vector3 position, float radius, NoiseKind kind, Transform source)
        {
            if (IsDown || !isActiveAndEnabled) return;
            if ((position - transform.position).sqrMagnitude > radius * radius) return;
            if (source != null && source.IsChildOf(transform)) return;
            OnHeard(position, kind, source);
        }

        /// <summary>A noise reached this NPC (already inside its radius).</summary>
        protected abstract void OnHeard(Vector3 position, NoiseKind kind, Transform source);

        /// <summary>Current sight range: day 18 m; night 10 m in lamp light, 6 m outside it.</summary>
        public float SightRange()
        {
            if (!LightZones.IsNight) return daySightRange;
            return LightZones.PlayerLit ? nightSightRange : darkSightRange;
        }

        /// <summary>Call every frame; refreshes PlayerVisible about 7x per second.</summary>
        protected void UpdateSenses(bool alerted)
        {
            if (PlayerTarget.IsGone)
            {
                PlayerVisible = false;
                return;
            }
            perceptionTimer -= Time.deltaTime;
            if (perceptionTimer > 0f) return;
            perceptionTimer = PerceptionInterval;

            float range = SightRange() * (alerted ? alertedRangeMultiplier : 1f);
            Vector3 eye = Eye;
            Vector3 target = PlayerTarget.Eye;
            Vector3 toTarget = target - eye;
            PlayerDistance = toTarget.magnitude;
            PlayerVisible = PlayerDistance <= range && InCone(toTarget, alerted ? 240f : viewAngle) && IsClear(eye, target);
            if (PlayerVisible)
            {
                LastSeenTime = Time.time;
                LastKnownPlayerPosition = PlayerTarget.Feet;
            }
        }

        bool InCone(Vector3 toTarget, float fullAngle)
        {
            toTarget.y = 0f;
            return toTarget.sqrMagnitude < 0.01f || Vector3.Angle(transform.forward, toTarget) <= fullAngle * 0.5f;
        }

        /// <summary>Fills the meter while the player is visible, drains it otherwise.</summary>
        protected void UpdateSuspicion(float multiplier)
        {
            if (PlayerVisible) Suspicion = Mathf.Min(1f, Suspicion + FillRate() * multiplier * Time.deltaTime);
            else Suspicion = Mathf.Max(0f, Suspicion - decayPerSecond * Time.deltaTime);
        }

        float FillRate()
        {
            float closeness = 1f - Mathf.Clamp01(PlayerDistance / Mathf.Max(0.1f, SightRange()));
            float rate = Mathf.Lerp(fillFar, fillNear, closeness * closeness);
            if (PlayerTarget.IsSprinting) rate *= sprintMultiplier;
            else if (PlayerTarget.IsCrouching) rate *= crouchMultiplier;
            if (PlayerTarget.InForbiddenZone) rate *= forbiddenZoneMultiplier;
            return rate;
        }

        /// <summary>No solid collider between the points except this NPC, the player and other people.</summary>
        protected bool IsClear(Vector3 from, Vector3 to) => FirstBlocker(from, to, true) == null;

        /// <summary>Nearest solid collider between the points, ignoring this NPC and the player (and people if asked).</summary>
        protected Collider FirstBlocker(Vector3 from, Vector3 to, bool ignoreCharacters)
        {
            Vector3 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.01f) return null;
            int count = Physics.RaycastNonAlloc(from, delta / length, hits, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Collider best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var col = hits[i].collider;
                if (col.transform.IsChildOf(transform) || PlayerTarget.IsPlayerCollider(col.transform)) continue;
                if (ignoreCharacters && col.GetComponentInParent<Character>() != null) continue;
                if (hits[i].distance < bestDistance)
                {
                    bestDistance = hits[i].distance;
                    best = col;
                }
            }
            return best;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.8f);
            const int segments = 16;
            Vector3 prev = eye + Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward * daySightRange;
            Gizmos.DrawLine(eye, prev);
            for (int i = 1; i <= segments; i++)
            {
                float angle = Mathf.Lerp(-viewAngle * 0.5f, viewAngle * 0.5f, i / (float)segments);
                Vector3 next = eye + Quaternion.Euler(0f, angle, 0f) * transform.forward * daySightRange;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
            Gizmos.DrawLine(eye, prev);
        }
    }
}
