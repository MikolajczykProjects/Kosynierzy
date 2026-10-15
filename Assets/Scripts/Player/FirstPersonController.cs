using UnityEngine;
using UnityEngine.InputSystem;

namespace Kosynierzy
{
    /// <summary>
    /// First-person movement: walk (WASD / left stick), sprint (Left Shift / left stick press),
    /// jump (Space / south button), look (mouse / right stick).
    /// Esc frees the cursor, left click captures it again.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] float walkSpeed = 4f;
        [SerializeField] float sprintSpeed = 7f;
        [Tooltip("How quickly speed reaches its target; higher = snappier.")]
        [SerializeField] float acceleration = 12f;
        [SerializeField] float jumpHeight = 1.1f;
        [SerializeField] float gravity = -20f;

        [Header("Look")]
        [Tooltip("Camera transform; found among children when empty.")]
        [SerializeField] Transform cameraPivot;
        [Tooltip("Degrees per pixel of mouse movement.")]
        [SerializeField] float mouseSensitivity = 0.1f;
        [Tooltip("Degrees per second at full right-stick tilt.")]
        [SerializeField] float stickSensitivity = 180f;
        [SerializeField] float pitchLimit = 85f;

        CharacterController controller;
        InputAction moveAction, lookMouseAction, lookStickAction, sprintAction, jumpAction;
        Vector3 horizontalVelocity;
        float verticalVelocity;
        float pitch;

        public bool IsSprinting { get; private set; }
        public bool IsGrounded => controller.isGrounded;
        public float CurrentSpeed => horizontalVelocity.magnitude;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (cameraPivot == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraPivot = cam.transform;
            }
            CreateActions();
        }

        void CreateActions()
        {
            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");

            lookMouseAction = new InputAction("LookMouse", InputActionType.Value, "<Mouse>/delta");
            lookStickAction = new InputAction("LookStick", InputActionType.Value, "<Gamepad>/rightStick");

            sprintAction = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            sprintAction.AddBinding("<Gamepad>/leftStickPress");

            jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");
        }

        InputAction[] AllActions => new[] { moveAction, lookMouseAction, lookStickAction, sprintAction, jumpAction };

        void OnEnable()
        {
            foreach (var a in AllActions) a.Enable();
            SetCursorLocked(true);
        }

        void OnDisable()
        {
            foreach (var a in AllActions) a.Disable();
            SetCursorLocked(false);
        }

        void OnDestroy()
        {
            foreach (var a in AllActions) a.Dispose();
        }

        void Update()
        {
            HandleCursor();
            Look();
            Move();
        }

        void HandleCursor()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) SetCursorLocked(false);

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
                SetCursorLocked(true);
        }

        void Look()
        {
            Vector2 delta = lookStickAction.ReadValue<Vector2>() * (stickSensitivity * Time.deltaTime);
            if (Cursor.lockState == CursorLockMode.Locked)
                delta += lookMouseAction.ReadValue<Vector2>() * mouseSensitivity;

            transform.Rotate(0f, delta.x, 0f);
            pitch = Mathf.Clamp(pitch - delta.y, -pitchLimit, pitchLimit);
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void Move()
        {
            Vector2 input = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
            IsSprinting = sprintAction.IsPressed() && input.y > 0.1f;

            float targetSpeed = IsSprinting ? sprintSpeed : walkSpeed;
            Vector3 targetVelocity = (transform.right * input.x + transform.forward * input.y) * targetSpeed;
            float blend = 1f - Mathf.Exp(-acceleration * Time.deltaTime);
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, blend);

            if (controller.isGrounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = -2f; // keeps the controller pressed to slopes
                if (jumpAction.WasPressedThisFrame()) verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            verticalVelocity += gravity * Time.deltaTime;

            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
