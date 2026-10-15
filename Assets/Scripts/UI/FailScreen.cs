using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kosynierzy
{
    /// <summary>
    /// "PORAŻKA" screen: 0.8 s (real time) after MissionFail.Failed it covers the screen with the failure message and
    /// "Spróbuj od punktu kontrolnego" (or "Zacznij od nowa" in historical mode / before the first checkpoint) and
    /// "Menu główne". Freezes time, frees the cursor, turns player input off. Also routes the pause menu's
    /// checkpoint button to Checkpoints.Reload(). Created automatically in scenes with a FirstPersonController.
    /// </summary>
    [DisallowMultipleComponent]
    public class FailScreen : MonoBehaviour
    {
        const string MenuSceneName = "Menu";
        const float ShowDelay = 0.8f;
        static readonly Color Background = new(0.024f, 0.031f, 0.039f, 0.7f);
        static readonly Color FailRed = MenuV2.Red;

        static FailScreen instance;

        /// <summary>True while the failure screen is visible.</summary>
        public static bool IsShown => instance != null && instance.shown;

        [SerializeField] int sortingOrder = 120;

        Canvas canvas;
        CanvasGroup group;
        UnityEngine.UI.Text messageText;
        UnityEngine.UI.Button checkpointButton, restartButton, menuButton;
        readonly PlayerControlLock controlLock = new();
        UnityEngine.UI.Selectable firstButton;
        bool shown;
        float shownAt;
        Coroutine pending;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            MissionFail.Failed -= OnFailed;
            MissionFail.Failed += OnFailed;
            PauseMenu.RestartFromCheckpointRequested -= OnCheckpointRequested;
            PauseMenu.RestartFromCheckpointRequested += OnCheckpointRequested;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            CreateIfPlayerPresent();
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => CreateIfPlayerPresent();

        static void CreateIfPlayerPresent()
        {
            if (instance == null && UnityEngine.Object.FindAnyObjectByType<FirstPersonController>() != null) Create();
        }

        static FailScreen Create() => new GameObject("[FailScreen]").AddComponent<FailScreen>();

        static void OnFailed(string message)
        {
            if (Epilogue.IsPlaying) return;
            var screen = instance != null ? instance : Create();
            screen.ScheduleShow();
        }

        static void OnCheckpointRequested() => Checkpoints.Reload();

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            SettingsMenuUi.EnsureEventSystem();
            Build();
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
            if (!shown) return;
            // Runs when a reload unloads the scene: the frozen time must not leak into the next scene.
            Time.timeScale = 1f;
            controlLock.Unlock();
        }

        void ScheduleShow()
        {
            if (shown || pending != null) return;
            pending = StartCoroutine(ShowAfterDelay());
        }

        IEnumerator ShowAfterDelay()
        {
            yield return new WaitForSecondsRealtime(ShowDelay);
            pending = null;
            Show();
        }

        void Show()
        {
            if (GameState.IsPaused) GameState.SetPaused(false); // close the pause menu if it was somehow open

            messageText.text = MissionFail.Message ?? "";
            bool canUseCheckpoint = !GameMode.Historical && Checkpoints.SavedStep >= 0;
            checkpointButton.gameObject.SetActive(canUseCheckpoint);
            restartButton.gameObject.SetActive(!canUseCheckpoint);
            menuButton.gameObject.SetActive(Application.CanStreamedLevelBeLoaded(MenuSceneName));

            var buttons = new List<UnityEngine.UI.Selectable>();
            foreach (var b in new[] { checkpointButton, restartButton, menuButton })
                if (b.gameObject.activeSelf) buttons.Add(b);
            SettingsMenuUi.LinkVertical(buttons);
            firstButton = buttons.Count > 0 ? buttons[0] : null;

            shown = true;
            shownAt = Time.unscaledTime;
            controlLock.Lock();
            Time.timeScale = 0f;
            PlayerControlLock.SetCursorFree(true);
            group.alpha = 0f;
            canvas.gameObject.SetActive(true);
            SettingsMenuUi.Select(firstButton);
        }

        void Update()
        {
            if (!shown) return;
            Time.timeScale = 0f;
            if (Cursor.lockState != CursorLockMode.None) PlayerControlLock.SetCursorFree(true);
            group.alpha = Mathf.Clamp01((Time.unscaledTime - shownAt) / 0.4f);
            SettingsMenuUi.RestoreSelection(firstButton);
        }

        void RetryFromCheckpoint() => Checkpoints.Reload();

        void StartOver()
        {
            GameReset.Wipe();
            Checkpoints.Restart();
        }

        void GoToMenu()
        {
            MissionFail.Clear();
            Time.timeScale = 1f;
            SceneManager.LoadScene(MenuSceneName);
        }

        void Build()
        {
            canvas = SettingsMenuUi.CreateCanvas("FailCanvas", transform, sortingOrder);
            group = canvas.gameObject.AddComponent<CanvasGroup>();
            var root = canvas.transform;

            var background = SettingsMenuUi.CreateImage("Background", root, Background, true);
            SettingsMenuUi.Stretch(background.rectTransform);

            // Bare centred banner on the wash: label, title, red rule, two buttons (no card).
            var banner = MenuV2.Label("Banner", root, "ZADANIE NIEUDANE", 16, TextAnchor.MiddleCenter, FailRed);
            SettingsMenuUi.Place(banner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(800f, 24f));

            messageText = MenuV2.Label("Message", root, "", 64, TextAnchor.MiddleCenter, MenuV2.Bone);
            messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
            messageText.resizeTextForBestFit = true;
            messageText.resizeTextMinSize = 32;
            messageText.resizeTextMaxSize = 64;
            SettingsMenuUi.Place(messageText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(1300f, 130f));

            var rule = MenuV2.Box("Rule", root, FailRed);
            SettingsMenuUi.Place(rule.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(160f, 3f));

            var row = SettingsMenuUi.CreateRect("Buttons", root);
            SettingsMenuUi.Place(row, new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(700f, 60f));
            var layout = row.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.spacing = 20f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;

            // Primary (bone-filled) retry, outlined secondary actions; only the relevant ones are shown.
            checkpointButton = MenuV2.CreateButton(row, "Od punktu kontrolnego", MenuV2.Kind.Filled, 60f, 22, RetryFromCheckpoint, out _);
            restartButton = MenuV2.CreateButton(row, "Zacznij od nowa", MenuV2.Kind.Filled, 60f, 22, StartOver, out _);
            menuButton = MenuV2.CreateButton(row, "Menu główne", MenuV2.Kind.Outline, 60f, 22, GoToMenu, out _);
        }

        static void Place(RectTransform rt, float x, float y, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = new Vector2(x, y);
        }
    }
}
