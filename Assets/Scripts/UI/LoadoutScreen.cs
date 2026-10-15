using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Kosynierzy
{
    public enum LoadoutMode { Start, Armoury }

    /// <summary>
    /// CS2-style weapon picker. Start of the game: PISTOLET (1 of 3) + BROŃ BIAŁA (1 of 2);
    /// the radio-station armoury: KARABIN (1 of 3). Category tabs on the left, a grid of cards with a
    /// silhouette and stat bars, the detail panel on the right, "ZATWIERDŹ" confirms.
    /// Mouse, keyboard (arrows/WASD, Tab, Enter) and gamepad (d-pad, A, shoulders). Freezes the game while open.
    /// </summary>
    public class LoadoutScreen : MonoBehaviour
    {
        static LoadoutScreen instance;
        public static bool IsOpen => instance != null;

        class Category { public string Title; public List<WeaponDef> Weapons; public WeaponDef Picked; }

        readonly List<Category> categories = new();
        int catIndex, cardIndex;
        Action onDone;
        PlayerControlLock controlLock;
        InputAction navigate, confirm, nextTab, prevTab;

        RectTransform grid, tabsRoot;
        Text detailName, detailCaliber, detailDesc, footerHint;
        readonly List<(RectTransform bar, string label)> detailBars = new();
        readonly List<Image> cardFrames = new();
        readonly List<Image> tabBacks = new();

        static readonly Color Panel = new(0.09f, 0.09f, 0.085f, 0.94f);
        static readonly Color Card = new(0.15f, 0.145f, 0.13f, 1f);
        static readonly Color CardHoverColor = new(0.22f, 0.2f, 0.16f, 1f);
        static readonly Color Accent = UiTheme.Brass;

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
            if (mode == LoadoutMode.Start)
            {
                categories.Add(new Category { Title = "PISTOLET", Weapons = WeaponCatalog.Pistols, Picked = WeaponCatalog.SelectedPistol });
                categories.Add(new Category { Title = "BROŃ BIAŁA", Weapons = WeaponCatalog.Melee, Picked = WeaponCatalog.SelectedMelee });
            }
            else categories.Add(new Category { Title = "KARABIN", Weapons = WeaponCatalog.Rifles, Picked = WeaponCatalog.SelectedRifle ?? WeaponCatalog.Rifles[0] });

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

            BuildUi(mode);
            SelectCategory(0);
        }

        void OnDestroy()
        {
            navigate?.Dispose(); confirm?.Dispose(); nextTab?.Dispose(); prevTab?.Dispose();
            if (instance == this) instance = null;
        }

        // ------------------------------------------------------------------ layout

        void BuildUi(LoadoutMode mode)
        {
            var canvas = SettingsMenuUi.CreateCanvas("LoadoutCanvas", transform, 60);
            var root = canvas.transform;

            var dim = SettingsMenuUi.CreateImage("Dim", root, new Color(0.03f, 0.03f, 0.025f, 0.92f), true);
            SettingsMenuUi.Stretch(dim.rectTransform);

            // Header strip.
            var header = SettingsMenuUi.CreateRect("Header", root);
            SettingsMenuUi.Anchor(header, 0f, 1f, 1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.sizeDelta = new Vector2(0f, 120f);
            header.anchoredPosition = Vector2.zero;
            var title = UiTheme.CreateHeading("Title", header, mode == LoadoutMode.Start ? "WYPOSAŻENIE AGENTA" : "ZBROJOWNIA RADIOSTACJI", 44, UiTheme.MutedWhite);
            SettingsMenuUi.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(80f, -34f), new Vector2(1000f, 56f));
            title.alignment = TextAnchor.MiddleLeft;
            var sub = SettingsMenuUi.CreateText("Sub", header, mode == LoadoutMode.Start
                ? "Ekspozytura nr 4 · Katowice · 31.08.1939 — wybierz broń na misję „Babcia umarła”"
                : "Weź jeden karabin. Huk karabinu słychać w całej okolicy — strzelaj tylko, gdy musisz.", 20, TextAnchor.MiddleLeft, FontStyle.Normal, UiTheme.WithAlpha(UiTheme.MutedWhite, 0.65f));
            SettingsMenuUi.Place(sub.rectTransform, new Vector2(0f, 1f), new Vector2(80f, -84f), new Vector2(1400f, 30f));
            var stamp = UiTheme.CreateStamp(header, "TAJNE", UiTheme.StampRed, 26);
            SettingsMenuUi.Place(stamp, new Vector2(1f, 1f), new Vector2(-180f, -60f), new Vector2(170f, 54f));
            var rule = UiTheme.CreateRule("Rule", root, UiTheme.WithAlpha(Accent, 0.6f), 2f);
            SettingsMenuUi.Anchor(rule.rectTransform, 0f, 1f, 1f, 1f);
            rule.rectTransform.sizeDelta = new Vector2(-160f, 2f);
            rule.rectTransform.anchoredPosition = new Vector2(0f, -122f);

            // Left: category tabs.
            tabsRoot = SettingsMenuUi.CreateRect("Tabs", root);
            SettingsMenuUi.Place(tabsRoot, new Vector2(0f, 1f), new Vector2(80f + 150f, -150f - 120f), new Vector2(300f, 240f));
            for (int i = 0; i < categories.Count; i++)
            {
                int idx = i;
                var tab = SettingsMenuUi.CreateImage("Tab_" + i, tabsRoot, Card, true);
                SettingsMenuUi.Place(tab.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -i * 76f - 34f), new Vector2(300f, 64f));
                var btn = tab.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => SelectCategory(idx));
                var label = SettingsMenuUi.CreateText("Label", tab.transform, $"{i + 1}   {categories[i].Title}", 24, TextAnchor.MiddleLeft, FontStyle.Bold, UiTheme.MutedWhite);
                SettingsMenuUi.Stretch(label.rectTransform, 24f, 10f, 0f, 0f);
                tabBacks.Add(tab);
            }

            // Centre: card grid.
            grid = SettingsMenuUi.CreateRect("Grid", root);
            SettingsMenuUi.Anchor(grid, 0f, 0f, 1f, 1f);
            grid.offsetMin = new Vector2(420f, 190f);
            grid.offsetMax = new Vector2(-620f, -160f);

            // Right: detail panel.
            var detail = SettingsMenuUi.CreateImage("Detail", root, Panel, false);
            SettingsMenuUi.Anchor(detail.rectTransform, 1f, 0f, 1f, 1f);
            detail.rectTransform.pivot = new Vector2(1f, 0.5f);
            detail.rectTransform.offsetMin = new Vector2(-560f, 190f);
            detail.rectTransform.offsetMax = new Vector2(-80f, -160f);
            UiTheme.AddFrame(detail.rectTransform, UiTheme.WithAlpha(Accent, 0.5f), 2f, 0f);
            detailName = UiTheme.CreateHeading("Name", detail.transform, "", 34, UiTheme.MutedWhite);
            SettingsMenuUi.PinTop(detailName.rectTransform, 30f, 44f, 34f);
            detailName.alignment = TextAnchor.MiddleLeft;
            detailCaliber = SettingsMenuUi.CreateText("Caliber", detail.transform, "", 20, TextAnchor.MiddleLeft, FontStyle.Normal, Accent);
            SettingsMenuUi.PinTop(detailCaliber.rectTransform, 80f, 28f, 34f);
            detailDesc = SettingsMenuUi.CreateText("Desc", detail.transform, "", 20, TextAnchor.UpperLeft, FontStyle.Normal, UiTheme.WithAlpha(UiTheme.MutedWhite, 0.8f));
            SettingsMenuUi.PinTop(detailDesc.rectTransform, 124f, 120f, 34f);
            string[] labels = { "SIŁA", "CELNOŚĆ", "CISZA", "OBSŁUGA" };
            for (int i = 0; i < labels.Length; i++)
            {
                var lbl = SettingsMenuUi.CreateText("StatLabel" + i, detail.transform, labels[i], 18, TextAnchor.MiddleLeft, FontStyle.Bold, UiTheme.WithAlpha(UiTheme.MutedWhite, 0.7f));
                SettingsMenuUi.PinTop(lbl.rectTransform, 270f + i * 54f, 24f, 34f);
                var track = SettingsMenuUi.CreateImage("Track" + i, detail.transform, UiTheme.WithAlpha(UiTheme.MutedWhite, 0.12f), false);
                SettingsMenuUi.PinTop(track.rectTransform, 298f + i * 54f, 10f, 34f);
                var fill = SettingsMenuUi.CreateImage("Fill", track.transform, Accent, false);
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
                detailBars.Add((fill.rectTransform, labels[i]));
            }

            // Footer: confirm.
            var ok = SettingsMenuUi.CreateImage("Confirm", root, Accent, true);
            SettingsMenuUi.Place(ok.rectTransform, new Vector2(1f, 0f), new Vector2(-80f - 240f, 100f), new Vector2(480f, 76f));
            var okBtn = ok.gameObject.AddComponent<Button>();
            okBtn.transition = Selectable.Transition.ColorTint;
            okBtn.onClick.AddListener(ConfirmAll);
            var okText = UiTheme.CreateHeading("Label", ok.transform, "ZATWIERDŹ  ▸", 30, UiTheme.Ink);
            SettingsMenuUi.Stretch(okText.rectTransform);
            okText.alignment = TextAnchor.MiddleCenter;
            footerHint = SettingsMenuUi.CreateText("Hint", root, "←→ wybór   ·   TAB kategoria   ·   ENTER zatwierdź", 18, TextAnchor.MiddleLeft, FontStyle.Normal, UiTheme.WithAlpha(UiTheme.MutedWhite, 0.5f));
            SettingsMenuUi.Place(footerHint.rectTransform, new Vector2(0f, 0f), new Vector2(80f + 400f, 100f), new Vector2(800f, 30f));
        }

        void SelectCategory(int idx)
        {
            catIndex = Mathf.Clamp(idx, 0, categories.Count - 1);
            for (int i = 0; i < tabBacks.Count; i++)
                tabBacks[i].color = i == catIndex ? UiTheme.WithAlpha(Accent, 0.9f) : Card;
            for (int i = 0; i < tabBacks.Count; i++)
                tabBacks[i].GetComponentInChildren<Text>().color = i == catIndex ? UiTheme.Ink : UiTheme.MutedWhite;
            var cat = categories[catIndex];
            cardIndex = Mathf.Max(0, cat.Weapons.IndexOf(cat.Picked));
            RebuildCards();
        }

        void RebuildCards()
        {
            for (int i = grid.childCount - 1; i >= 0; i--) Destroy(grid.GetChild(i).gameObject);
            cardFrames.Clear();
            var cat = categories[catIndex];
            int n = cat.Weapons.Count;
            float gap = 24f;
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                var def = cat.Weapons[i];
                var card = SettingsMenuUi.CreateImage("Card_" + def.Id, grid, Card, true);
                var rt = card.rectTransform;
                rt.anchorMin = new Vector2((float)i / n, 0f);
                rt.anchorMax = new Vector2((float)(i + 1) / n, 1f);
                rt.offsetMin = new Vector2(gap * 0.5f, 0f);
                rt.offsetMax = new Vector2(-gap * 0.5f, 0f);
                var btn = card.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => { cardIndex = idx; Pick(); });
                var hover = card.gameObject.AddComponent<CardHover>();
                hover.OnEnter = () => { cardIndex = idx; Refresh(); };

                var number = SettingsMenuUi.CreateText("Key", card.transform, (i + 1).ToString(), 22, TextAnchor.UpperLeft, FontStyle.Bold, UiTheme.WithAlpha(UiTheme.MutedWhite, 0.45f));
                SettingsMenuUi.PinTop(number.rectTransform, 14f, 28f, 18f);
                var silhouette = SettingsMenuUi.CreateRect("Silhouette", card.transform);
                SettingsMenuUi.Anchor(silhouette, 0.08f, 0.42f, 0.92f, 0.85f);
                silhouette.offsetMin = silhouette.offsetMax = Vector2.zero;
                DrawSilhouette(silhouette, def);
                var name = UiTheme.CreateHeading("Name", card.transform, def.Name.ToUpperInvariant(), 24, UiTheme.MutedWhite);
                SettingsMenuUi.Anchor(name.rectTransform, 0f, 0f, 1f, 0f);
                name.rectTransform.pivot = new Vector2(0.5f, 0f);
                name.rectTransform.sizeDelta = new Vector2(-36f, 34f);
                name.rectTransform.anchoredPosition = new Vector2(0f, 70f);
                name.alignment = TextAnchor.MiddleLeft;
                var cal = SettingsMenuUi.CreateText("Cal", card.transform, def.IsMelee ? def.Caliber : $"{def.Caliber} · {def.Magazine} naboi", 17, TextAnchor.MiddleLeft, FontStyle.Normal, UiTheme.WithAlpha(UiTheme.MutedWhite, 0.6f));
                SettingsMenuUi.Anchor(cal.rectTransform, 0f, 0f, 1f, 0f);
                cal.rectTransform.pivot = new Vector2(0.5f, 0f);
                cal.rectTransform.sizeDelta = new Vector2(-36f, 26f);
                cal.rectTransform.anchoredPosition = new Vector2(0f, 40f);

                var frame = SettingsMenuUi.CreateImage("Frame", card.transform, Color.clear, false);
                SettingsMenuUi.Stretch(frame.rectTransform);
                UiTheme.AddFrame(frame.rectTransform, Accent, 3f, 0f);
                cardFrames.Add(frame);
            }
            Refresh();
        }

        // Silhouette from blocks (pistol / revolver / rifle / smg / blade) — readable at a glance like CS icons.
        public static void DrawSilhouette(RectTransform area, WeaponDef def)
        {
            Color c = UiTheme.WithAlpha(UiTheme.MutedWhite, 0.88f);
            void Block(float x0, float y0, float x1, float y1, float rot = 0f)
            {
                var img = SettingsMenuUi.CreateImage("b", area, c, false);
                SettingsMenuUi.Anchor(img.rectTransform, x0, y0, x1, y1);
                img.rectTransform.offsetMin = img.rectTransform.offsetMax = Vector2.zero;
                img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rot);
            }
            switch (def.Slot)
            {
                case WeaponSlot.Pistol when def.Id == "nagant":
                    Block(0.30f, 0.62f, 0.85f, 0.72f); Block(0.30f, 0.50f, 0.50f, 0.78f); Block(0.18f, 0.08f, 0.32f, 0.62f, 18f); Block(0.24f, 0.70f, 0.30f, 0.82f);
                    break;
                case WeaponSlot.Pistol:
                    float s = def.Id == "browning" ? 0.85f : 1f;
                    Block(0.2f, 0.6f, 0.2f + 0.62f * s, 0.8f); Block(0.24f, 0.08f, 0.24f + 0.16f * s, 0.62f, 14f); Block(0.42f, 0.42f, 0.5f, 0.6f);
                    break;
                case WeaponSlot.Rifle when def.Id == "erma":
                    Block(0.05f, 0.42f, 0.32f, 0.58f); Block(0.30f, 0.5f, 0.80f, 0.64f); Block(0.78f, 0.55f, 0.98f, 0.6f); Block(0.42f, 0.64f, 0.56f, 0.94f); Block(0.30f, 0.22f, 0.38f, 0.5f, 10f);
                    break;
                case WeaponSlot.Rifle:
                    Block(0.02f, 0.32f, 0.30f, 0.56f, 6f); Block(0.28f, 0.46f, 0.66f, 0.58f); Block(0.60f, 0.52f, 0.99f, 0.57f); Block(0.42f, 0.58f, 0.46f, 0.70f); Block(0.32f, 0.30f, 0.38f, 0.46f);
                    break;
                default:
                    float len = def.Id == "noz" ? 0.55f : 0.78f;
                    Block(0.08f, 0.44f, 0.32f, 0.56f); Block(0.32f, 0.36f, 0.35f, 0.64f); Block(0.35f, 0.47f, 0.35f + len * 0.62f, 0.53f);
                    break;
            }
        }

        void Refresh()
        {
            var cat = categories[catIndex];
            for (int i = 0; i < cardFrames.Count; i++)
            {
                bool picked = cat.Weapons[i] == cat.Picked;
                bool hover = i == cardIndex;
                cardFrames[i].gameObject.SetActive(picked || hover);
                foreach (var img in cardFrames[i].GetComponentsInChildren<Image>())
                    if (img != cardFrames[i]) img.color = picked ? Accent : UiTheme.WithAlpha(UiTheme.MutedWhite, 0.35f);
                cardFrames[i].transform.parent.GetComponent<Image>().color = hover ? CardHoverColor : Card;
            }
            var def = cat.Weapons[Mathf.Clamp(cardIndex, 0, cat.Weapons.Count - 1)];
            detailName.text = def.Name.ToUpperInvariant();
            detailCaliber.text = def.IsMelee ? def.Caliber : $"{def.Caliber}  ·  magazynek {def.Magazine}  ·  słychać z {def.NoiseRadius:0} m";
            detailDesc.text = def.Description;
            float[] stats = { def.StatDamage, def.StatAccuracy, def.StatSilence, def.StatHandling };
            for (int i = 0; i < detailBars.Count; i++)
                detailBars[i].bar.anchorMax = new Vector2(Mathf.Clamp01(stats[i]), 1f);
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
                if (v.x > 0.5f) cardIndex = Mathf.Min(cat.Weapons.Count - 1, cardIndex + 1);
                else if (v.x < -0.5f) cardIndex = Mathf.Max(0, cardIndex - 1);
                else if (v.y < -0.5f && catIndex < categories.Count - 1) { SelectCategory(catIndex + 1); return; }
                else if (v.y > 0.5f && catIndex > 0) { SelectCategory(catIndex - 1); return; }
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
