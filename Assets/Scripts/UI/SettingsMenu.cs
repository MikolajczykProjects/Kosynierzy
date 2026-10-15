using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kosynierzy
{
    /// <summary>
    /// Settings screen with tabs Grafika / Sterowanie / Dźwięk / Treści, built in code with uGUI.
    /// Every change applies immediately; GameSettings and GameGraphicsSettings persist it (saves are batched).
    /// Open it from any menu with Open(onClosed); Esc / B is handled by the caller (PauseMenu, MainMenu) via Close().
    /// </summary>
    [DisallowMultipleComponent]
    public class SettingsMenu : MonoBehaviour
    {
        [SerializeField] int sortingOrder = 110;

        static readonly string[] OffOn = { "Wył.", "Wł." };
        static readonly string[] TabNames = { "Grafika", "Sterowanie", "Dźwięk", "Treści" };
        static readonly FullScreenMode[] ScreenModes = { FullScreenMode.ExclusiveFullScreen, FullScreenMode.FullScreenWindow, FullScreenMode.Windowed };
        static readonly string[] ScreenModeNames = { "Pełny ekran", "Okno bez ramki", "Okno" };
        const float SaveDelay = 0.25f;

        Canvas canvas;
        readonly List<UnityEngine.UI.Button> tabButtons = new();
        readonly List<GameObject> tabMarkers = new();
        readonly List<GameObject> pages = new();
        readonly List<List<UnityEngine.UI.Selectable>> pageItems = new();
        UnityEngine.UI.Button backButton;
        int currentTab;
        Action closedCallback;
        InputAction prevTabAction, nextTabAction;
        bool gameSettingsDirty, graphicsDirty;
        float saveAt;
        List<Vector2Int> resolutions = new();

        public bool IsOpen => canvas != null && canvas.gameObject.activeSelf;

        void Awake()
        {
            prevTabAction = new InputAction("PrevTab", InputActionType.Button, "<Gamepad>/leftShoulder");
            nextTabAction = new InputAction("NextTab", InputActionType.Button, "<Gamepad>/rightShoulder");
        }

        void OnDisable()
        {
            prevTabAction?.Disable();
            nextTabAction?.Disable();
            FlushSaves();
        }

        void OnDestroy()
        {
            prevTabAction?.Dispose();
            nextTabAction?.Dispose();
        }

        /// <summary>Shows the settings; <paramref name="onClosed"/> runs after Close().</summary>
        public void Open(Action onClosed = null)
        {
            closedCallback = onClosed;
            // Rebuilt on every open so values (and the resolution list) are always current.
            if (canvas != null) Destroy(canvas.gameObject);
            Build();
            canvas.gameObject.SetActive(true);
            ShowTab(currentTab, selectTab: true);
            prevTabAction.Enable();
            nextTabAction.Enable();
        }

        public void Close()
        {
            if (!IsOpen) return;
            FlushSaves();
            canvas.gameObject.SetActive(false);
            prevTabAction.Disable();
            nextTabAction.Disable();
            var callback = closedCallback;
            closedCallback = null;
            callback?.Invoke();
        }

        void Update()
        {
            if (IsOpen)
            {
                if (prevTabAction.WasPressedThisFrame()) ShowTab((currentTab + TabNames.Length - 1) % TabNames.Length, true);
                if (nextTabAction.WasPressedThisFrame()) ShowTab((currentTab + 1) % TabNames.Length, true);
                SettingsMenuUi.RestoreSelection(tabButtons[currentTab]);
            }
            if ((gameSettingsDirty || graphicsDirty) && Time.unscaledTime >= saveAt) FlushSaves();
        }

        void FlushSaves()
        {
            if (gameSettingsDirty) GameSettings.Save();
            if (graphicsDirty) GameGraphicsSettings.Save();
            gameSettingsDirty = graphicsDirty = false;
        }

        void GameSettingsChanged()
        {
            gameSettingsDirty = true;
            saveAt = Time.unscaledTime + SaveDelay;
        }

        void GraphicsChanged()
        {
            GameGraphicsSettings.Apply();
            graphicsDirty = true;
            saveAt = Time.unscaledTime + SaveDelay;
        }

        // ---------- Layout ----------

        void Build()
        {
            tabButtons.Clear();
            tabMarkers.Clear();
            pages.Clear();
            pageItems.Clear();

            canvas = SettingsMenuUi.CreateCanvas("SettingsCanvas", transform, sortingOrder);
            var panel = SettingsMenuUi.CreateDimmedPanel(canvas.transform, new Vector2(1180f, 990f));

            var title = UiTheme.CreateHeading("Title", panel, "Ustawienia", 50, UiTheme.Ink);
            SettingsMenuUi.PinTop(title.rectTransform, 20f, 70f, 48f);
            var stamp = UiTheme.CreateStamp(panel, "Akta", UiTheme.Olive, 22);
            stamp.anchorMin = stamp.anchorMax = new Vector2(1f, 1f);
            stamp.anchoredPosition = new Vector2(-110f, -52f);

            var tabBar = SettingsMenuUi.CreateRect("Tabs", panel);
            SettingsMenuUi.PinTop(tabBar, 104f, 56f, 48f);
            var tabLayout = tabBar.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            tabLayout.spacing = 12f;
            tabLayout.childControlWidth = tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = tabLayout.childForceExpandHeight = true;
            for (int i = 0; i < TabNames.Length; i++)
            {
                int tab = i;
                var button = SettingsMenuUi.CreateButton(tabBar, TabNames[i], 56f, 26, () => ShowTab(tab, true), out _);
                var marker = SettingsMenuUi.CreateImage("ActiveMarker", button.transform, UiTheme.StampRed, false);
                marker.rectTransform.anchorMin = new Vector2(0f, 0f);
                marker.rectTransform.anchorMax = new Vector2(1f, 0f);
                marker.rectTransform.pivot = new Vector2(0.5f, 0f);
                marker.rectTransform.offsetMin = Vector2.zero;
                marker.rectTransform.offsetMax = new Vector2(0f, 5f);
                tabButtons.Add(button);
                tabMarkers.Add(marker.gameObject);
            }

            var content = SettingsMenuUi.CreateRect("Content", panel);
            SettingsMenuUi.Stretch(content, 48f, 48f, 180f, 108f);

            BuildGraphicsPage(AddPage(content, "Grafika", out var gfxItems), gfxItems);
            BuildControlsPage(AddPage(content, "Sterowanie", out var ctrlItems), ctrlItems);
            BuildAudioPage(AddPage(content, "Dzwiek", out var audioItems), audioItems);
            BuildContentPage(AddPage(content, "Tresci", out var contentItems), contentItems);

            var hint = SettingsMenuUi.CreateText("Hint", panel, "Esc / B – wróć    LB / RB – zakładki    ‹ › – zmień",
                20, TextAnchor.MiddleLeft, FontStyle.Normal, SettingsMenuUi.MutedTextColor);
            var hintRt = hint.rectTransform;
            hintRt.anchorMin = new Vector2(0f, 0f);
            hintRt.anchorMax = new Vector2(1f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.offsetMin = new Vector2(48f, 28f);
            hintRt.offsetMax = new Vector2(-400f, 88f);

            backButton = SettingsMenuUi.CreateButton(panel, "Wróć", 60f, 28, Close, out _);
            var backRt = (RectTransform)backButton.transform;
            backRt.anchorMin = backRt.anchorMax = backRt.pivot = new Vector2(1f, 0f);
            backRt.sizeDelta = new Vector2(320f, 60f);
            backRt.anchoredPosition = new Vector2(-48f, 28f);
        }

        RectTransform AddPage(RectTransform content, string pageName, out List<UnityEngine.UI.Selectable> items)
        {
            var page = SettingsMenuUi.CreateColumn("Page" + pageName, content, 6f);
            SettingsMenuUi.Stretch(page);
            pages.Add(page.gameObject);
            items = new List<UnityEngine.UI.Selectable>();
            pageItems.Add(items);
            return page;
        }

        void ShowTab(int index, bool selectTab)
        {
            currentTab = Mathf.Clamp(index, 0, pages.Count - 1);
            for (int i = 0; i < pages.Count; i++)
            {
                pages[i].SetActive(i == currentTab);
                tabMarkers[i].SetActive(i == currentTab);
            }
            LinkNavigation();
            if (selectTab) SettingsMenuUi.Select(tabButtons[currentTab]);
        }

        void LinkNavigation()
        {
            var items = pageItems[currentTab];
            int tabs = tabButtons.Count;
            UnityEngine.UI.Selectable first = items.Count > 0 ? items[0] : backButton;
            UnityEngine.UI.Selectable last = items.Count > 0 ? items[items.Count - 1] : tabButtons[currentTab];

            for (int i = 0; i < tabs; i++)
                SettingsMenuUi.SetNavigation(tabButtons[i], backButton, first, tabButtons[(i + tabs - 1) % tabs], tabButtons[(i + 1) % tabs]);
            for (int i = 0; i < items.Count; i++)
            {
                var up = i == 0 ? tabButtons[currentTab] : items[i - 1];
                var down = i == items.Count - 1 ? (UnityEngine.UI.Selectable)backButton : items[i + 1];
                SettingsMenuUi.SetNavigation(items[i], up, down);
            }
            SettingsMenuUi.SetNavigation(backButton, last, tabButtons[currentTab]);
        }

        // ---------- Pages ----------

        void BuildGraphicsPage(RectTransform page, List<UnityEngine.UI.Selectable> items)
        {
            items.Add(SettingsMenuUi.CreateStepper(page, "Profil jakości", QualitySettings.names, GameGraphicsSettings.QualityLevel, i =>
            {
                GameGraphicsSettings.QualityLevel = i;
                GraphicsChanged();
            }));

            resolutions = CollectResolutions();
            var resolutionNames = resolutions.ConvertAll(r => $"{r.x} × {r.y}").ToArray();
            items.Add(SettingsMenuUi.CreateStepper(page, "Rozdzielczość", resolutionNames, CurrentResolutionIndex(), i =>
            {
                GameGraphicsSettings.ResolutionWidth = resolutions[i].x;
                GameGraphicsSettings.ResolutionHeight = resolutions[i].y;
                GraphicsChanged();
            }));

            int modeIndex = Mathf.Max(0, Array.IndexOf(ScreenModes, GameGraphicsSettings.ScreenMode));
            items.Add(SettingsMenuUi.CreateStepper(page, "Tryb ekranu", ScreenModeNames, modeIndex, i =>
            {
                GameGraphicsSettings.ScreenMode = ScreenModes[i];
                GraphicsChanged();
            }));

            items.Add(SettingsMenuUi.CreateStepper(page, "Synchronizacja pionowa (VSync)", OffOn, GameGraphicsSettings.VSync ? 1 : 0, i =>
            {
                GameGraphicsSettings.VSync = i == 1;
                GraphicsChanged();
            }));

            var caps = GameGraphicsSettings.FrameCaps;
            var capNames = Array.ConvertAll(caps, c => c > 0 ? c.ToString() : "Bez limitu");
            int capIndex = Array.IndexOf(caps, GameGraphicsSettings.FrameCap);
            if (capIndex < 0) capIndex = caps.Length - 1;
            items.Add(SettingsMenuUi.CreateStepper(page, "Limit klatek (FPS)", capNames, capIndex, i =>
            {
                GameGraphicsSettings.FrameCap = caps[i];
                GraphicsChanged();
            }));

            items.Add(SettingsMenuUi.CreateSlider(page, "Skala renderowania", 5, 15, Mathf.RoundToInt(GameGraphicsSettings.RenderScale * 10f),
                v => $"{v * 10f:0}%", v =>
                {
                    GameGraphicsSettings.RenderScale = v / 10f;
                    GraphicsChanged();
                }));

            items.Add(SettingsMenuUi.CreateStepper(page, "Cienie", new[] { "Wył.", "Niskie", "Wysokie" }, (int)GameGraphicsSettings.Shadows, i =>
            {
                GameGraphicsSettings.Shadows = (GameGraphicsSettings.ShadowSetting)i;
                GraphicsChanged();
            }));

            items.Add(SettingsMenuUi.CreateStepper(page, "Wygładzanie krawędzi", new[] { "Wył.", "FXAA", "SMAA", "MSAA 4×" },
                (int)GameGraphicsSettings.AntiAliasing, i =>
                {
                    GameGraphicsSettings.AntiAliasing = (GameGraphicsSettings.AntiAliasingSetting)i;
                    GraphicsChanged();
                }));

            items.Add(SettingsMenuUi.CreateStepper(page, "Efekty obrazu (post-processing)", OffOn, GameGraphicsSettings.PostProcessing ? 1 : 0, i =>
            {
                GameGraphicsSettings.PostProcessing = i == 1;
                GraphicsChanged();
            }));

            items.Add(SettingsMenuUi.CreateStepper(page, "Mgła", OffOn, GameGraphicsSettings.Fog ? 1 : 0, i =>
            {
                GameGraphicsSettings.Fog = i == 1;
                GraphicsChanged();
            }));

            // FOV in 5-degree steps so a gamepad needs 8 presses end to end; the camera reads GameSettings every frame.
            items.Add(SettingsMenuUi.CreateSlider(page, "Pole widzenia (FOV)", 12, 20, Mathf.RoundToInt(GameSettings.FieldOfView / 5f),
                v => $"{v * 5f:0}°", v =>
                {
                    GameSettings.FieldOfView = v * 5f;
                    GameSettingsChanged();
                }));
        }

        void BuildControlsPage(RectTransform page, List<UnityEngine.UI.Selectable> items)
        {
            items.Add(SettingsMenuUi.CreateSlider(page, "Czułość celowania", 1, 30, Mathf.RoundToInt(GameSettings.MouseSensitivity * 10f),
                v => $"{v / 10f:0.0}×", v =>
                {
                    GameSettings.MouseSensitivity = v / 10f;
                    GameSettingsChanged();
                }));

            items.Add(SettingsMenuUi.CreateStepper(page, "Odwróć oś Y", OffOn, GameSettings.InvertY ? 1 : 0, i =>
            {
                GameSettings.InvertY = i == 1;
                GameSettingsChanged();
            }));

            items.Add(SettingsMenuUi.CreateStepper(page, "Kołysanie kamery przy chodzie", OffOn, GameSettings.HeadBob ? 1 : 0, i =>
            {
                GameSettings.HeadBob = i == 1;
                GameSettingsChanged();
            }));

            items.Add(SettingsMenuUi.CreateSlider(page, "Wstrząsy kamery", 0, 10, Mathf.RoundToInt(GameSettings.CameraShake * 10f),
                v => $"{v * 10f:0}%", v =>
                {
                    GameSettings.CameraShake = v / 10f;
                    GameSettingsChanged();
                }));
        }

        void BuildAudioPage(RectTransform page, List<UnityEngine.UI.Selectable> items)
        {
            items.Add(SettingsMenuUi.CreateSlider(page, "Głośność ogólna", 0, 20, Mathf.RoundToInt(GameSettings.MasterVolume * 20f),
                v => $"{v * 5f:0}%", v =>
                {
                    GameSettings.MasterVolume = v / 20f;
                    AudioListener.volume = GameSettings.MasterVolume; // audible right away; Save() follows
                    GameSettingsChanged();
                }));
        }

        void BuildContentPage(RectTransform page, List<UnityEngine.UI.Selectable> items)
        {
            items.Add(SettingsMenuUi.CreateStepper(page, "Krew (16+)", OffOn, GameSettings.MatureContent ? 1 : 0, i =>
            {
                GameSettings.MatureContent = i == 1;
                GameSettingsChanged();
            }));

            // Hint under the row; leading spaces line it up with the row label.
            var note = SettingsMenuUi.CreateText("Note", page, "      Brutalniejsze efekty trafień. Domyślnie wyłączone.",
                22, TextAnchor.MiddleLeft, FontStyle.Italic, SettingsMenuUi.MutedTextColor);
            var layout = note.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            layout.preferredHeight = 36f;
        }

        // ---------- Resolutions ----------

        static List<Vector2Int> CollectResolutions()
        {
            var list = new List<Vector2Int>();
            foreach (var r in Screen.resolutions)
            {
                var size = new Vector2Int(r.width, r.height);
                if (!list.Contains(size)) list.Add(size);
            }
            var current = new Vector2Int(Screen.width, Screen.height);
            if (!list.Contains(current)) list.Add(current);
            list.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return list;
        }

        int CurrentResolutionIndex()
        {
            var wanted = GameGraphicsSettings.ResolutionWidth > 0
                ? new Vector2Int(GameGraphicsSettings.ResolutionWidth, GameGraphicsSettings.ResolutionHeight)
                : new Vector2Int(Screen.width, Screen.height);
            int index = resolutions.IndexOf(wanted);
            if (index < 0) index = resolutions.IndexOf(new Vector2Int(Screen.width, Screen.height));
            return Mathf.Max(0, index);
        }
    }
}
