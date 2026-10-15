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

        void StartGame(bool historical, bool resume = false)
        {
            if (resume && GameReset.HasSave) Checkpoints.ResumeStep = Checkpoints.SavedStep;
            else GameReset.Wipe();
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

            var background = SettingsMenuUi.CreateImage("Background", root, UiTheme.Dark, true);
            SettingsMenuUi.Stretch(background.rectTransform);
            var vignette = SettingsMenuUi.CreateImage("Vignette", root, new Color(0f, 0f, 0f, 0.8f), false);
            vignette.sprite = UiTheme.VignetteSprite;
            SettingsMenuUi.Stretch(vignette.rectTransform);

            // Title block, left: two 132 px lines, then the date line.
            string[] words = title.Split(' ');
            string line1 = words[0].ToUpperInvariant();
            string line2 = words.Length > 1 ? string.Join(" ", words, 1, words.Length - 1).ToUpperInvariant() : "";
            var t1 = MenuV2.Label("Title1", root, line1, 132, TextAnchor.LowerLeft, MenuV2.Bone);
            SettingsMenuUi.Place(t1.rectTransform, new Vector2(0f, 1f), new Vector2(96f, -230f), new Vector2(1200f, 140f));
            var t2 = MenuV2.Label("Title2", root, line2, 132, TextAnchor.LowerLeft, MenuV2.Bone);
            SettingsMenuUi.Place(t2.rectTransform, new Vector2(0f, 1f), new Vector2(96f, -366f), new Vector2(1200f, 140f));
            var date = MenuV2.Label("Date", root, subtitle + "  ·  KATOWICE, EKSPOZYTURA NR 4", 20, TextAnchor.MiddleLeft, MenuV2.Amber);
            SettingsMenuUi.Place(date.rectTransform, new Vector2(0f, 1f), new Vector2(100f, -520f), new Vector2(1000f, 28f));

            var column = SettingsMenuUi.CreateColumn("Buttons", root, 4f);
            SettingsMenuUi.Place(column, new Vector2(0f, 0f), new Vector2(96f, 150f), new Vector2(700f, (GameReset.HasSave ? 5 : 4) * 68f + 16f));

            UnityEngine.UI.Button continueButton = null;
            if (GameReset.HasSave) continueButton = MenuV2.CreateButton(column, "Kontynuuj", MenuV2.Kind.Item, 68f, 40, () => StartGame(false, true), out _);
            newGameButton = MenuV2.CreateButton(column, "Nowa gra", MenuV2.Kind.Item, 68f, 40, () => StartGame(false), out _);
            var historicalButton = MenuV2.CreateButton(column, "Tryb historyczny", MenuV2.Kind.Item, 68f, 40, () => StartGame(true), out _);
            settingsButton = MenuV2.CreateButton(column, "Ustawienia", MenuV2.Kind.Item, 68f, 40, OpenSettings, out _);
            var quitButton = MenuV2.CreateButton(column, "Wyjście", MenuV2.Kind.Item, 68f, 40, Quit, out _);
            var order = new System.Collections.Generic.List<UnityEngine.UI.Selectable>();
            if (continueButton != null) order.Add(continueButton);
            order.AddRange(new UnityEngine.UI.Selectable[] { newGameButton, historicalButton, settingsButton, quitButton });
            SettingsMenuUi.LinkVertical(order.ToArray());
            if (continueButton != null) newGameButton = continueButton;
        }
    }
}
