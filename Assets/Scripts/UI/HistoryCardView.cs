using UnityEngine;
using UnityEngine.InputSystem;

namespace Kosynierzy
{
    /// <summary>
    /// Parchment card with a short historical fact ("Karta historii X/6") or a found note (ShowText).
    /// Freezes the world with timeScale 0 (NOT GameState pause, which would open the pause menu) and turns
    /// player input off; E / Esc / gamepad B closes and gives control back. uGUI built in code.
    /// </summary>
    public class HistoryCardView : MonoBehaviour
    {
        static readonly (string Title, string Body)[] Facts =
        {
            ("Wieża z modrzewia",
             "Wieża radiostacji w Gliwicach ma około 111 metrów i jest zbudowana z drewna modrzewiowego. " +
             "To jedna z najwyższych drewnianych konstrukcji w Europie. Stoi do dziś przy ul. Tarnogórskiej."),
            ("Alfred Naujocks",
             "Napadem na radiostację dowodził Alfred Naujocks z SD, na rozkaz Reinharda Heydricha. " +
             "Jego ludzie byli przebrani za polskich powstańców. Prawdę o prowokacji Naujocks ujawnił sam, " +
             "w zeznaniu dla Trybunału w Norymberdze w 1945 roku."),
            ("„Großmutter gestorben”",
             "Hasłem do rozpoczęcia akcji były słowa „Großmutter gestorben” — „Babcia umarła”. " +
             "Naujocks czekał na nie w gliwickim hotelu Haus Oberschlesien. " +
             "Dziś w tym budynku przy ul. Zwycięstwa mieści się Urząd Miejski."),
            ("Mikrofon burzowy",
             "Napastnicy nie potrafili puścić sygnału przez główne studio. Użyli mikrofonu burzowego, " +
             "który służył do nadawania komunikatów o burzach. Komunikat po polsku trwał kilka minut " +
             "i słychać go było tylko w okolicy."),
            ("Franciszek Honiok",
             "Franciszek Honiok był Ślązakiem sympatyzującym z Polską, uczestnikiem powstania śląskiego. " +
             "Dzień przed prowokacją zatrzymało go Gestapo; jego ciało porzucono w radiostacji jako „dowód” " +
             "polskiego napadu. Takie ofiary oprawcy nazywali kryptonimem „Konserve”; dziś pamiętamy go " +
             "z imienia i nazwiska."),
            ("Operacja Himmler",
             "Prowokacja gliwicka była częścią Operacji Himmler — serii upozorowanych „polskich napadów”, " +
             "m.in. w Stodołach (Hochlinden) i Byczynie (Pitschen). 1 września 1939 r. Niemcy zaatakowały Polskę, " +
             "a Hitler mówił w Reichstagu, że „od 5:45 odpowiadamy ogniem”."),
        };

        static readonly Color Ink = UiTheme.Ink;
        static readonly Color FadedInk = UiTheme.FadedInk;
        const float InputDelay = 0.3f; // the E that picked the card up must not close it

        static HistoryCardView instance;

        /// <summary>True while a card or note is on screen.</summary>
        public static bool IsOpen => instance != null && instance.open;

        /// <summary>Also true on the frame the card closed, so the Esc that closed it does not open the pause menu.</summary>
        public static bool BlocksPauseInput => IsOpen || (instance != null && instance.closedFrame == Time.frameCount);

        [SerializeField] int sortingOrder = 90;

        Canvas canvas;
        CanvasGroup group;
        UnityEngine.UI.Text headerText, titleText, bodyText;
        RectTransform archiveStamp, noteStamp;
        InputAction closeAction;
        readonly PlayerControlLock controlLock = new();
        bool open, pendingUnlock;
        float openedAt;
        int closedFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        static HistoryCardView Instance
        {
            get
            {
                if (instance == null) instance = new GameObject("[HistoryCardView]").AddComponent<HistoryCardView>();
                return instance;
            }
        }

        /// <summary>Shows history card 1..6 (facts from docs/FABULA.md).</summary>
        public static void ShowCard(int index)
        {
            if (index < 1 || index > Facts.Length)
            {
                Debug.LogWarning($"Kosynierzy: no history card {index} (1..{Facts.Length}).");
                index = Mathf.Clamp(index, 1, Facts.Length);
            }
            var fact = Facts[index - 1];
            int found = Mathf.Max(1, GameProgress.HistoryCards.Count);
            Instance.Show($"Karta historii {found}/{GameProgress.HistoryCardTotal}", fact.Title, fact.Body);
        }

        /// <summary>Shows a note or document (e.g. Gruber's note) on the same parchment.</summary>
        public static void ShowText(string title, string body) => Instance.Show(null, title, body);

