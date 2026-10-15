using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// NPC that watches for the player (SD, Schupo) and feeds Detection: a sight cone with line of sight from the eyes
    /// (1.6 m) to the player's camera (triggers, itself and other people ignored), day / night / lamp-light sight ranges,
    /// hiding spots, hearing through Noise.Emitted (quiet sounds only along a short walking path) and bodies in view.
    /// The "?" meter (0..1) fills Hitman-style: faster when the player is close, lit, moving fast or in a ForbiddenZone
    /// (3x); slower crouched, in darkness or far away; it drains while the player is out of sight.
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
        [Tooltip("Sight range multiplier for a player crouching in darkness.")]
        [SerializeField] float darkCrouchMultiplier = 0.65f;
        [Tooltip("Once alarmed: sight range multiplier; the cone also widens to 240° (tracking a known target).")]
        [SerializeField] float alertedRangeMultiplier = 1.5f;
        [Tooltip("An alarmed NPC still sees a player in a hiding spot closer than this.")]
        [SerializeField] float hidingSpotRevealDistance = 2.5f;

        [Header("Suspicion '?' meter (fill per second)")]
        [Tooltip("Fill speed with the player at the edge of sight range (before exposure multipliers).")]
        float fillFar = 0.15f;
        [Tooltip("Fill speed with the player right in front (before exposure multipliers).")]
        float fillNear = 1.0f;
        [Tooltip("Multiplier at Detection.Visibility01 = 0 (hidden, dark, crouched, still).")]
        float hiddenMultiplier = 0.25f;
        [Tooltip("Multiplier at Detection.Visibility01 = 1 (lit, standing, moving).")]
        float exposedMultiplier = 1.0f;
        [SerializeField] float sprintMultiplier = 1.3f;
        [SerializeField] float forbiddenZoneMultiplier = 3f;
        [Tooltip("Meter drop per second while the player is out of sight.")]
        [SerializeField] float decayPerSecond = 0.15f;

        float perceptionTimer;
        static float nextDiagLog;

        /// <summary>The "?" meter, 0..1 (1 = alarm / report).</summary>
        public float Suspicion { get; protected set; }
        public bool PlayerVisible { get; private set; }
        public float PlayerDistance { get; private set; } = float.MaxValue;
        public Vector3 LastKnownPlayerPosition { get; protected set; }
        public float LastSeenTime { get; private set; } = -999f;
        /// <summary>Red/blue "!" above the head.</summary>
        public abstract bool IsAlerted { get; }
        /// <summary>"?" with the meter above the head.</summary>
        public abstract bool ShowSuspicion { get; }
        /// <summary>Level reported to Detection (HUD).</summary>
        public abstract AwarenessLevel Awareness { get; }
        public virtual bool IsPolice => false;

        protected override Vector3 Eye => transform.position + Vector3.up * eyeHeight;

        protected override void Awake()
        {
            base.Awake();
            perceptionTimer = Random.Range(0f, PerceptionInterval); // spread raycasts across frames
            LastKnownPlayerPosition = transform.position;
        }

        protected virtual void OnEnable()
        {
            Noise.Emitted += HandleNoise;
            Detection.Register(this);
        }

        protected virtual void OnDisable()
        {
            Noise.Emitted -= HandleNoise;
            Detection.Unregister(this);
            PlayerVisible = false;
        }

        void HandleNoise(Vector3 position, float radius, NoiseKind kind, Transform source)
        {
            if (IsDown || !isActiveAndEnabled) return;
            if ((position - transform.position).sqrMagnitude > radius * radius) return;
            if (source != null && source.IsChildOf(transform)) return;
            // Sneaking (crouched steps, radius 1.5 m) is silent, so a takedown from behind stays possible.
            if (kind == NoiseKind.Footstep && radius < 2f) return;
            // Gunshots carry everywhere; steps and the like only along a short walking path.
            if (kind != NoiseKind.Gunshot && !HeardAlongPath(position, radius)) return;
            OnHeard(position, kind, source);
        }

        /// <summary>A noise reached this NPC.</summary>
        protected abstract void OnHeard(Vector3 position, NoiseKind kind, Transform source);

        /// <summary>A body came into view (each body once per NPC).</summary>
        protected abstract void OnBodySeen(Body body);

        /// <summary>Current sight range: day 18 m; night 10 m in lamp light, 6 m outside it (less when crouched).</summary>
        public float SightRange()
        {
            if (!LightZones.IsNight) return daySightRange;
            if (LightZones.PlayerLit) return nightSightRange;
            return darkSightRange * (PlayerTarget.IsCrouching ? darkCrouchMultiplier : 1f);
        }

        /// <summary>Call every frame; refreshes PlayerVisible about 7x per second and looks for bodies 2x per second.</summary>
        protected void UpdateSenses(bool alerted)
        {
            if (PlayerTarget.IsGone)
            {
                PlayerVisible = false;
                return;
            }

            var body = SpotBody(LightZones.IsNight ? nightSightRange : daySightRange, darkSightRange, viewAngle);
            if (body != null) OnBodySeen(body);

            perceptionTimer -= Time.deltaTime;
            if (perceptionTimer > 0f) return;
            perceptionTimer = PerceptionInterval;

            float range = SightRange() * (alerted ? alertedRangeMultiplier : 1f);
            Vector3 eye = Eye;
            Vector3 target = PlayerTarget.Eye;
            Vector3 toTarget = target - eye;
            PlayerDistance = toTarget.magnitude;
            if (PlayerDistance < 25f && Time.unscaledTime > nextDiagLog && Application.isEditor)
            {
                nextDiagLog = Time.unscaledTime + 1f;
                Debug.Log($"[Detect] {name} dist={PlayerDistance:0.0} range={range:0} cone={InCone(toTarget, alerted ? 240f : viewAngle)} clear={IsClear(eye, target)} hiding={Detection.InHidingSpot} susp={Suspicion:0.00} fill={FillRate():0.00}/s vis01={Detection.Visibility01:0.00}");
            }
            bool visible = PlayerDistance <= range && InCone(toTarget, alerted ? 240f : viewAngle) && IsClear(eye, target);
            if (visible && Detection.InHidingSpot && !(alerted && PlayerDistance < hidingSpotRevealDistance)) visible = false;
            PlayerVisible = visible;
            if (visible)
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
            float rate = Mathf.Lerp(fillFar, fillNear, closeness); // ~2.3 s to full at 10 m in plain view; slower far, crouched, in shadow
            if (PlayerTarget.WeaponDrawn) rate *= 1.6f;
            rate *= Mathf.Lerp(hiddenMultiplier, exposedMultiplier, Detection.Visibility01);
            if (PlayerTarget.IsSprinting) rate *= sprintMultiplier;
            if (PlayerTarget.InForbiddenZone) rate *= forbiddenZoneMultiplier;
            return rate;
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
