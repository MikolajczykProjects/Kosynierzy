using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Bottom of the screen: magazine as cartridge icons + reserve and reload bar (pistol only, bottom-right),
    /// stamina strip (only when not full) and the hotbar (1 Vis wz.35 · 2 Aparat · 3 Bagnet, equipped one
    /// highlighted) at the bottom centre, and the "OTRZYMANO" card that pops up and flies into its hotbar slot.
    /// </summary>
    public sealed class HudEquipment : HudWidget
    {
        static readonly string[] DefaultSlotNames = { "Vis wz.35", "Aparat", "Bagnet" };
        static readonly Color Copper = new(0.62f, 0.38f, 0.2f, 1f);

        // Ammo
        RectTransform ammoPanel, cartridgeRow;
        readonly List<UnityEngine.UI.Image> casings = new();
        readonly List<UnityEngine.UI.Image> bullets = new();
        UnityEngine.UI.Text reserveText, ammoHint;
        RectTransform reloadTrack;
        UnityEngine.UI.Image reloadFill;
        CanvasGroup ammoGroup;
        int builtMagazine = -1;

        // Stamina
        RectTransform staminaTrack;
        UnityEngine.UI.Image staminaFill;
        CanvasGroup staminaGroup;

        // Hotbar
        sealed class Slot
        {
            public RectTransform Root, Icon;
            public UnityEngine.UI.Image Background, Accent;
            public UnityEngine.UI.Text Number, Name, Ammo;
            public CanvasGroup Group;
            public float PulseUntil;
            public string IconFor;
        }
        readonly Slot[] slots = new Slot[4];
        RectTransform hotbar;
        CanvasGroup hotbarGroup;
        float showListUntil = 3f;
        HandItem lastEquipped = HandItem.Empty;
        int lastInventoryCount = -1;

        // Item card
        readonly Queue<Inventory.Item> pending = new();
        RectTransform cardRoot;
        CanvasGroup cardGroup;
        UnityEngine.UI.Text cardName, cardDescription;
        Inventory.Item cardItem;
        float cardTime;

        const float CardPop = 0.25f, CardHold = 2.8f, CardFly = 0.55f;

        public override void Build(HudContext ctx)
        {
            var root = ctx.Root;
            BuildAmmo(root);
            BuildHotbar(root);
            BuildStamina(root);
            BuildCard(root);
            Inventory.ItemReceived += OnItemReceived;
        }

        public override void Dispose() => Inventory.ItemReceived -= OnItemReceived;

        void OnItemReceived(Inventory.Item item)
        {
            if (item != null) pending.Enqueue(item);
        }

        // ---------- Ammo ----------

        void BuildAmmo(RectTransform root)
        {
            var bar = UiTheme.CreateDarkBar("Ammo", root, 0.72f);
            ammoPanel = bar.rectTransform;
            SettingsMenuUi.Place(ammoPanel, new Vector2(1f, 0f), new Vector2(-30f, 30f), new Vector2(300f, 74f));
            ammoGroup = ammoPanel.gameObject.AddComponent<CanvasGroup>();

            cartridgeRow = SettingsMenuUi.CreateRect("Cartridges", ammoPanel);
            SettingsMenuUi.Place(cartridgeRow, new Vector2(0f, 0.5f), new Vector2(16f, 6f), new Vector2(180f, 40f));
            cartridgeRow.pivot = new Vector2(0f, 0.5f);

            reserveText = SettingsMenuUi.CreateText("Reserve", ammoPanel, "", 30, TextAnchor.MiddleRight, FontStyle.Bold, UiTheme.MutedWhite);
            SettingsMenuUi.Place(reserveText.rectTransform, new Vector2(1f, 0.5f), new Vector2(-14f, 6f), new Vector2(100f, 40f));
            reserveText.rectTransform.pivot = new Vector2(1f, 0.5f);

            var track = SettingsMenuUi.CreateImage("ReloadTrack", ammoPanel, UiTheme.WithAlpha(UiTheme.MutedWhite, 0.15f), false);
            reloadTrack = track.rectTransform;
            reloadTrack.anchorMin = new Vector2(0f, 0f);
            reloadTrack.anchorMax = new Vector2(1f, 0f);
            reloadTrack.pivot = new Vector2(0.5f, 0f);
            reloadTrack.offsetMin = new Vector2(16f, 8f);
            reloadTrack.offsetMax = new Vector2(-16f, 13f);
            reloadFill = SettingsMenuUi.CreateImage("Fill", reloadTrack, UiTheme.LightOlive, false);
            SettingsMenuUi.Stretch(reloadFill.rectTransform);
            reloadFill.rectTransform.pivot = new Vector2(0f, 0.5f);

            ammoHint = SettingsMenuUi.CreateText("Hint", ammoPanel, "R — PRZEŁADUJ", 18, TextAnchor.LowerRight, FontStyle.Bold, UiTheme.AlertRed);
            SettingsMenuUi.Place(ammoHint.rectTransform, new Vector2(1f, 1f), new Vector2(0f, 6f), new Vector2(300f, 26f));
            ammoHint.rectTransform.pivot = new Vector2(1f, 0f);
            AddShadow(ammoHint);
        }

        void BuildCartridges(int magazine)
        {
            builtMagazine = magazine;
            foreach (Transform child in cartridgeRow) Object.Destroy(child.gameObject);
            casings.Clear();
            bullets.Clear();
            int shown = Mathf.Clamp(magazine, 1, 12);
            const float step = 15f;
            for (int i = 0; i < shown; i++)
            {
                var c = SettingsMenuUi.CreateRect("Cartridge", cartridgeRow);
                c.anchorMin = c.anchorMax = new Vector2(0f, 0.5f);
                c.pivot = new Vector2(0f, 0.5f);
                c.sizeDelta = new Vector2(10f, 36f);
                c.anchoredPosition = new Vector2(i * step, 0f);
                var casing = SettingsMenuUi.CreateImage("Casing", c, UiTheme.Brass, false);
                var crt = casing.rectTransform;
                crt.anchorMin = new Vector2(0f, 0f);
                crt.anchorMax = new Vector2(1f, 0f);
                crt.pivot = new Vector2(0.5f, 0f);
                crt.sizeDelta = new Vector2(0f, 24f);
                var bullet = SettingsMenuUi.CreateImage("Bullet", c, Copper, false);
                var brt = bullet.rectTransform;
                brt.anchorMin = new Vector2(0.15f, 0f);
                brt.anchorMax = new Vector2(0.85f, 0f);
                brt.pivot = new Vector2(0.5f, 0f);
                brt.sizeDelta = new Vector2(0f, 10f);
                brt.anchoredPosition = new Vector2(0f, 25f);
                casings.Add(casing);
                bullets.Add(bullet);
            }
            cartridgeRow.sizeDelta = new Vector2(shown * step, 40f);
        }

        void TickAmmo(HudContext ctx)
        {
            var weapon = ctx.Weapon;
            bool show = ctx.PistolOut && !ctx.ViewfinderUp;
            ammoGroup.alpha = Mathf.MoveTowards(ammoGroup.alpha, show ? 1f : 0f, ctx.Dt * 6f);
            if (weapon == null) return;
            if (weapon.MagazineSize != builtMagazine) BuildCartridges(weapon.MagazineSize);

            int loaded = weapon.AmmoInMagazine;
            bool low = loaded <= Mathf.Max(1, weapon.MagazineSize / 4);
            for (int i = 0; i < casings.Count; i++)
            {
                bool full = i < loaded;
                casings[i].color = full ? (low ? Color.Lerp(UiTheme.Brass, UiTheme.AlertRed, 0.45f) : UiTheme.Brass)
                                        : UiTheme.WithAlpha(UiTheme.MutedWhite, 0.14f);
                bullets[i].color = full ? Copper : UiTheme.WithAlpha(UiTheme.MutedWhite, 0.08f);
            }
            reserveText.text = $"+{weapon.ReserveAmmo}";

            reloadTrack.gameObject.SetActive(weapon.IsReloading);
            reloadFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(weapon.ReloadProgress), 1f);
            ammoHint.text = weapon.IsReloading ? "PRZEŁADOWANIE…" : "R — PRZEŁADUJ";
            ammoHint.color = weapon.IsReloading ? UiTheme.MutedWhite : UiTheme.AlertRed;
            ammoHint.gameObject.SetActive(weapon.IsReloading || (low && weapon.ReserveAmmo > 0));
        }

        // ---------- Stamina ----------

        void BuildStamina(RectTransform root)
        {
            var track = SettingsMenuUi.CreateImage("Stamina", root, UiTheme.WithAlpha(UiTheme.Dark, 0.6f), false);
            staminaTrack = track.rectTransform;
            SettingsMenuUi.Place(staminaTrack, new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(300f, 6f));
            staminaGroup = staminaTrack.gameObject.AddComponent<CanvasGroup>();
            staminaGroup.alpha = 0f;
            staminaFill = SettingsMenuUi.CreateImage("Fill", staminaTrack, UiTheme.MutedWhite, false);
            SettingsMenuUi.Stretch(staminaFill.rectTransform, 1f, 1f, 1f, 1f);
        }

        void TickStamina(HudContext ctx)
        {
            var player = ctx.Player;
            float value = player != null ? Mathf.Clamp01(player.StaminaNormalized) : 1f;
            bool show = player != null && value < 0.99f && !ctx.ViewfinderUp;
            staminaGroup.alpha = Mathf.MoveTowards(staminaGroup.alpha, show ? 1f : 0f, ctx.Dt * 4f);
            staminaFill.rectTransform.anchorMax = new Vector2(value, 1f);
            bool exhausted = player != null && player.IsExhausted;
            staminaFill.color = exhausted ? UiTheme.AlertRed : UiTheme.WithAlpha(UiTheme.MutedWhite, 0.85f);
        }

        // ---------- Hotbar ----------

        // CS2-style weapon list: right side, slots 1–4 stacked, key + silhouette + name + ammo; shown on
        // switching / scrolling / receiving an item, fades out after a moment.
        void BuildHotbar(RectTransform root)
        {
            hotbar = SettingsMenuUi.CreateRect("WeaponList", root);
            const float w = 330f, h = 64f, gap = 6f;
            hotbar.anchorMin = hotbar.anchorMax = new Vector2(1f, 0f);
            hotbar.pivot = new Vector2(1f, 0f);
            hotbar.sizeDelta = new Vector2(w, 4 * h + 3 * gap);
            hotbar.anchoredPosition = new Vector2(-30f, 150f);
            hotbarGroup = hotbar.gameObject.AddComponent<CanvasGroup>();
            for (int i = 0; i < 4; i++)
            {
                var bg = UiTheme.CreateDarkBar("Slot" + (i + 1), hotbar, 0.72f);
                var rt = bg.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.sizeDelta = new Vector2(w, h);
                rt.anchoredPosition = new Vector2(0f, -i * (h + gap));
                var slot = new Slot { Root = rt, Background = bg, Group = rt.gameObject.AddComponent<CanvasGroup>() };

                slot.Accent = SettingsMenuUi.CreateImage("Accent", rt, UiTheme.Brass, false);
                var art = slot.Accent.rectTransform;
                art.anchorMin = new Vector2(1f, 0f);
                art.anchorMax = new Vector2(1f, 1f);
                art.pivot = new Vector2(1f, 0.5f);
                art.sizeDelta = new Vector2(4f, 0f);

                slot.Number = SettingsMenuUi.CreateText("Number", rt, (i + 1).ToString(), 18, TextAnchor.UpperLeft, FontStyle.Bold, UiTheme.WithAlpha(UiTheme.MutedWhite, 0.6f));
                SettingsMenuUi.Place(slot.Number.rectTransform, new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(24f, 24f));

                slot.Icon = SettingsMenuUi.CreateRect("Icon", rt);
                slot.Icon.anchorMin = new Vector2(0f, 0f);
                slot.Icon.anchorMax = new Vector2(0f, 1f);
                slot.Icon.pivot = new Vector2(0f, 0.5f);
                slot.Icon.sizeDelta = new Vector2(120f, -12f);
                slot.Icon.anchoredPosition = new Vector2(36f, 0f);

                slot.Name = SettingsMenuUi.CreateText("Name", rt, DefaultSlotNames[Mathf.Min(i, DefaultSlotNames.Length - 1)], 17, TextAnchor.UpperRight, FontStyle.Bold, UiTheme.MutedWhite);
                slot.Name.horizontalOverflow = HorizontalWrapMode.Overflow;
                SettingsMenuUi.Stretch(slot.Name.rectTransform, 160f, 16f, 10f, 30f);
                slot.Ammo = SettingsMenuUi.CreateText("Ammo", rt, "", 16, TextAnchor.LowerRight, FontStyle.Normal, UiTheme.WithAlpha(UiTheme.MutedWhite, 0.7f));
                SettingsMenuUi.Stretch(slot.Ammo.rectTransform, 160f, 16f, 30f, 8f);
                slots[i] = slot;
            }
        }

        void TickHotbar(HudContext ctx)
        {
            var hands = PlayerHands.Instance;
            var current = hands != null ? hands.Current : HandItem.Empty;
            int equippedSlot = PlayerHands.SlotNumber(current);
            var items = Inventory.Items;
            int count = items != null ? items.Count : 0;
            if (current != lastEquipped || count != lastInventoryCount || (hands != null && hands.IsSwapping))
                showListUntil = ctx.Now + 2.5f;
            lastEquipped = current;
            lastInventoryCount = count;

            var weapon = ctx.Weapon;
            for (int i = 0; i < 4; i++)
            {
                var slot = slots[i];
                var owned = Inventory.InSlot(i + 1);
                string name = owned != null && !string.IsNullOrEmpty(owned.Name) ? owned.Name : "—";
                if (slot.Name.text != name.ToUpperInvariant()) slot.Name.text = name.ToUpperInvariant();

                string iconKey = owned != null ? owned.Id : "";
                if (slot.IconFor != iconKey)
                {
                    for (int c = slot.Icon.childCount - 1; c >= 0; c--) UnityEngine.Object.Destroy(slot.Icon.GetChild(c).gameObject);
                    var def = FindDef(iconKey);
                    if (def != null) LoadoutScreen.DrawSilhouette(slot.Icon, def);
                    else if (iconKey == Inventory.Camera) DrawCameraIcon(slot.Icon);
                    slot.IconFor = iconKey;
                }

                bool isEquipped = i + 1 == equippedSlot && owned != null;
                if (isEquipped && weapon != null && weapon.Definition != null && (i == 0 || i == 1))
                    slot.Ammo.text = $"{weapon.AmmoInMagazine} / {weapon.ReserveAmmo}";
                else slot.Ammo.text = owned == null ? "" : i == 2 ? "cicha likwidacja" : i == 3 ? "F — kadr" : "";

                bool pulse = ctx.Now < slot.PulseUntil;
                slot.Group.alpha = owned != null ? (isEquipped ? 1f : 0.7f) : 0.25f;
                slot.Accent.enabled = isEquipped || pulse;
                slot.Background.color = isEquipped || pulse
                    ? UiTheme.WithAlpha(new Color(0.3f, 0.26f, 0.16f), pulse ? 0.95f : 0.9f)
                    : UiTheme.WithAlpha(UiTheme.Dark, 0.62f);
                slot.Root.localScale = Vector3.one * (pulse ? 1f + 0.06f * Mathf.Sin((slot.PulseUntil - ctx.Now) / 0.4f * Mathf.PI) : 1f);
            }
            float target = ctx.Now < showListUntil ? 1f : 0f;
            hotbarGroup.alpha = Mathf.MoveTowards(hotbarGroup.alpha, target, ctx.Dt * (target > 0f ? 10f : 2.5f));
            hotbar.gameObject.SetActive(!ctx.ViewfinderUp);
        }

        static WeaponDef FindDef(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var list in new[] { WeaponCatalog.Pistols, WeaponCatalog.Rifles, WeaponCatalog.Melee })
                foreach (var d in list) if (d.Id == id) return d;
            return null;
        }

        static void DrawCameraIcon(RectTransform area)
        {
            var c = UiTheme.WithAlpha(UiTheme.MutedWhite, 0.88f);
            var body = SettingsMenuUi.CreateImage("Body", area, c, false);
            SettingsMenuUi.Anchor(body.rectTransform, 0.2f, 0.25f, 0.8f, 0.75f);
            body.rectTransform.offsetMin = body.rectTransform.offsetMax = Vector2.zero;
            var lens = SettingsMenuUi.CreateImage("Lens", area, UiTheme.Dark, false);
            lens.sprite = UiTheme.CircleSprite;
            SettingsMenuUi.Anchor(lens.rectTransform, 0.4f, 0.3f, 0.6f, 0.7f);
            lens.rectTransform.offsetMin = lens.rectTransform.offsetMax = Vector2.zero;
        }

        // ---------- "OTRZYMANO" card ----------

        void BuildCard(RectTransform root)
        {
            var paper = UiTheme.CreatePaper(root, "ItemCard", new Vector2(540f, 210f), out cardRoot);
            cardRoot.anchoredPosition = new Vector2(0f, 150f);
            cardGroup = cardRoot.gameObject.AddComponent<CanvasGroup>();
            var stamp = UiTheme.CreateStamp(paper, "Otrzymano", UiTheme.StampRed, 20);
            stamp.anchorMin = stamp.anchorMax = new Vector2(0f, 1f);
            stamp.anchoredPosition = new Vector2(110f, -30f);
            cardName = UiTheme.CreateHeading("Name", paper, "", 32, UiTheme.Ink, TextAnchor.MiddleLeft);
            SettingsMenuUi.PinTop(cardName.rectTransform, 60f, 44f, 30f);
            var rule = UiTheme.CreateRule("Rule", paper, UiTheme.FadedInk, 1.5f);
            SettingsMenuUi.PinTop(rule.rectTransform, 108f, 1.5f, 30f);
            cardDescription = SettingsMenuUi.CreateText("Description", paper, "", 19, TextAnchor.UpperLeft, FontStyle.Normal, UiTheme.Ink);
            SettingsMenuUi.Stretch(cardDescription.rectTransform, 30f, 30f, 118f, 14f);
            cardRoot.gameObject.SetActive(false);
        }

        void TickCard(HudContext ctx)
        {
            if (cardItem == null)
            {
                if (!ctx.Visible || pending.Count == 0) return;
                cardItem = pending.Dequeue();
                cardTime = 0f;
                cardName.text = (cardItem.Name ?? "").ToUpperInvariant();
                cardDescription.text = cardItem.Description ?? "";
                cardRoot.gameObject.SetActive(true);
            }
            if (ctx.Visible) cardTime += ctx.Dt; // waits while the HUD is hidden (dialogue, pause)

            Vector2 home = new(0f, 150f);
            if (cardTime < CardPop)
            {
                float k = cardTime / CardPop;
                cardRoot.anchoredPosition = home;
                cardRoot.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, 1f - (1f - k) * (1f - k));
                cardGroup.alpha = k;
            }
            else if (cardTime < CardPop + CardHold)
            {
                cardRoot.anchoredPosition = home;
                cardRoot.localScale = Vector3.one;
                cardGroup.alpha = 1f;
            }
            else if (cardTime < CardPop + CardHold + CardFly)
            {
                float k = (cardTime - CardPop - CardHold) / CardFly;
                float ease = k * k;
                cardRoot.anchoredPosition = Vector2.Lerp(home, SlotTarget(ctx, cardItem.Slot), ease);
                cardRoot.localScale = Vector3.one * Mathf.Lerp(1f, 0.12f, ease);
                cardGroup.alpha = Mathf.Lerp(1f, 0.2f, ease);
            }
            else
            {
                int index = cardItem.Slot - 1;
                if (index >= 0 && index < slots.Length) slots[index].PulseUntil = ctx.Now + 0.4f;
                cardItem = null;
                cardRoot.gameObject.SetActive(false);
            }
        }

        /// <summary>Hotbar slot centre in the card's coordinate frame (both are centred on the canvas).</summary>
        Vector2 SlotTarget(HudContext ctx, int slotNumber)
        {
            int index = Mathf.Clamp(slotNumber - 1, 0, slots.Length - 1);
            var slot = slots[index].Root;
            Vector3 world = slot.TransformPoint(slot.rect.center);
            Vector3 local = ctx.Root.InverseTransformPoint(world);
            return new Vector2(local.x, local.y);
        }

        public override void Tick(HudContext ctx)
        {
            TickAmmo(ctx);
            TickStamina(ctx);
            TickHotbar(ctx);
            TickCard(ctx);
        }

        static void AddShadow(UnityEngine.UI.Graphic g)
        {
            var s = g.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.75f);
            s.effectDistance = new Vector2(1.5f, -1.5f);
        }
    }
}
