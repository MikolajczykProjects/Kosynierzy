using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Kosynierzy
{
    /// <summary>Who a character is for the mission rules (team doc "Postac": Wrog / Policja / Cywil / Sojusznik; Worker = station staff).</summary>
    public enum CharacterType { Enemy, Police, Civilian, Ally, Worker }

    /// <summary>
    /// Every NPC: its type, the one-hit-down rule and the shared Animator parameters (Speed from the NavMeshAgent,
    /// MotionSpeed 1, Grounded true — only parameters the controller has are set). When downed: AI, NavMeshAgent and
    /// body colliders go off; the Animator gets "Trafiony", otherwise DeathEffect (or a simple fall) topples the body,
    /// which stays on the ground. A player bullet hitting anyone but an enemy fails the mission (ShotInnocent).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public class Character : MonoBehaviour
    {
        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int MotionSpeedId = Animator.StringToHash("MotionSpeed");
        static readonly int GroundedId = Animator.StringToHash("Grounded");
        static readonly int HitId = Animator.StringToHash("Trafiony");
        static readonly int AimId = Animator.StringToHash("Celuje");
        static readonly int HandsUpId = Animator.StringToHash("RecePodniesione");

        [SerializeField] CharacterType type = CharacterType.Civilian;
        [Tooltip("HUD name under the crosshair; empty = default for the type (\"SD\", \"Cywil — nie strzelaj\", …).")]
        [SerializeField] string labelOverride = "";

        readonly Dictionary<int, AnimatorControllerParameterType> animParams = new();
        bool paramsCached;
        int cacheTries;

        /// <summary>Raised for every character that goes down (any type).</summary>
        public static event Action<Character> AnyDowned;
        public event Action<Character> Downed;

        public CharacterType Type { get => type; set => type = value; }
        /// <summary>Shooting this character fails the mission.</summary>
        public bool IsInnocent => type != CharacterType.Enemy;
        /// <summary>A legitimate target (only SD).</summary>
        public bool IsHostile => type == CharacterType.Enemy;
        /// <summary>Polish name the HUD shows when the crosshair is on this character.</summary>
        public string Label
        {
            get
            {
                if (!string.IsNullOrEmpty(labelOverride)) return labelOverride;
                return type switch
                {
                    CharacterType.Enemy => "SD",
                    CharacterType.Police => "Schupo — nie strzelaj",
                    CharacterType.Civilian => "Cywil — nie strzelaj",
                    CharacterType.Worker => "Pracownik stacji — nie strzelaj",
                    _ => "Sojusznik — nie strzelaj",
                };
            }
            set => labelOverride = value;
        }
        public bool IsDown { get; private set; }
        /// <summary>Downed by a silent takedown (set just before the kill).</summary>
        public bool SilentKill { get; private set; }
        public Health Health { get; private set; }
        public NavMeshAgent Agent { get; private set; }
        public Animator Anim { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => AnyDowned = null;

        void Awake()
        {
            Health = GetComponent<Health>();
            NpcFactory.ConfigureOneHit(Health);
            Agent = GetComponent<NavMeshAgent>();
            Anim = GetComponentInChildren<Animator>();
        }

        void OnEnable()
        {
            Health.Died += OnDied;
            ProjectileSystem.AnyHit += OnAnyHit;
        }

        void OnDisable()
        {
            Health.Died -= OnDied;
            ProjectileSystem.AnyHit -= OnAnyHit;
        }

        void Update()
        {
            if (IsDown || Anim == null || Agent == null || !Agent.isActiveAndEnabled) return;
            SetAnimFloat(SpeedId, Agent.velocity.magnitude);
            SetAnimFloat(MotionSpeedId, 1f);
            SetAnimBool(GroundedId, true);
        }

        // ---------- Animator (only parameters that exist) ----------

        public bool HasAnimParam(string paramName)
        {
            if (!AnimReady()) return false;
            CacheParams();
            return animParams.ContainsKey(Animator.StringToHash(paramName));
        }

        public void SetAnimBool(string paramName, bool value) => SetAnimBool(Animator.StringToHash(paramName), value);
        public void SetAnimFloat(string paramName, float value) => SetAnimFloat(Animator.StringToHash(paramName), value);
        public void SetAnimTrigger(string paramName) => SetAnimTrigger(Animator.StringToHash(paramName));

        public void SetAnimBool(int id, bool value)
        {
            if (Has(id, AnimatorControllerParameterType.Bool)) Anim.SetBool(id, value);
        }

        public void SetAnimFloat(int id, float value)
        {
            if (Has(id, AnimatorControllerParameterType.Float)) Anim.SetFloat(id, value);
        }

        public void SetAnimTrigger(int id)
        {
            if (Has(id, AnimatorControllerParameterType.Trigger)) Anim.SetTrigger(id);
        }

        bool AnimReady() => Anim != null && Anim.isActiveAndEnabled && Anim.runtimeAnimatorController != null;

        bool Has(int id, AnimatorControllerParameterType kind)
        {
            if (!AnimReady()) return false;
            CacheParams();
            return animParams.TryGetValue(id, out var have) && have == kind;
        }

        void CacheParams()
        {
            if (paramsCached) return;
            animParams.Clear();
            foreach (var p in Anim.parameters) animParams[p.nameHash] = p.type;
            // Retry for a while if the Animator is not initialised yet (parameters still empty).
            paramsCached = animParams.Count > 0 || ++cacheTries > 30;
        }

        // ---------- Rules ----------

        /// <summary>The next death is a silent takedown (no shot, no noise).</summary>
        public void MarkSilentKill() => SilentKill = true;

        void OnAnyHit(HitResult hit)
        {
            if (hit.FromPlayer && IsInnocent && hit.Health != null && hit.Health == Health)
                MissionFail.Trigger(MissionFail.ShotInnocent);
        }

        void OnDied(Health _)
        {
            if (IsDown) return;
            IsDown = true;

            foreach (var brain in GetComponents<NpcBrain>())
            {
                brain.NotifyDown();
                brain.enabled = false;
            }

            if (Agent != null)
            {
                if (Agent.isActiveAndEnabled && Agent.isOnNavMesh)
                {
                    Agent.isStopped = true;
                    Agent.ResetPath();
                }
                Agent.enabled = false;
            }

            foreach (var c in GetComponentsInChildren<Collider>())
                if (!c.isTrigger) c.enabled = false;

            SetAnimFloat(SpeedId, 0f);
            SetAnimBool(AimId, false);
            SetAnimBool(HandsUpId, false);

            if (Has(HitId, AnimatorControllerParameterType.Trigger)) Anim.SetTrigger(HitId);
            else
            {
                var death = GetComponent<DeathEffect>();
                if (death == null || !death.enabled) StartCoroutine(Fall());
            }

            var body = GetComponent<Body>();
            if (body == null) body = gameObject.AddComponent<Body>();
            body.Init(type, SilentKill);

            Downed?.Invoke(this);
            AnyDowned?.Invoke(this);
        }

        /// <summary>Fallback topple for objects with neither a "Trafiony" animation nor a DeathEffect.</summary>
        IEnumerator Fall()
        {
            Quaternion start = transform.rotation;
            Quaternion end = Quaternion.AngleAxis(85f, transform.right) * start;
            for (float t = 0f; t < 0.45f; t += Time.deltaTime)
            {
                float k = t / 0.45f;
                transform.rotation = Quaternion.Slerp(start, end, k * k);
                yield return null;
            }
            transform.rotation = end;
        }
    }
}
