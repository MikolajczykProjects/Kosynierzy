using System;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Hit points for anything that can be damaged: enemies, the radio station, the player.
    /// Damage dealers look it up with GetComponentInParent&lt;Health&gt;() on the collider they hit,
    /// so models from the art team can be dropped in as children without code changes.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [SerializeField] float maxHealth = 100f;
        [Tooltip("Destroy this GameObject when health reaches zero.")]
        [SerializeField] bool destroyOnDeath = true;
        [SerializeField] float destroyDelay = 0f;

        public float Max => maxHealth;
        public bool DestroyOnDeath { get => destroyOnDeath; set => destroyOnDeath = value; }
        public float Current { get; private set; }
        public float Normalized => maxHealth > 0f ? Current / maxHealth : 0f;
        public bool IsDead { get; private set; }

        /// <summary>amount, hit point (world), hit normal (world).</summary>
        public event Action<float, Vector3, Vector3> Damaged;
        public event Action<float> Healed;
        public event Action<Health> Died;

        /// <summary>Raised for every Health that dies, e.g. for wave counters and score.</summary>
        public static event Action<Health> AnyDied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => AnyDied = null;

        void Awake() => Current = maxHealth;

        public void TakeDamage(float amount) => TakeDamage(amount, transform.position, Vector3.up);

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Max(0f, Current - amount);
            Damaged?.Invoke(amount, hitPoint, hitNormal);
            if (Current <= 0f) Die();
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Min(maxHealth, Current + amount);
            Healed?.Invoke(amount);
        }

        /// <summary>Bring back to full health, e.g. player respawn.</summary>
        public void Revive()
        {
            IsDead = false;
            Current = maxHealth;
            Healed?.Invoke(maxHealth);
        }

        void Die()
        {
            IsDead = true;
            Died?.Invoke(this);
            AnyDied?.Invoke(this);
            if (destroyOnDeath) Destroy(gameObject, destroyDelay);
        }
    }
}
