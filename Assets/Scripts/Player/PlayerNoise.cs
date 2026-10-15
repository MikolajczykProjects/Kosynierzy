using UnityEngine;

namespace Kosynierzy
{
    /// <summary>Emits the player's movement noise a few times per second (put on the player).</summary>
    [RequireComponent(typeof(FirstPersonController))]
    public class PlayerNoise : MonoBehaviour
    {
        [SerializeField] float walkRadius = 4f;
        [SerializeField] float runRadius = 12f;
        [SerializeField] float sneakRadius = 1.5f;
        [SerializeField] float landRadius = 6f;
        [SerializeField] float interval = 0.3f;

        FirstPersonController player;
        float next;

        /// <summary>Current movement noise radius (0 when still) — for HUD/debug.</summary>
        public float CurrentRadius { get; private set; }

        void Awake() => player = GetComponent<FirstPersonController>();
        void OnEnable() => player.Landed += OnLanded;
        void OnDisable() => player.Landed -= OnLanded;

        void OnLanded(float speed) => Noise.Emit(transform.position, landRadius, NoiseKind.Land, transform);

        void Update()
        {
            CurrentRadius = !player.IsGrounded || player.CurrentSpeed < 0.5f ? 0f
                : player.IsSprinting || player.IsSliding ? runRadius
                : player.IsCrouching ? sneakRadius : walkRadius;
            if (CurrentRadius <= 0f || Time.time < next) return;
            next = Time.time + interval;
            Noise.Emit(transform.position, CurrentRadius, CurrentRadius >= runRadius ? NoiseKind.Run : NoiseKind.Footstep, transform);
        }
    }
}
