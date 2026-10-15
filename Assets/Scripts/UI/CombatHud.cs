using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>What every HUD widget may read this frame.</summary>
    public sealed class HudContext
    {
        public RectTransform Root;
        public float Scale = 1f;
        public float Now, Dt;
        public bool Visible;

        public Camera Cam;
        public FirstPersonController Player;
        public PlayerWeapon Weapon;
        public PlayerInteractor Interactor;
        public PhotoCamera Photo;
        public CombatFeedback Feedback;

        public bool PistolOut;
        public bool ViewfinderUp;
        /// <summary>Vertical space already used at the top centre (cable countdown), for the detection badge.</summary>
        public float TopCentreUsed;

        /// <summary>Canvas size in reference units (height 1080 with match-height scaling).</summary>
        public Vector2 Size => Root.rect.size;

        /// <summary>Screen pixels → anchored position in a full-screen container anchored bottom-left.</summary>
        public Vector2 ScreenToCanvas(Vector3 screen) => new(screen.x / Scale, screen.y / Scale);
    }

    /// <summary>One self-contained part of the HUD; built once, ticked every LateUpdate.</summary>
    public abstract class HudWidget
    {
        public abstract void Build(HudContext ctx);
        public abstract void Tick(HudContext ctx);
        public virtual void Dispose() { }
    }

    /// <summary>
    /// The in-game HUD (uGUI, 1939 document style from UiTheme): objectives slip with clock and collections,
    /// objective marker, crosshair with hit marker and popups, target ID, interaction tag, ammo cartridges,
    /// stamina, hotbar and received-item card, detection arcs/badge/eye, photo viewfinder and prints.
    /// Fades out while paused, during history cards, failure, epilogue and full-screen dialogue.
    /// Keeps the class name so existing scene references stay valid.
    /// </summary>
    [RequireComponent(typeof(CombatFeedback))]
    public class CombatHud : MonoBehaviour
    {
        [SerializeField] int sortingOrder = 10;

        readonly HudContext ctx = new();
        readonly List<HudWidget> widgets = new();
        Canvas canvas;
        CanvasGroup group;
        float nextFind;

        void Awake()
        {
            PhotoCamera.UiHandledExternally = true;
            ctx.Feedback = GetComponent<CombatFeedback>();

            canvas = SettingsMenuUi.CreateCanvas("HudCanvas", transform, sortingOrder);
            var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster != null) Destroy(raycaster); // the HUD never takes clicks
            group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            group.alpha = 0f;
            ctx.Root = (RectTransform)canvas.transform;

            // Order = draw order (later on top).
            widgets.Add(new HudCombat());
            widgets.Add(new HudDetection());
            widgets.Add(new HudObjectives());
            widgets.Add(new HudEquipment());
            widgets.Add(new HudPhoto());
            foreach (var w in widgets) w.Build(ctx);

            canvas.gameObject.SetActive(true);
        }

        void OnDestroy()
        {
            foreach (var w in widgets) w.Dispose();
        }

        void LateUpdate()
        {
            FindReferences();
            ctx.Now = Time.unscaledTime;
            ctx.Dt = Time.unscaledDeltaTime;
            ctx.Scale = canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;

            bool fullScreenDialogue = Dialog.IsPlaying && (ctx.Player == null || !ctx.Player.enabled);
            bool hidden = GameState.IsPaused || HistoryCardView.IsOpen || MissionFail.IsFailed || Epilogue.IsPlaying || fullScreenDialogue;
            ctx.Visible = !hidden;
            group.alpha = Mathf.MoveTowards(group.alpha, hidden ? 0f : 1f, ctx.Dt * 6f);

            var hands = PlayerHands.Instance;
            ctx.PistolOut = ctx.Weapon != null && (hands == null ? ctx.Weapon.enabled : (hands.Current == HandItem.Pistol || hands.Current == HandItem.Rifle));
            ctx.ViewfinderUp = ctx.Photo != null && ctx.Photo.IsRaised;
            ctx.TopCentreUsed = 0f;

            foreach (var w in widgets) w.Tick(ctx);
        }

        void FindReferences()
        {
            if (Time.unscaledTime < nextFind) return;
            nextFind = Time.unscaledTime + 1f;
            if (ctx.Player == null) ctx.Player = Object.FindAnyObjectByType<FirstPersonController>();
            if (ctx.Weapon == null) ctx.Weapon = Object.FindAnyObjectByType<PlayerWeapon>();
            if (ctx.Interactor == null) ctx.Interactor = Object.FindAnyObjectByType<PlayerInteractor>();
            if (ctx.Photo == null) ctx.Photo = Object.FindAnyObjectByType<PhotoCamera>();
            if (ctx.Cam == null || !ctx.Cam.isActiveAndEnabled)
                ctx.Cam = ctx.Player != null ? ctx.Player.GetComponentInChildren<Camera>() : Camera.main;
        }
    }
}
