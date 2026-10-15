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
        readonly List<UnityEngine.UI.Image> badgeFrame = new();
        string badgeLabel;
        RectTransform eyePanel, eyeWhite, eyePupil, eyeLid;
        UnityEngine.UI.Image eyeWhiteImage;
        UnityEngine.UI.Text eyeText;

        public override void Build(HudContext ctx)
        {
            var root = ctx.Root;

            arcCentre = SettingsMenuUi.CreateRect("DetectionArcs", root);
            SettingsMenuUi.Place(arcCentre, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var badgeBar = UiTheme.CreateDarkBar("DetectionBadge", root, 0.8f);
            badge = badgeBar.rectTransform;
            SettingsMenuUi.Place(badge, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(260f, 50f));
            badge.localRotation = Quaternion.Euler(0f, 0f, -3f);
            int before = badge.childCount;
            UiTheme.AddFrame(badge, Color.white, 2.5f, 0f);
            UiTheme.AddFrame(badge, Color.white, 1f, 5f);
            for (int i = before; i < badge.childCount; i++) badgeFrame.Add(badge.GetChild(i).GetComponent<UnityEngine.UI.Image>());
            badgeText = SettingsMenuUi.CreateText("Label", badge, "", 26, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);
            badgeText.horizontalOverflow = HorizontalWrapMode.Overflow;
            SettingsMenuUi.Stretch(badgeText.rectTransform);
            badge.gameObject.SetActive(false);

            // Visibility eye, bottom-left.
            var eyeBar = UiTheme.CreateDarkBar("Visibility", root, 0.7f);
            eyePanel = eyeBar.rectTransform;
            SettingsMenuUi.Place(eyePanel, Vector2.zero, new Vector2(30f, 30f), new Vector2(196f, 58f));
            var eye = SettingsMenuUi.CreateRect("Eye", eyePanel);
            SettingsMenuUi.Place(eye, new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(54f, 30f));
            eye.pivot = new Vector2(0.5f, 0.5f);
            var white = SettingsMenuUi.CreateImage("White", eye, UiTheme.MutedWhite, false);
            white.sprite = UiTheme.CircleSprite;
            eyeWhiteImage = white;
            eyeWhite = white.rectTransform;
            SettingsMenuUi.Place(eyeWhite, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(50f, 26f));
            var pupil = SettingsMenuUi.CreateImage("Pupil", eye, UiTheme.Dark, false);
            pupil.sprite = UiTheme.CircleSprite;
            eyePupil = pupil.rectTransform;
            SettingsMenuUi.Place(eyePupil, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, 14f));
            var lid = SettingsMenuUi.CreateImage("Lid", eye, UiTheme.MutedWhite, false);
            eyeLid = lid.rectTransform;
            SettingsMenuUi.Place(eyeLid, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48f, 3f));
            eyeText = SettingsMenuUi.CreateText("Label", eyePanel, "", 15, TextAnchor.MiddleLeft, FontStyle.Bold, UiTheme.MutedWhite);
            SettingsMenuUi.Stretch(eyeText.rectTransform, 74f, 8f);
        }

        Arc CreateArc()
        {
            var pivot = SettingsMenuUi.CreateRect("Arc", arcCentre);
            SettingsMenuUi.Place(pivot, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var arc = new Arc { Pivot = pivot };
            arc.Back = ArcImage(pivot, UiTheme.WithAlpha(UiTheme.MutedWhite, 0.18f));
            arc.Fill = ArcImage(pivot, Color.white);
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

                    Color c = level >= 3 ? UiTheme.AlertRed
                        : meter < 0.5f ? Color.Lerp(UiTheme.MutedWhite, UiTheme.Amber, meter * 2f)
                        : Color.Lerp(UiTheme.Amber, UiTheme.AlertRed, (meter - 0.5f) * 2f);
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
            string label;
            Color color;
            switch (status)
            {
                case 1: label = "PODEJRZENIE"; color = UiTheme.Amber; break;
                case 2: label = "POSZUKIWANIA"; color = Color.Lerp(UiTheme.Amber, UiTheme.AlertRed, 0.5f); break;
                case 3: label = "ALARM"; color = UiTheme.AlertRed; break;
                default:
                    label = Detection.PlayerHidden ? "UKRYTY" : null;
                    color = UiTheme.LightOlive;
                    break;
            }

            bool show = label != null && !ctx.ViewfinderUp;
            badge.gameObject.SetActive(show);
            if (!show) return;
            if (label != badgeLabel)
            {
                badgeLabel = label;
                badgeText.text = label;
                badge.sizeDelta = new Vector2(badgeText.preferredWidth + 56f, 50f);
            }
            badge.anchoredPosition = new Vector2(0f, -28f - ctx.TopCentreUsed);
            float pulse = status == 3 ? 1f + 0.06f * Mathf.Abs(Mathf.Sin(ctx.Now * 7f)) : 1f;
            badge.localScale = Vector3.one * pulse;
            badgeText.color = color;
            for (int i = 0; i < badgeFrame.Count; i++) badgeFrame[i].color = UiTheme.WithAlpha(color, i < 4 ? 0.9f : 0.5f); // outer, inner frame
        }

        void TickEye(HudContext ctx)
        {
            bool hidden = Detection.PlayerHidden;
            float vis = Mathf.Clamp01(Detection.Visibility01);
            float open = hidden ? 0.08f : Mathf.Lerp(0.25f, 1f, vis);
            eyeWhite.localScale = new Vector3(1f, open, 1f);
            eyePupil.localScale = Vector3.one * (hidden ? 0f : Mathf.Lerp(0.6f, 1f, vis));
            eyeLid.gameObject.SetActive(hidden);
            Color tint = vis > 0.7f && !hidden ? UiTheme.Amber : UiTheme.MutedWhite;
            eyeWhiteImage.color = tint;
            eyeText.text = hidden ? "UKRYTY" : $"WIDOCZNOŚĆ\n{Mathf.RoundToInt(vis * 100f)}%";
            eyePanel.gameObject.SetActive(!ctx.ViewfinderUp);
        }
    }
}
