using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Kosynierzy
{
    /// <summary>
    /// Pause menu: Esc / gamepad Start toggles GameState pause; B or Esc goes back (settings → pause → game).
    /// Buttons: Wznów, Ustawienia, Spróbuj od punktu kontrolnego (raises RestartFromCheckpointRequested), Wyjdź z gry.
    /// uGUI built in code at runtime; works at timeScale 0 (uGUI transitions and input use unscaled time).
    /// </summary>
    [DisallowMultipleComponent]
    public class PauseMenu : MonoBehaviour
    {
        /// <summary>"Spróbuj od punktu kontrolnego" pressed; raised right after the game is unpaused.</summary>
        public static event Action RestartFromCheckpointRequested;

        const string QuitLabel = "WYJDŹ Z GRY";
        const string QuitConfirmLabel = "NA PEWNO?";

        [SerializeField] int sortingOrder = 100;
        [Tooltip("Optional font for all menus; the built-in LegacyRuntime font is used when empty.")]
        [SerializeField] Font fontOverride;
        [Tooltip("Found or added on this GameObject when empty.")]
        [SerializeField] SettingsMenu settingsMenu;

        InputAction pauseAction, backAction;
        Canvas canvas;
        UnityEngine.UI.Button resumeButton, settingsButton;
        UnityEngine.UI.Text quitText;
        bool quitArmed;
        bool relockCursor;

        public bool IsOpen => canvas != null && canvas.gameObject.activeSelf;

        /// <summary>Esc belongs to another screen (history card, failure, epilogue) and must not open the pause menu.</summary>
        static bool PauseBlocked => MissionFail.IsFailed || Epilogue.IsPlaying || HistoryCardView.BlocksPauseInput;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => RestartFromCheckpointRequested = null;

        void Awake()
        {
            if (fontOverride != null) SettingsMenuUi.Font = fontOverride;
            if (settingsMenu == null) settingsMenu = GetComponent<SettingsMenu>();
            if (settingsMenu == null) settingsMenu = gameObject.AddComponent<SettingsMenu>();
            SettingsMenuUi.EnsureEventSystem();

            pauseAction = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            pauseAction.AddBinding("<Gamepad>/start");
            backAction = new InputAction("MenuBack", InputActionType.Button, "<Gamepad>/buttonEast");

            Build();
        }

        void OnEnable()
        {
            pauseAction.Enable();
            backAction.Enable();
            GameState.PauseChanged += OnPauseChanged;
            if (GameState.IsPaused) ShowPausePanel();
        }

        void OnDisable()
        {
            pauseAction.Disable();
            backAction.Disable();
            GameState.PauseChanged -= OnPauseChanged;
        }

        void OnDestroy()
        {
            pauseAction?.Dispose();
            backAction?.Dispose();
        }

        void Update()
        {
            bool pausePressed = pauseAction.WasPressedThisFrame();
            bool backPressed = backAction.WasPressedThisFrame();

            if (!GameState.IsPaused)
            {
                if (pausePressed && !PauseBlocked) GameState.SetPaused(true);
                return;
            }
            if (pausePressed || backPressed)
            {
                Back();
                return;
            }
            if (IsOpen) SettingsMenuUi.RestoreSelection(resumeButton);
        }

        void LateUpdate()
        {
            // Gameplay scripts may touch the cursor in the same frame we resume; lock it last.
            if (!relockCursor) return;
            relockCursor = false;
            if (GameState.IsPaused) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>Esc / B while paused: leave the settings first, then resume.</summary>
        public void Back()
        {
            if (settingsMenu != null && settingsMenu.IsOpen) settingsMenu.Close();
            else Resume();
        }

        public void Resume() => GameState.SetPaused(false);

        void OnPauseChanged(bool paused)
        {
            if (paused) ShowPausePanel();
            else HideAll();
        }

        void ShowPausePanel()
        {
            if (canvas == null) return;
            quitArmed = false;
            quitText.text = QuitLabel;
            canvas.gameObject.SetActive(true);
            SettingsMenuUi.Select(resumeButton);
        }

        void HideAll()
        {
            if (settingsMenu != null && settingsMenu.IsOpen) settingsMenu.Close();
            if (canvas != null) canvas.gameObject.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            relockCursor = true;
        }

        void OpenSettings()
        {
            canvas.gameObject.SetActive(false);
            settingsMenu.Open(OnSettingsClosed);
        }

        void OnSettingsClosed()
        {
            if (!GameState.IsPaused) return; // closed because the game resumed
            canvas.gameObject.SetActive(true);
            SettingsMenuUi.Select(settingsButton);
        }

        void RestartFromCheckpoint()
        {
            GameState.SetPaused(false);
            if (RestartFromCheckpointRequested == null)
                Debug.LogWarning("Kosynierzy: nothing is subscribed to PauseMenu.RestartFromCheckpointRequested.");
            RestartFromCheckpointRequested?.Invoke();
        }

        void Quit()
        {
            if (!quitArmed)
            {
                quitArmed = true;
                quitText.text = QuitConfirmLabel;
                return;
            }
            GameState.SetPaused(false);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Build()
        {
            canvas = SettingsMenuUi.CreateCanvas("PauseCanvas", transform, sortingOrder);
            var root = canvas.transform;
            // Dark wash + vignette over the frozen game (no blur available in uGUI without a render texture).
            var dim = SettingsMenuUi.CreateImage("Dim", root, MenuV2.Wash, true);
            SettingsMenuUi.Stretch(dim.rectTransform);
            var vignette = SettingsMenuUi.CreateImage("Vignette", root, new Color(0f, 0f, 0f, 0.6f), false);
            vignette.sprite = UiTheme.VignetteSprite;
            SettingsMenuUi.Stretch(vignette.rectTransform);

            var label = MenuV2.Label("Caption", root, "PAUZA", 16, TextAnchor.MiddleLeft, MenuV2.Bone3);
            SettingsMenuUi.Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(96f, 220f), new Vector2(400f, 24f));

            var column = SettingsMenuUi.CreateColumn("Buttons", root, 8f);
            SettingsMenuUi.Place(column, new Vector2(0f, 0.5f), new Vector2(96f, -20f), new Vector2(900f, 4 * 84f + 3 * 8f));

            resumeButton = MenuV2.CreateButton(column, "Wznów", MenuV2.Kind.Item, 84f, 52, Resume, out _);
            settingsButton = MenuV2.CreateButton(column, "Ustawienia", MenuV2.Kind.Item, 84f, 52, OpenSettings, out _);
            var checkpointButton = MenuV2.CreateButton(column, "Punkt kontrolny", MenuV2.Kind.Item, 84f, 52, RestartFromCheckpoint, out _);
            var quitButton = MenuV2.CreateButton(column, QuitLabel, MenuV2.Kind.Item, 84f, 52, Quit, out quitText);

            // Mission block, bottom right.
            var missionLabel = MenuV2.Label("MissionLabel", root, "MISJA", 14, TextAnchor.LowerRight, MenuV2.Bone3);
            SettingsMenuUi.Place(missionLabel.rectTransform, new Vector2(1f, 0f), new Vector2(-64f, 120f), new Vector2(520f, 20f));
            var missionName = MenuV2.Label("MissionName", root, "BABCIA UMARŁA", 32, TextAnchor.LowerRight, MenuV2.Bone);
            SettingsMenuUi.Place(missionName.rectTransform, new Vector2(1f, 0f), new Vector2(-64f, 80f), new Vector2(520f, 40f));
            var missionPlace = MenuV2.Label("MissionPlace", root, "Gliwice, 31.08.1939", 16, TextAnchor.LowerRight, MenuV2.Bone2, isBody: true);
            SettingsMenuUi.Place(missionPlace.rectTransform, new Vector2(1f, 0f), new Vector2(-64f, 48f), new Vector2(520f, 24f));

            SettingsMenuUi.LinkVertical(new UnityEngine.UI.Selectable[] { resumeButton, settingsButton, checkpointButton, quitButton });
        }
    }
}
