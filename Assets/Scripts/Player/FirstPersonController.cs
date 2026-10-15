using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kosynierzy
{
    /// <summary>
    /// First-person movement and camera.
    /// Walk (WASD / left stick), sprint with stamina (Shift / L3), crouch (C or Ctrl / B),
    /// slide (crouch while sprinting), jump with coyote time and input buffer (Space / A),
    /// look (mouse / right stick). Camera: head bob, landing dip, slide tilt, sprint/slide FOV,
    /// weapon recoil and trauma shake. Settings (sensitivity, FOV, bob) come from GameSettings.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Speeds (m/s)")]
        [SerializeField] float walkSpeed = 2.5f;   // team doc: walk 1.5–2.5
        [SerializeField] float sprintSpeed = 5.5f; // team doc: run 5.5
        [SerializeField] float crouchSpeed = 2.0f; // team doc: sneak 2.0
        [Tooltip("How quickly speed reaches its target on the ground; higher = snappier.")]
        [SerializeField] float acceleration = 12f;
        [SerializeField] float airControl = 0.35f;

        [Header("Stamina")]
        [SerializeField] float maxStamina = 160f;
        [SerializeField] float sprintDrain = 16f;
        [SerializeField] float staminaRegen = 31.2f;
        [SerializeField] float regenDelay = 0.8f;
        [Tooltip("After running dry, sprint unlocks again at this much stamina.")]
        [SerializeField] float exhaustedRecover = 30f;
        [SerializeField] float jumpCost = 8f;
        [SerializeField] float slideCost = 15f;

        [Header("Jump")]
        [SerializeField] float jumpHeight = 1.1f;
        [SerializeField] float gravity = -22f;
        [SerializeField] float coyoteTime = 0.1f;
        [SerializeField] float jumpBuffer = 0.12f;

        [Header("Crouch & slide")]
        [SerializeField] float standHeight = 1.8f;
        [SerializeField] float crouchHeight = 1.1f;
        [SerializeField] float slideStartBoost = 3f;
        [SerializeField] float slideFriction = 7f;
        [SerializeField] float slideMinSpeed = 3.5f;
        [SerializeField] float slideCooldown = 0.6f;

        [Header("Look")]
        [Tooltip("Camera transform; found among children when empty.")]
        [SerializeField] Transform cameraPivot;
        [Tooltip("Degrees per pixel of mouse movement at sensitivity 1.")]
        [SerializeField] float mouseSensitivity = 0.1f;
        [Tooltip("Degrees per second at full right-stick tilt.")]
        [SerializeField] float stickSensitivity = 180f;
        [SerializeField] float pitchLimit = 85f;
        [SerializeField] float eyeHeightRatio = 0.9f;

        [Header("Camera feel")]
        [SerializeField] float bobFrequency = 1.9f;
        [SerializeField] float bobAmplitude = 0.035f;
        [SerializeField] float sprintBobMultiplier = 1.4f;
        [SerializeField] float sprintFovBoost = 8f;
        [SerializeField] float slideFovBoost = 12f;
        [SerializeField] float slideTilt = 6f;
        [SerializeField] float landingDipPerSpeed = 0.012f;
        [SerializeField] float recoilReturn = 10f;

        CharacterController controller;
        Camera cam;
        InputAction moveAction, lookMouseAction, lookStickAction, sprintAction, jumpAction, crouchAction;

        Vector3 horizontalVelocity;
        float verticalVelocity, yaw, pitch;
        float stamina, lastStaminaUse;
        bool exhausted, crouchHeld, wasGrounded;
        float lastGroundedTime = -10f, jumpPressedTime = -10f;
        float slideEndTime = -10f;
        float currentHeight, eyeHeight;
        float bobDistance, landingOffset, landingVelocity;
        float tilt, fovBoost;
        Vector2 recoil;   // x = pitch up, y = yaw, degrees; springs back to zero

        public bool IsSprinting { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsSliding { get; private set; }
        public bool IsGrounded => controller.isGrounded;
        public float Stamina => stamina;
        public float StaminaNormalized => stamina / maxStamina;
        public bool IsExhausted => exhausted;
        public float CurrentSpeed => horizontalVelocity.magnitude;
        public Vector3 Velocity => horizontalVelocity + Vector3.up * verticalVelocity;
        /// <summary>Look delta of the last frame in degrees (for weapon sway).</summary>
        public Vector2 LookDelta { get; private set; }

        /// <summary>Set by the weapon while aiming down sights.</summary>
        public float AimFovMultiplier { get; set; } = 1f;
        public float AimSensitivityMultiplier { get; set; } = 1f;
        /// <summary>Weapons can block sprint (e.g. while aiming or reloading).</summary>
        public bool SprintBlocked { get; set; }

        public event Action Jumped;
        public event Action<float> Landed;      // impact speed (m/s, positive)
        public event Action SlideStarted;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (cameraPivot == null)
            {
                var c = GetComponentInChildren<Camera>();
                if (c != null) cameraPivot = c.transform;
            }
            cam = cameraPivot != null ? cameraPivot.GetComponent<Camera>() : null;
            maxStamina = Mathf.Max(maxStamina, 160f);      // +60 % (user playtest 15.10); scenes saved with 100 get lifted
            staminaRegen = Mathf.Max(staminaRegen, 31.2f); // +30 %
            stamina = maxStamina;
            currentHeight = standHeight;
            eyeHeight = standHeight * eyeHeightRatio;
            yaw = transform.eulerAngles.y;
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

            crouchAction = new InputAction("Crouch", InputActionType.Button, "<Keyboard>/c");
            crouchAction.AddBinding("<Keyboard>/leftCtrl");
            crouchAction.AddBinding("<Gamepad>/buttonEast");
        }

        InputAction[] AllActions => new[] { moveAction, lookMouseAction, lookStickAction, sprintAction, jumpAction, crouchAction };

        void OnEnable()
        {
            foreach (var a in AllActions) a.Enable();
            if (!GameState.IsPaused) SetCursorLocked(true);
        }

        void OnDisable()
        {
            foreach (var a in AllActions) a.Disable();
        }

        void OnDestroy()
        {
            foreach (var a in AllActions) a.Dispose();
        }

        /// <summary>Weapon recoil: kicks the view; 70 % springs back, 30 % stays (aim climb).</summary>
        public void AddRecoil(float pitchUp, float yawSide)
        {
            recoil += new Vector2(pitchUp, yawSide) * 0.7f;
            pitch -= pitchUp * 0.3f;
            yaw += yawSide * 0.3f;
        }

        /// <summary>Instantly move the player (respawn, cutscenes).</summary>
        public void Teleport(Vector3 position, float newYaw)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            yaw = newYaw;
            // Look() only turns the body while this component is enabled (a dialogue may have it off), so turn it here too.
            transform.rotation = Quaternion.Euler(0f, newYaw, 0f);
            pitch = 0f;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }

        void Update()
        {
            if (GameState.IsPaused) return;

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
                SetCursorLocked(true);

            Look();
            UpdateCrouchAndSlide();
            Move();
            UpdateStamina();
            UpdateCamera();
        }

        void Look()
        {
            float sens = GameSettings.MouseSensitivity * AimSensitivityMultiplier;
            Vector2 delta = lookStickAction.ReadValue<Vector2>() * (stickSensitivity * sens * Time.deltaTime);
            if (Cursor.lockState == CursorLockMode.Locked)
                delta += lookMouseAction.ReadValue<Vector2>() * (mouseSensitivity * sens);
            if (GameSettings.InvertY) delta.y = -delta.y;

            LookDelta = delta;
            yaw += delta.x;
            pitch = Mathf.Clamp(pitch - delta.y, -pitchLimit, pitchLimit);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        void UpdateCrouchAndSlide()
        {
            bool crouchPressed = crouchAction.WasPressedThisFrame();
            crouchHeld = crouchAction.IsPressed();

            if (crouchPressed && IsSprinting && controller.isGrounded && !IsSliding
                && Time.time > slideEndTime + slideCooldown && stamina >= slideCost)
                StartSlide();

            if (IsSliding)
            {
                float speed = horizontalVelocity.magnitude - slideFriction * Time.deltaTime;
                horizontalVelocity = horizontalVelocity.normalized * Mathf.Max(0f, speed);
                if (speed < slideMinSpeed || !controller.isGrounded || jumpAction.WasPressedThisFrame())
                {
                    IsSliding = false;
                    slideEndTime = Time.time;
                }
            }

            bool wantCrouch = IsSliding || crouchHeld;
            if (!wantCrouch && IsCrouching && !CanStandUp()) wantCrouch = true;
            IsCrouching = wantCrouch;

            float targetHeight = IsCrouching ? crouchHeight : standHeight;
            currentHeight = Mathf.MoveTowards(currentHeight, targetHeight, 6f * Time.deltaTime);
            controller.height = currentHeight;
            controller.center = new Vector3(0f, currentHeight * 0.5f, 0f);
            eyeHeight = Mathf.Lerp(eyeHeight, currentHeight * eyeHeightRatio, 1f - Mathf.Exp(-14f * Time.deltaTime));
        }

        void StartSlide()
        {
            IsSliding = true;
            IsSprinting = false;
            UseStamina(slideCost);
            Vector3 dir = horizontalVelocity.sqrMagnitude > 0.1f ? horizontalVelocity.normalized : transform.forward;
            horizontalVelocity = dir * (Mathf.Max(horizontalVelocity.magnitude, sprintSpeed) + slideStartBoost);
            CameraShake.AddTrauma(0.12f);
            SlideStarted?.Invoke();
        }

        bool CanStandUp()
        {
            // Sphere at standing head height; it sits above the crouched capsule, so it never hits ourselves.
            float radius = controller.radius * 0.95f;
            Vector3 head = transform.position + Vector3.up * (standHeight - radius);
            return !Physics.CheckSphere(head, radius, ~0, QueryTriggerInteraction.Ignore);
        }

        void Move()
        {
            Vector2 input = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
            bool grounded = controller.isGrounded;
            if (grounded) lastGroundedTime = Time.time;
            if (jumpAction.WasPressedThisFrame()) jumpPressedTime = Time.time;

            bool wantsSprint = sprintAction.IsPressed() && input.y > 0.1f && !IsCrouching && !SprintBlocked;
            // Sprint starts only on the ground but continues through a jump.
            IsSprinting = !IsSliding && wantsSprint && !exhausted && stamina > 0f && (grounded || IsSprinting);

            if (!IsSliding)
            {
                float targetSpeed = IsCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed;
                Vector3 target = (transform.right * input.x + transform.forward * input.y) * targetSpeed;
                float accel = grounded ? acceleration : acceleration * airControl;
                horizontalVelocity = Vector3.Lerp(horizontalVelocity, target, 1f - Mathf.Exp(-accel * Time.deltaTime));
                if (grounded) bobDistance += horizontalVelocity.magnitude * Time.deltaTime;
            }

            if (grounded && verticalVelocity < 0f) verticalVelocity = -2f; // keeps the controller pressed to slopes

            bool canJump = Time.time - lastGroundedTime <= coyoteTime && verticalVelocity <= 0f;
            bool buffered = Time.time - jumpPressedTime <= jumpBuffer;
            if (canJump && buffered && !IsCrouching && stamina >= jumpCost * 0.5f)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpPressedTime = -10f;
                lastGroundedTime = -10f;
                UseStamina(jumpCost);
                Jumped?.Invoke();
            }

            float fallSpeed = -verticalVelocity;
            verticalVelocity += gravity * Time.deltaTime;
            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

            bool nowGrounded = controller.isGrounded;
            if (nowGrounded && !wasGrounded && fallSpeed > 3f)
            {
                landingVelocity -= fallSpeed * landingDipPerSpeed * 10f;
                if (fallSpeed > 8f) CameraShake.AddTrauma(Mathf.Clamp01((fallSpeed - 8f) * 0.05f));
                Landed?.Invoke(fallSpeed);
            }
            wasGrounded = nowGrounded;
        }

        void UpdateStamina()
        {
            if (IsSprinting && CurrentSpeed > 1f) UseStamina(sprintDrain * Time.deltaTime);
            else if (Time.time - lastStaminaUse > regenDelay)
                stamina = Mathf.Min(maxStamina, stamina + staminaRegen * Time.deltaTime);

            if (stamina <= 0f) exhausted = true;
            else if (exhausted && stamina >= exhaustedRecover) exhausted = false;
        }

        void UseStamina(float amount)
        {
            stamina = Mathf.Max(0f, stamina - amount);
            lastStaminaUse = Time.time;
        }

        void UpdateCamera()
        {
            if (cameraPivot == null) return;
            float dt = Time.deltaTime;

            // Head bob (zero when disabled in settings).
            Vector3 bob = Vector3.zero;
            if (GameSettings.HeadBob && controller.isGrounded && !IsSliding)
            {
                float speed01 = Mathf.Clamp01(CurrentSpeed / walkSpeed);
                float amp = bobAmplitude * speed01 * (IsSprinting ? sprintBobMultiplier : 1f) * (IsCrouching ? 0.6f : 1f);
                float phase = bobDistance * bobFrequency;
                bob = new Vector3(Mathf.Cos(phase * 0.5f) * amp * 0.6f, Mathf.Sin(phase) * amp, 0f);
            }

            // Landing dip: critically-damped-ish spring back to 0.
            landingVelocity += (-landingOffset * 120f - landingVelocity * 18f) * dt;
            landingOffset += landingVelocity * dt;

            cameraPivot.localPosition = new Vector3(bob.x, eyeHeight + bob.y + landingOffset, 0f);

            recoil = Vector2.Lerp(recoil, Vector2.zero, 1f - Mathf.Exp(-recoilReturn * dt));
            tilt = Mathf.Lerp(tilt, IsSliding ? slideTilt : 0f, 1f - Mathf.Exp(-10f * dt));
            Vector3 shake = CameraShake.Evaluate(dt);
            cameraPivot.localRotation = Quaternion.Euler(pitch - recoil.x + shake.x, recoil.y + shake.y, tilt + shake.z);

            if (cam != null)
            {
                float targetBoost = IsSliding ? slideFovBoost : IsSprinting && CurrentSpeed > walkSpeed ? sprintFovBoost : 0f;
                fovBoost = Mathf.Lerp(fovBoost, targetBoost, 1f - Mathf.Exp(-8f * dt));
                cam.fieldOfView = (GameSettings.FieldOfView + fovBoost) * AimFovMultiplier;
            }
        }

        static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
