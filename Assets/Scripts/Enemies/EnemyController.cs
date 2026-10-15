using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Kosynierzy
{
    /// <summary>States of an SD soldier.</summary>
    public enum EnemyState { Patrol, Suspicious, Search, Alarm, Down }

    /// <summary>
    /// SD soldier. Patrol (route, wander around the start or stand guard) → Suspicious (stops and stares while the
    /// "?" meter fills) → Search (walks to the last known position, looks around, gives up after ~8 s) → Alarm (chases
    /// to firing range, shoots real bullets after a 0.6–1.2 s reaction, calls every SD within 25 m). Hears Noise:
    /// steps/running make it suspicious toward the sound, a gunshot sends it to Alarm (near) or Search (far).
    /// Barks bark_sd_podejrzenie / bark_sd_alarm / bark_sd_szukanie; Animator "Celuje" while aiming, "Strzal" per shot.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyController : WatcherBrain
    {
        static readonly List<EnemyController> active = new();
        static readonly int AimId = Animator.StringToHash("Celuje");
        static readonly int ShotId = Animator.StringToHash("Strzal");

        [Header("Patrol")]
        [Tooltip("Patrol route; empty = wander around the start (Wander Radius) or stand guard (0).")]
        [SerializeField] Transform[] waypoints = new Transform[0];
        [Tooltip("Without a route: radius of random walks around the start position; 0 = stand guard.")]
        [SerializeField] float wanderRadius;
        [Tooltip("Seconds to wait at each patrol stop (min, max).")]
        [SerializeField] Vector2 waypointWait = new(1.5f, 4f);
        [SerializeField] float walkSpeed = 1.6f;
        [SerializeField] float searchSpeed = 2.4f;
        [SerializeField] float runSpeed = 4.2f;

        [Header("Alarm")]
        [Tooltip("Every SD within this distance joins the alarm.")]
        [SerializeField] float callRadius = 25f;
        [Tooltip("Search: seconds of looking around at the last known position before going back to patrol.")]
        [SerializeField] float searchDuration = 8f;
        [Tooltip("Alarm: seconds without any contact (sight or sound) before dropping to Search.")]
        [SerializeField] float alarmMemory = 12f;
        [Tooltip("A gunshot closer than this goes straight to Alarm; farther ones start a Search.")]
        [SerializeField] float gunshotAlarmDistance = 15f;

        [Header("Rifle")]
        [SerializeField] float fireRange = 15f;
        [Tooltip("Reaction from first seeing the player in Alarm to the first shot (min, max seconds).")]
        [SerializeField] Vector2 firstShotDelay = new(0.6f, 1.2f);
        [SerializeField] Vector2 shotInterval = new(1.2f, 1.8f);
        [Tooltip("Chance a shot is aimed on target at 3 m.")]
        [SerializeField, Range(0f, 1f)] float hitChanceNear = 0.55f;
        [Tooltip("Chance a shot is aimed on target at fire range.")]
        [SerializeField, Range(0f, 1f)] float hitChanceFar = 0.12f;
        [Tooltip("Hit chance multiplier while the player sprints or slides.")]
        [SerializeField, Range(0f, 1f)] float sprintAccuracy = 0.6f;
        [Tooltip("Hit chance multiplier while the player crouches behind cover (line to 1.0 m height blocked).")]
        [SerializeField, Range(0f, 1f)] float coverAccuracy = 0.2f;
        [SerializeField] float bulletSpeed = 160f;
        [Tooltip("Optional muzzle; defaults to chest height in front of the soldier.")]
        [SerializeField] Transform muzzle;
        [SerializeField] Color tracerColor = new(1f, 0.72f, 0.35f, 1f);

        /// <summary>Raised for every SD that goes down (quests, score).</summary>
        public static event Action<EnemyController> AnyEnemyKilled;
        /// <summary>Raised when an SD enters Alarm (HUD, music).</summary>
        public static event Action<EnemyController> AnyAlarm;
        /// <summary>Enabled, living soldiers.</summary>
        public static IReadOnlyList<EnemyController> All => active;

        public EnemyState State { get; private set; }
        public bool IsAiming { get; private set; }
        public Health Health => character != null ? character.Health : GetComponent<Health>();
        public override bool IsAlerted => State == EnemyState.Alarm;
        public override bool ShowSuspicion =>
            State == EnemyState.Suspicious || State == EnemyState.Search || Suspicion > 0.02f;
        public override AwarenessLevel Awareness => State switch
        {
            EnemyState.Suspicious => AwarenessLevel.Suspicious,
            EnemyState.Search => AwarenessLevel.Searching,
            EnemyState.Alarm => AwarenessLevel.Alarmed,
            _ => AwarenessLevel.Unaware,
        };
        /// <summary>Frozen by a silent takedown in progress.</summary>
        public bool IsBeingTakenDown { get; private set; }

        protected override CharacterType DefaultType => CharacterType.Enemy;

        readonly List<Vector3> route = new();
        Vector3 investigatePoint;
        float stateStart, lastStimulus, searchArrived = -1f, lookBaseYaw, alarmContact, nextShotTime;
        float alertness = 1f;
        bool heardNoise, hasSight, alarmArrived;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            active.Clear();
            AnyEnemyKilled = null;
            AnyAlarm = null;
        }

        protected override void Awake()
        {
            base.Awake();
            foreach (var w in waypoints)
                if (w != null) route.Add(w.position);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!active.Contains(this)) active.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            active.Remove(this);
            IsAiming = false;
        }

        /// <summary>Patrol between these points in order (looping).</summary>
        public void SetRoute(IList<Vector3> points)
        {
            route.Clear();
            if (points != null) route.AddRange(points);
            ResetRoamIndex();
            ResetRoam();
        }

        /// <summary>No route: random walks within radius of the start position (0 = stand guard).</summary>
        public void SetWander(float radius)
        {
            route.Clear();
            wanderRadius = Mathf.Max(0f, radius);
            ResetRoamIndex();
            ResetRoam();
        }

        /// <summary>Puts this soldier into Alarm with the player's last known position (Schupo report, act 4).</summary>
        public void RaiseAlarm(Vector3 lastKnownPlayerPosition, bool callOthers = false)
        {
            if (IsDown || !isActiveAndEnabled) return;
            EnterAlarm(lastKnownPlayerPosition, callOthers);
        }

        /// <summary>Walk over and search here (a body was found, a Schupo reported something).</summary>
        public void Investigate(Vector3 position)
        {
            if (IsDown || !isActiveAndEnabled || State == EnemyState.Alarm) return;
            alertness = 1.5f;
            heardNoise = true;
            Suspicion = Mathf.Max(Suspicion, 0.4f);
            EnterSearch(position, "bark_sd_szukanie");
        }

        /// <summary>Called by TakedownTarget: stop moving, seeing and shooting until the kill lands.</summary>
        public void BeginTakedown()
        {
            IsBeingTakenDown = true;
            IsAiming = false;
            Halt();
            character.SetAnimBool(AimId, false);
        }

        void Update()
        {
            if (IsDown || IsBeingTakenDown) return;
            UpdateSenses(State == EnemyState.Alarm);
            switch (State)
            {
                case EnemyState.Patrol: UpdatePatrol(); break;
                case EnemyState.Suspicious: UpdateSuspicious(); break;
                case EnemyState.Search: UpdateSearch(); break;
                case EnemyState.Alarm: UpdateAlarm(); break;
            }
            character.SetAnimBool(AimId, IsAiming);
        }

        // ---------- Patrol ----------

        void UpdatePatrol()
        {
            UpdateSuspicion(alertness);
            if (PlayerVisible && Suspicion > 0.15f)
            {
                EnterSuspicious(LastKnownPlayerPosition);
                return;
            }
            Roam(route, false, wanderRadius, walkSpeed, waypointWait);
        }

        void ReturnToPatrol()
        {
            State = EnemyState.Patrol;
            Suspicion = 0f;
            alertness = 1f;
            heardNoise = false;
            IsAiming = false;
            ResetRoam();
        }

        // ---------- Suspicious ----------

        void EnterSuspicious(Vector3 point)
        {
            State = EnemyState.Suspicious;
            investigatePoint = point;
            stateStart = lastStimulus = Time.time;
            IsAiming = false;
            Halt();
            Bark("bark_sd_podejrzenie");
        }

        void UpdateSuspicious()
        {
            UpdateSuspicion(alertness);
            if (PlayerVisible)
            {
                investigatePoint = LastKnownPlayerPosition;
                lastStimulus = Time.time;
            }
            if (Suspicion >= 1f)
            {
                EnterAlarm(LastKnownPlayerPosition, true);
                return;
            }

            // Hitman-style: a short stare, then walk over for a closer look while the meter keeps filling.
            bool staring = Time.time - stateStart < 1f;
            if (!staring && FlatDistanceTo(investigatePoint) > 2.5f) MoveTo(investigatePoint, walkSpeed * 0.9f);
            else
            {
                Halt();
                FaceTowards(investigatePoint, 240f);
            }

            if (Time.time - lastStimulus > 3f)
            {
                if (Suspicion > 0.25f || heardNoise) EnterSearch(investigatePoint);
                else ReturnToPatrol();
            }
        }

        // ---------- Search ----------

        void EnterSearch(Vector3 point, string bark = "bark_sd_szukanie")
        {
            State = EnemyState.Search;
            investigatePoint = point;
            stateStart = Time.time;
            searchArrived = -1f;
            IsAiming = false;
            Bark(bark, "bark_sd_szukanie");
        }

        protected override void OnBodySeen(Body body)
        {
            if (State == EnemyState.Alarm) return;
            body.MarkDiscovered();
            alertness = 1.5f;
            heardNoise = true;
            Suspicion = Mathf.Max(Suspicion, 0.5f);
            EnterSearch(body.Position, "bark_sd_cialo");
            // Comrades nearby come to look too.
            float r2 = callRadius * callRadius * 0.36f;
            for (int i = 0; i < active.Count; i++)
            {
                var other = active[i];
                if (other == this || other.IsDown) continue;
                if ((other.transform.position - transform.position).sqrMagnitude <= r2) other.Investigate(body.Position);
            }
        }

        void UpdateSearch()
        {
            UpdateSuspicion(alertness);
            if (PlayerVisible)
            {
                if (Suspicion >= 1f) EnterAlarm(LastKnownPlayerPosition, true);
                else EnterSuspicious(LastKnownPlayerPosition);
                return;
            }

            if (searchArrived < 0f)
            {
                if (Arrived(investigatePoint, 1.2f) || Time.time - stateStart > 20f)
                {
                    searchArrived = Time.time;
                    lookBaseYaw = transform.eulerAngles.y;
                    Halt();
                }
                else MoveTo(investigatePoint, searchSpeed);
                return;
            }

            float t = Time.time - searchArrived;
            TurnTo(Quaternion.Euler(0f, lookBaseYaw + Mathf.Sin(t * 1.2f) * 80f, 0f), 160f);
            if (t >= searchDuration) ReturnToPatrol();
        }

        // ---------- Alarm ----------

        void EnterAlarm(Vector3 lastKnown, bool callOthers)
        {
            if (!PlayerVisible) LastKnownPlayerPosition = lastKnown;
            alarmContact = Time.time;
            alarmArrived = false;
            Suspicion = 1f;
            if (State == EnemyState.Alarm) return;

            State = EnemyState.Alarm;
            hasSight = false;
            Bark("bark_sd_alarm");
            AnyAlarm?.Invoke(this);
            if (callOthers) CallNearby();
        }

        void CallNearby()
        {
            float r2 = callRadius * callRadius;
            for (int i = 0; i < active.Count; i++)
            {
                var other = active[i];
                if (other == this || other.IsDown) continue;
                if ((other.transform.position - transform.position).sqrMagnitude <= r2)
                    other.RaiseAlarm(LastKnownPlayerPosition, false);
            }
        }

        void UpdateAlarm()
        {
            Suspicion = 1f;
            if (PlayerTarget.IsGone)
            {
                IsAiming = false;
                Halt();
                return;
            }

            if (PlayerVisible)
            {
                alarmContact = Time.time;
                alarmArrived = false;
                if (!hasSight)
                {
                    hasSight = true;
                    nextShotTime = Time.time + UnityEngine.Random.Range(firstShotDelay.x, firstShotDelay.y);
                }

                Vector3 playerPos = PlayerTarget.Feet;
                if (FlatDistanceTo(playerPos) <= fireRange)
                {
                    Halt();
                    FaceTowards(playerPos, 540f);
                    IsAiming = true;
                    if (Time.time >= nextShotTime && IsFacing(playerPos, 20f))
                        nextShotTime = Time.time + (TryShoot() ? UnityEngine.Random.Range(shotInterval.x, shotInterval.y) : 0.4f);
                }
                else
                {
                    IsAiming = false;
                    MoveTo(playerPos, runSpeed);
                }
                return;
            }

            IsAiming = false;
            if (Time.time - LastSeenTime > 1f) hasSight = false; // next sighting gets a fresh reaction delay
            if (!alarmArrived)
            {
                if (Arrived(LastKnownPlayerPosition, 1.5f))
                {
                    alarmArrived = true;
                    lookBaseYaw = transform.eulerAngles.y;
                    Halt();
                }
                else MoveTo(LastKnownPlayerPosition, runSpeed);
            }
            else TurnTo(Quaternion.Euler(0f, lookBaseYaw + Mathf.Sin(Time.time * 1.5f) * 90f, 0f), 200f);

            if (Time.time - alarmContact > alarmMemory)
            {
                alertness = 1.5f;
                EnterSearch(LastKnownPlayerPosition, "bark_sd_zgubil");
            }
        }

        /// <summary>Fires one real bullet at the player's chest; false when a person stands in the line of fire.</summary>
        bool TryShoot()
        {
            Vector3 target = PlayerTarget.Chest;
            Vector3 origin = muzzle != null ? muzzle.position : Eye + transform.forward * 0.35f;
            Vector3 visual = muzzle != null
                ? muzzle.position
                : transform.position + Vector3.up * 1.4f + transform.forward * 0.45f + transform.right * 0.15f;

            var blocker = FirstBlocker(origin, target, false);
            if (blocker != null && blocker.GetComponentInParent<Character>() != null) return false;

            float distance = Vector3.Distance(origin, target);
            float chance = Mathf.Lerp(hitChanceNear, hitChanceFar, Mathf.InverseLerp(3f, fireRange, distance));
            if (PlayerTarget.IsSprinting) chance *= sprintAccuracy;
            if (PlayerTarget.IsCrouching && !IsClear(Eye, PlayerTarget.Feet + Vector3.up * 1.0f)) chance *= coverAccuracy;
            bool onTarget = UnityEngine.Random.value < chance;

            // A hit stays inside the player's capsule; a miss passes 0.6–2 m beside it (wider the farther away).
            Vector3 aim = (target - origin).normalized;
            Vector3 side = Vector3.Cross(aim, UnityEngine.Random.onUnitSphere);
            if (side.sqrMagnitude < 0.0001f) side = transform.right;
            float offset = onTarget
                ? UnityEngine.Random.Range(0f, 0.12f)
                : UnityEngine.Random.Range(0.6f, 1.4f) * (1f + 0.5f * Mathf.Clamp01(distance / fireRange));
            Vector3 direction = (target + side.normalized * offset - origin).normalized;

            ProjectileSystem.Fire(new ShotInfo
            {
                Origin = origin,
                Direction = direction,
                VisualOrigin = visual,
                Speed = bulletSpeed,
                Damage = 1f,
                GravityScale = 0f,
                MaxDistance = 120f,
                Impulse = 1.5f,
                Owner = transform,
                FromPlayer = false,
                TracerColor = tracerColor,
            });

            VfxLibrary.PlayMuzzleFlash(visual, Quaternion.LookRotation(direction));
            SfxBank.PlayAt(SfxBank.Sfx.EnemyShot, visual);
            character.SetAnimTrigger(ShotId);
            Noise.Emit(visual, 30f, NoiseKind.Gunshot, transform);
            return true;
        }

        // ---------- Hearing ----------

        protected override void OnHeard(Vector3 position, NoiseKind kind, Transform source)
        {
            bool fromPlayer = PlayerTarget.IsPlayerSource(source);

            if (kind == NoiseKind.Gunshot)
            {
                if (!fromPlayer && source != null)
                {
                    var other = source.GetComponentInParent<EnemyController>();
                    if (other != null)
                    {
                        if (other.State == EnemyState.Alarm) RaiseAlarm(other.LastKnownPlayerPosition, false);
                        return;
                    }
                }
                if (State == EnemyState.Alarm)
                {
                    NoteContact(position);
                    return;
                }
                if (PlayerVisible || FlatDistanceTo(position) <= gunshotAlarmDistance) EnterAlarm(position, false);
                else
                {
                    Suspicion = Mathf.Max(Suspicion, 0.6f);
                    heardNoise = true;
                    alertness = 1.5f;
                    EnterSearch(position);
                }
                return;
            }

            if (!fromPlayer) return;
            if (State == EnemyState.Alarm)
            {
                NoteContact(position);
                return;
            }

            // Sounds alone never raise the alarm (capped below 1) — they make the soldier look.
            heardNoise = true;
            Suspicion = Mathf.Min(0.9f, Suspicion + (kind == NoiseKind.Footstep ? 0.08f : 0.18f));
            switch (State)
            {
                case EnemyState.Patrol:
                    EnterSuspicious(position);
                    break;
                case EnemyState.Suspicious:
                    if (!PlayerVisible)
                    {
                        investigatePoint = position;
                        lastStimulus = Time.time;
                    }
                    break;
                case EnemyState.Search:
                    if (FlatDistance(investigatePoint, position) > 2f)
                    {
                        investigatePoint = position;
                        searchArrived = -1f;
                        stateStart = Time.time;
                    }
                    break;
            }
        }

        void NoteContact(Vector3 position)
        {
            if (PlayerVisible) return;
            LastKnownPlayerPosition = position;
            alarmContact = Time.time;
            alarmArrived = false;
        }

        public override void NotifyDown()
        {
            State = EnemyState.Down;
            IsAiming = false;
            Suspicion = 0f;
            AnyEnemyKilled?.Invoke(this);
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, fireRange);
            if (waypoints == null) return;
            Gizmos.color = Color.green;
            for (int i = 0; i < waypoints.Length; i++)
            {
                var a = waypoints[i];
                var b = waypoints[(i + 1) % waypoints.Length];
                if (a != null && b != null) Gizmos.DrawLine(a.position, b.position);
            }
        }
    }
}
