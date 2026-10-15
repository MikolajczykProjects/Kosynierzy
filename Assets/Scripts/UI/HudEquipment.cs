using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// UI v2 bottom of the screen: weapon block bottom-right (silhouette, name, cartridge pips, count + reserve; melee shows
    /// a hint), stamina as 5 thin segments bottom-left (only when not full), the quick weapon list on the right
    /// (shown on switching), and the item card that pops up and flies into its list slot.
    /// </summary>
    public sealed class HudEquipment : HudWidget
    {
        // Weapon block
        RectTransform ammoPanel, cartridgeRow, silhouette;
        readonly List<UnityEngine.UI.Image> casings = new();
        UnityEngine.UI.Text nameText, countText, reserveText, ammoHint, meleeHint;
        RectTransform reloadTrack;
        UnityEngine.UI.Image reloadFill;
        CanvasGroup ammoGroup;
        int builtMagazine = -1;
        string silhouetteFor;

        // Stamina (5 thin segments)
        const int StaminaSegments = 5;
        RectTransform staminaRoot;
        readonly UnityEngine.UI.Image[] staminaFills = new UnityEngine.UI.Image[StaminaSegments];
        CanvasGroup staminaGroup;

        // Quick weapon list
        sealed class Slot
        {
            public RectTransform Root, Icon;
            public UnityEngine.UI.Image Strip, DigitBox;
            public UnityEngine.UI.Text Digit;
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

        // ---------- Weapon block ----------

        static UnityEngine.UI.Text BottomRightText(RectTransform parent, string name, int size, Vector2 pos, Vector2 box, Color color, bool bodyFace = false)
        {
            var t = UiTheme.CreateHudText(name, parent, "", size, TextAnchor.LowerRight, color, bodyFace);
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(1f, 0f);
            t.rectTransform.pivot = new Vector2(1f, 0f);
            t.rectTransform.sizeDelta = box;
            t.rectTransform.anchoredPosition = pos;
            return t;
        }

        void BuildAmmo(RectTransform root)
        {
            ammoPanel = SettingsMenuUi.CreateRect("WeaponBlock", root);
            ammoPanel.anchorMin = ammoPanel.anchorMax = ammoPanel.pivot = new Vector2(1f, 0f);
            ammoPanel.sizeDelta = new Vector2(380f, 180f);
            ammoPanel.anchoredPosition = new Vector2(-64f, 48f);
            ammoGroup = ammoPanel.gameObject.AddComponent<CanvasGroup>();

            silhouette = SettingsMenuUi.CreateRect("Silhouette", ammoPanel);
            silhouette.anchorMin = silhouette.anchorMax = silhouette.pivot = new Vector2(1f, 0f);
            silhouette.sizeDelta = new Vector2(200f, 64f);
            silhouette.anchoredPosition = new Vector2(0f, 112f);

            nameText = BottomRightText(ammoPanel, "Name", 16, new Vector2(0f, 84f), new Vector2(380f, 20f), UiTheme.Bone);
            meleeHint = BottomRightText(ammoPanel, "MeleeHint", 17, new Vector2(0f, 52f), new Vector2(380f, 24f), UiTheme.Bone2, true);
            countText = BottomRightText(ammoPanel, "Count", 56, new Vector2(-52f, 0f), new Vector2(150f, 60f), UiTheme.Bone);
            reserveText = BottomRightText(ammoPanel, "Reserve", 26, new Vector2(0f, 4f), new Vector2(60f, 30f), UiTheme.Bone3);

            cartridgeRow = SettingsMenuUi.CreateRect("Cartridges", ammoPanel);
            cartridgeRow.anchorMin = cartridgeRow.anchorMax = cartridgeRow.pivot = new Vector2(1f, 0f);
            cartridgeRow.sizeDelta = new Vector2(120f, 14f);
            cartridgeRow.anchoredPosition = new Vector2(0f, 64f);

            var track = SettingsMenuUi.CreateImage("ReloadTrack", ammoPanel, UiTheme.WithAlpha(UiTheme.Bone, 0.2f), false);
            reloadTrack = track.rectTransform;
            reloadTrack.anchorMin = reloadTrack.anchorMax = reloadTrack.pivot = new Vector2(1f, 0f);
            reloadTrack.sizeDelta = new Vector2(120f, 3f);
            reloadTrack.anchoredPosition = new Vector2(0f, 58f);
            reloadFill = SettingsMenuUi.CreateImage("Fill", reloadTrack, UiTheme.Bone, false);
            SettingsMenuUi.Stretch(reloadFill.rectTransform);
            reloadFill.rectTransform.pivot = new Vector2(0f, 0.5f);

            ammoHint = BottomRightText(ammoPanel, "Hint", 20, new Vector2(-210f, 8f), new Vector2(170f, 26f), UiTheme.Danger);
        }

        void BuildCartridges(int magazine)
        {
            builtMagazine = magazine;
            foreach (Transform child in cartridgeRow) Object.Destroy(child.gameObject);
            casings.Clear();
            if (magazine < 1 || magazine > 10) return; // bigger magazines: the count alone
            const float step = 11f;
            for (int i = 0; i < magazine; i++)
            {
                var pip = SettingsMenuUi.CreateImage("Pip", cartridgeRow, UiTheme.Bone, false);
                var rt = pip.rectTransform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
                rt.sizeDelta = new Vector2(7f, 14f);
                rt.anchoredPosition = new Vector2(-(magazine - 1 - i) * step, 0f);
                UiTheme.AddSoftShadow(pip);
                casings.Add(pip);
            }
        }

        void TickAmmo(HudContext ctx)
        {
            var hands = PlayerHands.Instance;
            var current = hands != null ? hands.Current : HandItem.Pistol;
            var owned = Inventory.InSlot(PlayerHands.SlotNumber(current));
            bool firearm = ctx.PistolOut && ctx.Weapon != null;
            bool show = owned != null && !ctx.ViewfinderUp;
            ammoGroup.alpha = Mathf.MoveTowards(ammoGroup.alpha, show ? 1f : 0f, ctx.Dt * 6f);
            if (owned == null) return;

            string id = owned.Id ?? "";
            if (silhouetteFor != id)
            {
                silhouetteFor = id;
                for (int c = silhouette.childCount - 1; c >= 0; c--) Object.Destroy(silhouette.GetChild(c).gameObject);
                var def = FindDef(id);
                if (def != null) LoadoutScreen.DrawSilhouette(silhouette, def);
                else if (id == Inventory.Camera) DrawCameraIcon(silhouette);
            }
            nameText.text = (owned.Name ?? "").ToUpperInvariant();

            countText.gameObject.SetActive(firearm);
            reserveText.gameObject.SetActive(firearm);
            cartridgeRow.gameObject.SetActive(firearm);
            meleeHint.gameObject.SetActive(!firearm);
            if (!firearm)
            {
                reloadTrack.gameObject.SetActive(false);
                ammoHint.gameObject.SetActive(false);
                meleeHint.text = current == HandItem.Camera ? "F — kadr" : "cicha likwidacja";
                return;
            }

            var weapon = ctx.Weapon;
            if (weapon.MagazineSize != builtMagazine) BuildCartridges(weapon.MagazineSize);
            int loaded = weapon.AmmoInMagazine;
            bool low = loaded <= Mathf.Max(1, weapon.MagazineSize / 4);
            Color tone = low ? (loaded == 0 ? UiTheme.Danger : UiTheme.Warn) : UiTheme.Bone;
            for (int i = 0; i < casings.Count; i++)
            {
                bool full = i < loaded;
                casings[i].color = full ? tone : UiTheme.WithAlpha(UiTheme.Bone, 0.22f);
            }
            countText.text = loaded.ToString("00");
            countText.color = tone;
            reserveText.text = "/ " + weapon.ReserveAmmo.ToString("00");

            reloadTrack.gameObject.SetActive(weapon.IsReloading);
            reloadFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(weapon.ReloadProgress), 1f);
            ammoHint.text = weapon.IsReloading ? "Przeładowanie…" : "R  Przeładuj";
            ammoHint.color = weapon.IsReloading ? UiTheme.Bone2 : (loaded == 0 ? UiTheme.Danger : UiTheme.Warn);
            ammoHint.gameObject.SetActive(weapon.IsReloading || (low && weapon.ReserveAmmo > 0));
        }

        // ---------- Stamina (bottom-left, above the visibility block) ----------

        void BuildStamina(RectTransform root)
        {
            staminaRoot = SettingsMenuUi.CreateRect("Stamina", root);
            staminaRoot.anchorMin = staminaRoot.anchorMax = staminaRoot.pivot = Vector2.zero;
            staminaRoot.sizeDelta = new Vector2(StaminaSegments * 38f - 4f, 3f);
            staminaRoot.anchoredPosition = new Vector2(64f, 136f);
            staminaGroup = staminaRoot.gameObject.AddComponent<CanvasGroup>();
            staminaGroup.alpha = 0f;
            for (int i = 0; i < StaminaSegments; i++)
            {
                var back = SettingsMenuUi.CreateImage("Seg", staminaRoot, UiTheme.WithAlpha(UiTheme.Bone, 0.2f), false);
                back.rectTransform.anchorMin = back.rectTransform.anchorMax = back.rectTransform.pivot = Vector2.zero;
                back.rectTransform.sizeDelta = new Vector2(34f, 3f);
                back.rectTransform.anchoredPosition = new Vector2(i * 38f, 0f);
                var fill = SettingsMenuUi.CreateImage("Fill", back.rectTransform, UiTheme.WithAlpha(UiTheme.Bone, 0.92f), false);
                SettingsMenuUi.Stretch(fill.rectTransform);
                fill.rectTransform.pivot = new Vector2(0f, 0.5f);
                staminaFills[i] = fill;
            }
        }

        void TickStamina(HudContext ctx)
        {
            var player = ctx.Player;
            float value = player != null ? Mathf.Clamp01(player.StaminaNormalized) : 1f;
            bool show = player != null && value < 0.99f && !ctx.ViewfinderUp;
            staminaGroup.alpha = Mathf.MoveTowards(staminaGroup.alpha, show ? 1f : 0f, ctx.Dt * 4f);
            bool exhausted = player != null && player.IsExhausted;
            for (int i = 0; i < StaminaSegments; i++)
            {
                float f = Mathf.Clamp01(value * StaminaSegments - i);
                staminaFills[i].rectTransform.anchorMax = new Vector2(f, 1f);
                staminaFills[i].color = exhausted ? UiTheme.Danger : UiTheme.WithAlpha(UiTheme.Bone, 0.92f);
            }
        }

        // ---------- Quick weapon list ----------

        // Right side, rows pitch 62: silhouette right-aligned, slot digit at the edge, selected = full white + 3 px strip + boxed digit.
        void BuildHotbar(RectTransform root)
        {
            hotbar = SettingsMenuUi.CreateRect("WeaponList", root);
            const float w = 260f, h = 62f;
            hotbar.anchorMin = hotbar.anchorMax = hotbar.pivot = new Vector2(1f, 0.5f);
            hotbar.sizeDelta = new Vector2(w, 4 * h);
            hotbar.anchoredPosition = new Vector2(-64f, 0f);
            hotbarGroup = hotbar.gameObject.AddComponent<CanvasGroup>();
            for (int i = 0; i < 4; i++)
            {
                var rt = SettingsMenuUi.CreateRect("Slot" + (i + 1), hotbar);
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
                rt.sizeDelta = new Vector2(w, h);
                rt.anchoredPosition = new Vector2(0f, -i * h);
                var slot = new Slot { Root = rt, Group = rt.gameObject.AddComponent<CanvasGroup>() };

                slot.Icon = SettingsMenuUi.CreateRect("Icon", rt);
                slot.Icon.anchorMin = slot.Icon.anchorMax = slot.Icon.pivot = new Vector2(1f, 0.5f);
                slot.Icon.sizeDelta = new Vector2(172f, 46f);
                slot.Icon.anchoredPosition = new Vector2(-42f, 2f);

                slot.Strip = SettingsMenuUi.CreateImage("Strip", rt, UiTheme.Bone, false);
                slot.Strip.rectTransform.anchorMin = slot.Strip.rectTransform.anchorMax = slot.Strip.rectTransform.pivot = new Vector2(1f, 0f);
                slot.Strip.rectTransform.sizeDelta = new Vector2(172f, 3f);
                slot.Strip.rectTransform.anchoredPosition = new Vector2(-42f, 2f);
                UiTheme.AddSoftShadow(slot.Strip);

                slot.DigitBox = SettingsMenuUi.CreateImage("DigitBox", rt, UiTheme.Bone, false);
                slot.DigitBox.rectTransform.anchorMin = slot.DigitBox.rectTransform.anchorMax = slot.DigitBox.rectTransform.pivot = new Vector2(1f, 0.5f);
                slot.DigitBox.rectTransform.sizeDelta = new Vector2(24f, 24f);
                slot.DigitBox.rectTransform.anchoredPosition = Vector2.zero;
                slot.Digit = SettingsMenuUi.CreateText("Digit", slot.DigitBox.rectTransform, (i + 1).ToString(), 16, TextAnchor.MiddleCenter, FontStyle.Normal, UiTheme.Bone3);
                slot.Digit.font = UiTheme.Label;
                SettingsMenuUi.Stretch(slot.Digit.rectTransform);
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

            for (int i = 0; i < 4; i++)
            {
                var slot = slots[i];
                var owned = Inventory.InSlot(i + 1);

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
                bool pulse = ctx.Now < slot.PulseUntil;
                slot.Group.alpha = owned != null ? (isEquipped || pulse ? 1f : 0.4f) : 0.18f;
                slot.Strip.enabled = isEquipped;
                slot.DigitBox.color = isEquipped ? UiTheme.Bone : new Color(1f, 1f, 1f, 0f);
                slot.Digit.color = isEquipped ? UiTheme.KeyInk : UiTheme.Bone3;
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
            var c = UiTheme.WithAlpha(UiTheme.Bone, 0.88f);
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
            cardRoot = SettingsMenuUi.CreateRect("ItemCard", root);
            SettingsMenuUi.Place(cardRoot, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(560f, 150f));
            cardGroup = cardRoot.gameObject.AddComponent<CanvasGroup>();
            var label = UiTheme.CreateHudText("Label", cardRoot, "OTRZYMANO", 13, TextAnchor.UpperCenter, UiTheme.Warn);
            SettingsMenuUi.PinTop(label.rectTransform, 0f, 18f, 0f);
            cardName = UiTheme.CreateHudText("Name", cardRoot, "", 36, TextAnchor.UpperCenter, UiTheme.Bone);
            SettingsMenuUi.PinTop(cardName.rectTransform, 24f, 44f, 0f);
            var rule = SettingsMenuUi.CreateImage("Rule", cardRoot, UiTheme.Hairline, false);
            SettingsMenuUi.Place(rule.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(160f, 1f));
            rule.rectTransform.pivot = new Vector2(0.5f, 1f);
            cardDescription = UiTheme.CreateHudText("Description", cardRoot, "", 18, TextAnchor.UpperCenter, UiTheme.Bone2, true);
            cardDescription.horizontalOverflow = HorizontalWrapMode.Wrap;
            SettingsMenuUi.PinTop(cardDescription.rectTransform, 88f, 60f, 20f);
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
