using System;
using UnityEngine;
using UnityEngine.UI;

namespace Kosynierzy
{
    /// <summary>
    /// "UI v2" building blocks for menus (loadout, pause, settings, fail, main menu): Bahnschrift display font, Segoe UI body,
    /// bone text on dark panels, thin hairlines, bar-marked menu items. Tokens come from docs/design/UI_V2_SPEC.md.
    /// </summary>
    public static class MenuV2
    {
        public enum Kind { Item, Filled, Outline, Tab }

        public static readonly Color Bone = Hex(0xEEEAE0, 1f);
        public static readonly Color Bone2 = Hex(0xEEEAE0, 0.65f);
        public static readonly Color Bone3 = Hex(0xEEEAE0, 0.38f);
        public static readonly Color Hair = Hex(0xEEEAE0, 0.18f);
        public static readonly Color Amber = Hex(0xE2A93B, 1f);
        public static readonly Color Red = Hex(0xDB4630, 1f);
        public static readonly Color Olive = Hex(0x93AD66, 1f);
        public static readonly Color Ink = Hex(0x0C0E10, 1f);
        public static readonly Color Panel = Hex(0x14120F, 0.62f);
        public static readonly Color Card = Hex(0x1A1E22, 0.8f);
        public static readonly Color CardSelected = Hex(0x2A2F36, 0.9f);
        public static readonly Color Wash = Hex(0x06080A, 0.84f);

        static Color Hex(int rgb, float a) =>
            new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, a);

        // ---------- Fonts ----------

        static Font display, body;

        /// <summary>Labels, numbers, titles (condensed grotesque).</summary>
        public static Font Display => display != null ? display : display = Load(new[] { "Bahnschrift SemiCondensed", "Bahnschrift", "Segoe UI" }, 48);

        /// <summary>Sentences.</summary>
        public static Font Body => body != null ? body : body = Load(new[] { "Segoe UI", "Arial" }, 32);

        static Font Load(string[] names, int size)
        {
            try
            {
                var installed = Font.GetOSInstalledFontNames();
                foreach (var name in names)
                    if (Array.IndexOf(installed, name) >= 0)
                        return Font.CreateDynamicFontFromOSFont(name, size);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Kosynierzy: menu font lookup failed ({e.Message}); using the built-in font.");
            }
            return UiTheme.Fallback;
        }

        // ---------- Elements ----------

        /// <summary>Single-line label (display font), or wrapped body text when <paramref name="isBody"/>.</summary>
        public static Text Label(string name, Transform parent, string content, int size, TextAnchor alignment, Color color,
            bool isBody = false, FontStyle style = FontStyle.Normal)
        {
            var t = SettingsMenuUi.CreateText(name, parent, content, size, alignment, style, color);
            t.font = isBody ? Body : Display;
            t.horizontalOverflow = isBody ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            return t;
        }

        public static Image Box(string name, Transform parent, Color color) => SettingsMenuUi.CreateImage(name, parent, color, false);

        /// <summary>1 px line along the bottom edge of <paramref name="parent"/>.</summary>
        public static Image HairlineBottom(Transform parent, float inset = 0f)
        {
            var img = Box("Hairline", parent, Hair);
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(inset, 0f);
            rt.offsetMax = new Vector2(-inset, 1f);
            return img;
        }

        /// <summary>Horizontal 1 px rule pinned <paramref name="top"/> px below the top edge.</summary>
        public static Image HairlineTop(Transform parent, float top, float side, Color? color = null)
        {
            var img = Box("Hairline", parent, color ?? Hair);
            SettingsMenuUi.PinTop(img.rectTransform, top, 1f, side);
            return img;
        }

        /// <summary>Full-screen dark wash + a centred dark panel with a hairline frame. Returns the panel (content area).</summary>
        public static RectTransform CreateDimmedPanel(Transform canvasRoot, Vector2 size)
        {
            var dim = SettingsMenuUi.CreateImage("Dim", canvasRoot, Wash, true);
            SettingsMenuUi.Stretch(dim.rectTransform);
            var panel = SettingsMenuUi.CreateImage("Panel", canvasRoot, Panel, false);
            SettingsMenuUi.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            UiTheme.AddFrame(panel.rectTransform, Hair, 1f, 0f);
            return panel.rectTransform;
        }

        /// <summary>Two thin strokes forming a chevron inside a 16x16 box; <paramref name="pointsRight"/> picks the direction.</summary>
        public static RectTransform Chevron(Transform parent, bool pointsRight, Color color)
        {
            var box = SettingsMenuUi.CreateRect(pointsRight ? "ChevronRight" : "ChevronLeft", parent);
            box.sizeDelta = new Vector2(16f, 16f);
            float sx = pointsRight ? -1f : 1f;
            for (int i = 0; i < 2; i++)
            {
                float sy = i == 0 ? 1f : -1f;
                var arm = Box("Arm", box, color);
                var rt = arm.rectTransform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(11f, 2f);
                rt.anchoredPosition = new Vector2(1.5f * sx, 3.5f * sy);
                rt.localRotation = Quaternion.Euler(0f, 0f, 45f * sx * sy);
            }
            return box;
        }

        /// <summary>
        /// Menu button. Item = big caps entry with a bar on the left when active; Filled = bone button with dark text;
        /// Outline = hairline outline; Tab = text that brightens. Has a LayoutElement, so it fits layout groups; place it otherwise.
        /// </summary>
        public static MenuV2Button CreateButton(Transform parent, string label, Kind kind, float height, int fontSize,
            UnityEngine.Events.UnityAction onClick, out Text text, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var rt = SettingsMenuUi.CreateRect(label, parent);
            var layout = rt.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;

            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = Color.clear;
            bg.raycastTarget = true;
            var button = rt.gameObject.AddComponent<MenuV2Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = bg;

            Image bar = null;
            if (kind == Kind.Outline) UiTheme.AddFrame(rt, new Color(Bone.r, Bone.g, Bone.b, 0.4f), 1f, 0f);
            if (kind == Kind.Item)
            {
                bar = Box("ActiveBar", rt, Bone);
                var br = bar.rectTransform;
                br.anchorMin = new Vector2(0f, 0.12f);
                br.anchorMax = new Vector2(0f, 0.88f);
                br.pivot = new Vector2(0f, 0.5f);
                br.sizeDelta = new Vector2(4f, 0f);
                br.anchoredPosition = Vector2.zero;
            }

            if (kind == Kind.Item) align = TextAnchor.MiddleLeft;
            text = Label("Label", rt, label.ToUpperInvariant(), fontSize, align, Bone);
            SettingsMenuUi.Stretch(text.rectTransform, kind == Kind.Item ? 28f : 12f, 12f);
            button.Init(kind, bg, bar, text);
            if (onClick != null) button.onClick.AddListener(onClick);
            return button;
        }
    }
}
