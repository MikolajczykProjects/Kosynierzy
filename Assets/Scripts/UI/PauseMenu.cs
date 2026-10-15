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
        const string QuitConfirmLabel = "NA PEWNO? NACIŚNIJ PONOWNIE";

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
            var panel = SettingsMenuUi.CreateDimmedPanel(canvas.transform, new Vector2(680f, 540f));

            var title = UiTheme.CreateHeading("Title", panel, "Pauza", 60, UiTheme.Ink);
            SettingsMenuUi.PinTop(title.rectTransform, 28f, 84f, 40f);
            var rule = UiTheme.CreateRule("Rule", panel, UiTheme.FadedInk);
            SettingsMenuUi.PinTop(rule.rectTransform, 120f, 2f, 56f);
            var stamp = UiTheme.CreateStamp(panel, "Tajne", UiTheme.StampRed, 24);
            stamp.anchorMin = stamp.anchorMax = new Vector2(1f, 1f);
            stamp.anchoredPosition = new Vector2(-96f, -46f);

            var column = SettingsMenuUi.CreateColumn("Buttons", panel, 14f);
            SettingsMenuUi.Stretch(column, 56f, 56f, 140f, 40f);

            resumeButton = SettingsMenuUi.CreateButton(column, "Wznów", 64f, 28, Resume, out _);
            settingsButton = SettingsMenuUi.CreateButton(column, "Ustawienia", 64f, 28, OpenSettings, out _);
            var checkpointButton = SettingsMenuUi.CreateButton(column, "Spróbuj od punktu kontrolnego", 64f, 25, RestartFromCheckpoint, out _);
            var quitButton = SettingsMenuUi.CreateButton(column, QuitLabel, 64f, 28, Quit, out quitText);

            SettingsMenuUi.LinkVertical(new UnityEngine.UI.Selectable[] { resumeButton, settingsButton, checkpointButton, quitButton });
        }
    }
}
