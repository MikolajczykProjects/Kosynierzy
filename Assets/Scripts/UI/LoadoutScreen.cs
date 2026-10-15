using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Kosynierzy
{
    public enum LoadoutMode { Start, Armoury }

    /// <summary>
    /// CS2-style weapon picker in the UI v2 look. Start of the game: PISTOLET (1 of 3) + BROŃ BIAŁA (1 of 2) columns and a locked
    /// KARABIN column; the radio-station armoury: one KARABIN column (1 of 3). Cards = silhouette + name + descriptor tags,
    /// hero panel on the right with four real numbers, loadout strip bottom-left, "ZATWIERDŹ" confirms.
    /// Mouse, keyboard (W/S card, A/D column, Tab, Enter) and gamepad (d-pad, A, shoulders). Freezes the game while open.
    /// </summary>
    public class LoadoutScreen : MonoBehaviour
    {
        static LoadoutScreen instance;
        public static bool IsOpen => instance != null;

        class Category { public string Title; public List<WeaponDef> Weapons; public WeaponDef Picked; }

        class CardView
        {
            public Image Background;
            public RectTransform Art;
            public GameObject Strip, Cursor;
            public Text Tags;
        }

        const float ColumnX = 64f, ColumnStep = 304f, CardW = 280f, CardH = 140f, CardGap = 16f, ColumnTop = 196f;
        static readonly Color CardFocus = new(0.137f, 0.157f, 0.18f, 0.92f);

        readonly List<Category> categories = new();
        readonly List<List<CardView>> cards = new();
        readonly List<Text> counts = new();
        int catIndex, cardIndex;
        bool armoury;
        Action onDone;
        PlayerControlLock controlLock;
        InputAction navigate, confirm, nextTab, prevTab;

        Text heroName, heroCaliber, heroDesc;
        RectTransform heroArt;
        readonly List<Text> heroValues = new();
        readonly List<Text> heroCaptions = new();
        readonly List<RectTransform> heroFills = new();
        readonly List<RectTransform> slotAreas = new();

        public static void Open(LoadoutMode mode, Action done)
        {
            if (instance != null) return;
            var go = new GameObject("[LoadoutScreen]");
            instance = go.AddComponent<LoadoutScreen>();
            instance.Build(mode, done);
        }

        void Build(LoadoutMode mode, Action done)
        {
            onDone = done;
            armoury = mode == LoadoutMode.Armoury;
            if (!armoury)
            {
                categories.Add(new Category { Title = "PISTOLET", Weapons = WeaponCatalog.Pistols, Picked = WeaponCatalog.SelectedPistol });
                categories.Add(new Category { Title = "BROŃ BIAŁA", Weapons = WeaponCatalog.Melee, Picked = WeaponCatalog.SelectedMelee });
            }
            else
            {
                var rifle = WeaponCatalog.SelectedRifle;
                categories.Add(new Category { Title = "KARABIN", Weapons = WeaponCatalog.Rifles, Picked = rifle != null ? rifle : WeaponCatalog.Rifles[0] });
            }

            controlLock = new PlayerControlLock();
            controlLock.Lock();
            Time.timeScale = 0f;
            PlayerControlLock.SetCursorFree(true);
            SettingsMenuUi.EnsureEventSystem();

            navigate = new InputAction("LoadoutNav", InputActionType.Value);
            navigate.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            navigate.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            navigate.AddBinding("<Gamepad>/dpad");
            confirm = new InputAction("LoadoutOk", InputActionType.Button, "<Keyboard>/enter");
            confirm.AddBinding("<Keyboard>/space");
            confirm.AddBinding("<Gamepad>/buttonSouth");
            nextTab = new InputAction("LoadoutNext", InputActionType.Button, "<Keyboard>/tab");
            nextTab.AddBinding("<Gamepad>/rightShoulder");
            prevTab = new InputAction("LoadoutPrev", InputActionType.Button, "<Keyboard>/q");
            prevTab.AddBinding("<Gamepad>/leftShoulder");
            navigate.Enable(); confirm.Enable(); nextTab.Enable(); prevTab.Enable();

            BuildUi();
            SelectCategory(0);
        }

        void OnDestroy()
        {
            navigate?.Dispose(); confirm?.Dispose(); nextTab?.Dispose(); prevTab?.Dispose();
            if (instance == this) instance = null;
            if (controlLock != null && controlLock.IsLocked) { controlLock.Unlock(); Time.timeScale = 1f; PlayerControlLock.SetCursorFree(false); }
        }

        // ------------------------------------------------------------------ layout (1920x1080 reference, top-left origin)

        static readonly Vector2 TopLeft = new(0f, 1f);

        static void PlaceTL(RectTransform rt, float x, float y, float w, float h) =>
            SettingsMenuUi.Place(rt, TopLeft, new Vector2(x, -y), new Vector2(w, h));

        void BuildUi()
        {
            var canvas = SettingsMenuUi.CreateCanvas("LoadoutCanvas", transform, 60);
            var root = canvas.transform;

            var dim = SettingsMenuUi.CreateImage("Dim", root, MenuV2.Wash, true);
            SettingsMenuUi.Stretch(dim.rectTransform);

            // Header: caps title, one short line, hairline.
            var title = MenuV2.Label("Title", root, armoury ? "ZBROJOWNIA" : "WYPOSAŻENIE", 32, TextAnchor.MiddleLeft, MenuV2.Bone);
            PlaceTL(title.rectTransform, 64f, 40f, 1000f, 44f);
            var sub = MenuV2.Label("Sub", root, armoury ? "Weź jeden karabin. Strzał słychać w całej okolicy." : "Pistolet i broń biała na start. Karabin odbierzesz w zbrojowni.",
                16, TextAnchor.MiddleLeft, MenuV2.Bone2, isBody: true);
            PlaceTL(sub.rectTransform, 64f, 90f, 1200f, 24f);
            MenuV2.HairlineTop(root, 128f, 64f);

            // Columns.
            for (int c = 0; c < categories.Count; c++) BuildColumn(root, c);
            if (!armoury) BuildLockedColumn(root, categories.Count);

            // Hero panel.
            BuildHero(root);

            // Loadout strip.
            var stripLabel = MenuV2.Label("StripLabel", root, "TWOJE WYPOSAŻENIE", 12, TextAnchor.MiddleLeft, MenuV2.Bone3);
            PlaceTL(stripLabel.rectTransform, 64f, 800f, 300f, 16f);
            for (int i = 0; i < 4; i++)
            {
                var slot = MenuV2.Box("Slot" + (i + 1), root, MenuV2.Card);
                PlaceTL(slot.rectTransform, 64f + i * 140f, 824f, 128f, 64f);
                var key = MenuV2.Label("Key", slot.transform, (i + 1).ToString(), 12, TextAnchor.UpperLeft, MenuV2.Bone3);
                SettingsMenuUi.Stretch(key.rectTransform, 8f, 0f, 6f, 0f);
                var art = SettingsMenuUi.CreateRect("Art", slot.transform);
                SettingsMenuUi.Stretch(art, 20f, 20f, 8f, 8f);
                slotAreas.Add(art);
            }

            // Confirm.
            var ok = MenuV2.CreateButton(root, "ZATWIERDŹ", MenuV2.Kind.Filled, 56f, 22, ConfirmAll, out _);
            SettingsMenuUi.Place((RectTransform)ok.transform, new Vector2(1f, 1f), new Vector2(-64f, -800f), new Vector2(300f, 56f));

            // Footer key caps.
            float x = 64f;
            x = KeyHint(root, x, new[] { "W", "S" }, "Wybór");
            x = KeyHint(root, x, new[] { "A", "D" }, "Kategoria");
            x = KeyHint(root, x, new[] { "TAB" }, "Dalej");
            KeyHint(root, x, new[] { "ENTER" }, "Zatwierdź");
        }

        /// <summary>Bone key caps (24 px high) followed by a label; returns the x for the next hint.</summary>
        static float KeyHint(Transform root, float x, string[] keys, string label)
        {
            foreach (var k in keys)
            {
                float w = k.Length > 1 ? 14f + k.Length * 9f : 24f;
                var cap = MenuV2.Box("KeyCap", root, MenuV2.Bone);
                PlaceTL(cap.rectTransform, x, 1004f, w, 24f);
                var t = MenuV2.Label("Key", cap.transform, k, 15, TextAnchor.MiddleCenter, MenuV2.Ink);
                SettingsMenuUi.Stretch(t.rectTransform);
                x += w + 4f;
            }
            x += 6f;
            var text = MenuV2.Label("Hint", root, label, 18, TextAnchor.MiddleLeft, MenuV2.Bone2, isBody: true);
            PlaceTL(text.rectTransform, x, 1003f, 200f, 26f);
            return x + label.Length * 9f + 40f;
        }

        void BuildColumn(Transform root, int c)
        {
            var cat = categories[c];
            float x = ColumnX + c * ColumnStep;
            ColumnHeader(root, x, cat.Title, out var count);
            counts.Add(count);

            var list = new List<CardView>();
            for (int i = 0; i < cat.Weapons.Count; i++)
            {
                int ci = c, idx = i;
                var def = cat.Weapons[i];
                var card = MenuV2.Box("Card_" + def.Id, root, MenuV2.Card);
                card.raycastTarget = true;
                PlaceTL(card.rectTransform, x, ColumnTop + i * (CardH + CardGap), CardW, CardH);
                var btn = card.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => { catIndex = ci; cardIndex = idx; Pick(); });
                var hover = card.gameObject.AddComponent<CardHover>();
                hover.OnEnter = () => { catIndex = ci; cardIndex = idx; Refresh(); };

                var cv = new CardView { Background = card };
                var art = SettingsMenuUi.CreateRect("Art", card.transform);
                PlaceTL(art, 20f, 14f, CardW - 40f, CardH - 62f);
                DrawSilhouette(art, def);
                cv.Art = art;
                var name = MenuV2.Label("Name", card.transform, def.Name.ToUpperInvariant(), 17, TextAnchor.MiddleLeft, MenuV2.Bone);
                PlaceTL(name.rectTransform, 16f, CardH - 42f, CardW - 32f, 20f);
                cv.Tags = MenuV2.Label("Tags", card.transform, Tags(def), 13, TextAnchor.MiddleLeft, MenuV2.Bone3);
                PlaceTL(cv.Tags.rectTransform, 16f, CardH - 22f, CardW - 32f, 16f);
                var key = MenuV2.Label("Key", card.transform, (i + 1).ToString(), 13, TextAnchor.UpperRight, MenuV2.Bone3);
                PlaceTL(key.rectTransform, CardW - 34f, 10f, 24f, 16f);

                var strip = MenuV2.Box("Strip", card.transform, MenuV2.Bone);
                strip.rectTransform.anchorMin = new Vector2(0f, 0f);
                strip.rectTransform.anchorMax = new Vector2(1f, 0f);
                strip.rectTransform.pivot = new Vector2(0.5f, 0f);
                strip.rectTransform.sizeDelta = new Vector2(0f, 3f);
                strip.rectTransform.anchoredPosition = Vector2.zero;
                cv.Strip = strip.gameObject;

                var cursor = SettingsMenuUi.CreateRect("Cursor", card.transform);
                SettingsMenuUi.Stretch(cursor);
                UiTheme.AddFrame(cursor, UiTheme.WithAlpha(MenuV2.Bone, 0.45f), 1f, 0f);
                cv.Cursor = cursor.gameObject;
                list.Add(cv);
            }
            cards.Add(list);
        }

        static void ColumnHeader(Transform root, float x, string label, out Text count)
        {
            var diamond = MenuV2.Box("Diamond", root, MenuV2.Bone2);
            SettingsMenuUi.Place(diamond.rectTransform, TopLeft, new Vector2(x + 4f, -172f), new Vector2(7f, 7f));
            diamond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var t = MenuV2.Label("ColumnTitle", root, label, 14, TextAnchor.MiddleLeft, MenuV2.Bone2);
            PlaceTL(t.rectTransform, x + 20f, 164f, CardW - 20f, 18f);
            count = MenuV2.Label("Count", root, "", 14, TextAnchor.MiddleRight, MenuV2.Bone3);
            PlaceTL(count.rectTransform, x + CardW - 120f, 164f, 120f, 18f);
        }

        void BuildLockedColumn(Transform root, int c)
        {
            float x = ColumnX + c * ColumnStep;
            ColumnHeader(root, x, "KARABIN", out var count);
            count.text = "";
            var box = SettingsMenuUi.CreateRect("LockedCard", root);
            PlaceTL(box, x, ColumnTop, CardW, CardH);
            UiTheme.AddFrame(box, MenuV2.Hair, 1f, 0f);
            var a = MenuV2.Label("Locked", box, "ZBROJOWNIA", 15, TextAnchor.LowerCenter, MenuV2.Bone3);
            SettingsMenuUi.Anchor(a.rectTransform, 0f, 0.5f, 1f, 0.5f);
            a.rectTransform.sizeDelta = new Vector2(0f, 24f);
            a.rectTransform.pivot = new Vector2(0.5f, 0f);
            a.rectTransform.anchoredPosition = new Vector2(0f, 2f);
            var b = MenuV2.Label("LockedNote", box, "Odblokujesz w akcie 3", 15, TextAnchor.UpperCenter, MenuV2.Bone3, isBody: true);
            SettingsMenuUi.Anchor(b.rectTransform, 0f, 0.5f, 1f, 0.5f);
            b.rectTransform.sizeDelta = new Vector2(0f, 24f);
            b.rectTransform.pivot = new Vector2(0.5f, 1f);
            b.rectTransform.anchoredPosition = new Vector2(0f, -4f);
        }

        void BuildHero(Transform root)
        {
            const float w = 816f, h = 600f;
            var panel = MenuV2.Box("Hero", root, MenuV2.Panel);
            SettingsMenuUi.Place(panel.rectTransform, new Vector2(1f, 1f), new Vector2(-64f, -164f), new Vector2(w, h));
            UiTheme.AddFrame(panel.rectTransform, UiTheme.WithAlpha(MenuV2.Bone, 0.14f), 1f, 0f);
            var p = panel.transform;

            heroName = MenuV2.Label("Name", p, "", 56, TextAnchor.MiddleLeft, MenuV2.Bone);
            PlaceTL(heroName.rectTransform, 40f, 30f, w - 80f, 64f);
            heroCaliber = MenuV2.Label("Caliber", p, "", 18, TextAnchor.MiddleLeft, MenuV2.Bone2);
            PlaceTL(heroCaliber.rectTransform, 40f, 98f, w - 80f, 24f);
            heroArt = SettingsMenuUi.CreateRect("Art", p);
            PlaceTL(heroArt, 40f, 150f, w - 80f, 190f);
            MenuV2.HairlineTop(p, 366f, 40f);

            float cw = (w - 80f) / 4f;
            for (int i = 0; i < 4; i++)
            {
                var v = MenuV2.Label("Value" + i, p, "", 44, TextAnchor.MiddleLeft, MenuV2.Bone);
                PlaceTL(v.rectTransform, 40f + i * cw, 380f, cw, 50f);
                var cap = MenuV2.Label("Caption" + i, p, "", 13, TextAnchor.MiddleLeft, MenuV2.Bone3);
                PlaceTL(cap.rectTransform, 40f + i * cw, 432f, cw, 16f);
                var track = MenuV2.Box("Track" + i, p, MenuV2.Hair);
                PlaceTL(track.rectTransform, 40f + i * cw, 458f, cw - 24f, 2f);
                var fill = MenuV2.Box("Fill", track.transform, UiTheme.WithAlpha(MenuV2.Bone, 0.85f));
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
                heroValues.Add(v);
                heroCaptions.Add(cap);
                heroFills.Add(fill.rectTransform);
            }
            heroDesc = MenuV2.Label("Desc", p, "", 20, TextAnchor.UpperLeft, MenuV2.Bone, isBody: true);
            PlaceTL(heroDesc.rectTransform, 40f, 498f, w - 80f, 90f);
        }

        // ------------------------------------------------------------------ data helpers

        /// <summary>Two-word descriptor from the 0..1 card stats (e.g. "CELNY, GŁOŚNY").</summary>
        static string Tags(WeaponDef d)
        {
            var tags = new List<string>();
            if (d.StatSilence >= 0.7f) tags.Add("CICHY");
            else if (d.StatSilence <= 0.35f) tags.Add("GŁOŚNY");
            if (d.StatAccuracy >= 0.8f) tags.Add("CELNY");
            if (d.StatDamage >= 0.75f) tags.Add("MOCNY");
            if (d.StatHandling >= 0.85f) tags.Add("SZYBKI");
            if (d.IsMelee && d.MeleeRange >= 1.9f) tags.Add("DŁUŻSZY ZASIĘG");
            if (tags.Count == 0) tags.Add(d.IsMelee ? "CICHY" : "UNIWERSALNY");
            return string.Join(", ", tags.GetRange(0, Mathf.Min(2, tags.Count)));
        }

        static string Num(float v, string format) => v.ToString(format, CultureInfo.InvariantCulture).Replace('.', ',');

        // ------------------------------------------------------------------ silhouettes

        /// <summary>
        /// Solid right-facing silhouette from blocks, fitted (aspect-correct) into <paramref name="area"/>; readable at a glance like CS icons.
        /// <paramref name="tint"/> defaults to bone at 92 %.
        /// </summary>
        public static void DrawSilhouette(RectTransform area, WeaponDef def, Color? tint = null)
        {
            Color c = tint ?? UiTheme.WithAlpha(MenuV2.Bone, 0.92f);
            float aspect;
            switch (def.Slot)
            {
                case WeaponSlot.Pistol: aspect = def.Id == "nagant" ? 1.9f : 1.7f; break;
                case WeaponSlot.Rifle: aspect = def.Id == "erma" ? 3.2f : 4.4f; break;
                default: aspect = def.Id == "noz" ? 3.6f : 4.6f; break;
            }
            var fit = SettingsMenuUi.CreateRect("Fit", area);
            SettingsMenuUi.Anchor(fit, 0f, 0f, 1f, 1f);
            var fitter = fit.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = aspect;

            void Block(float x0, float y0, float x1, float y1, float rot = 0f)
            {
                var img = SettingsMenuUi.CreateImage("b", fit, c, false);
                SettingsMenuUi.Anchor(img.rectTransform, x0, y0, x1, y1);
                img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rot);
            }
            switch (def.Slot)
            {
                case WeaponSlot.Pistol when def.Id == "nagant":
                    Block(0.34f, 0.62f, 0.99f, 0.74f);   // barrel
                    Block(0.18f, 0.50f, 0.52f, 0.86f);   // frame + drum
                    Block(0.08f, 0.06f, 0.30f, 0.62f, 16f); // grip
                    Block(0.38f, 0.28f, 0.52f, 0.52f);   // trigger guard
                    Block(0.12f, 0.80f, 0.22f, 0.92f);   // hammer
                    break;
                case WeaponSlot.Pistol:
                    float s = def.Id == "browning" ? 0.88f : 1f;
                    Block(0.04f, 0.58f, 0.04f + 0.92f * s, 0.90f);  // slide
                    Block(0.16f, 0.42f, 0.16f + 0.62f * s, 0.62f);  // frame
                    Block(0.12f, 0.04f, 0.34f, 0.62f, 12f);         // grip
                    Block(0.40f, 0.28f, 0.58f, 0.46f);              // trigger guard
                    Block(0.04f + 0.80f * s, 0.88f, 0.04f + 0.86f * s, 0.98f); // front sight
                    break;
                case WeaponSlot.Rifle when def.Id == "erma":
                    Block(0.00f, 0.40f, 0.12f, 0.64f, 6f);   // stock
                    Block(0.10f, 0.44f, 0.50f, 0.68f);       // receiver
                    Block(0.48f, 0.50f, 0.92f, 0.64f);       // barrel jacket
                    Block(0.90f, 0.54f, 1.00f, 0.60f);       // muzzle
                    Block(0.42f, 0.10f, 0.52f, 0.46f);       // magazine
                    Block(0.24f, 0.14f, 0.32f, 0.46f, 10f);  // grip
                    break;
                case WeaponSlot.Rifle:
                    Block(0.00f, 0.26f, 0.27f, 0.58f, 7f);   // stock
                    Block(0.24f, 0.42f, 0.54f, 0.64f);       // receiver
                    Block(0.44f, 0.46f, 0.76f, 0.60f);       // handguard
                    Block(0.50f, 0.53f, 0.99f, 0.59f);       // barrel
                    Block(0.33f, 0.20f, 0.40f, 0.44f);       // trigger guard / magazine
                    Block(0.93f, 0.58f, 0.96f, 0.70f);       // front sight
                    Block(0.34f, 0.64f, 0.38f, 0.72f);       // rear sight
                    break;
                default:
                    float len = def.Id == "noz" ? 0.8f : 1f;
                    Block(0.02f, 0.36f, 0.28f, 0.64f);                       // handle
                    Block(0.28f, 0.20f, 0.33f, 0.80f);                       // guard
                    Block(0.33f, 0.40f, 0.33f + 0.38f * len, 0.64f);         // blade
                    Block(0.33f + 0.38f * len, 0.44f, 0.33f + 0.54f * len, 0.62f);
                    Block(0.33f + 0.54f * len, 0.50f, 0.33f + 0.64f * len, 0.58f);
                    break;
            }
        }

        /// <summary>Recolours a silhouette (all its blocks) without rebuilding it.</summary>
        static void Tint(RectTransform area, Color c)
        {
            foreach (var img in area.GetComponentsInChildren<Image>(true)) img.color = c;
        }

        // ------------------------------------------------------------------ state

        void SelectCategory(int idx)
        {
            catIndex = Mathf.Clamp(idx, 0, categories.Count - 1);
            var cat = categories[catIndex];
            cardIndex = Mathf.Max(0, cat.Weapons.IndexOf(cat.Picked));
            Refresh();
        }

        void Refresh()
        {
            for (int c = 0; c < categories.Count; c++)
            {
                var cat = categories[c];
                for (int i = 0; i < cards[c].Count; i++)
                {
                    var cv = cards[c][i];
                    bool picked = cat.Weapons[i] == cat.Picked;
                    bool focus = c == catIndex && i == cardIndex;
                    cv.Background.color = picked ? MenuV2.CardSelected : focus ? CardFocus : MenuV2.Card;
                    cv.Strip.SetActive(picked);
                    cv.Cursor.SetActive(focus);
                    Tint(cv.Art, picked || focus ? UiTheme.WithAlpha(MenuV2.Bone, 0.96f) : UiTheme.WithAlpha(MenuV2.Bone, 0.6f));
                    cv.Tags.color = picked ? MenuV2.Bone2 : MenuV2.Bone3;
                }
                counts[c].text = $"{Mathf.Max(0, cat.Weapons.IndexOf(cat.Picked)) + 1} / {cat.Weapons.Count}";
            }

            var focusCat = categories[catIndex];
            var def = focusCat.Weapons[Mathf.Clamp(cardIndex, 0, focusCat.Weapons.Count - 1)];
            heroName.text = def.Name.ToUpperInvariant();
            heroCaliber.text = def.Caliber.ToUpperInvariant();
            heroDesc.text = def.Description;
            for (int i = heroArt.childCount - 1; i >= 0; i--) Destroy(heroArt.GetChild(i).gameObject);
            DrawSilhouette(heroArt, def);

            string[] values, captions;
            float[] rel;
            if (def.IsMelee)
            {
                values = new[] { Num(def.MeleeRange, "0.0") + " m", Num(def.TakedownTime, "0.00") + " s", "0 m", "" };
                captions = new[] { "ZASIĘG", "ELIMINACJA", "SŁYCHAĆ", "" };
                rel = new[] { Mathf.Clamp01(def.MeleeRange / 3f), Mathf.Clamp01(1f - def.TakedownTime / 1.5f), 0f, 0f };
            }
            else
            {
                values = new[] { Num(def.Damage, "0"), Num(def.FireRate, "0"), def.Magazine.ToString(CultureInfo.InvariantCulture), Num(def.NoiseRadius, "0") + " m" };
                captions = new[] { "OBRAŻENIA", "STRZ./MIN", "NABOJÓW", "SŁYCHAĆ" };
                rel = new[] { Mathf.Clamp01(def.Damage / 70f), Mathf.Clamp01(def.FireRate / 900f), Mathf.Clamp01(def.Magazine / 30f), Mathf.Clamp01(def.NoiseRadius / 80f) };
            }
            for (int i = 0; i < 4; i++)
            {
                heroValues[i].text = values[i];
                heroCaptions[i].text = captions[i];
                heroFills[i].anchorMax = new Vector2(rel[i], 1f);
                heroFills[i].parent.gameObject.SetActive(captions[i].Length > 0);
            }

            RefreshStrip();
        }

        /// <summary>Slots 1-4: pistol, rifle, melee, camera (as picked so far; empty slot = a dash).</summary>
        void RefreshStrip()
        {
            WeaponDef pistol = null, rifle = null, melee = null;
            foreach (var cat in categories)
            {
                if (cat.Picked == null) continue;
                if (cat.Picked.Slot == WeaponSlot.Pistol) pistol = cat.Picked;
                else if (cat.Picked.Slot == WeaponSlot.Rifle) rifle = cat.Picked;
                else melee = cat.Picked;
            }
            if (rifle == null) rifle = WeaponCatalog.SelectedRifle;
            var defs = new[] { pistol, rifle, melee };
            for (int i = 0; i < slotAreas.Count; i++)
            {
                var area = slotAreas[i];
                for (int k = area.childCount - 1; k >= 0; k--) Destroy(area.GetChild(k).gameObject);
                if (i < 3 && defs[i] != null) DrawSilhouette(area, defs[i]);
                else if (i == 3) DrawCamera(area);
                else
                {
                    var dash = MenuV2.Box("Dash", area, MenuV2.Bone3);
                    SettingsMenuUi.Place(dash.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16f, 2f));
                }
            }
        }

        static void DrawCamera(RectTransform area)
        {
            var body = MenuV2.Box("Body", area, UiTheme.WithAlpha(MenuV2.Bone, 0.92f));
            SettingsMenuUi.Place(body.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(54f, 32f));
            var top = MenuV2.Box("Top", area, UiTheme.WithAlpha(MenuV2.Bone, 0.92f));
            SettingsMenuUi.Place(top.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-8f, 19f), new Vector2(18f, 7f));
            var ring = MenuV2.Box("Ring", area, MenuV2.Ink);
            ring.sprite = UiTheme.CircleSprite;
            SettingsMenuUi.Place(ring.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 22f));
            var lens = MenuV2.Box("Lens", area, UiTheme.WithAlpha(MenuV2.Bone, 0.92f));
            lens.sprite = UiTheme.CircleSprite;
            SettingsMenuUi.Place(lens.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12f, 12f));
        }

        void Pick()
        {
            categories[catIndex].Picked = categories[catIndex].Weapons[cardIndex];
            SfxBank.Play(SfxBank.Sfx.ReloadEnd);
            Refresh();
        }

        void ConfirmAll()
        {
            foreach (var cat in categories) if (cat.Picked != null) WeaponCatalog.Choose(cat.Picked);
            WeaponCatalog.LoadoutChosen = true;
            SfxBank.Play(SfxBank.Sfx.QuestStep);
            Time.timeScale = 1f;
            controlLock.Unlock();
            PlayerControlLock.SetCursorFree(false);
            var done = onDone;
            Destroy(gameObject);
            done?.Invoke();
        }

        void Update()
        {
            var cat = categories[catIndex];
            if (navigate.WasPressedThisFrame())
            {
                var v = navigate.ReadValue<Vector2>();
                if (v.y < -0.5f) cardIndex = Mathf.Min(cat.Weapons.Count - 1, cardIndex + 1);
                else if (v.y > 0.5f) cardIndex = Mathf.Max(0, cardIndex - 1);
                else if (v.x > 0.5f && catIndex < categories.Count - 1) { SelectCategory(catIndex + 1); return; }
                else if (v.x < -0.5f && catIndex > 0) { SelectCategory(catIndex - 1); return; }
                Refresh();
            }
            var kb = Keyboard.current;
            if (kb != null)
                for (int i = 0; i < cat.Weapons.Count && i < 9; i++)
                    if (kb[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame) { cardIndex = i; Pick(); }
            if (nextTab.WasPressedThisFrame()) SelectCategory((catIndex + 1) % categories.Count);
            if (prevTab.WasPressedThisFrame()) SelectCategory((catIndex - 1 + categories.Count) % categories.Count);
            if (confirm.WasPressedThisFrame())
            {
                // Enter on a card picks it; the second Enter (already picked) confirms the whole loadout.
                if (cat.Picked != cat.Weapons[cardIndex]) Pick();
                else if (catIndex < categories.Count - 1) SelectCategory(catIndex + 1);
                else ConfirmAll();
            }
            Time.timeScale = 0f; // stay frozen even if something tries to resume
        }

        /// <summary>Pointer-enter hook for cards (uGUI events need the EventSystem from SettingsMenuUi).</summary>
        class CardHover : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler
        {
            public Action OnEnter;
            public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData) => OnEnter?.Invoke();
        }
    }
}
