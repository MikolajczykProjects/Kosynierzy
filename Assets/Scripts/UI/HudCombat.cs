using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Centre-screen HUD: damage flash, objective marker (diamond + distance), crosshair that opens with spread
    /// (hidden while aiming down the sights or looking through the camera), hit marker, kill/streak popups,
    /// target ID under the crosshair (Character.Label, red when hostile) and the interaction paper tag "E — …".
    /// </summary>
    public sealed class HudCombat : HudWidget
    {
        const float TargetRange = 40f;

        UnityEngine.UI.Image damageFlash;
        RectTransform markerContainer, marker;
        UnityEngine.UI.Text markerDistance;
        RectTransform crosshair;
        readonly UnityEngine.UI.Image[] ticks = new UnityEngine.UI.Image[4];
        UnityEngine.UI.Image dot;
        RectTransform hitMarker;
        readonly UnityEngine.UI.Image[] hitTicks = new UnityEngine.UI.Image[4];
        readonly List<UnityEngine.UI.Text> popups = new();
        RectTransform targetTag;
        UnityEngine.UI.Text targetText;
        RectTransform promptTag;
        UnityEngine.UI.Text promptText;
        string shownPrompt;
        Character lastTarget;
        float nextRaycast;

        public override void Build(HudContext ctx)
        {
            var root = ctx.Root;

            damageFlash = SettingsMenuUi.CreateImage("DamageFlash", root, UiTheme.WithAlpha(UiTheme.Danger, 0f), false);
            damageFlash.sprite = UiTheme.VignetteSprite;
            SettingsMenuUi.Stretch(damageFlash.rectTransform);

            // Objective marker lives in a full-screen container anchored bottom-left (screen-space positions).
            markerContainer = SettingsMenuUi.CreateRect("MarkerSpace", root);
            SettingsMenuUi.Stretch(markerContainer);
            marker = SettingsMenuUi.CreateRect("Marker", markerContainer);
            marker.anchorMin = marker.anchorMax = Vector2.zero;
            marker.sizeDelta = new Vector2(40f, 40f);
            var diamond = UiTheme.CreateDiamond(marker, 14f, UiTheme.Bone);
            SettingsMenuUi.Place(diamond, new Vector2(0.5f, 0.5f), Vector2.zero, diamond.sizeDelta);
            markerDistance = UiTheme.CreateHudText("Distance", marker, "", 16, TextAnchor.UpperCenter, UiTheme.Bone2);
            SettingsMenuUi.Place(markerDistance.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(140f, 22f));
            markerDistance.rectTransform.pivot = new Vector2(0.5f, 1f);

            BuildCompass(root);

            // Crosshair: four short ticks with a gap that opens with spread, plus a dot; soft shadow so it reads anywhere.
            crosshair = SettingsMenuUi.CreateRect("Crosshair", root);
            SettingsMenuUi.Place(crosshair, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f));
            for (int i = 0; i < 4; i++)
            {
                ticks[i] = SettingsMenuUi.CreateImage("Tick", crosshair, UiTheme.Bone, false);
                ticks[i].rectTransform.anchorMin = ticks[i].rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                ticks[i].rectTransform.sizeDelta = i < 2 ? new Vector2(9f, 2f) : new Vector2(2f, 9f);
                AddShadow(ticks[i]);
            }
            dot = SettingsMenuUi.CreateImage("Dot", crosshair, UiTheme.Bone, false);
            SettingsMenuUi.Place(dot.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, 2f));
            AddShadow(dot);

            hitMarker = SettingsMenuUi.CreateRect("HitMarker", root);
            SettingsMenuUi.Place(hitMarker, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f));
            for (int i = 0; i < 4; i++)
            {
                var arm = SettingsMenuUi.CreateRect("Arm", hitMarker);
                SettingsMenuUi.Place(arm, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                arm.localRotation = Quaternion.Euler(0f, 0f, 45f + 90f * i);
                hitTicks[i] = SettingsMenuUi.CreateImage("Stroke", arm, Color.white, false);
                hitTicks[i].rectTransform.anchorMin = hitTicks[i].rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                hitTicks[i].rectTransform.pivot = new Vector2(0f, 0.5f);
                AddShadow(hitTicks[i]);
            }

            for (int i = 0; i < 5; i++)
            {
                var p = UiTheme.CreateHudText("Popup", root, "", 24, TextAnchor.MiddleCenter, UiTheme.Bone);
                SettingsMenuUi.Place(p.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 34f));
                p.gameObject.SetActive(false);
                popups.Add(p);
            }

            // Target tag: bare caps under the crosshair (red when hostile).
            targetTag = SettingsMenuUi.CreateRect("TargetId", root);
            SettingsMenuUi.Place(targetTag, new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(300f, 24f));
            targetText = UiTheme.CreateHudText("Label", targetTag, "", 15, TextAnchor.MiddleCenter, UiTheme.Bone);
            SettingsMenuUi.Stretch(targetText.rectTransform);
            targetTag.gameObject.SetActive(false);

            // Interaction prompt at ~60 % height: cream key cap (dark letter) + 22 px label, bare.
            promptTag = SettingsMenuUi.CreateRect("PromptTag", root);
            SettingsMenuUi.Place(promptTag, new Vector2(0.5f, 0.5f), new Vector2(0f, -108f), new Vector2(320f, 28f));
            var key = SettingsMenuUi.CreateImage("Key", promptTag, UiTheme.Bone, false);
            key.rectTransform.anchorMin = key.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            key.rectTransform.pivot = new Vector2(0f, 0.5f);
            key.rectTransform.sizeDelta = new Vector2(26f, 26f);
            key.rectTransform.anchoredPosition = Vector2.zero;
            UiTheme.AddSoftShadow(key);
            var keyText = SettingsMenuUi.CreateText("E", key.rectTransform, "E", 16, TextAnchor.MiddleCenter, FontStyle.Normal, UiTheme.KeyInk);
            keyText.font = UiTheme.Label;
            SettingsMenuUi.Stretch(keyText.rectTransform);
            promptText = UiTheme.CreateHudText("Prompt", promptTag, "", 22, TextAnchor.MiddleLeft, UiTheme.Bone);
            SettingsMenuUi.Stretch(promptText.rectTransform, 38f, 0f);
            promptTag.gameObject.SetActive(false);
        }

        // ---------- Compass (top centre, bare ticks with edge fade) ----------

        const float CompassPxPerDeg = 6f;
        const float CompassHalf = 60f;
        const int CompassSlots = 26;
        static readonly string[] CardinalNames = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        RectTransform compass;
        readonly UnityEngine.UI.Image[] compassTicks = new UnityEngine.UI.Image[CompassSlots];
        readonly UnityEngine.UI.Text[] compassLabels = new UnityEngine.UI.Text[CompassSlots];
        UnityEngine.UI.Text compassHeading, compassMarkerText;
        RectTransform compassMarker;

        void BuildCompass(RectTransform root)
        {

            compass = SettingsMenuUi.CreateRect("Compass", root);
            SettingsMenuUi.Place(compass, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(CompassHalf * CompassPxPerDeg * 2f, 110f));
            for (int i = 0; i < CompassSlots; i++)
            {
                var tick = SettingsMenuUi.CreateImage("Tick", compass, UiTheme.Bone, false);
                tick.rectTransform.anchorMin = tick.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                tick.rectTransform.pivot = new Vector2(0.5f, 1f);
                UiTheme.AddSoftShadow(tick);
                compassTicks[i] = tick;
                var label = UiTheme.CreateHudText("Label", compass, "", 16, TextAnchor.UpperCenter, UiTheme.Bone);
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                label.rectTransform.pivot = new Vector2(0.5f, 1f);
                label.rectTransform.sizeDelta = new Vector2(40f, 20f);
                compassLabels[i] = label;
            }
            compassHeading = UiTheme.CreateHudText("Heading", compass, "000", 15, TextAnchor.UpperCenter, UiTheme.Bone);
            SettingsMenuUi.Place(compassHeading.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(80f, 18f));
            compassHeading.rectTransform.pivot = new Vector2(0.5f, 1f);
            var caret = UiTheme.CreateDiamond(compass, 7f, UiTheme.Bone);
            SettingsMenuUi.Place(caret, new Vector2(0.5f, 1f), new Vector2(0f, -38f), caret.sizeDelta);

            compassMarker = UiTheme.CreateDiamond(compass, 9f, UiTheme.Warn);
            compassMarker.anchorMin = compassMarker.anchorMax = new Vector2(0.5f, 1f);
            compassMarker.pivot = new Vector2(0.5f, 1f);
            compassMarkerText = UiTheme.CreateHudText("MarkerDistance", compassMarker, "", 13, TextAnchor.UpperCenter, UiTheme.Bone2);
            SettingsMenuUi.Place(compassMarkerText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -2f), new Vector2(80f, 16f));
            compassMarkerText.rectTransform.pivot = new Vector2(0.5f, 1f);
        }

        void TickCompass(HudContext ctx)
        {
            bool show = ctx.Cam != null && !ctx.ViewfinderUp;
            compass.gameObject.SetActive(show);
            if (!show) return;

            float heading = ctx.Cam.transform.eulerAngles.y;
            compassHeading.text = Mathf.RoundToInt(heading) % 360 == 0 ? "000" : (Mathf.RoundToInt(heading) % 360).ToString("000");
            float edge = CompassHalf * CompassPxPerDeg;
            int first = Mathf.CeilToInt((heading - CompassHalf) / 5f);
            for (int i = 0; i < CompassSlots; i++)
            {
                float bearing = (first + i) * 5f;
                float dx = (bearing - heading) * CompassPxPerDeg;
                float a = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(Mathf.PI * 0.5f * Mathf.Min(1f, Mathf.Abs(dx) / edge))), 1.15f);
                bool inRange = bearing <= heading + CompassHalf && a >= 0.04f;
                compassTicks[i].gameObject.SetActive(inRange);
                compassLabels[i].gameObject.SetActive(false);
                if (!inRange) continue;
                int m = ((Mathf.RoundToInt(bearing) % 360) + 360) % 360;
                bool card = m % 45 == 0, mid = m % 15 == 0;
                var trt = compassTicks[i].rectTransform;
                trt.sizeDelta = new Vector2(card ? 2f : 1f, card ? 16f : mid ? 11f : 6f);
                trt.anchoredPosition = new Vector2(dx, -48f);
                compassTicks[i].color = UiTheme.WithAlpha(UiTheme.Bone, a * (card ? 1f : mid ? 0.85f : 0.55f));
                if (card || m % 30 == 0)
                {
                    var l = compassLabels[i];
                    l.gameObject.SetActive(true);
                    l.text = card ? CardinalNames[m / 45] : m.ToString();
                    l.fontSize = card ? 16 : 12;
                    l.color = UiTheme.WithAlpha(card ? UiTheme.Bone : UiTheme.Bone2, a);
                    l.rectTransform.anchoredPosition = new Vector2(dx, -68f);
                }
            }

            Vector3? world = Mission.Instance != null && Mission.Instance.Current != null ? Mission.Instance.Current.MarkerPosition : null;
            if (world == null && QuestManager.Instance != null && QuestManager.Instance.Current != null && QuestManager.Instance.Current.ShowMarker)
                world = QuestManager.Instance.Current.MarkerPosition;
            compassMarker.gameObject.SetActive(world != null);
            if (world == null) return;
            Vector3 to = world.Value - ctx.Cam.transform.position;
            float bear = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            float delta = Mathf.Repeat(bear - heading + 180f, 360f) - 180f;
            bool clamped = Mathf.Abs(delta) > CompassHalf;
            compassMarker.anchoredPosition = new Vector2(Mathf.Clamp(delta, -CompassHalf, CompassHalf) * CompassPxPerDeg, -41f);
            compassMarkerText.text = $"{Mathf.RoundToInt(new Vector2(to.x, to.z).magnitude)} m";
            compassMarkerText.color = UiTheme.WithAlpha(UiTheme.Bone2, clamped ? 0.6f : 1f);
        }

        public override void Tick(HudContext ctx)
        {
            var fb = ctx.Feedback;
            float now = ctx.Now;

            // Damage flash.
            float dmg = fb != null ? Mathf.Clamp01(1f - (now - fb.LastDamageTime) / 0.45f) : 0f;
            damageFlash.color = UiTheme.WithAlpha(UiTheme.Danger, dmg * 0.7f);

            TickCompass(ctx);
            TickMarker(ctx);
            TickCrosshair(ctx);
            TickPopups(ctx);
            TickTargetId(ctx);
            TickPrompt(ctx);
        }

        void TickMarker(HudContext ctx)
        {
            Vector3? world = Mission.Instance != null && Mission.Instance.Current != null ? Mission.Instance.Current.MarkerPosition : null;
            if (world == null && QuestManager.Instance != null && QuestManager.Instance.Current != null && QuestManager.Instance.Current.ShowMarker)
                world = QuestManager.Instance.Current.MarkerPosition;
            bool show = world != null && ctx.Cam != null && !ctx.ViewfinderUp;
            marker.gameObject.SetActive(show);
            if (!show) return;

            Vector3 target = world.Value + Vector3.up * 1.5f;
            Vector3 sp = ctx.Cam.WorldToScreenPoint(target);
            Vector2 size = ctx.Size;
            Vector2 p = ctx.ScreenToCanvas(sp);
            if (sp.z < 0f) p = new Vector2(size.x - p.x, 60f); // behind: mirror to the bottom edge
            const float margin = 46f;
            p.x = Mathf.Clamp(p.x, margin, size.x - margin);
            p.y = Mathf.Clamp(p.y, margin + 60f, size.y - margin - 40f);
            p.y += Mathf.Sin(ctx.Now * 3f) * 3f;
            marker.anchoredPosition = p;
            int meters = Mathf.RoundToInt(Vector3.Distance(ctx.Cam.transform.position, world.Value));
            markerDistance.text = $"{meters} m";
        }

        void TickCrosshair(HudContext ctx)
        {
            var weapon = ctx.Weapon;
            bool aiming = ctx.PistolOut && weapon.IsAiming;
            bool show = !ctx.ViewfinderUp && !aiming;
            crosshair.gameObject.SetActive(show);
            if (show)
            {
                float spread = ctx.PistolOut ? weapon.CurrentSpread : 0f;
                float gap = 5f + spread * 9f;
                bool ticksOn = ctx.PistolOut;
                for (int i = 0; i < 4; i++) ticks[i].enabled = ticksOn;
                ticks[0].rectTransform.anchoredPosition = new Vector2(-gap - 4.5f, 0f);
                ticks[1].rectTransform.anchoredPosition = new Vector2(gap + 4.5f, 0f);
                ticks[2].rectTransform.anchoredPosition = new Vector2(0f, gap + 4.5f);
                ticks[3].rectTransform.anchoredPosition = new Vector2(0f, -gap - 4.5f);
                dot.color = UiTheme.WithAlpha(UiTheme.Bone, ticksOn ? 0.95f : 0.6f);
            }

            var fb = ctx.Feedback;
            float age = fb != null ? ctx.Now - fb.LastHitTime : 99f;
            float dur = fb != null && fb.LastHitKilled ? 0.45f : 0.2f;
            bool hit = age < dur && !ctx.ViewfinderUp;
            hitMarker.gameObject.SetActive(hit);
            if (!hit) return;
            float k = 1f - age / dur;
            float length = (fb.LastHitKilled ? 15f : 10f) * (1f + 0.6f * Mathf.Exp(-age * 25f));
            float thick = fb.LastHitKilled ? 3.5f : 2.5f;
            Color c = fb.LastHitKilled ? UiTheme.Danger : fb.LastHitHeadshot ? UiTheme.Warn : UiTheme.Bone;
            for (int i = 0; i < 4; i++)
            {
                hitTicks[i].color = UiTheme.WithAlpha(c, k);
                hitTicks[i].rectTransform.anchoredPosition = new Vector2(8f, 0f);
                hitTicks[i].rectTransform.sizeDelta = new Vector2(length, thick);
            }
        }

        void TickPopups(HudContext ctx)
        {
            var fb = ctx.Feedback;
            int count = fb != null ? fb.Popups.Count : 0;
            for (int i = 0; i < popups.Count; i++)
            {
                var text = popups[i];
                int index = count - 1 - i; // newest on top
                if (index < 0 || ctx.ViewfinderUp)
                {
                    text.gameObject.SetActive(false);
                    continue;
                }
                var p = fb.Popups[index];
                float age = ctx.Now - p.Born;
                float alpha = Mathf.Clamp01(1.6f - age);
                float punch = 1f + 0.4f * Mathf.Exp(-age * 12f);
                text.gameObject.SetActive(true);
                text.text = (p.Text ?? "").ToUpperInvariant();
                text.fontSize = Mathf.RoundToInt(24f * Mathf.Max(0.6f, p.Scale));
                text.rectTransform.localScale = Vector3.one * punch;
                text.color = UiTheme.WithAlpha(PopupColor(p.Color), alpha);
                text.rectTransform.anchoredPosition = new Vector2(0f, -150f - i * 34f + age * 16f);
            }
        }

        static Color PopupColor(Color c)
        {
            // Map CombatFeedback's colours onto the palette: reds → alert red, golds → brass, else muted white.
            if (c.r > 0.8f && c.g < 0.5f) return UiTheme.Danger;
            if (c.r > 0.8f && c.g > 0.6f && c.b < 0.5f) return UiTheme.Warn;
            return UiTheme.Bone;
        }

        void TickTargetId(HudContext ctx)
        {
            if (ctx.Cam == null || ctx.ViewfinderUp)
            {
                targetTag.gameObject.SetActive(false);
                return;
            }
            if (ctx.Now >= nextRaycast)
            {
                nextRaycast = ctx.Now + 0.08f;
                lastTarget = null;
                var t = ctx.Cam.transform;
                if (Physics.Raycast(t.position, t.forward, out var hit, TargetRange, ~0, QueryTriggerInteraction.Ignore))
                {
                    var character = hit.collider.GetComponentInParent<Character>();
                    if (character != null && !character.IsDown) lastTarget = character;
                }
            }

            string label = lastTarget != null ? lastTarget.Label : null;
            bool show = !string.IsNullOrEmpty(label);
            targetTag.gameObject.SetActive(show);
            if (!show) return;
            bool hostile = lastTarget.IsHostile;
            targetText.text = label.ToUpperInvariant();
            targetText.color = hostile ? UiTheme.Danger : UiTheme.Bone;
            targetTag.sizeDelta = new Vector2(300f, 24f);
        }

        void TickPrompt(HudContext ctx)
        {
            var target = ctx.Interactor != null ? ctx.Interactor.CurrentTarget : null;
            string prompt = target != null && !ctx.ViewfinderUp ? target.Prompt : null;
            bool show = !string.IsNullOrEmpty(prompt);
            promptTag.gameObject.SetActive(show);
            if (!show || prompt == shownPrompt) return;
            shownPrompt = prompt;
            promptText.text = prompt;
            promptTag.sizeDelta = new Vector2(promptText.preferredWidth + 46f, promptTag.sizeDelta.y);
        }

        static void AddShadow(UnityEngine.UI.Graphic g)
        {
            var s = g.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.75f);
            s.effectDistance = new Vector2(1.5f, -1.5f);
        }
    }
}
