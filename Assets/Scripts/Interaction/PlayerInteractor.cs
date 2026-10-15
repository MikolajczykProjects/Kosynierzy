using UnityEngine;
using UnityEngine.InputSystem;

namespace Kosynierzy
{
    /// <summary>
    /// E / gamepad Y uses an <see cref="Interactable"/>: the one under the crosshair (up to 3 m), otherwise
    /// the nearest one within 2 m (team doc). Put it on the player's camera.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] float reach = 3f;
        [SerializeField] float nearbyRadius = 2f;
        [SerializeField] LayerMask mask = ~0;

        InputAction interactAction;
        readonly Collider[] overlap = new Collider[32];

        public Interactable CurrentTarget { get; private set; }

        void Awake()
        {
            interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
            interactAction.AddBinding("<Gamepad>/buttonNorth");
        }

        void OnEnable() => interactAction.Enable();
        void OnDisable() => interactAction.Disable();
        void OnDestroy() => interactAction.Dispose();

        void Update()
        {
            CurrentTarget = null;
            if (GameState.IsPaused || Dialog.BlocksPlayer || HistoryCardView.IsOpen || MissionFail.IsFailed) return;

            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, reach, mask, QueryTriggerInteraction.Collide))
            {
                var target = hit.collider.GetComponentInParent<Interactable>();
                if (target != null && target.CanInteract) CurrentTarget = target;
            }
            if (CurrentTarget == null) CurrentTarget = Nearest();

            if (CurrentTarget != null && interactAction.WasPressedThisFrame())
                CurrentTarget.Interact();
        }

        Interactable Nearest()
        {
            Vector3 feet = transform.position + Vector3.down * 1f;
            int n = Physics.OverlapSphereNonAlloc(feet, nearbyRadius + 0.6f, overlap, mask, QueryTriggerInteraction.Collide);
            Interactable best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var it = overlap[i].GetComponentInParent<Interactable>();
                if (it == null || !it.CanInteract) continue;
                float d = Vector3.Distance(transform.position, overlap[i].ClosestPoint(transform.position));
                if (d <= nearbyRadius && d < bestDist) { bestDist = d; best = it; }
            }
            return best;
        }
    }
}
