using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Hitman-style detection read from Detection: an arc around the crosshair for every observer, turned towards
    /// them and filling white → amber → red with their meter; a stamp badge at the top centre (UKRYTY / PODEJRZENIE /
    /// POSZUKIWANIA / ALARM, the alarm pulsing) and a small visibility eye bottom-left.
    /// </summary>
    public sealed class HudDetection : HudWidget
    {
        const float Radius = 118f;
        const int MaxArcs = 10;

        sealed class Arc
        {
            public RectTransform Pivot;
            public UnityEngine.UI.Image Back, Fill;
        }

        RectTransform arcCentre;
        readonly List<Arc> arcs = new();
        RectTransform badge;
        UnityEngine.UI.Text badgeText;
        UnityEngine.UI.Image badgeRule, alarmVignette;
        float vignetteAlpha;
        string badgeLabel;
        RectTransform eyePanel, eyeWhite, eyePupil, eyeLid;
        UnityEngine.UI.Image eyeWhiteImage;
        UnityEngine.UI.Text eyeText, eyeDetail;
        UnityEngine.UI.Image eyeFill;

        public override void Build(HudContext ctx)
        {
            var root = ctx.Root;

            arcCentre = SettingsMenuUi.CreateRect("DetectionArcs", root);
            SettingsMenuUi.Place(arcCentre, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // Alarm: red edge vignette (only in alarm) and the badge - tracked red caps with a short rule, no box.
            alarmVignette = SettingsMenuUi.CreateImage("AlarmVignette", root, UiTheme.WithAlpha(UiTheme.Danger, 0f), false);
            alarmVignette.sprite = UiTheme.VignetteSprite;
            SettingsMenuUi.Stretch(alarmVignette.rectTransform);

            badge = SettingsMenuUi.CreateRect("DetectionBadge", root);
            SettingsMenuUi.Place(badge, new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(700f, 60f));
            badge.pivot = new Vector2(0.5f, 1f);
            badgeText = UiTheme.CreateHudText("Label", badge, "", 36, TextAnchor.UpperCenter, UiTheme.Danger);
            badgeText.rectTransform.anchorMin = new Vector2(0f, 1f);
            badgeText.rectTransform.anchorMax = new Vector2(1f, 1f);
            badgeText.rectTransform.pivot = new Vector2(0.5f, 1f);
            badgeText.rectTransform.sizeDelta = new Vector2(0f, 40f);
            badgeText.rectTransform.anchoredPosition = Vector2.zero;
            var rule = SettingsMenuUi.CreateImage("Rule", badge, UiTheme.Danger, false);
            SettingsMenuUi.Place(rule.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(120f, 2f));
            rule.rectTransform.pivot = new Vector2(0.5f, 1f);
            badgeRule = rule;
            badge.gameObject.SetActive(false);

            // Visibility, bottom-left: eye, state word, 3 px bar, detail line.
            eyePanel = SettingsMenuUi.CreateRect("Visibility", root);
            eyePanel.anchorMin = eyePanel.anchorMax = eyePanel.pivot = Vector2.zero;
            eyePanel.sizeDelta = new Vector2(320f, 90f);
            eyePanel.anchoredPosition = new Vector2(64f, 48f);
            var eye = SettingsMenuUi.CreateRect("Eye", eyePanel);
            SettingsMenuUi.Place(eye, new Vector2(0f, 1f), new Vector2(15f, -41f), new Vector2(30f, 20f));
            var white = SettingsMenuUi.CreateImage("White", eye, UiTheme.Bone, false);
            white.sprite = UiTheme.CircleSprite;
            eyeWhiteImage = white;
            eyeWhite = white.rectTransform;
            SettingsMenuUi.Place(eyeWhite, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28f, 16f));
            UiTheme.AddSoftShadow(white);
            var pupil = SettingsMenuUi.CreateImage("Pupil", eye, UiTheme.KeyInk, false);
            pupil.sprite = UiTheme.CircleSprite;
            eyePupil = pupil.rectTransform;
            SettingsMenuUi.Place(eyePupil, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 8f));
            var lid = SettingsMenuUi.CreateImage("Lid", eye, UiTheme.Bone, false);
            eyeLid = lid.rectTransform;
            SettingsMenuUi.Place(eyeLid, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28f, 2f));
            eyeText = UiTheme.CreateHudText("State", eyePanel, "", 22, TextAnchor.UpperLeft, UiTheme.Bone);
            eyeText.rectTransform.anchorMin = eyeText.rectTransform.anchorMax = new Vector2(0f, 1f);
            eyeText.rectTransform.pivot = new Vector2(0f, 1f);
            eyeText.rectTransform.sizeDelta = new Vector2(280f, 28f);
            eyeText.rectTransform.anchoredPosition = new Vector2(38f, -27f);
            var track = SettingsMenuUi.CreateImage("Track", eyePanel, UiTheme.WithAlpha(UiTheme.Bone, 0.2f), false);
            SettingsMenuUi.Place(track.rectTransform, new Vector2(0f, 1f), new Vector2(0f, -66f), new Vector2(180f, 3f));
            track.rectTransform.pivot = new Vector2(0f, 1f);
            eyeFill = SettingsMenuUi.CreateImage("Fill", track.rectTransform, UiTheme.Bone, false);
            eyeFill.rectTransform.anchorMin = Vector2.zero;
            eyeFill.rectTransform.anchorMax = Vector2.one;
            eyeFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            eyeFill.rectTransform.offsetMin = eyeFill.rectTransform.offsetMax = Vector2.zero;
            eyeDetail = UiTheme.CreateHudText("Detail", eyePanel, "", 15, TextAnchor.UpperLeft, UiTheme.Bone2, true);
            eyeDetail.rectTransform.anchorMin = eyeDetail.rectTransform.anchorMax = new Vector2(0f, 1f);
            eyeDetail.rectTransform.pivot = new Vector2(0f, 1f);
            eyeDetail.rectTransform.sizeDelta = new Vector2(320f, 20f);
            eyeDetail.rectTransform.anchoredPosition = new Vector2(0f, -72f);
        }

        Arc CreateArc()
        {
            var pivot = SettingsMenuUi.CreateRect("Arc", arcCentre);
            SettingsMenuUi.Place(pivot, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var arc = new Arc { Pivot = pivot };
            arc.Back = ArcImage(pivot, UiTheme.WithAlpha(UiTheme.Bone, 0.18f));
            arc.Fill = ArcImage(pivot, UiTheme.Bone);
            arc.Fill.type = UnityEngine.UI.Image.Type.Filled;
            arc.Fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            arc.Fill.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
            return arc;
        }

        static UnityEngine.UI.Image ArcImage(RectTransform parent, Color color)
        {
            var img = SettingsMenuUi.CreateImage("Band", parent, color, false);
            img.sprite = UiTheme.ArcSprite;
            float s = Mathf.Sin(UiTheme.ArcHalfAngle * Mathf.Deg2Rad);
            float y0 = UiTheme.ArcInner * Mathf.Cos(UiTheme.ArcHalfAngle * Mathf.Deg2Rad);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(2f * s * Radius, (1f - y0) * Radius);
            rt.anchoredPosition = new Vector2(0f, y0 * Radius);
            return img;
        }

        public override void Tick(HudContext ctx)
        {
            TickArcs(ctx);
            TickBadge(ctx);
            TickEye(ctx);
        }

        void TickArcs(HudContext ctx)
        {
            int used = 0;
            var observers = Detection.Observers;
            if (ctx.Cam != null && observers != null)
            {
                var camT = ctx.Cam.transform;
                foreach (var o in observers)
                {
                    if (used >= MaxArcs) break;
                    if (o.Transform == null) continue;
                    float meter = Mathf.Clamp01(o.Meter01);
                    int level = (int)o.Level;
                    if (meter <= 0.02f && level <= 0) continue;

                    Vector3 local = camT.InverseTransformDirection(o.Transform.position - camT.position);
                    float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                    while (arcs.Count <= used) arcs.Add(CreateArc());
                    var arc = arcs[used++];
                    arc.Pivot.gameObject.SetActive(true);
                    arc.Pivot.localRotation = Quaternion.Euler(0f, 0f, -angle);
                    arc.Pivot.localScale = Vector3.one * (o.IsPolice ? 1.08f : 1f);

                    Color c = level >= 3 ? UiTheme.Danger
                        : meter < 0.5f ? Color.Lerp(UiTheme.Bone, UiTheme.Warn, meter * 2f)
                        : Color.Lerp(UiTheme.Warn, UiTheme.Danger, (meter - 0.5f) * 2f);
                    float alpha = 0.45f + 0.55f * Mathf.Clamp01(meter * 3f);
                    if (level >= 3) alpha *= 0.75f + 0.25f * Mathf.Abs(Mathf.Sin(ctx.Now * 8f));
                    arc.Fill.color = UiTheme.WithAlpha(c, alpha);
                    arc.Fill.fillAmount = level >= 3 ? 1f : meter;
                }
            }
            for (int i = used; i < arcs.Count; i++) arcs[i].Pivot.gameObject.SetActive(false);
        }

        void TickBadge(HudContext ctx)
        {
            int status = (int)Detection.PlayerStatus;
            string label = null;
            Color color = UiTheme.Warn;
            switch (status)
            {
                case 1: label = "PODEJRZENIE"; break;
                case 2: label = "POSZUKIWANIA"; break;
                case 3: label = "ALARM"; color = UiTheme.Danger; break;
            }

            bool show = label != null && !ctx.ViewfinderUp;
            badge.gameObject.SetActive(show);
            float vignetteTarget = status == 3 && !ctx.ViewfinderUp ? 0.4f + 0.1f * Mathf.Abs(Mathf.Sin(ctx.Now * 4f)) : 0f;
            vignetteAlpha = Mathf.MoveTowards(vignetteAlpha, vignetteTarget, ctx.Dt * 1.5f);
            alarmVignette.color = UiTheme.WithAlpha(UiTheme.Danger, vignetteAlpha);
            if (!show) return;
            if (label != badgeLabel)
            {
                badgeLabel = label;
                badgeText.text = label;
            }
            badge.anchoredPosition = new Vector2(0f, -118f - ctx.TopCentreUsed);
            badgeText.fontSize = status == 3 ? 36 : 28;
            badgeText.color = color;
            badgeRule.color = color;
        }

        void TickEye(HudContext ctx)
        {
            bool hidden = Detection.PlayerHidden;
            float vis = Mathf.Clamp01(Detection.Visibility01);
            int status = (int)Detection.PlayerStatus;
            float open = hidden ? 0.08f : Mathf.Lerp(0.25f, 1f, vis);
            eyeWhite.localScale = new Vector3(1f, open, 1f);
            eyePupil.localScale = Vector3.one * (hidden ? 0f : Mathf.Lerp(0.6f, 1f, vis));
            eyeLid.gameObject.SetActive(hidden);

            string word;
            Color tint;
            if (status >= 3) { word = "ALARM"; tint = UiTheme.Danger; }
            else if (status == 2) { word = "POSZUKIWANIA"; tint = UiTheme.Warn; }
            else if (status == 1) { word = "PODEJRZENIE"; tint = UiTheme.Warn; }
            else if (hidden) { word = "UKRYTY"; tint = UiTheme.Safe; }
            else { word = "NIEZAUWAŻONY"; tint = UiTheme.Bone; }
            eyeWhiteImage.color = tint;
            eyeLid.GetComponent<UnityEngine.UI.Image>().color = tint;
            eyeText.text = word;
            eyeText.color = tint;
            eyeFill.color = tint;
            eyeFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.01f, hidden ? 0.05f : vis), 1f);
            eyeDetail.text = hidden ? "Poza zasięgiem wzroku" : $"Widoczność {Mathf.RoundToInt(vis * 100f)}%";
            eyePanel.gameObject.SetActive(!ctx.ViewfinderUp);
        }
    }
}
