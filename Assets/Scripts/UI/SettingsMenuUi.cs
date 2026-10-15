using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Kosynierzy
{
    /// <summary>
    /// Builds the uGUI pieces shared by every menu and overlay, in the UiTheme 1939 document style:
    /// paper panels with ink text, olive selection, stamp-red press; dark variants for black screens.
    /// Legacy uGUI Text with dynamic OS fonts (Courier New / Georgia), so Polish letters need no font assets.
    /// </summary>
    public static class SettingsMenuUi
    {
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;
        public const float RowHeight = 56f;

        public static readonly Color DimColor = MenuV2.Wash;
        public static readonly Color PanelColor = MenuV2.Panel;
        public static readonly Color TextColor = MenuV2.Bone;
        public static readonly Color MutedTextColor = MenuV2.Bone2;
        public static readonly Color AccentColor = MenuV2.Bone;
        public static readonly Color TrackColor = MenuV2.Hair;

        static Font fontOverride;

        /// <summary>Body font for every label (UI v2: Segoe UI; Bahnschrift via MenuV2.Display); set to override for all menus.</summary>
        public static Font Font
        {
            get => fontOverride != null ? fontOverride : MenuV2.Body;
            set => fontOverride = value;
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
            // Match height: tall panels always fit, and the team plays on ultrawide screens.
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

        /// <summary>Fixed-height rect pinned to the top of its parent, <paramref name="top"/> px below the edge.</summary>
        public static void PinTop(RectTransform rt, float top, float height, float sideMargin)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(sideMargin, -top - height);
            rt.offsetMax = new Vector2(-sideMargin, -top);
        }

        /// <summary>Fixed-size rect at a point of the parent (anchor = pivot = <paramref name="anchor"/>).</summary>
        public static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
        }

        public static UnityEngine.UI.Image CreateImage(string name, Transform parent, Color color, bool raycast)
        {
            var rt = CreateRect(name, parent);
            var image = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        /// <summary>Body text in the typewriter font.</summary>
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

        /// <summary>Full-screen dark dim + centred paper panel with a heading rule. Returns the paper (content area).</summary>
        public static RectTransform CreateDimmedPanel(Transform canvasRoot, Vector2 size)
        {
            return MenuV2.CreateDimmedPanel(canvasRoot, size);
        }

        /// <summary>Buttons on paper: faint ink wash, olive when selected, stamp red when pressed.</summary>
        public static UnityEngine.UI.ColorBlock ButtonColors() => new()
        {
            normalColor = UiTheme.WithAlpha(UiTheme.Ink, 0.07f),
            highlightedColor = UiTheme.WithAlpha(UiTheme.Olive, 0.24f),
            pressedColor = UiTheme.WithAlpha(UiTheme.StampRed, 0.4f),
            selectedColor = UiTheme.WithAlpha(UiTheme.Olive, 0.4f),
            disabledColor = UiTheme.WithAlpha(UiTheme.Ink, 0.03f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f,
        };

        /// <summary>Buttons on black or dark overlays.</summary>
        public static UnityEngine.UI.ColorBlock DarkButtonColors() => new()
        {
            normalColor = UiTheme.WithAlpha(UiTheme.MutedWhite, 0.07f),
            highlightedColor = UiTheme.WithAlpha(UiTheme.LightOlive, 0.3f),
            pressedColor = UiTheme.WithAlpha(UiTheme.AlertRed, 0.45f),
            selectedColor = UiTheme.WithAlpha(UiTheme.LightOlive, 0.42f),
            disabledColor = UiTheme.WithAlpha(UiTheme.MutedWhite, 0.03f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f,
        };

        public static UnityEngine.UI.ColorBlock RowColors() => new()
        {
            normalColor = Color.clear,
            highlightedColor = UiTheme.WithAlpha(MenuV2.Bone, 0.06f),
            pressedColor = UiTheme.WithAlpha(MenuV2.Bone, 0.14f),
            selectedColor = UiTheme.WithAlpha(MenuV2.Bone, 0.10f),
            disabledColor = Color.clear,
            colorMultiplier = 1f,
            fadeDuration = 0.08f,
        };

        /// <summary>Upper-case typewriter button with a thin rule under it; <paramref name="dark"/> for black screens.</summary>
        public static UnityEngine.UI.Button CreateButton(Transform parent, string label, float height, int fontSize,
            UnityEngine.Events.UnityAction onClick, out UnityEngine.UI.Text text, bool dark = false)
        {
            // UI v2: every generic button is an outlined caps button (dark menus only now, so "dark" no longer matters).
            return MenuV2.CreateButton(parent, label ?? "", MenuV2.Kind.Outline, height, fontSize, onClick, out text);
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

            var text = MenuV2.Label("Label", row, label, 22, TextAnchor.MiddleLeft, MenuV2.Bone, isBody: true);
            Anchor(text.rectTransform, 0f, 0f, 0.47f, 1f);
            text.rectTransform.offsetMin = new Vector2(16f, 0f);

            MenuV2.HairlineBottom(row);

            control = CreateRect("Control", row);
            Anchor(control, 0.49f, 0f, 1f, 1f);
            control.offsetMax = new Vector2(-16f, 0f);
            return row;
        }

        /// <summary>Row with "‹ value ›" cycling through options.</summary>
        public static SettingsMenuStepper CreateStepper(Transform parent, string label, string[] options, int index, Action<int> onChanged)
        {
            var row = CreateRow(parent, label, out var background, out var control);

            var left = MenuV2.Chevron(control, false, MenuV2.Bone2);
            Place(left, new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(16f, 16f));
            var right = MenuV2.Chevron(control, true, MenuV2.Bone2);
            Place(right, new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(16f, 16f));
            var value = MenuV2.Label("Value", control, "", 26, TextAnchor.MiddleCenter, MenuV2.Bone);
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
            Anchor(sliderRt, 0.02f, 0.455f, 0.74f, 0.545f);

            var track = CreateImage("Track", sliderRt, TrackColor, true);
            Stretch(track.rectTransform);

            var fillArea = CreateRect("FillArea", sliderRt);
            Stretch(fillArea);
            var fill = CreateImage("Fill", fillArea, MenuV2.Bone, false);
            Stretch(fill.rectTransform);

            var handleArea = CreateRect("HandleArea", sliderRt);
            Stretch(handleArea, 4f, 4f);
            var handle = CreateImage("Handle", handleArea, MenuV2.Bone, true);
            // The slider drives the anchors (x = value, y = 0..1); sizeDelta.y makes the handle taller than the track.
            handle.rectTransform.sizeDelta = new Vector2(8f, 24f);

            var valueText = MenuV2.Label("Value", control, "", 26, TextAnchor.MiddleRight, MenuV2.Bone);
            Anchor(valueText.rectTransform, 0.76f, 0f, 1f, 1f);

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
