using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Door into an interior that sits far from the city in the same map prefab: E → door animation
    /// (Animator trigger "Otworz" on the OB_Drzwi model, if any) → 0.3 s black fade → player moved to the
    /// target point (name from the contract, e.g. "spawn_hotel_wejscie") facing its +Z.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class DoorTeleport : MonoBehaviour
    {
        [SerializeField] string targetPoint;
        [Tooltip("Used when the target point is missing or for exits: offset in front of this door.")]
        [SerializeField] Transform targetOverride;
        [SerializeField] bool locked;
        [SerializeField] string lockedMessage = "Zamknięte";

        Interactable interactable;

        public string TargetPoint => targetPoint;
        public bool Locked { get => locked; set { locked = value; UpdatePrompt(); } }

        public void Setup(string target, string prompt = "Wejdź", Transform overrideTarget = null)
        {
            targetPoint = target;
            targetOverride = overrideTarget;
            GetComponent<Interactable>().SetPrompt(prompt);
        }

        void Awake()
        {
            interactable = GetComponent<Interactable>();
            interactable.Interacted += OnUse;
            UpdatePrompt();
        }

        void OnDestroy()
        {
            if (interactable != null) interactable.Interacted -= OnUse;
        }

        void UpdatePrompt()
        {
            if (interactable == null) return;
            if (locked) interactable.SetPrompt(lockedMessage);
        }

        void OnUse(Interactable _)
        {
            if (locked) return;
            // OB_Drzwi (AC_Drzwi): trigger "Otworz" swings the door open and closes it again after 1 s. Guarded write.
            AnimParams.SetTrigger(GetComponentInChildren<Animator>(), "Otworz");
            SfxBank.Play(SfxBank.Sfx.Pickup);

            if (targetOverride == null && !Points.Has(targetPoint))
            {
                Debug.LogWarning($"Kosynierzy: drzwi prowadzą do nieistniejącego punktu '{targetPoint}' — zostajesz na miejscu.");
                return;
            }
            Vector3 pos;
            float yaw;
            if (targetOverride != null) { pos = targetOverride.position; yaw = targetOverride.eulerAngles.y; }
            else { pos = Points.Position(targetPoint); yaw = Points.Rotation(targetPoint).eulerAngles.y; }

            bool leavingInterior = transform.position.x >= 1900f && pos.x < 1900f;
            ScreenFade.Transition(() =>
            {
                var player = Object.FindAnyObjectByType<FirstPersonController>();
                if (player != null) player.Teleport(pos + Vector3.up * 0.05f, yaw);
                if (leavingInterior) Mission.TryTravelAfterExit();
            });
        }
    }
}
