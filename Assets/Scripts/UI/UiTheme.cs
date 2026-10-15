using System;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// The 1939 wartime-document look shared by every screen: aged paper panels with a darker edge, ink text,
    /// stamp red for warnings, olive accents, muted white on dark bars for in-game overlays; typewriter body font
    /// (Courier New) and serif headings (Georgia / Times New Roman), small rotated stamps, thin rules.
    /// Sprites (paper grain, circle, ring, detection arc, viewfinder mask, vignette) are generated once at runtime.
    /// </summary>
    public static class UiTheme
    {
        public static readonly Color Paper = Hex(0xE6DCC3);
        public static readonly Color PaperEdge = Hex(0xB8A784);
        public static readonly Color Ink = Hex(0x231F1A);
        public static readonly Color StampRed = Hex(0x8E1B1B);
        public static readonly Color Olive = Hex(0x4B5320);
        public static readonly Color MutedWhite = Hex(0xF2EEE3);
        public static readonly Color Dark = Hex(0x14130F);

        /// <summary>Semi-transparent dark bar behind in-game overlay text.</summary>
        public static readonly Color DarkBar = new(0.078f, 0.075f, 0.059f, 0.78f);
        /// <summary>Stamp red lifted for legibility on dark overlays (alarm, hostile label).</summary>
        public static readonly Color AlertRed = new(0.86f, 0.27f, 0.22f, 1f);
        /// <summary>Mid detection / warning on dark overlays.</summary>
        public static readonly Color Amber = new(0.93f, 0.62f, 0.22f, 1f);
        /// <summary>Olive lifted for legibility on dark overlays.</summary>
        public static readonly Color LightOlive = new(0.62f, 0.67f, 0.38f, 1f);
        public static readonly Color Brass = new(0.8f, 0.64f, 0.32f, 1f);
        public static readonly Color FadedInk = new(0.137f, 0.122f, 0.102f, 0.62f);

        public static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }

        static Color Hex(int rgb) => new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        // ---------- Fonts ----------

        static Font typewriter, heading, fallback;

        public static Font Fallback
        {
            get
            {
                if (fallback == null) fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return fallback;
            }
        }

        /// <summary>Body text and HUD numbers.</summary>
        public static Font Typewriter
        {
            get
            {
                if (typewriter == null) typewriter = OsFont(new[] { "Courier New", "Courier" }, 32);
                return typewriter;
            }
        }

        /// <summary>Headings (use bold, upper case).</summary>
        public static Font Heading
        {
            get
            {
                if (heading == null) heading = OsFont(new[] { "Georgia", "Times New Roman" }, 48);
                return heading;
            }
        }

        static Font OsFont(string[] names, int size)
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
                Debug.LogWarning($"Kosynierzy: OS font lookup failed ({e.Message}); using the built-in font.");
            }
            return Fallback;
        }

        // ---------- Building blocks ----------

        /// <summary>Text in the heading font, bold, upper case.</summary>
        public static UnityEngine.UI.Text CreateHeading(string name, Transform parent, string content, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var text = SettingsMenuUi.CreateText(name, parent, (content ?? "").ToUpperInvariant(), size, alignment, FontStyle.Bold, color);
            text.font = Heading;
            return text;
        }

        /// <summary>Aged paper panel: darker edge, grainy paper inside. Returns the paper (put content there); root = edge.</summary>
        public static RectTransform CreatePaper(Transform parent, string name, Vector2 size, out RectTransform root, float edge = 4f)
        {
            var edgeImage = SettingsMenuUi.CreateImage(name, parent, PaperEdge, true);
            root = edgeImage.rectTransform;
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = size + new Vector2(edge * 2f, edge * 2f);

            var paper = SettingsMenuUi.CreateImage("Paper", root, Paper, false);
            paper.sprite = PaperSprite;
            paper.type = UnityEngine.UI.Image.Type.Tiled;
            SettingsMenuUi.Stretch(paper.rectTransform, edge, edge, edge, edge);
            return paper.rectTransform;
        }

        /// <summary>Semi-transparent dark bar for in-game overlays.</summary>
        public static UnityEngine.UI.Image CreateDarkBar(string name, Transform parent, float alpha = 0.78f)
        {
            return SettingsMenuUi.CreateImage(name, parent, WithAlpha(Dark, alpha), false);
        }

        /// <summary>Thin rule line; position it with anchors (it starts stretched horizontally at the top).</summary>
        public static UnityEngine.UI.Image CreateRule(string name, Transform parent, Color color, float thickness = 2f)
        {
            var rule = SettingsMenuUi.CreateImage(name, parent, color, false);
            var rt = rule.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, thickness);
            return rule;
        }

        /// <summary>Rubber-stamp label: upper-case text in a double frame, rotated (default −6°). Anchored at its centre.</summary>
        public static RectTransform CreateStamp(Transform parent, string content, Color color, int fontSize, float angle = -6f)
        {
            string label = (content ?? "").ToUpperInvariant();
            var rt = SettingsMenuUi.CreateRect("Stamp " + label, parent);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            float width = label.Length * fontSize * 0.68f + fontSize * 1.2f;
            float height = fontSize * 1.75f;
            rt.sizeDelta = new Vector2(width, height);
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);

            float thick = Mathf.Max(2f, fontSize * 0.11f);
            AddFrame(rt, WithAlpha(color, 0.88f), thick, 0f);
            AddFrame(rt, WithAlpha(color, 0.6f), Mathf.Max(1f, thick * 0.45f), thick + 3f);

            var text = SettingsMenuUi.CreateText("Label", rt, label, fontSize, TextAnchor.MiddleCenter, FontStyle.Bold, WithAlpha(color, 0.9f));
            text.font = Typewriter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            SettingsMenuUi.Stretch(text.rectTransform);
            return rt;
        }

        /// <summary>Four thin images along the inside of rt, inset by <paramref name="inset"/>.</summary>
        public static void AddFrame(RectTransform rt, Color color, float thickness, float inset)
        {
            Edge(rt, color, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(inset, -inset - thickness), new Vector2(-inset, -inset));
            Edge(rt, color, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(inset, inset), new Vector2(-inset, inset + thickness));
            Edge(rt, color, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(inset, inset), new Vector2(inset + thickness, -inset));
            Edge(rt, color, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-inset - thickness, inset), new Vector2(-inset, -inset));
        }

        static void Edge(RectTransform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var img = SettingsMenuUi.CreateImage("Frame", parent, color, false);
            var rt = img.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        // ---------- Procedural sprites ----------

        static Sprite paperSprite, circleSprite, ringSprite, arcSprite, viewfinderMask, vignetteSprite;

        /// <summary>Subtle paper grain, tiled (multiply with Paper).</summary>
        public static Sprite PaperSprite
        {
            get
            {
                if (paperSprite != null) return paperSprite;
                const int size = 128;
                var tex = NewTexture(size, size, TextureWrapMode.Repeat);
                var px = new Color32[size * size];
                var rng = new System.Random(1939);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float fibre = Mathf.PerlinNoise(x * 0.09f, y * 0.6f) * 0.05f;
                    float blot = Mathf.PerlinNoise(x * 0.03f + 7f, y * 0.03f + 3f) * 0.06f;
                    float grain = (float)rng.NextDouble() * 0.035f;
                    byte v = (byte)(255f * (1f - fibre - blot - grain));
                    px[y * size + x] = new Color32(v, v, v, 255);
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                paperSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                return paperSprite;
            }
        }

        public static Sprite CircleSprite => circleSprite != null ? circleSprite : circleSprite = RadialSprite(64, 0f, 1f);
        public static Sprite RingSprite => ringSprite != null ? ringSprite : ringSprite = RadialSprite(64, 0.78f, 1f);

        static Sprite RadialSprite(int size, float inner, float outer)
        {
            var tex = NewTexture(size, size, TextureWrapMode.Clamp);
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            float aa = 1.5f / c;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float r = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a = Mathf.Clamp01((outer - r) / aa) * (inner <= 0f ? 1f : Mathf.Clamp01((r - inner) / aa));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        /// <summary>Half-angle of the detection arc in degrees, and its ring band (fractions of the radius).</summary>
        public const float ArcHalfAngle = 26f;
        public const float ArcInner = 0.86f;

        /// <summary>
        /// Ring segment centred on +Y, cropped to its bounding box (width 2·sin(a)·R, height R − inner·cos(a)·R),
        /// so an Image of type Filled/Horizontal fills it across.
        /// </summary>
        public static Sprite ArcSprite
        {
            get
            {
                if (arcSprite != null) return arcSprite;
                float s = Mathf.Sin(ArcHalfAngle * Mathf.Deg2Rad);
                float y0 = ArcInner * Mathf.Cos(ArcHalfAngle * Mathf.Deg2Rad);
                const int w = 160;
                int h = Mathf.Max(8, Mathf.RoundToInt(w * (1f - y0) / (2f * s)));
                var tex = NewTexture(w, h, TextureWrapMode.Clamp);
                var px = new Color32[w * h];
                float aa = 2f / w;
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float ux = Mathf.Lerp(-s, s, (x + 0.5f) / w);
                    float uy = Mathf.Lerp(y0, 1f, (y + 0.5f) / h);
                    float r = Mathf.Sqrt(ux * ux + uy * uy);
                    float ang = Mathf.Abs(Mathf.Atan2(ux, uy)) * Mathf.Rad2Deg;
                    float a = Mathf.Clamp01((1f - r) / aa) * Mathf.Clamp01((r - ArcInner) / aa) * Mathf.Clamp01((ArcHalfAngle - ang) / 1.5f);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                arcSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect);
                return arcSprite;
            }
        }

        /// <summary>Rangefinder viewfinder: opaque outside a rounded window, darkening towards the window edge.</summary>
        public static Sprite ViewfinderMask
        {
            get
            {
                if (viewfinderMask != null) return viewfinderMask;
                const int w = 320, h = 180;
                var tex = NewTexture(w, h, TextureWrapMode.Clamp);
                var px = new Color32[w * h];
                const float insetX = 0.07f, insetY = 0.1f, radius = 0.09f; // in fractions of height
                float aspect = (float)w / h;
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    // Signed distance to a rounded rectangle, in units of the texture height.
                    float u = ((x + 0.5f) / w - 0.5f) * aspect;
                    float v = (y + 0.5f) / h - 0.5f;
                    float hx = 0.5f * aspect - insetX - radius;
                    float hy = 0.5f - insetY - radius;
                    float dx = Mathf.Max(Mathf.Abs(u) - hx, 0f);
                    float dy = Mathf.Max(Mathf.Abs(v) - hy, 0f);
                    float outside = Mathf.Sqrt(dx * dx + dy * dy) - radius
                                    + Mathf.Min(Mathf.Max(Mathf.Abs(u) - hx, Mathf.Abs(v) - hy), 0f);
                    float a = outside > 0f ? 1f : Mathf.Lerp(0.45f, 0f, Mathf.Clamp01(-outside / 0.12f));
                    if (outside > 0f && outside < 0.006f) a = Mathf.Lerp(0.45f, 1f, outside / 0.006f);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                viewfinderMask = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                return viewfinderMask;
            }
        }

        /// <summary>Transparent centre, opaque edges (damage flash, low light).</summary>
        public static Sprite VignetteSprite
        {
            get
            {
                if (vignetteSprite != null) return vignetteSprite;
                const int size = 128;
                var tex = NewTexture(size, size, TextureWrapMode.Clamp);
                var px = new Color32[size * size];
                float c = (size - 1) * 0.5f;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - c) / c, dy = (y - c) / c;
                    float d = Mathf.Clamp01((Mathf.Sqrt(dx * dx + dy * dy) - 0.55f) / 0.6f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(d * d * 255f));
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                vignetteSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                return vignetteSprite;
            }
        }

        static Texture2D NewTexture(int w, int h, TextureWrapMode wrap) =>
            new(w, h, TextureFormat.RGBA32, false) { wrapMode = wrap, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
    }
}
