using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Kosynierzy
{
    /// <summary>
    /// Builds the uGUI pieces shared by the main, pause and settings menus (placeholder style: dark sepia panels,
    /// cream text, warm accent). Legacy uGUI Text with the built-in font, so no TextMeshPro resources are needed
    /// and Polish letters render through the dynamic font.
    /// </summary>
    public static class SettingsMenuUi
    {
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;
        public const float RowHeight = 50f;

        public static readonly Color DimColor = new(0f, 0f, 0f, 0.6f);
        public static readonly Color PanelColor = new(0.09f, 0.075f, 0.06f, 0.95f);
        public static readonly Color TextColor = new(0.95f, 0.9f, 0.82f, 1f);
        public static readonly Color MutedTextColor = new(0.7f, 0.64f, 0.56f, 1f);
        public static readonly Color AccentColor = new(0.85f, 0.55f, 0.24f, 1f);
        public static readonly Color TrackColor = new(0.25f, 0.21f, 0.18f, 1f);

        static Font font;

        /// <summary>Font for every menu label; defaults to the built-in LegacyRuntime font.</summary>
        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
            set => font = value;
        }

        static int UiLayer
        {
            get
            {
                int layer = LayerMask.NameToLayer("UI");
                return layer < 0 ? 0 : layer;
            }
        }

        /// <summary>Exactly one EventSystem driven by the Input System (the old Input class is disabled).</summary>
        public static void EnsureEventSystem()
        {
            var es = EventSystem.current != null ? EventSystem.current : UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (es == null) es = new GameObject("EventSystem").AddComponent<EventSystem>();

            var legacy = es.GetComponent<StandaloneInputModule>();
            if (legacy != null)
            {
                legacy.enabled = false;
                UnityEngine.Object.Destroy(legacy);
            }
            if (es.GetComponent<InputSystemUIInputModule>() == null) es.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        public static void Select(UnityEngine.UI.Selectable selectable)
        {
            var es = EventSystem.current;
            if (es == null || selectable == null) return;
            es.SetSelectedGameObject(null);
            es.SetSelectedGameObject(selectable.gameObject);
        }

        /// <summary>
        /// A mouse click on empty space clears the selection, which strands gamepad/keyboard users;
        /// call every frame while a menu is open to reselect <paramref name="fallback"/> on the next up/down press.
        /// </summary>
        public static void RestoreSelection(UnityEngine.UI.Selectable fallback)
        {
            var es = EventSystem.current;
            if (es == null || fallback == null) return;
            var current = es.currentSelectedGameObject;
            if (current != null && current.activeInHierarchy) return;
            if (NavigationPressed()) Select(fallback);
        }

        static bool NavigationPressed()
        {
            var pad = Gamepad.current;
            if (pad != null && (pad.dpad.up.wasPressedThisFrame || pad.dpad.down.wasPressedThisFrame ||
                                pad.leftStick.up.wasPressedThisFrame || pad.leftStick.down.wasPressedThisFrame))
                return true;
            var keys = Keyboard.current;
            return keys != null && (keys.upArrowKey.wasPressedThisFrame || keys.downArrowKey.wasPressedThisFrame ||
                                    keys.wKey.wasPressedThisFrame || keys.sKey.wasPressedThisFrame || keys.tabKey.wasPressedThisFrame);
        }

        // ---------- Building blocks ----------

        /// <summary>Screen-space overlay canvas, created INACTIVE so Selectables get their colours on first enable.</summary>
        public static Canvas CreateCanvas(string name, Transform parent, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = UiLayer;
            go.transform.SetParent(parent, false);
            go.SetActive(false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            // Match height: the settings panel is tall, and the team plays on ultrawide screens.
            scaler.matchWidthOrHeight = 1f;

            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = UiLayer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Anchors to a fraction of the parent (x0,y0)-(x1,y1) with zero offsets.</summary>
        public static void Anchor(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>Fixed-size rect pinned to the top centre of its parent, <paramref name="top"/> px below the edge.</summary>
        public static void PinTop(RectTransform rt, float top, float height, float sideMargin)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(sideMargin, -top - height);
            rt.offsetMax = new Vector2(-sideMargin, -top);
        }

        public static UnityEngine.UI.Image CreateImage(string name, Transform parent, Color color, bool raycast)
        {
            var rt = CreateRect(name, parent);
            var image = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        public static UnityEngine.UI.Text CreateText(string name, Transform parent, string content, int size,
            TextAnchor alignment, FontStyle style, Color color)
        {
            var rt = CreateRect(name, parent);
            var text = rt.gameObject.AddComponent<UnityEngine.UI.Text>();
            text.font = Font;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        /// <summary>Full-screen dim + centred panel with an accent bar on top. Returns the panel.</summary>
        public static RectTransform CreateDimmedPanel(Transform canvasRoot, Vector2 size)
        {
            var dim = CreateImage("Dim", canvasRoot, DimColor, true);
            Stretch(dim.rectTransform);

            var panel = CreateImage("Panel", canvasRoot, PanelColor, true);
            var rt = panel.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;

            var bar = CreateImage("AccentBar", rt, AccentColor, false);
            PinTop(bar.rectTransform, 0f, 6f, 0f);
            return rt;
        }

        public static UnityEngine.UI.ColorBlock ButtonColors() => new()
        {
            normalColor = new Color(0.17f, 0.14f, 0.12f, 1f),
            highlightedColor = new Color(0.32f, 0.24f, 0.16f, 1f),
            pressedColor = new Color(0.66f, 0.4f, 0.17f, 1f),
            selectedColor = new Color(0.55f, 0.33f, 0.14f, 1f),
            disabledColor = new Color(0.12f, 0.11f, 0.1f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f,
        };

        public static UnityEngine.UI.ColorBlock RowColors() => new()
        {
            normalColor = new Color(1f, 1f, 1f, 0.04f),
            highlightedColor = new Color(1f, 1f, 1f, 0.1f),
            pressedColor = new Color(0.85f, 0.55f, 0.24f, 0.55f),
            selectedColor = new Color(0.85f, 0.55f, 0.24f, 0.35f),
            disabledColor = new Color(1f, 1f, 1f, 0.02f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f,
        };

        public static UnityEngine.UI.Button CreateButton(Transform parent, string label, float height, int fontSize,
            UnityEngine.Events.UnityAction onClick, out UnityEngine.UI.Text text)
        {
            var rt = CreateRect(label, parent);
            var layout = rt.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;

            var image = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.white; // tinted by the button's ColorBlock
            var button = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.colors = ButtonColors();

            text = CreateText("Label", rt, label, fontSize, TextAnchor.MiddleCenter, FontStyle.Bold, TextColor);
            Stretch(text.rectTransform, 12f, 12f);
            if (onClick != null) button.onClick.AddListener(onClick);
            return button;
        }

        /// <summary>Vertical stack (VerticalLayoutGroup) that sizes children by their LayoutElement height.</summary>
        public static RectTransform CreateColumn(string name, Transform parent, float spacing)
        {
            var rt = CreateRect(name, parent);
            var layout = rt.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return rt;
        }

        static RectTransform CreateRow(Transform parent, string label, out UnityEngine.UI.Image background, out RectTransform control)
        {
            var row = CreateRect(label, parent);
            var layout = row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            layout.preferredHeight = RowHeight;
            layout.minHeight = RowHeight;

            background = row.gameObject.AddComponent<UnityEngine.UI.Image>();
            background.color = Color.white; // tinted by the row control's ColorBlock
            background.raycastTarget = true;

            var text = CreateText("Label", row, label, 28, TextAnchor.MiddleLeft, FontStyle.Normal, TextColor);
            Anchor(text.rectTransform, 0f, 0f, 0.47f, 1f);
            text.rectTransform.offsetMin = new Vector2(22f, 0f);

            control = CreateRect("Control", row);
            Anchor(control, 0.49f, 0f, 1f, 1f);
            control.offsetMax = new Vector2(-22f, 0f);
            return row;
        }

        /// <summary>Row with "‹ value ›" cycling through options.</summary>
        public static SettingsMenuStepper CreateStepper(Transform parent, string label, string[] options, int index, Action<int> onChanged)
        {
            var row = CreateRow(parent, label, out var background, out var control);

            var left = CreateText("Left", control, "‹", 40, TextAnchor.MiddleLeft, FontStyle.Bold, AccentColor);
            Anchor(left.rectTransform, 0f, 0f, 0.15f, 1f);
            var right = CreateText("Right", control, "›", 40, TextAnchor.MiddleRight, FontStyle.Bold, AccentColor);
            Anchor(right.rectTransform, 0.85f, 0f, 1f, 1f);
            var value = CreateText("Value", control, "", 28, TextAnchor.MiddleCenter, FontStyle.Bold, TextColor);
            Anchor(value.rectTransform, 0.12f, 0f, 0.88f, 1f);

            var stepper = row.gameObject.AddComponent<SettingsMenuStepper>();
            stepper.targetGraphic = background;
            stepper.colors = RowColors();
            stepper.Setup(value, control, options, index, onChanged);
            return stepper;
        }

        /// <summary>Row with a whole-number slider (gamepad left/right = one step) and a formatted value label.</summary>
        public static UnityEngine.UI.Slider CreateSlider(Transform parent, string label, int min, int max, int value,
            Func<float, string> format, Action<float> onChanged)
        {
            CreateRow(parent, label, out var background, out var control);

            var sliderRt = CreateRect("Slider", control);
            Anchor(sliderRt, 0.02f, 0.3f, 0.76f, 0.7f);

            var track = CreateImage("Track", sliderRt, TrackColor, true);
            Stretch(track.rectTransform);

            var fillArea = CreateRect("FillArea", sliderRt);
            Stretch(fillArea);
            var fill = CreateImage("Fill", fillArea, AccentColor, false);
            Stretch(fill.rectTransform);

            var handleArea = CreateRect("HandleArea", sliderRt);
            Stretch(handleArea, 9f, 9f);
            var handle = CreateImage("Handle", handleArea, TextColor, true);
            // The slider drives the anchors (x = value, y = 0..1); sizeDelta.y makes the handle taller than the track.
            handle.rectTransform.sizeDelta = new Vector2(18f, 16f);

            var valueText = CreateText("Value", control, "", 28, TextAnchor.MiddleRight, FontStyle.Bold, TextColor);
            Anchor(valueText.rectTransform, 0.78f, 0f, 1f, 1f);

            var slider = sliderRt.gameObject.AddComponent<UnityEngine.UI.Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = background;
            slider.colors = RowColors();
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.wholeNumbers = true;
            slider.minValue = min;
            slider.maxValue = max;
            slider.SetValueWithoutNotify(Mathf.Clamp(value, min, max));
            valueText.text = format(slider.value);
            slider.onValueChanged.AddListener(v =>
            {
                valueText.text = format(v);
                onChanged?.Invoke(v);
            });
            return slider;
        }

        /// <summary>Explicit navigation; null neighbours block movement (sliders/steppers then use left/right themselves).</summary>
        public static void SetNavigation(UnityEngine.UI.Selectable s, UnityEngine.UI.Selectable up, UnityEngine.UI.Selectable down,
            UnityEngine.UI.Selectable left = null, UnityEngine.UI.Selectable right = null)
        {
            if (s == null) return;
            s.navigation = new UnityEngine.UI.Navigation
            {
                mode = UnityEngine.UI.Navigation.Mode.Explicit,
                selectOnUp = up,
                selectOnDown = down,
                selectOnLeft = left,
                selectOnRight = right,
            };
        }

        /// <summary>Links a vertical list of buttons top to bottom, wrapping around.</summary>
        public static void LinkVertical(IReadOnlyList<UnityEngine.UI.Selectable> items)
        {
            int n = items.Count;
            for (int i = 0; i < n; i++)
                SetNavigation(items[i], items[(i + n - 1) % n], items[(i + 1) % n]);
        }
    }
}
