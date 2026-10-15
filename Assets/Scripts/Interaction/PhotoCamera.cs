using UnityEngine;
using UnityEngine.InputSystem;

namespace Kosynierzy
{
    /// <summary>
    /// Photo mode on F (needs GameProgress.HasCamera — Zofia gives it in act 1): black frame, slight zoom,
    /// LMB takes a photo. Every Evidence inside the frame (viewport 0.2–0.8), closer than 15 m and not
    /// blocked (line of sight) is counted. Flash + click. F / RMB / Esc leaves. Put it on the player camera.
    /// </summary>
    public class PhotoCamera : MonoBehaviour
    {
        [SerializeField] float maxDistance = 15f;
        [SerializeField] float zoomMultiplier = 0.8f;
        [SerializeField] float cooldown = 0.6f;

        InputAction toggleAction, shootAction, exitAction;
        Camera cam;
        FirstPersonController player;
        PlayerWeapon weapon;
        float nextShot, flashUntil;

        public bool IsActive { get; private set; }
        public float FlashAlpha => Mathf.Clamp01((flashUntil - Time.unscaledTime) / 0.25f);
        /// <summary>Name of the evidence caught by the last photo, or null when nothing counted.</summary>
        public string LastResult { get; private set; }
        public float LastResultTime { get; private set; } = -10f;

        void Awake()
        {
            cam = GetComponent<Camera>();
            player = GetComponentInParent<FirstPersonController>();
            weapon = GetComponentInChildren<PlayerWeapon>();
            toggleAction = new InputAction("Photo", InputActionType.Button, "<Keyboard>/f");
            toggleAction.AddBinding("<Gamepad>/dpad/up");
            shootAction = new InputAction("PhotoShoot", InputActionType.Button, "<Mouse>/leftButton");
            shootAction.AddBinding("<Gamepad>/rightTrigger");
            exitAction = new InputAction("PhotoExit", InputActionType.Button, "<Mouse>/rightButton");
            exitAction.AddBinding("<Gamepad>/buttonEast");
        }

        void OnEnable() { toggleAction.Enable(); shootAction.Enable(); exitAction.Enable(); }
        void OnDisable() { toggleAction.Disable(); shootAction.Disable(); exitAction.Disable(); SetActive(false); }
        void OnDestroy() { toggleAction.Dispose(); shootAction.Dispose(); exitAction.Dispose(); }

        void Update()
        {
            if (GameState.IsPaused || Dialog.IsPlaying) return;
            if (toggleAction.WasPressedThisFrame())
            {
                if (!GameProgress.HasCamera && !IsActive)
                {
                    LastResult = "Nie masz jeszcze aparatu";
                    LastResultTime = Time.unscaledTime;
                }
                else SetActive(!IsActive);
            }
            if (!IsActive) return;
            if (exitAction.WasPressedThisFrame()) { SetActive(false); return; }
            if (shootAction.WasPressedThisFrame() && Time.unscaledTime >= nextShot) TakePhoto();
        }

        void SetActive(bool on)
        {
            if (IsActive == on) return;
            IsActive = on;
            if (weapon != null) weapon.enabled = !on;
            if (player != null) player.AimFovMultiplier = on ? zoomMultiplier : 1f;
        }

        void TakePhoto()
        {
            nextShot = Time.unscaledTime + cooldown;
            flashUntil = Time.unscaledTime + 0.25f;
            SfxBank.Play(SfxBank.Sfx.Pickup);
            LastResult = null;

            foreach (var ev in Object.FindObjectsByType<Evidence>())
            {
                if (ev.IsPhotographed || !ev.isActiveAndEnabled) continue;
                Vector3 p = ev.FocusPoint;
                Vector3 vp = cam.WorldToViewportPoint(p);
                if (vp.z <= 0f || vp.z > maxDistance || vp.x < 0.2f || vp.x > 0.8f || vp.y < 0.2f || vp.y > 0.8f) continue;
                if (!Visible(ev, p)) continue;
                ev.MarkPhotographed();
                LastResult = ev.DisplayName;
            }
            LastResultTime = Time.unscaledTime;
            if (LastResult == null) LastResult = "Nic ważnego w kadrze";
        }

        bool Visible(Evidence ev, Vector3 point)
        {
            Vector3 from = cam.transform.position;
            Vector3 dir = point - from;
            var hits = Physics.RaycastAll(from, dir.normalized, dir.magnitude, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                if (player != null && h.collider.transform.IsChildOf(player.transform)) continue;
                if (h.collider.transform.IsChildOf(ev.transform)) continue;
                return false;
            }
            return true;
        }

        void OnGUI()
        {
            if (!IsActive && FlashAlpha <= 0f && Time.unscaledTime - LastResultTime > 2f) return;
            float scale = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale, h = 1080f;
            var tex = Texture2D.whiteTexture;

            if (IsActive)
            {
                // Black frame outside the 0.2–0.8 counting area, thin corner marks inside.
                GUI.color = new Color(0f, 0f, 0f, 0.85f);
                GUI.DrawTexture(new Rect(0f, 0f, w, h * 0.12f), tex);
                GUI.DrawTexture(new Rect(0f, h * 0.88f, w, h * 0.12f), tex);
                GUI.DrawTexture(new Rect(0f, 0f, w * 0.1f, h), tex);
                GUI.DrawTexture(new Rect(w * 0.9f, 0f, w * 0.1f, h), tex);
                GUI.color = new Color(1f, 1f, 1f, 0.8f);
                float x0 = w * 0.2f, x1 = w * 0.8f, y0 = h * 0.2f, y1 = h * 0.8f, l = 40f, t = 3f;
                GUI.DrawTexture(new Rect(x0, y0, l, t), tex); GUI.DrawTexture(new Rect(x0, y0, t, l), tex);
                GUI.DrawTexture(new Rect(x1 - l, y0, l, t), tex); GUI.DrawTexture(new Rect(x1 - t, y0, t, l), tex);
                GUI.DrawTexture(new Rect(x0, y1 - t, l, t), tex); GUI.DrawTexture(new Rect(x0, y1 - l, t, l), tex);
                GUI.DrawTexture(new Rect(x1 - l, y1 - t, l, t), tex); GUI.DrawTexture(new Rect(x1 - t, y1 - l, t, l), tex);
                GUI.Label(new Rect(w * 0.1f + 20f, h * 0.88f + 20f, 800f, 40f), "LPM — zdjęcie   F / PPM — wyjdź");
            }

            if (Time.unscaledTime - LastResultTime < 2f && LastResult != null)
            {
                GUI.color = Color.white;
                var style = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(0f, h * 0.7f, w, 50f), LastResult, style);
            }

            if (FlashAlpha > 0f)
            {
                GUI.color = new Color(1f, 1f, 1f, FlashAlpha);
                GUI.DrawTexture(new Rect(0f, 0f, w, h), tex);
            }
            GUI.color = Color.white;
        }
    }
}
