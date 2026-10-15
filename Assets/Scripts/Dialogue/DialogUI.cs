using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Subtitles (uGUI, UiTheme style): a dark bar at the bottom, the speaker's name in small capitals above the
    /// line, typewriter text in muted white; Kruk's thoughts in italics without a name. Added by Dialog.
    /// </summary>
    public class DialogUI : MonoBehaviour
    {
        const float BarWidth = 1240f;

        [SerializeField] int sortingOrder = 20;

        Canvas canvas;
        CanvasGroup group;
        RectTransform bar;
        UnityEngine.UI.Text nameText, lineText;
        UnityEngine.UI.Image nameRule;
        Dialog.Line shownLine;

        static Color SpeakerColor(string who) => who switch
        {
            "kruk" => UiTheme.LightOlive,
            "zofia" => UiTheme.Brass,
            "naujocks" or "heinz" or "sd" => UiTheme.AlertRed,
            "schupo" => UiTheme.Amber,
            "gruber" => new Color(0.72f, 0.8f, 0.68f),
            "sowa" => new Color(0.78f, 0.74f, 0.9f),
            _ => UiTheme.WithAlpha(UiTheme.MutedWhite, 0.85f),
        };

        static string DisplayName(string who) => string.IsNullOrEmpty(who) ? "" : who switch
        {
            "kruk" => "Kruk",
            "zofia" => "Pani Zofia",
            "sd" => "SD",
            "schupo" => "Schupo",
            _ => char.ToUpperInvariant(who[0]) + who.Substring(1),
        };

        void Awake()
        {
            canvas = SettingsMenuUi.CreateCanvas("SubtitleCanvas", transform, sortingOrder);
            var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster != null) Destroy(raycaster);
            group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            var root = canvas.transform;

            var barImage = UiTheme.CreateDarkBar("Bar", root, 0.82f);
            bar = barImage.rectTransform;
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = new Vector2(0f, 128f);
            bar.sizeDelta = new Vector2(BarWidth, 120f);

            nameText = SettingsMenuUi.CreateText("Speaker", bar, "", 18, TextAnchor.MiddleLeft, FontStyle.Bold, UiTheme.MutedWhite);
            nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            SettingsMenuUi.PinTop(nameText.rectTransform, 12f, 24f, 40f);
            nameRule = UiTheme.CreateRule("Rule", bar, UiTheme.WithAlpha(UiTheme.MutedWhite, 0.25f), 1f);
            SettingsMenuUi.PinTop(nameRule.rectTransform, 40f, 1f, 40f);

            lineText = SettingsMenuUi.CreateText("Line", bar, "", 27, TextAnchor.UpperLeft, FontStyle.Normal, UiTheme.MutedWhite);
            lineText.lineSpacing = 1.1f;

            canvas.gameObject.SetActive(true);
        }

        void LateUpdate()
        {
            var line = Dialog.CurrentLine;
            bool show = line != null && !GameState.IsPaused;
            group.alpha = Mathf.MoveTowards(group.alpha, show ? 1f : 0f, Time.unscaledDeltaTime * 8f);
            if (line == null || line == shownLine) return;
            shownLine = line;

            string text = string.IsNullOrEmpty(line.Subtitle) ? line.Text : line.Subtitle;
            bool thought = line.Thought;
            bool named = !thought && !string.IsNullOrEmpty(line.Who);

            nameText.gameObject.SetActive(named);
            nameRule.gameObject.SetActive(named);
            if (named)
            {
                nameText.text = SmallCaps(DisplayName(line.Who));
                nameText.color = SpeakerColor(line.Who);
            }

            lineText.text = text ?? "";
            lineText.fontStyle = thought ? FontStyle.Italic : FontStyle.Normal;
            lineText.color = thought ? UiTheme.WithAlpha(UiTheme.MutedWhite, 0.82f) : UiTheme.MutedWhite;
            lineText.alignment = thought ? TextAnchor.UpperCenter : TextAnchor.UpperLeft;

            float top = named ? 50f : 18f;
            float width = BarWidth - 80f;
            var settings = lineText.GetGenerationSettings(new Vector2(width, 0f));
            float ppu = lineText.pixelsPerUnit > 0f ? lineText.pixelsPerUnit : 1f;
            float textHeight = Mathf.Max(34f, lineText.cachedTextGeneratorForLayout.GetPreferredHeight(lineText.text, settings) / ppu);
            SettingsMenuUi.PinTop(lineText.rectTransform, top, textHeight, 40f);
            bar.sizeDelta = new Vector2(BarWidth, top + textHeight + 18f);
        }

        /// <summary>"Pani Zofia" → "P A N I  Z O F I A"-like small caps: upper case with thin spacing.</summary>
        static string SmallCaps(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var upper = s.ToUpperInvariant();
            var sb = new System.Text.StringBuilder(upper.Length * 2);
            for (int i = 0; i < upper.Length; i++)
            {
                sb.Append(upper[i]);
                if (i < upper.Length - 1) sb.Append(upper[i] == ' ' ? "  " : " ");
            }
            return sb.ToString();
        }
    }
}
