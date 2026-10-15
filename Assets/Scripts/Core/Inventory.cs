using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// The player's items, mirrored from the weapon picks (WeaponCatalog) plus the camera from Zofia.
    /// Slot = hotbar key (CS-style): 1 pistol, 2 rifle, 3 blade, 4 camera.
    /// ItemReceived drives the "OTRZYMANO" card in the HUD and the 3D hand-over in PlayerHands.
    /// </summary>
    public static class Inventory
    {
        public class Item
        {
            public string Id;
            public string Name;
            public string Description;
            public int Slot;
        }

        public const string Camera = "aparat";

        static readonly List<Item> items = new();
        public static IReadOnlyList<Item> Items => items;

        public static event Action<Item> ItemReceived;
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            items.Clear();
            ItemReceived = null;
            Changed = null;
        }

        public static bool Has(string id) => items.Exists(i => i.Id == id);
        public static Item InSlot(int slot) => items.Find(i => i.Slot == slot);

        public static void Give(string id, string name, string description, int slot, bool announce = true)
        {
            items.RemoveAll(i => i.Slot == slot && i.Id != id);
            if (Has(id)) return;
            var item = new Item { Id = id, Name = name, Description = description, Slot = slot };
            items.Add(item);
            items.Sort((a, b) => a.Slot.CompareTo(b.Slot));
            if (id == Camera) GameProgress.HasCamera = true;
            Changed?.Invoke();
            if (announce) ItemReceived?.Invoke(item);
        }

        public static void GiveWeapon(WeaponDef def, bool announce = true) =>
            Give(def.Id, def.Name, def.Description, (int)def.Slot, announce);

        public static void GiveCamera(bool announce = true) =>
            Give(Camera, "Aparat Leica", "Mały aparat małoobrazkowy od pani Zofii. Film na 36 klatek — każde zdjęcie dowodu się liczy.", 4, announce);

        /// <summary>Rebuild from the current weapon picks (start, checkpoint reload, loadout screen).</summary>
        public static void NewGame(bool withCamera)
        {
            items.Clear();
            if (WeaponCatalog.SelectedPistol != null) GiveWeapon(WeaponCatalog.SelectedPistol, false);
            if (WeaponCatalog.SelectedRifle != null) GiveWeapon(WeaponCatalog.SelectedRifle, false);
            if (WeaponCatalog.SelectedMelee != null) GiveWeapon(WeaponCatalog.SelectedMelee, false);
            if (withCamera) GiveCamera(false);
            Changed?.Invoke();
        }
    }
}
