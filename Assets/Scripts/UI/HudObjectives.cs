using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Top-right objectives (UI v2): bare right-aligned text on the image with soft shadows - clock, date, hairline, act,
    /// current task (diamond at the right edge), detail line, "OPCJONALNE" list and the evidence/card counters.
    /// A finished task gets a green check and a strike-through, stays ~2.5 s, slides out; the next one slides in
    /// with a typewriter reveal. Also the top-centre cable countdown.
    /// </summary>
    public sealed class HudObjectives : HudWidget
    {
        const float Width = 460f;
        const float RightMargin = 64f;
        const float HeaderTop = 36f;
        const float SlipTop = 156f;
        const float TaskRight = 32f;
        const float RowRight = 22f;
        const float CompleteHold = 2.5f;

        enum Phase { Showing, Completing, Leaving, Entering }

        RectTransform clockBar;
        UnityEngine.UI.Text clockText, dateText, actText;
        RectTransform slipRoot;
        CanvasGroup slipGroup;
        RectTransform taskBlock;
        CanvasGroup taskGroup;
        UnityEngine.UI.Text taskTitle, taskProgress;
        RectTransform taskTick, taskDiamond;
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
            public RectTransform Tick, Diamond;
            public HudStrike Strike;
            public CanvasGroup Group;
        }

        public override void Build(HudContext ctx)
        {
            var root = ctx.Root;

            // Header: time, date, hairline, act - one block so the viewfinder can hide it.
            clockBar = SettingsMenuUi.CreateRect("Clock", root);
            TopRight(clockBar, HeaderTop, new Vector2(Width, 116f));
            clockText = UiTheme.CreateHudText("Time", clockBar, "15:30", 36, TextAnchor.UpperRight, UiTheme.Bone);
            PinTopLeft(clockText.rectTransform, 0f, 40f, 0f, 0f);
            dateText = UiTheme.CreateHudText("Date", clockBar, "", 13, TextAnchor.UpperRight, UiTheme.Bone2);
            PinTopLeft(dateText.rectTransform, 46f, 18f, 0f, 0f);
            var rule = SettingsMenuUi.CreateImage("Hairline", clockBar, UiTheme.Hairline, false);
            rule.rectTransform.anchorMin = rule.rectTransform.anchorMax = new Vector2(1f, 1f);
            rule.rectTransform.pivot = new Vector2(1f, 1f);
            rule.rectTransform.sizeDelta = new Vector2(300f, 1f);
            rule.rectTransform.anchoredPosition = new Vector2(0f, -76f);
            actText = UiTheme.CreateHudText("Act", clockBar, "", 13, TextAnchor.UpperRight, UiTheme.Bone2);
            PinTopLeft(actText.rectTransform, 90f, 18f, 0f, 0f);

            slipRoot = SettingsMenuUi.CreateRect("ObjectivesSlip", root);
            TopRight(slipRoot, SlipTop, new Vector2(Width, 160f));
            slipGroup = slipRoot.gameObject.AddComponent<CanvasGroup>();

            taskBlock = SettingsMenuUi.CreateRect("Task", slipRoot);
            taskGroup = taskBlock.gameObject.AddComponent<CanvasGroup>();
            taskTick = CreateTick(taskBlock, UiTheme.Safe);
            taskTick.anchorMin = taskTick.anchorMax = new Vector2(1f, 1f);
            taskTick.anchoredPosition = new Vector2(-22f, -4f);
            taskDiamond = UiTheme.CreateDiamond(taskBlock, 10f, UiTheme.Bone);
            taskDiamond.anchorMin = taskDiamond.anchorMax = new Vector2(1f, 1f);
            taskDiamond.pivot = new Vector2(1f, 0.5f);
            taskDiamond.anchoredPosition = new Vector2(0f, -15f);
            taskTitle = UiTheme.CreateHudText("Title", taskBlock, "", 26, TextAnchor.UpperRight, UiTheme.Bone);
            taskTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            taskStrike = new HudStrike(taskTitle, UiTheme.Bone);
            taskProgress = UiTheme.CreateHudText("Progress", taskBlock, "", 16, TextAnchor.UpperRight, UiTheme.Bone2, true);
            taskProgress.horizontalOverflow = HorizontalWrapMode.Wrap;

            optionalHeader = UiTheme.CreateHudText("OptionalHeader", slipRoot, "OPCJONALNE", 12, TextAnchor.MiddleRight, UiTheme.Bone3);

            countsBar = SettingsMenuUi.CreateRect("Collections", root);
            TopRight(countsBar, SlipTop + 200f, new Vector2(Width, 22f));
            countsText = UiTheme.CreateHudText("Text", countsBar, "", 13, TextAnchor.MiddleRight, UiTheme.Bone3);
            SettingsMenuUi.Stretch(countsText.rectTransform);

            // Cable countdown: bare red caps with a thin bar, top centre under the compass.
            cableBar = SettingsMenuUi.CreateRect("CableCountdown", root);
            SettingsMenuUi.Place(cableBar, new Vector2(0.5f, 1f), new Vector2(0f, -122f), new Vector2(640f, 44f));
            cableText = UiTheme.CreateHudText("Text", cableBar, "", 26, TextAnchor.UpperCenter, UiTheme.Danger);
            PinTopLeft(cableText.rectTransform, 0f, 30f, 0f, 0f);
            var track = SettingsMenuUi.CreateImage("Track", cableBar, UiTheme.Hairline, false);
            PinTopLeft(track.rectTransform, 36f, 3f, 120f, 120f);
            cableFill = SettingsMenuUi.CreateImage("Fill", track.rectTransform, UiTheme.Danger, false);
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
            dateText.text = "CZWARTEK 31 SIERPNIA 1939";
            countsText.text = $"DOWODY {GameProgress.Photos.Count}/{GameProgress.EvidenceTotal}   KARTY {GameProgress.HistoryCards.Count}/{GameProgress.HistoryCardTotal}";
            actText.text = mission != null ? (mission.CurrentActName ?? "").ToUpperInvariant() : "";

            TickTask(ctx);
            float height = Layout(mission);

            bool anything = (shown != null && !string.IsNullOrEmpty(fullTitle)) || rows.Exists(r => r.Root.gameObject.activeSelf);
            clockBar.gameObject.SetActive(!ctx.ViewfinderUp);
            countsBar.gameObject.SetActive(!ctx.ViewfinderUp);
            slipAlpha = Mathf.MoveTowards(slipAlpha, anything && !ctx.ViewfinderUp ? 1f : 0f, ctx.Dt * (ctx.ViewfinderUp ? 12f : 4f));
            slipGroup.alpha = slipAlpha;
            slipRoot.sizeDelta = new Vector2(Width, height);
            countsBar.anchoredPosition = new Vector2(-RightMargin, -(SlipTop + (anything ? height + 6f : 0f)));

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
                    taskTitle.color = Color.Lerp(UiTheme.Bone, UiTheme.Bone2, k);
                    taskDiamond.gameObject.SetActive(false);
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
            taskTitle.color = UiTheme.Bone;
            taskDiamond.gameObject.SetActive(true);
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
            float y = 0f;

            bool hasTask = shown != null && !string.IsNullOrEmpty(fullTitle);
            taskBlock.gameObject.SetActive(hasTask);
            if (hasTask)
            {
                float titleW = Width - TaskRight;
                float titleH = Mathf.Max(30f, MeasureHeight(taskTitle, fullTitle, titleW));
                bool hasProgress = !string.IsNullOrEmpty(taskProgress.text);
                float blockH = titleH + (hasProgress ? 24f : 0f);
                PinTopLeft(taskBlock, y, blockH, slideX, -slideX);
                PinTopLeft(taskTitle.rectTransform, 0f, titleH, 0f, TaskRight);
                PinTopLeft(taskProgress.rectTransform, titleH + 4f, 22f, 0f, TaskRight);
                y += blockH + 14f;
            }

            var optional = mission != null ? mission.Optional : null;
            int count = optional != null ? optional.Count : 0;
            optionalHeader.gameObject.SetActive(count > 0);
            if (count > 0)
            {
                PinTopLeft(optionalHeader.rectTransform, y, 16f, 0f, 0f);
                y += 22f;
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
                float h = Mathf.Max(22f, MeasureHeight(row.Text, label, Width - RowRight));
                PinTopLeft(row.Root, y, h, 0f, 0f);
                PinTopLeft(row.Text.rectTransform, 0f, h, 0f, RowRight);
                row.Tick.gameObject.SetActive(done);
                row.Diamond.gameObject.SetActive(!done);
                row.Text.color = done ? UiTheme.Bone3 : UiTheme.Bone2;
                row.Strike.Set(done ? 1f : 0f);
                y += h + 4f;
            }

            return y + 4f;
        }

        OptionalRow CreateRow()
        {
            var rt = SettingsMenuUi.CreateRect("Optional", slipRoot);
            var row = new OptionalRow { Root = rt, Group = rt.gameObject.AddComponent<CanvasGroup>() };
            row.Tick = CreateTick(rt, UiTheme.Safe);
            row.Tick.anchorMin = row.Tick.anchorMax = new Vector2(1f, 1f);
            row.Tick.anchoredPosition = new Vector2(-16f, -3f);
            row.Tick.localScale = Vector3.one * 0.7f;
            row.Diamond = UiTheme.CreateDiamond(rt, 8f, UiTheme.Bone2, false);
            row.Diamond.anchorMin = row.Diamond.anchorMax = new Vector2(1f, 1f);
            row.Diamond.pivot = new Vector2(1f, 0.5f);
            row.Diamond.anchoredPosition = new Vector2(0f, -11f);
            row.Text = UiTheme.CreateHudText("Text", rt, "", 16, TextAnchor.UpperRight, UiTheme.Bone2);
            row.Text.horizontalOverflow = HorizontalWrapMode.Wrap;
            row.Strike = new HudStrike(row.Text, UiTheme.Bone3);
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
            cableText.text = $"KOMUNIKAT NA ANTENIE  {Mathf.CeilToInt(cable.Remaining)} s";
            cableFill.rectTransform.anchorMax = new Vector2(left, 1f);
            float pulse = left < 0.25f ? 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(ctx.Now * 6f)) : 1f;
            cableText.color = UiTheme.WithAlpha(UiTheme.Danger, pulse);
            ctx.TopCentreUsed = 70f;
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
