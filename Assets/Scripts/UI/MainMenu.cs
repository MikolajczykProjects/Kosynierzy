using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Kosynierzy
{
    /// <summary>
    /// Main menu (scene Menu): title "SENDER GLEIWITZ 31.08.1939", buttons Nowa gra / Tryb historyczny / Ustawienia / Wyjście.
    /// Both game buttons load the scene named gameSceneName; Tryb historyczny also sets GameMode.Historical.
    /// uGUI built in code at runtime. Created by Kosynierzy > Create Main Menu Scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] string gameSceneName = "Gra";
        [SerializeField] string title = "SENDER GLEIWITZ";
        [SerializeField] string subtitle = "31.08.1939";
        [SerializeField] int sortingOrder = 100;
        [Tooltip("Optional font for all menus; the built-in LegacyRuntime font is used when empty.")]
        [SerializeField] Font fontOverride;
        [SerializeField] SettingsMenu settingsMenu;

        Canvas canvas;
        UnityEngine.UI.Button newGameButton, settingsButton;
        InputAction backAction;

        void Awake()
        {
            if (fontOverride != null) SettingsMenuUi.Font = fontOverride;
            if (settingsMenu == null) settingsMenu = GetComponent<SettingsMenu>();
            if (settingsMenu == null) settingsMenu = gameObject.AddComponent<SettingsMenu>();
            SettingsMenuUi.EnsureEventSystem();

            backAction = new InputAction("MenuBack", InputActionType.Button, "<Keyboard>/escape");
            backAction.AddBinding("<Gamepad>/buttonEast");
            Build();
        }

        void OnEnable() => backAction.Enable();
        void OnDisable() => backAction.Disable();
        void OnDestroy() => backAction?.Dispose();

        void Start()
        {
            // Coming back from a paused game must not leave time frozen or the cursor hidden.
            if (GameState.IsPaused) GameState.SetPaused(false);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            canvas.gameObject.SetActive(true);
            SettingsMenuUi.Select(newGameButton);
        }

        void Update()
        {
            if (backAction.WasPressedThisFrame() && settingsMenu != null && settingsMenu.IsOpen)
            {
                settingsMenu.Close();
                return;
            }
            if (canvas.gameObject.activeSelf) SettingsMenuUi.RestoreSelection(newGameButton);
        }

        void StartGame(bool historical)
        {
            GameMode.Historical = historical;
            Time.timeScale = 1f;
            if (Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                SceneManager.LoadScene(gameSceneName);
                return;
            }
#if UNITY_EDITOR
            // Not in Build Settings: still playable from the Editor if the scene asset exists.
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets(gameSceneName + " t:Scene"))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) != gameSceneName) continue;
                Debug.LogWarning($"Kosynierzy: scene '{gameSceneName}' is not in Build Settings; loading {path} for the Editor only. " +
                                 "Run Kosynierzy > Create Main Menu Scene to add it.");
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            Debug.LogWarning($"Kosynierzy: scene '{gameSceneName}' is not in Build Settings (File > Build Profiles), cannot start the game.");
        }

        void OpenSettings()
        {
            canvas.gameObject.SetActive(false);
            settingsMenu.Open(() =>
            {
                canvas.gameObject.SetActive(true);
                SettingsMenuUi.Select(settingsButton);
            });
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Build()
        {
            canvas = SettingsMenuUi.CreateCanvas("MainMenuCanvas", transform, sortingOrder);
            var root = (RectTransform)canvas.transform;

            var background = SettingsMenuUi.CreateImage("Background", root, new Color(0.07f, 0.06f, 0.05f, 1f), true);
            SettingsMenuUi.Stretch(background.rectTransform);

            var titleText = SettingsMenuUi.CreateText("Title", root, title, 96, TextAnchor.MiddleCenter, FontStyle.Bold, SettingsMenuUi.TextColor);
            SettingsMenuUi.PinTop(titleText.rectTransform, 150f, 120f, 80f);

            var bar = SettingsMenuUi.CreateImage("AccentBar", root, SettingsMenuUi.AccentColor, false);
            var barRt = bar.rectTransform;
            barRt.anchorMin = barRt.anchorMax = new Vector2(0.5f, 1f);
            barRt.pivot = new Vector2(0.5f, 1f);
            barRt.sizeDelta = new Vector2(520f, 6f);
            barRt.anchoredPosition = new Vector2(0f, -280f);

            var subtitleText = SettingsMenuUi.CreateText("Subtitle", root, subtitle, 44, TextAnchor.MiddleCenter, FontStyle.Normal, SettingsMenuUi.AccentColor);
            SettingsMenuUi.PinTop(subtitleText.rectTransform, 296f, 60f, 80f);

            var column = SettingsMenuUi.CreateColumn("Buttons", root, 16f);
            column.anchorMin = column.anchorMax = new Vector2(0.5f, 0.5f);
            column.pivot = new Vector2(0.5f, 1f);
            column.sizeDelta = new Vector2(560f, 4 * 68f + 3 * 16f);
            column.anchoredPosition = new Vector2(0f, -10f);

            newGameButton = SettingsMenuUi.CreateButton(column, "Nowa gra", 68f, 34, () => StartGame(false), out _);
            var historicalButton = SettingsMenuUi.CreateButton(column, "Tryb historyczny", 68f, 34, () => StartGame(true), out _);
            settingsButton = SettingsMenuUi.CreateButton(column, "Ustawienia", 68f, 34, OpenSettings, out _);
            var quitButton = SettingsMenuUi.CreateButton(column, "Wyjście", 68f, 34, Quit, out _);
            SettingsMenuUi.LinkVertical(new UnityEngine.UI.Selectable[] { newGameButton, historicalButton, settingsButton, quitButton });
        }
    }
}
