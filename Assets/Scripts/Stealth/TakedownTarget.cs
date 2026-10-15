using System.Collections;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Silent bayonet takedown on an SD. While the player stands within 1.6 m behind the soldier (inside 70° of its back),
    /// the soldier is not alarmed and does not see the player, the Interactable on it is enabled with the prompt
    /// "Zlikwiduj po cichu (bagnet)". Using it plays PlayerHands.PlayStab(), freezes the soldier and kills it 0.25 s later
    /// without a shot: no gunshot noise, no ProjectileSystem hit (so it never counts as shooting anyone). The body stays.
    /// </summary>
    [DisallowMultipleComponent]
    public class TakedownTarget : MonoBehaviour
    {
        const string Prompt = "Zlikwiduj po cichu (bagnet)";

        [Tooltip("Max distance from the soldier to the player (flat).")]
        [SerializeField] float reach = 1.9f;
        [Tooltip("Max angle between the soldier's back and the direction to the player.")]
        [SerializeField] float maxBackAngle = 70f;
        [SerializeField] float killDelay = 0.25f;

        WatcherBrain enemy;
        Character character;
        Interactable interactable;
        bool busy;

        void Awake()
        {
            enemy = GetComponent<WatcherBrain>();
            character = GetComponent<Character>();
            interactable = GetComponent<Interactable>();
            if (interactable == null) interactable = gameObject.AddComponent<Interactable>();
            interactable.SetPrompt(Prompt);
            interactable.SetSingleUse(false);
            interactable.enabled = false;
        }

        void OnEnable()
        {
            if (interactable != null) interactable.Interacted += OnInteracted;
        }

        void OnDisable()
        {
            if (interactable == null) return;
            interactable.Interacted -= OnInteracted;
            interactable.enabled = false;
        }

        void Update()
        {
            bool can = !busy && CanBeTakenDown();
            if (interactable.enabled != can) interactable.enabled = can;
        }

        /// <summary>The player is right behind an unaware soldier.</summary>
        public bool CanBeTakenDown()
        {
            if (character != null && character.IsDown) return false;
            if (enemy == null || !enemy.isActiveAndEnabled || enemy.IsAlerted || enemy.PlayerVisible) return false;
            if (PlayerTarget.IsGone) return false;
            Vector3 toPlayer = PlayerTarget.Feet - transform.position;
            if (Mathf.Abs(toPlayer.y) > 1.2f) return false;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > reach * reach) return false;
            return toPlayer.sqrMagnitude < 0.0001f || Vector3.Angle(-transform.forward, toPlayer) < maxBackAngle;
        }

        void OnInteracted(Interactable _)
        {
            if (busy || !CanBeTakenDown()) return;
            busy = true;
            interactable.enabled = false;
            if (enemy is EnemyController soldier) soldier.BeginTakedown();
            else if (enemy != null) enemy.enabled = false; // Schupo: stop thinking until the kill lands
            if (character != null) character.MarkSilentKill(); // before the stab: PlayerHands may land the hit itself
            var hands = PlayerHands.Instance;
            if (hands != null) hands.PlayStab();
            StartCoroutine(Kill());
        }

        IEnumerator Kill()
        {
            yield return new WaitForSeconds(killDelay);
            CameraShake.AddTrauma(0.15f);
            var health = GetComponent<Health>();
            // Hit normal = -forward: the body falls forward, away from the player behind it.
            if (health != null && !health.IsDead) health.TakeDamage(999f, transform.position + Vector3.up * 1.2f, -transform.forward);
        }
    }
}
