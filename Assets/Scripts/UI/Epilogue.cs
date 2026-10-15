using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Kosynierzy
{
    /// <summary>
    /// Ending: black boards with white text. Boards come from dialogues "epilog_1".."epilog_6" (napis/tekst + optional
    /// Resources/Audio clip); a missing dialogue falls back to the texts from docs/FABULA.md. The last board is always
    /// "Prowokacja trwała kilka minut. Wojna trwała sześć lat." Then "KONIEC" with Menu główne / Zagraj ponownie.
    /// Space / E / gamepad A skips a board. The world is frozen (timeScale 0) and player input is off.
    /// </summary>
    public class Epilogue : MonoBehaviour
    {
        const string MenuSceneName = "Menu";
        const string FinalBoard = "Prowokacja trwała kilka minut. Wojna trwała sześć lat.";
        const float SecondsPerChar = 0.06f;
        const float MinBoardSeconds = 4f;

        /// <summary>Fallbacks for epilog_1..epilog_6 (FABULA.md, "Epilog").</summary>
        static readonly string[] FallbackBoards =
        {
            "1 września 1939 roku o świcie pancernik „Schleswig-Holstein” otwiera ogień do Westerplatte. Niemcy atakują Polskę.",
            "Tego samego dnia Hitler mówi w Reichstagu, że „od 5:45 odpowiadamy ogniem”. Na dowód podaje „polskie napady” — takie jak ten w Gliwicach.",
            "Franciszek Honiok, Ślązak i uczestnik powstania śląskiego, nie był napastnikiem. Został zamordowany, a jego ciało porzucono w radiostacji jako „dowód”.",
            "Jan Wrona, kryptonim „Kruk”, jest postacią fikcyjną. Prowokacja gliwicka wydarzyła się naprawdę.",
            "Prawdę o prowokacji ujawnił sam Alfred Naujocks w zeznaniu dla Trybunału w Norymberdze 20 listopada 1945 roku.",
            "Drewniana wieża radiostacji stoi do dziś. Przy ul. Tarnogórskiej 129 w Gliwicach działa muzeum.",
        };

        static Epilogue instance;

        /// <summary>True from Play() until the scene changes (also on the final "KONIEC" screen).</summary>
        public static bool IsPlaying => instance != null && instance.playing;

        [SerializeField] int sortingOrder = 130;

        Canvas canvas;
        CanvasGroup blackGroup, textGroup;
        UnityEngine.UI.Text boardText, hintText;
        GameObject endPanel;
        UnityEngine.UI.Button menuButton, replayButton;
        UnityEngine.UI.Selectable firstEndButton;
        AudioSource voice;
        InputAction skipAction;
        readonly PlayerControlLock controlLock = new();
        bool playing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        /// <summary>Starts the ending; called by the mission after the courier scene.</summary>
        public static void Play()
        {
            if (IsPlaying) return;
            if (instance == null) instance = new GameObject("[Epilogue]").AddComponent<Epilogue>();
            instance.StartCoroutine(instance.Run());
        }

        void Awake()
        {
            SettingsMenuUi.EnsureEventSystem();
            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0f;
            skipAction = new InputAction("EpilogueSkip", InputActionType.Button, "<Keyboard>/space");
            skipAction.AddBinding("<Keyboard>/e");
            skipAction.AddBinding("<Gamepad>/buttonSouth");
            Build();
        }

        void OnEnable() => skipAction.Enable();
        void OnDisable() => skipAction.Disable();

        void OnDestroy()
        {
            if (instance == this) instance = null;
            if (playing)
            {
                Time.timeScale = 1f;
                controlLock.Unlock();
            }
            skipAction?.Dispose();
        }

        void Update()
        {
            if (!playing) return;
            Time.timeScale = 0f; // keep the world (and its enemies) frozen behind the boards
            if (endPanel.activeSelf) SettingsMenuUi.RestoreSelection(firstEndButton);
        }

        IEnumerator Run()
        {
            playing = true;
            if (GameState.IsPaused) GameState.SetPaused(false);
            controlLock.Lock();
            PlayerControlLock.SetCursorFree(false);
            Time.timeScale = 0f;

            endPanel.SetActive(false);
            boardText.text = "";
            textGroup.alpha = 0f;
            blackGroup.alpha = 0f;
            hintText.gameObject.SetActive(true);
            canvas.gameObject.SetActive(true);

            yield return Fade(blackGroup, 0f, 1f, 1.2f);
            ScreenFade.SetAlpha(0f); // the IMGUI door fade would draw over this canvas

            for (int i = 1; i <= FallbackBoards.Length; i++)
            {
                var lines = Dialog.GetLines("epilog_" + i);
                if (lines == null || lines.Count == 0)
                {
                    yield return Board(FallbackBoards[i - 1], null);
                    continue;
                }
                foreach (var line in lines)
                {
                    string text = string.IsNullOrEmpty(line.Subtitle) ? line.Text : line.Subtitle;
                    var clip = string.IsNullOrEmpty(line.Audio) ? null : Resources.Load<AudioClip>("Audio/" + line.Audio);
                    yield return Board(text, clip);
                }
            }

            yield return Board(FinalBoard, null);

            ShowEndScreen();
        }

        IEnumerator Board(string text, AudioClip clip)
        {
            boardText.text = text ?? "";
            float duration = clip != null
                ? clip.length + 0.3f
                : Mathf.Max(MinBoardSeconds, (text?.Length ?? 0) * SecondsPerChar);
            if (clip != null) voice.PlayOneShot(clip);

            yield return Fade(textGroup, 0f, 1f, 0.6f);
            float end = Time.unscaledTime + duration;
            yield return null; // a key held from the previous board must not skip this one
            while (Time.unscaledTime < end)
            {
                if (skipAction.WasPressedThisFrame()) break;
                yield return null;
            }
            voice.Stop();
            yield return Fade(textGroup, 1f, 0f, 0.5f);
        }

        static IEnumerator Fade(CanvasGroup group, float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }
            group.alpha = to;
        }

        void ShowEndScreen()
        {
            boardText.text = "";
            hintText.gameObject.SetActive(false);
            menuButton.gameObject.SetActive(Application.CanStreamedLevelBeLoaded(MenuSceneName));
            firstEndButton = menuButton.gameObject.activeSelf ? menuButton : replayButton;
            if (menuButton.gameObject.activeSelf)
                SettingsMenuUi.LinkVertical(new UnityEngine.UI.Selectable[] { menuButton, replayButton });
            else
                SettingsMenuUi.LinkVertical(new UnityEngine.UI.Selectable[] { replayButton });

            endPanel.SetActive(true);
            PlayerControlLock.SetCursorFree(true);
            SettingsMenuUi.Select(firstEndButton);
        }

        void GoToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(MenuSceneName);
        }

        void PlayAgain()
        {
            Checkpoints.NewGame();
            Checkpoints.Restart();
        }

        void Build()
        {
            canvas = SettingsMenuUi.CreateCanvas("EpilogueCanvas", transform, sortingOrder);
            blackGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            var root = canvas.transform;

            var black = SettingsMenuUi.CreateImage("Black", root, Color.black, true);
            SettingsMenuUi.Stretch(black.rectTransform);

            boardText = SettingsMenuUi.CreateText("Board", root, "", 46, TextAnchor.MiddleCenter, FontStyle.Normal, Color.white);
            var boardRt = boardText.rectTransform;
            boardRt.anchorMin = boardRt.anchorMax = boardRt.pivot = new Vector2(0.5f, 0.5f);
            boardRt.sizeDelta = new Vector2(1300f, 600f);
            boardRt.anchoredPosition = Vector2.zero;
            boardText.lineSpacing = 1.2f;
            textGroup = boardText.gameObject.AddComponent<CanvasGroup>();

            hintText = SettingsMenuUi.CreateText("Hint", root, "Spacja / E — dalej", 22, TextAnchor.LowerRight, FontStyle.Normal,
                new Color(1f, 1f, 1f, 0.35f));
            SettingsMenuUi.Stretch(hintText.rectTransform, 40f, 40f, 40f, 32f);

            var panel = SettingsMenuUi.CreateRect("End", root);
            SettingsMenuUi.Stretch(panel);
            endPanel = panel.gameObject;

            var title = SettingsMenuUi.CreateText("Koniec", panel, "KONIEC", 120, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = titleRt.pivot = new Vector2(0.5f, 0.5f);
            titleRt.sizeDelta = new Vector2(1200f, 160f);
            titleRt.anchoredPosition = new Vector2(0f, 140f);

            var column = SettingsMenuUi.CreateColumn("Buttons", panel, 16f);
            column.anchorMin = column.anchorMax = new Vector2(0.5f, 0.5f);
            column.pivot = new Vector2(0.5f, 1f);
            column.sizeDelta = new Vector2(520f, 2 * 66f + 16f);
            column.anchoredPosition = new Vector2(0f, -10f);

            menuButton = SettingsMenuUi.CreateButton(column, "Menu główne", 66f, 32, GoToMenu, out _);
            replayButton = SettingsMenuUi.CreateButton(column, "Zagraj ponownie", 66f, 32, PlayAgain, out _);
        }
    }
}
