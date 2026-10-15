using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kosynierzy
{
    /// <summary>
    /// The camera from Zofia, held in hand (PlayerHands slot 2). RMB or F raises it to the eye (viewfinder),
    /// LMB takes a photo. Every Evidence inside the frame (viewport 0.2–0.8), closer than 15 m and not blocked
    /// is counted. A snapshot of the frame is kept (LastPhoto) for the HUD's print. The HUD draws the
    /// viewfinder; a minimal fallback is drawn here unless UiHandledExternally is set. On the player camera.
    /// </summary>
    public class PhotoCamera : MonoBehaviour
    {
        [SerializeField] float maxDistance = 15f;
        [SerializeField] float zoomMultiplier = 0.75f;
        [SerializeField] float cooldown = 0.7f;
        [SerializeField] int filmFrames = 36;

        /// <summary>Set by the HUD when it draws the viewfinder itself.</summary>
        public static bool UiHandledExternally;

        InputAction raiseHold, raiseToggle, shootAction;
        Camera cam;
        FirstPersonController player;
        float nextShot, flashUntil;
        bool toggledUp;

        public bool IsRaised { get; private set; }
        public float FlashAlpha => Mathf.Clamp01((flashUntil - Time.unscaledTime) / 0.25f);
        public string LastResult { get; private set; }
        public float LastResultTime { get; private set; } = -10f;
        public Texture2D LastPhoto { get; private set; }
        public int ShotsLeft { get; private set; }
        /// <summary>Snapshot, caption (evidence name or "Nic ważnego w kadrze"), counted as evidence.</summary>
        public event Action<Texture2D, string, bool> PhotoTaken;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => UiHandledExternally = false;

        void Awake()
        {
            cam = GetComponent<Camera>();
            player = GetComponentInParent<FirstPersonController>();
            ShotsLeft = filmFrames;
            raiseHold = new InputAction("PhotoRaise", InputActionType.Button, "<Mouse>/rightButton");
            raiseHold.AddBinding("<Gamepad>/leftTrigger");
            raiseToggle = new InputAction("PhotoToggle", InputActionType.Button, "<Keyboard>/f");
            raiseToggle.AddBinding("<Gamepad>/dpad/up");
            shootAction = new InputAction("PhotoShoot", InputActionType.Button, "<Mouse>/leftButton");
            shootAction.AddBinding("<Gamepad>/rightTrigger");
        }

        void OnEnable() { raiseHold.Enable(); raiseToggle.Enable(); shootAction.Enable(); }

        void OnDisable()
        {
            raiseHold.Disable(); raiseToggle.Disable(); shootAction.Disable();
            toggledUp = false;
            SetRaised(false);
        }

        void OnDestroy() { raiseHold.Dispose(); raiseToggle.Dispose(); shootAction.Dispose(); }

        void Update()
        {
            if (GameState.IsPaused || Dialog.IsPlaying) { SetRaised(false); return; }
            if (raiseToggle.WasPressedThisFrame()) toggledUp = !toggledUp;
            if (raiseHold.WasReleasedThisFrame()) toggledUp = false;
            SetRaised(toggledUp || raiseHold.IsPressed());
            if (IsRaised && shootAction.WasPressedThisFrame() && Time.unscaledTime >= nextShot) TakePhoto();
        }

        void SetRaised(bool on)
        {
            if (IsRaised == on) return;
            IsRaised = on;
            if (player != null)
            {
                player.AimFovMultiplier = on ? zoomMultiplier : 1f;
                player.AimSensitivityMultiplier = on ? 0.7f : 1f;
                player.SprintBlocked = on;
            }
        }

        void TakePhoto()
        {
            nextShot = Time.unscaledTime + cooldown;
            flashUntil = Time.unscaledTime + 0.25f;
            ShotsLeft = Mathf.Max(0, ShotsLeft - 1);
            SfxBank.Play(SfxBank.Sfx.Pickup);
            Noise.Emit(transform.position, 2f, NoiseKind.Other, player != null ? player.transform : transform);

            string caught = null;
            foreach (var ev in UnityEngine.Object.FindObjectsByType<Evidence>())
            {
                if (ev.IsPhotographed || !ev.isActiveAndEnabled) continue;
                Vector3 p = ev.FocusPoint;
                Vector3 vp = cam.WorldToViewportPoint(p);
                if (vp.z <= 0f || vp.z > maxDistance || vp.x < 0.2f || vp.x > 0.8f || vp.y < 0.2f || vp.y > 0.8f) continue;
                if (!Visible(ev, p)) continue;
                ev.MarkPhotographed();
                caught = caught == null ? ev.DisplayName : caught + ", " + ev.DisplayName;
            }
            LastResult = caught ?? "Nic ważnego w kadrze";
            LastResultTime = Time.unscaledTime;
            StartCoroutine(Capture(LastResult, caught != null));
        }

        IEnumerator Capture(string caption, bool counted)
        {
            yield return new WaitForEndOfFrame();
            var full = ScreenCapture.CaptureScreenshotAsTexture();
            // Downscale to a small print so many photos stay cheap.
            int w = 480, h = Mathf.RoundToInt(480f * full.height / Mathf.Max(1, full.width));
            var rt = RenderTexture.GetTemporary(w, h);
            Graphics.Blit(full, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var small = new Texture2D(w, h, TextureFormat.RGB24, false);
            small.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            small.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            Destroy(full);
            if (LastPhoto != null) Destroy(LastPhoto);
            LastPhoto = small;
            PhotoTaken?.Invoke(small, caption, counted);
        }

        bool Visible(Evidence ev, Vector3 point)
        {
            Vector3 from = cam.transform.position;
            Vector3 dir = point - from;
            foreach (var h in Physics.RaycastAll(from, dir.normalized, dir.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (player != null && h.collider.transform.IsChildOf(player.transform)) continue;
                if (h.collider.transform.IsChildOf(ev.transform)) continue;
                return false;
            }
            return true;
        }

        // Minimal fallback viewfinder (the war-style HUD replaces it).
        void OnGUI()
        {
            if (UiHandledExternally) return;
            if (!IsRaised && FlashAlpha <= 0f && Time.unscaledTime - LastResultTime > 2f) return;
            var tex = Texture2D.whiteTexture;
            float w = Screen.width, h = Screen.height;
            if (IsRaised)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.85f);
                GUI.DrawTexture(new Rect(0f, 0f, w, h * 0.12f), tex);
                GUI.DrawTexture(new Rect(0f, h * 0.88f, w, h * 0.12f), tex);
                GUI.DrawTexture(new Rect(0f, 0f, w * 0.1f, h), tex);
                GUI.DrawTexture(new Rect(w * 0.9f, 0f, w * 0.1f, h), tex);
                GUI.color = new Color(1f, 1f, 1f, 0.7f);
                float x0 = w * 0.2f, x1 = w * 0.8f, y0 = h * 0.2f, y1 = h * 0.8f, t = 2f;
                GUI.DrawTexture(new Rect(x0, y0, x1 - x0, t), tex); GUI.DrawTexture(new Rect(x0, y1, x1 - x0, t), tex);
                GUI.DrawTexture(new Rect(x0, y0, t, y1 - y0), tex); GUI.DrawTexture(new Rect(x1, y0, t, y1 - y0), tex);
            }
            if (Time.unscaledTime - LastResultTime < 2f && LastResult != null)
            {
                GUI.color = Color.white;
                GUI.Label(new Rect(0f, h * 0.7f, w, 40f), LastResult, new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter });
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
