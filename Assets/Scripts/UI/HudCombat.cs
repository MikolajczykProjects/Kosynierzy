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

            damageFlash = SettingsMenuUi.CreateImage("DamageFlash", root, UiTheme.WithAlpha(UiTheme.AlertRed, 0f), false);
            damageFlash.sprite = UiTheme.VignetteSprite;
            SettingsMenuUi.Stretch(damageFlash.rectTransform);

            // Objective marker lives in a full-screen container anchored bottom-left (screen-space positions).
            markerContainer = SettingsMenuUi.CreateRect("MarkerSpace", root);
            SettingsMenuUi.Stretch(markerContainer);
            marker = SettingsMenuUi.CreateRect("Marker", markerContainer);
            marker.anchorMin = marker.anchorMax = Vector2.zero;
            marker.sizeDelta = new Vector2(40f, 40f);
            var outer = SettingsMenuUi.CreateImage("Outer", marker, UiTheme.WithAlpha(UiTheme.Dark, 0.85f), false);
            SettingsMenuUi.Place(outer.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(19f, 19f));
            outer.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var inner = SettingsMenuUi.CreateImage("Inner", marker, UiTheme.MutedWhite, false);
            SettingsMenuUi.Place(inner.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12f, 12f));
            inner.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var core = SettingsMenuUi.CreateImage("Core", marker, UiTheme.Olive, false);
            SettingsMenuUi.Place(core.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5f, 5f));
            core.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            markerDistance = SettingsMenuUi.CreateText("Distance", marker, "", 18, TextAnchor.UpperCenter, FontStyle.Bold, UiTheme.MutedWhite);
            SettingsMenuUi.Place(markerDistance.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -4f), new Vector2(140f, 26f));
            markerDistance.rectTransform.pivot = new Vector2(0.5f, 1f);
            AddShadow(markerDistance);

            // Crosshair: four ticks and a dot, each with a dark shadow so it reads on any background.
            crosshair = SettingsMenuUi.CreateRect("Crosshair", root);
            SettingsMenuUi.Place(crosshair, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f));
            for (int i = 0; i < 4; i++)
            {
                ticks[i] = SettingsMenuUi.CreateImage("Tick", crosshair, UiTheme.WithAlpha(UiTheme.MutedWhite, 0.9f), false);
                ticks[i].rectTransform.anchorMin = ticks[i].rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                ticks[i].rectTransform.sizeDelta = i < 2 ? new Vector2(9f, 2f) : new Vector2(2f, 9f);
                AddShadow(ticks[i]);
            }
            dot = SettingsMenuUi.CreateImage("Dot", crosshair, UiTheme.MutedWhite, false);
            SettingsMenuUi.Place(dot.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(3f, 3f));
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
                var p = SettingsMenuUi.CreateText("Popup", root, "", 24, TextAnchor.MiddleCenter, FontStyle.Bold, UiTheme.MutedWhite);
                SettingsMenuUi.Place(p.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 34f));
                p.horizontalOverflow = HorizontalWrapMode.Overflow;
                AddShadow(p);
                p.gameObject.SetActive(false);
                popups.Add(p);
            }

            // Target ID: small dark bar under the crosshair.
            var targetBar = UiTheme.CreateDarkBar("TargetId", root, 0.7f);
            targetTag = targetBar.rectTransform;
            SettingsMenuUi.Place(targetTag, new Vector2(0.5f, 0.5f), new Vector2(0f, -48f), new Vector2(200f, 30f));
            targetText = SettingsMenuUi.CreateText("Label", targetTag, "", 19, TextAnchor.MiddleCenter, FontStyle.Bold, UiTheme.MutedWhite);
            targetText.horizontalOverflow = HorizontalWrapMode.Overflow;
            SettingsMenuUi.Stretch(targetText.rectTransform);
            targetTag.gameObject.SetActive(false);

            // Interaction prompt: a small paper tag with a key box.
            var tagPaper = UiTheme.CreatePaper(root, "PromptTag", new Vector2(260f, 40f), out promptTag, 2f);
            promptTag.anchoredPosition = new Vector2(0f, -100f);
            var key = SettingsMenuUi.CreateRect("Key", tagPaper);
            SettingsMenuUi.Place(key, new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(28f, 28f));
            key.pivot = new Vector2(0.5f, 0.5f);
            UiTheme.AddFrame(key, UiTheme.Ink, 2f, 0f);
            var keyText = SettingsMenuUi.CreateText("E", key, "E", 20, TextAnchor.MiddleCenter, FontStyle.Bold, UiTheme.Ink);
            SettingsMenuUi.Stretch(keyText.rectTransform);
            promptText = SettingsMenuUi.CreateText("Prompt", tagPaper, "", 20, TextAnchor.MiddleLeft, FontStyle.Bold, UiTheme.Ink);
            promptText.horizontalOverflow = HorizontalWrapMode.Overflow;
            SettingsMenuUi.Stretch(promptText.rectTransform, 46f, 10f);
            promptTag.gameObject.SetActive(false);
        }

        public override void Tick(HudContext ctx)
        {
            var fb = ctx.Feedback;
            float now = ctx.Now;

            // Damage flash.
            float dmg = fb != null ? Mathf.Clamp01(1f - (now - fb.LastDamageTime) / 0.45f) : 0f;
            damageFlash.color = UiTheme.WithAlpha(UiTheme.AlertRed, dmg * 0.7f);

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
                dot.color = UiTheme.WithAlpha(UiTheme.MutedWhite, ticksOn ? 0.95f : 0.6f);
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
            Color c = fb.LastHitKilled ? UiTheme.AlertRed : fb.LastHitHeadshot ? UiTheme.Brass : UiTheme.MutedWhite;
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
            if (c.r > 0.8f && c.g < 0.5f) return UiTheme.AlertRed;
            if (c.r > 0.8f && c.g > 0.6f && c.b < 0.5f) return UiTheme.Brass;
            return UiTheme.MutedWhite;
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
            targetText.color = hostile ? UiTheme.AlertRed : UiTheme.MutedWhite;
            targetTag.sizeDelta = new Vector2(targetText.preferredWidth + 28f, 30f);
        }

        void TickPrompt(HudContext ctx)
        {
            var target = ctx.Interactor != null ? ctx.Interactor.CurrentTarget : null;
            string prompt = target != null && !ctx.ViewfinderUp ? target.Prompt : null;
            bool show = !string.IsNullOrEmpty(prompt);
            promptTag.gameObject.SetActive(show);
            if (!show || prompt == shownPrompt) return;
            shownPrompt = prompt;
            promptText.text = "— " + prompt;
            promptTag.sizeDelta = new Vector2(promptText.preferredWidth + 64f, promptTag.sizeDelta.y);
        }

        static void AddShadow(UnityEngine.UI.Graphic g)
        {
            var s = g.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.75f);
            s.effectDistance = new Vector2(1.5f, -1.5f);
        }
    }
}