        void Awake()
        {
            closeAction = new InputAction("CloseCard", InputActionType.Button, "<Keyboard>/e");
            closeAction.AddBinding("<Keyboard>/escape");
            closeAction.AddBinding("<Gamepad>/buttonEast");
            Build();
        }

        void OnEnable() => closeAction.Enable();
        void OnDisable() => closeAction.Disable();

        void OnDestroy()
        {
            if (open) Time.timeScale = 1f; // scene reloaded under the card: do not carry frozen time over
            controlLock.Unlock();
            closeAction?.Dispose();
        }

        void Show(string header, string title, string body)
        {
            bool hasHeader = !string.IsNullOrEmpty(header);
            headerText.text = hasHeader ? header : "";
            headerText.gameObject.SetActive(hasHeader);
            archiveStamp.gameObject.SetActive(hasHeader);
            noteStamp.gameObject.SetActive(!hasHeader);
            titleText.text = (title ?? "").ToUpperInvariant();
            bodyText.text = body ?? "";

            pendingUnlock = false;
            if (!open)
            {
                open = true;
                controlLock.Lock();
                canvas.gameObject.SetActive(true);
                group.alpha = 0f;
            }
            openedAt = Time.unscaledTime;
            Time.timeScale = 0f;
        }

        void Update()
        {
            if (pendingUnlock && Time.frameCount > closedFrame)
            {
                // One frame after closing, so the closing E press is not seen by the re-enabled interactor.
                pendingUnlock = false;
                controlLock.Unlock(); // FirstPersonController.OnEnable locks the cursor again
                PlayerControlLock.SetCursorFree(false);
            }
            if (!open) return;
            if (GameState.IsPaused) return; // the pause menu owns time while it is open
            Time.timeScale = 0f;            // slow-motion recovery must not restart the world under the card
            group.alpha = Mathf.Clamp01((Time.unscaledTime - openedAt) / 0.2f);
            if (Time.unscaledTime - openedAt >= InputDelay && closeAction.WasPressedThisFrame()) Close();
        }

        void Close()
        {
            open = false;
            closedFrame = Time.frameCount;
            canvas.gameObject.SetActive(false);
            Time.timeScale = 1f;
            pendingUnlock = true;
        }

        void Build()
        {
            canvas = SettingsMenuUi.CreateCanvas("HistoryCardCanvas", transform, sortingOrder);
            group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            var root = canvas.transform;

            var dim = SettingsMenuUi.CreateImage("Dim", root, UiTheme.WithAlpha(UiTheme.Dark, 0.6f), false);
            SettingsMenuUi.Stretch(dim.rectTransform);

            var panel = UiTheme.CreatePaper(root, "Card", new Vector2(1000f, 620f), out var cardRoot);
            cardRoot.localRotation = Quaternion.Euler(0f, 0f, 0.8f); // a sheet dropped on the desk, not a window

            headerText = SettingsMenuUi.CreateText("Header", panel, "", 22, TextAnchor.MiddleLeft, FontStyle.Bold, FadedInk);
            SettingsMenuUi.PinTop(headerText.rectTransform, 28f, 30f, 60f);

            archiveStamp = UiTheme.CreateStamp(panel, "Archiwum", UiTheme.Olive, 22);
            noteStamp = UiTheme.CreateStamp(panel, "Notatka", UiTheme.StampRed, 22);
            foreach (var s in new[] { archiveStamp, noteStamp })
            {
                s.anchorMin = s.anchorMax = new Vector2(1f, 1f);
                s.anchoredPosition = new Vector2(-130f, -48f);
            }

            titleText = UiTheme.CreateHeading("Title", panel, "", 42, Ink);
            SettingsMenuUi.PinTop(titleText.rectTransform, 74f, 64f, 60f);

            var rule = UiTheme.CreateRule("Rule", panel, FadedInk);
            SettingsMenuUi.PinTop(rule.rectTransform, 148f, 2f, 60f);

            bodyText = SettingsMenuUi.CreateText("Body", panel, "", 28, TextAnchor.UpperLeft, FontStyle.Normal, Ink);
            bodyText.lineSpacing = 1.15f;
            SettingsMenuUi.Stretch(bodyText.rectTransform, 70f, 70f, 176f, 80f);

            var hint = SettingsMenuUi.CreateText("Hint", panel, "E — zamknij", 22, TextAnchor.MiddleRight, FontStyle.Bold, FadedInk);
            var hintRt = hint.rectTransform;
            hintRt.anchorMin = new Vector2(0f, 0f);
            hintRt.anchorMax = new Vector2(1f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.offsetMin = new Vector2(60f, 24f);
            hintRt.offsetMax = new Vector2(-50f, 60f);
        }

        static void Centre(RectTransform rt, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
        }
    }
}
