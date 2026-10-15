using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Top-right objectives: clock on a dark bar, a paper slip (stamp "ZADANIE", act name, current task with progress,
    /// "OPCJONALNE" side tasks) and "Dowody x/6 · Karty y/6" under it. A finished task gets a tick and a strike-through,
    /// stays ~2.5 s, slides out; the next one slides in with a typewriter reveal. Also the top-centre cable countdown.
    /// </summary>
    public sealed class HudObjectives : HudWidget
    {
        const float Width = 470f;
        const float Pad = 18f;
        const float RightMargin = 30f;
        const float SlipTop = 88f;
        const float TitleLeft = 46f;
        const float CompleteHold = 2.5f;

        enum Phase { Showing, Completing, Leaving, Entering }

        RectTransform clockBar;
        UnityEngine.UI.Text clockText;
        RectTransform slipRoot, slipPaper;
        CanvasGroup slipGroup;
        UnityEngine.UI.Text actText;
        RectTransform taskBlock;
        CanvasGroup taskGroup;
        UnityEngine.UI.Text taskTitle, taskProgress;
        RectTransform taskTick;
        HudStrike taskStrike;
        UnityEngine.UI.Text optionalHeader;
        readonly List<OptionalRow> rows = new();
        RectTransform countsBar;
        UnityEngine.UI.Text countsText;
        RectTransform cableBar;
        UnityEngine.UI.Text cableText;
        UnityEngine.UI.Image cableFill;

        Phase phase = Phase.Showing;
        object shown;
        string fullTitle = "";
        float phaseStart;
        float slipAlpha;
        float slideX;

        sealed class OptionalRow
        {
            public RectTransform Root;
            public UnityEngine.UI.Text Text;
            public RectTransform Tick;
            public HudStrike Strike;
            public CanvasGroup Group;
        }

        public override void Build(HudContext ctx)
        {
            var root = ctx.Root;

            var clock = UiTheme.CreateDarkBar("Clock", root);
            clockBar = clock.rectTransform;
            TopRight(clockBar, 24f, new Vector2(170f, 52f));
            clockText = SettingsMenuUi.CreateText("Time", clockBar, "15:30", 36, TextAnchor.MiddleCenter, FontStyle.Bold, UiTheme.MutedWhite);
            SettingsMenuUi.Stretch(clockText.rectTransform);

            slipPaper = UiTheme.CreatePaper(root, "ObjectivesSlip", new Vector2(Width, 160f), out slipRoot);
            TopRight(slipRoot, SlipTop, slipRoot.sizeDelta);
            slipGroup = slipRoot.gameObject.AddComponent<CanvasGroup>();

            actText = SettingsMenuUi.CreateText("Act", slipPaper, "", 16, TextAnchor.MiddleRight, FontStyle.Bold, UiTheme.FadedInk);
            SettingsMenuUi.PinTop(actText.rectTransform, 14f, 22f, Pad);
            var rule = UiTheme.CreateRule("Rule", slipPaper, UiTheme.FadedInk, 1.5f);
            SettingsMenuUi.PinTop(rule.rectTransform, 44f, 1.5f, Pad);

            var stamp = UiTheme.CreateStamp(slipPaper, "Zadanie", UiTheme.StampRed, 17);
            stamp.anchorMin = stamp.anchorMax = new Vector2(0f, 1f);
            stamp.anchoredPosition = new Vector2(78f, -18f);

            taskBlock = SettingsMenuUi.CreateRect("Task", slipPaper);
            taskGroup = taskBlock.gameObject.AddComponent<CanvasGroup>();
            taskTick = CreateTick(taskBlock, UiTheme.Olive);
            taskTick.anchorMin = taskTick.anchorMax = new Vector2(0f, 1f);
            taskTick.anchoredPosition = new Vector2(Pad, -4f);
            taskTitle = SettingsMenuUi.CreateText("Title", taskBlock, "", 22, TextAnchor.UpperLeft, FontStyle.Bold, UiTheme.Ink);
            taskStrike = new HudStrike(taskTitle, UiTheme.StampRed);
            taskProgress = SettingsMenuUi.CreateText("Progress", taskBlock, "", 18, TextAnchor.UpperLeft, FontStyle.Normal, UiTheme.FadedInk);

            optionalHeader = SettingsMenuUi.CreateText("OptionalHeader", slipPaper, "OPCJONALNE", 15, TextAnchor.MiddleLeft, FontStyle.Bold, UiTheme.FadedInk);

            var counts = UiTheme.CreateDarkBar("Collections", root);
            countsBar = counts.rectTransform;
            TopRight(countsBar, SlipTop + 200f, new Vector2(Width + 8f, 34f));
            countsText = SettingsMenuUi.CreateText("Text", countsBar, "", 19, TextAnchor.MiddleRight, FontStyle.Bold, UiTheme.MutedWhite);
            SettingsMenuUi.Stretch(countsText.rectTransform, 14f, 14f);

            var cable = UiTheme.CreateDarkBar("CableCountdown", root, 0.85f);
            cableBar = cable.rectTransform;
            SettingsMenuUi.Place(cableBar, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(640f, 62f));
            UiTheme.AddFrame(cableBar, UiTheme.AlertRed, 2f, 0f);
            cableText = SettingsMenuUi.CreateText("Text", cableBar, "", 24, TextAnchor.MiddleCenter, FontStyle.Bold, UiTheme.AlertRed);
            SettingsMenuUi.PinTop(cableText.rectTransform, 6f, 34f, 12f);
            var track = SettingsMenuUi.CreateImage("Track", cableBar, UiTheme.WithAlpha(UiTheme.MutedWhite, 0.15f), false);
            SettingsMenuUi.PinTop(track.rectTransform, 44f, 8f, 18f);
            cableFill = SettingsMenuUi.CreateImage("Fill", track.rectTransform, UiTheme.AlertRed, false);
            cableFill.rectTransform.anchorMin = Vector2.zero;
            cableFill.rectTransform.anchorMax = Vector2.one;
            cableFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            cableFill.rectTransform.offsetMin = cableFill.rectTransform.offsetMax = Vector2.zero;
            cableBar.gameObject.SetActive(false);
        }

        static void TopRight(RectTransform rt, float top, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = new Vector2(-RightMargin, -top);
        }

        public override void Tick(HudContext ctx)
        {
            var mission = Mission.Instance;
            clockText.text = GameClock.Text;
            countsText.text = $"Dowody {GameProgress.Photos.Count}/{GameProgress.EvidenceTotal} · Karty {GameProgress.HistoryCards.Count}/{GameProgress.HistoryCardTotal}";
            actText.text = mission != null ? (mission.CurrentActName ?? "").ToUpperInvariant() : "";

            TickTask(ctx);
            float height = Layout(mission);

            bool anything = (shown != null && !string.IsNullOrEmpty(fullTitle)) || rows.Exists(r => r.Root.gameObject.activeSelf);
            clockBar.gameObject.SetActive(!ctx.ViewfinderUp);
            countsBar.gameObject.SetActive(!ctx.ViewfinderUp);
            slipAlpha = Mathf.MoveTowards(slipAlpha, anything && !ctx.ViewfinderUp ? 1f : 0f, ctx.Dt * (ctx.ViewfinderUp ? 12f : 4f));
            slipGroup.alpha = slipAlpha;
            slipRoot.sizeDelta = new Vector2(Width + 8f, height + 8f);
            countsBar.anchoredPosition = new Vector2(-RightMargin, -(SlipTop + (anything ? height + 8f + 10f : 0f)));

            TickCable(ctx, mission);
        }

        // ---------- Main task: Showing → Completing → Leaving → Entering ----------

        void TickTask(HudContext ctx)
        {
            object current = CurrentKey();
            float t = ctx.Now - phaseStart;
            switch (phase)
            {
                case Phase.Showing:
                    if (current != shown)
                    {
                        if (shown != null && !string.IsNullOrEmpty(fullTitle) && IsDone(shown)) Enter(Phase.Completing, ctx);
                        else if (shown != null && !string.IsNullOrEmpty(fullTitle)) Enter(Phase.Leaving, ctx);
                        else BeginEntering(current, ctx);
                    }
                    else taskProgress.text = ProgressOf(shown);
                    SetTaskPose(0f, 1f);
                    break;

                case Phase.Completing:
                {
                    float k = Mathf.Clamp01(t / 0.45f);
                    taskTick.gameObject.SetActive(true);
                    taskTick.localScale = Vector3.one * Overshoot(Mathf.Clamp01(t / 0.25f));
                    taskStrike.Set(k);
                    taskTitle.color = Color.Lerp(UiTheme.Ink, UiTheme.FadedInk, k);
                    if (t >= CompleteHold) Enter(Phase.Leaving, ctx);
                    break;
                }

                case Phase.Leaving:
                {
                    float k = Mathf.Clamp01(t / 0.3f);
                    SetTaskPose(70f * k * k, 1f - k);
                    if (k >= 1f) BeginEntering(CurrentKey(), ctx);
                    break;
                }

                case Phase.Entering:
                {
                    float k = Mathf.Clamp01(t / 0.3f);
                    float ease = 1f - (1f - k) * (1f - k);
                    SetTaskPose(70f * (1f - ease), ease);
                    int chars = Mathf.Clamp(Mathf.CeilToInt(fullTitle.Length * Mathf.Clamp01(t / 0.6f)), 0, fullTitle.Length);
                    taskTitle.text = fullTitle.Substring(0, chars);
                    taskProgress.text = t > 0.6f ? ProgressOf(shown) : "";
                    if (t >= 0.65f)
                    {
                        taskTitle.text = fullTitle;
                        phase = Phase.Showing;
                    }
                    break;
                }
            }
        }

        void Enter(Phase next, HudContext ctx)
        {
            phase = next;
            phaseStart = ctx.Now;
        }

        void BeginEntering(object key, HudContext ctx)
        {
            shown = key;
            fullTitle = TitleOf(key) ?? "";
            taskTick.gameObject.SetActive(false);
            taskStrike.Set(0f);
            taskTitle.color = UiTheme.Ink;
            taskTitle.text = "";
            taskProgress.text = "";
            if (string.IsNullOrEmpty(fullTitle))
            {
                phase = Phase.Showing;
                return;
            }
            Enter(Phase.Entering, ctx);
        }

        void SetTaskPose(float x, float alpha)
        {
            slideX = x; // applied by Layout, which owns the block's offsets
            taskGroup.alpha = alpha;
        }

        /// <summary>Ease-out-back: 0 → 1 with a small overshoot (the tick "lands").</summary>
        static float Overshoot(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        static object CurrentKey()
        {
            if (Mission.Instance != null) return Mission.Instance.Current;
            if (QuestManager.Instance != null) return QuestManager.Instance.Current;
            return null;
        }

        static string TitleOf(object key) => key switch
        {
            MissionStep step => step.Title,
            QuestObjective quest when quest != null => quest.Title,
            _ => null,
        };

        static string ProgressOf(object key) => key switch
        {
            MissionStep step => step.Progress ?? "",
            QuestObjective quest when quest != null => quest.ProgressText ?? "",
            _ => "",
        };

        static bool IsDone(object key) => key switch
        {
            MissionStep step => step.Done,
            QuestObjective quest when quest != null => quest.IsCompleted,
            _ => false,
        };

        // ---------- Layout (top-down, recomputed every frame) ----------

        float Layout(Mission mission)
        {
            float textWidth = Width - TitleLeft - Pad;
            float y = 54f;

            bool hasTask = shown != null && !string.IsNullOrEmpty(fullTitle);
            taskBlock.gameObject.SetActive(hasTask);
            if (hasTask)
            {
                float titleH = Mathf.Max(28f, MeasureHeight(taskTitle, fullTitle, textWidth));
                bool hasProgress = !string.IsNullOrEmpty(taskProgress.text);
                float blockH = titleH + (hasProgress ? 26f : 0f);
                PinTopLeft(taskBlock, y, blockH, slideX, -slideX);
                PinTopLeft(taskTitle.rectTransform, 0f, titleH, TitleLeft, Pad);
                PinTopLeft(taskProgress.rectTransform, titleH + 2f, 24f, TitleLeft, Pad);
                y += blockH + 10f;
            }

            var optional = mission != null ? mission.Optional : null;
            int count = optional != null ? optional.Count : 0;
            optionalHeader.gameObject.SetActive(count > 0);
            if (count > 0)
            {
                PinTopLeft(optionalHeader.rectTransform, y, 20f, Pad, Pad);
                y += 24f;
            }
            while (rows.Count < count) rows.Add(CreateRow());
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                bool active = i < count && optional[i] != null;
                row.Root.gameObject.SetActive(active);
                if (!active) continue;
                var task = optional[i];
                bool done = SafeDone(task);
                string progress = !done ? SafeProgress(task) : null;
                string label = string.IsNullOrEmpty(progress) ? task.Title : $"{task.Title} ({progress})";
                if (row.Text.text != label) row.Text.text = label;
                float h = Mathf.Max(22f, MeasureHeight(row.Text, label, textWidth - 4f));
                PinTopLeft(row.Root, y, h, 0f, 0f);
                PinTopLeft(row.Text.rectTransform, 0f, h, TitleLeft, Pad);
                row.Tick.gameObject.SetActive(done);
                row.Group.alpha = done ? 0.5f : 1f;
                row.Strike.Set(done ? 1f : 0f);
                y += h + 4f;
            }

            return y + 12f;
        }

        OptionalRow CreateRow()
        {
            var rt = SettingsMenuUi.CreateRect("Optional", slipPaper);
            var row = new OptionalRow { Root = rt, Group = rt.gameObject.AddComponent<CanvasGroup>() };
            row.Tick = CreateTick(rt, UiTheme.Olive);
            row.Tick.anchorMin = row.Tick.anchorMax = new Vector2(0f, 1f);
            row.Tick.anchoredPosition = new Vector2(Pad + 2f, -2f);
            row.Tick.localScale = Vector3.one * 0.8f;
            row.Text = SettingsMenuUi.CreateText("Text", rt, "", 17, TextAnchor.UpperLeft, FontStyle.Normal, UiTheme.Ink);
            row.Strike = new HudStrike(row.Text, UiTheme.Ink);
            return row;
        }

        static bool SafeDone(Mission.OptionalTask task)
        {
            try { return task.IsDone != null && task.IsDone(); }
            catch { return false; }
        }

        static string SafeProgress(Mission.OptionalTask task)
        {
            try { return task.Progress?.Invoke(); }
            catch { return null; }
        }

        static float MeasureHeight(UnityEngine.UI.Text text, string content, float width)
        {
            if (string.IsNullOrEmpty(content)) return 0f;
            var settings = text.GetGenerationSettings(new Vector2(width, 0f));
            float ppu = text.pixelsPerUnit > 0f ? text.pixelsPerUnit : 1f;
            return text.cachedTextGeneratorForLayout.GetPreferredHeight(content, settings) / ppu;
        }

        static void PinTopLeft(RectTransform rt, float top, float height, float left, float right)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(left, -top - height);
            rt.offsetMax = new Vector2(-right, -top);
        }

        // ---------- Cable countdown (act 3) ----------

        void TickCable(HudContext ctx, Mission mission)
        {
            var cable = mission != null ? mission.Current as CableStep : null;
            bool show = cable != null && !cable.Done;
            cableBar.gameObject.SetActive(show);
            if (!show) return;
            float left = cable.Duration > 0f ? Mathf.Clamp01(cable.Remaining / cable.Duration) : 0f;
            cableText.text = $"KOMUNIKAT NA ANTENIE — {Mathf.CeilToInt(cable.Remaining)} s";
            cableFill.rectTransform.anchorMax = new Vector2(left, 1f);
            float pulse = left < 0.25f ? 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(ctx.Now * 6f)) : 1f;
            cableText.color = UiTheme.WithAlpha(UiTheme.AlertRed, pulse);
            ctx.TopCentreUsed = 96f;
        }

        // ---------- Tick mark ----------

        /// <summary>A hand-drawn style check mark made of two strokes, 20×20, anchored by the caller.</summary>
        public static RectTransform CreateTick(Transform parent, Color color)
        {
            var rt = SettingsMenuUi.CreateRect("Tick", parent);
            rt.sizeDelta = new Vector2(20f, 20f);
            rt.pivot = new Vector2(0f, 1f);
            Stroke(rt, color, new Vector2(2f, 10f), 8f, -45f);
            Stroke(rt, color, new Vector2(6.6f, 4.5f), 16f, 52f);
            rt.gameObject.SetActive(false);
            return rt;
        }

        static void Stroke(RectTransform parent, Color color, Vector2 start, float length, float angle)
        {
            var img = SettingsMenuUi.CreateImage("Stroke", parent, color, false);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(length, 3f);
            rt.anchoredPosition = start;
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    /// <summary>Strike-through lines over a legacy uGUI Text, one per wrapped line, drawn left to right by progress.</summary>
    public sealed class HudStrike
    {
        readonly UnityEngine.UI.Text text;
        readonly Color color;
        readonly List<UnityEngine.UI.Image> lines = new();
        readonly List<UILineInfo> lineInfos = new();
        readonly List<UICharInfo> charInfos = new();

        public HudStrike(UnityEngine.UI.Text text, Color color)
        {
            this.text = text;
            this.color = color;
        }

        public void Set(float progress)
        {
            int used = 0;
            string s = text.text;
            if (progress > 0f && !string.IsNullOrEmpty(s))
            {
                var gen = text.cachedTextGenerator;
                gen.GetLines(lineInfos);
                gen.GetCharacters(charInfos);
                float ppu = text.pixelsPerUnit > 0f ? text.pixelsPerUnit : 1f;
                int lineCount = lineInfos.Count;
                for (int i = 0; i < lineCount; i++)
                {
                    int start = lineInfos[i].startCharIdx;
                    int end = (i + 1 < lineCount ? lineInfos[i + 1].startCharIdx : s.Length) - 1;
                    end = Mathf.Min(end, Mathf.Min(charInfos.Count, s.Length) - 1);
                    while (end > start && char.IsWhiteSpace(s[end])) end--;
                    if (start < 0 || start > end || start >= charInfos.Count) continue;

                    float x0 = charInfos[start].cursorPos.x / ppu;
                    float x1 = (charInfos[end].cursorPos.x + charInfos[end].charWidth) / ppu;
                    float y = (lineInfos[i].topY - lineInfos[i].height * 0.5f) / ppu;
                    float share = Mathf.Clamp01(progress * lineCount - i);
                    if (share <= 0f) continue;

                    var line = GetLine(used++);
                    var rt = line.rectTransform;
                    rt.anchorMin = rt.anchorMax = text.rectTransform.pivot;
                    rt.pivot = new Vector2(0f, 0.5f);
                    rt.anchoredPosition = new Vector2(x0, y);
                    rt.sizeDelta = new Vector2((x1 - x0) * share, 2.5f);
                }
            }
            for (int i = used; i < lines.Count; i++) lines[i].gameObject.SetActive(false);
        }

        UnityEngine.UI.Image GetLine(int index)
        {
            while (lines.Count <= index)
                lines.Add(SettingsMenuUi.CreateImage("Strike", text.rectTransform, UiTheme.WithAlpha(color, 0.85f), false));
            var line = lines[index];
            line.gameObject.SetActive(true);
            return line;
        }
    }
}
