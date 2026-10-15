using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    public enum WeaponSlot { Pistol = 1, Rifle = 2, Melee = 3 }

    /// <summary>One selectable weapon: gameplay numbers + what the loadout screen shows.</summary>
    public class WeaponDef
    {
        public string Id, Name, Caliber, Description, Prefab;
        public WeaponSlot Slot;
        public bool IsMelee;
        // Firearm stats.
        public float Damage = 34f, FireRate = 420f, BulletSpeed = 320f, HipSpread = 1.4f, AimSpread = 0.15f;
        public float RecoilUp = 1.6f, RecoilSide = 0.45f, ReloadTime = 1.5f, EmptyReloadTime = 2f, NoiseRadius = 30f;
        public int Magazine = 8, StartReserve = 0;
        public bool Automatic;
        public float MuzzleScale = 1f;
        /// <summary>False for revolvers: the empty casings stay in the drum.</summary>
        public bool EjectsCasings = true;
        // Melee.
        public float MeleeRange = 1.8f, TakedownTime = 0.25f;
        /// <summary>0..1 bars for the loadout card (CS2-style).</summary>
        public float StatDamage, StatAccuracy, StatSilence, StatHandling;
    }

    /// <summary>
    /// All weapons and the player's current picks. Start: one pistol (of 3) + one blade (of 2);
    /// a rifle (of 3) is chosen later at the radio station armoury. Prefab names are for team D;
    /// until they exist PlayerHands builds low-poly placeholders.
    /// </summary>
    public static class WeaponCatalog
    {
        public static readonly List<WeaponDef> Pistols = new()
        {
            new WeaponDef { Id = "vis", Name = "Vis wz. 35", Caliber = "9 mm Parabellum", Prefab = "OB_Pistolet_Vis", Slot = WeaponSlot.Pistol,
                Description = "Polski pistolet służbowy. Celny i niezawodny, ale słychać go z daleka.",
                Damage = 34f, FireRate = 400f, Magazine = 8, HipSpread = 1.2f, AimSpread = 0.12f, RecoilUp = 1.6f, NoiseRadius = 30f,
                ReloadTime = 1.8f, EmptyReloadTime = 2f, // = the AC_Vis "Przeladowanie" clip (1.8 s); empty = last-shot slide clip (0.17 s) + 1.8 s
                StatDamage = 0.7f, StatAccuracy = 0.85f, StatSilence = 0.3f, StatHandling = 0.7f },
            new WeaponDef { Id = "browning", Name = "Browning wz. 1922", Caliber = "7,65 mm", Prefab = "OB_Pistolet_Browning", Slot = WeaponSlot.Pistol,
                Description = "Mały pistolet kieszonkowy. Słabszy, ale cichszy i szybki w ręku.",
                Damage = 26f, FireRate = 480f, Magazine = 9, HipSpread = 1.5f, AimSpread = 0.2f, RecoilUp = 1.1f, NoiseRadius = 20f, ReloadTime = 1.3f, MuzzleScale = 0.75f,
                StatDamage = 0.5f, StatAccuracy = 0.65f, StatSilence = 0.55f, StatHandling = 0.9f },
            new WeaponDef { Id = "nagant", Name = "Nagant wz. 1930", Caliber = "7,62 mm", Prefab = "OB_Rewolwer_Nagant", Slot = WeaponSlot.Pistol,
                Description = "Rewolwer z gazoszczelnym bębnem — najcichszy strzał. Długie ładowanie nabój po naboju.",
                Damage = 40f, FireRate = 260f, Magazine = 7, HipSpread = 1.6f, AimSpread = 0.18f, RecoilUp = 2.2f, NoiseRadius = 12f, ReloadTime = 3.2f, EmptyReloadTime = 3.6f, MuzzleScale = 0.6f, EjectsCasings = false,
                StatDamage = 0.8f, StatAccuracy = 0.7f, StatSilence = 0.9f, StatHandling = 0.4f },
        };

        public static readonly List<WeaponDef> Melee = new()
        {
            new WeaponDef { Id = "bagnet", Name = "Bagnet wz. 1924", Caliber = "Ostrze 25 cm", Prefab = "OB_Bagnet", Slot = WeaponSlot.Melee, IsMelee = true,
                Description = "Długie ostrze — cicha likwidacja z większej odległości.", MeleeRange = 1.9f, TakedownTime = 0.3f,
                StatDamage = 1f, StatAccuracy = 0.6f, StatSilence = 1f, StatHandling = 0.6f },
            new WeaponDef { Id = "noz", Name = "Nóż sprężynowy", Caliber = "Ostrze 11 cm", Prefab = "OB_Noz", Slot = WeaponSlot.Melee, IsMelee = true,
                Description = "Krótki i szybki — likwidacja trwa chwilę krócej, ale trzeba podejść bliżej.", MeleeRange = 1.4f, TakedownTime = 0.18f,
                StatDamage = 1f, StatAccuracy = 0.5f, StatSilence = 1f, StatHandling = 1f },
        };

        public static readonly List<WeaponDef> Rifles = new()
        {
            new WeaponDef { Id = "wz29", Name = "Karabinek wz. 29", Caliber = "7,92 mm Mauser", Prefab = "OB_Karabin_wz29", Slot = WeaponSlot.Rifle,
                Description = "Polski karabinek powtarzalny. Zabójczo celny, huk słychać w całej okolicy.",
                Damage = 100f, FireRate = 55f, Magazine = 5, StartReserve = 10, BulletSpeed = 600f, HipSpread = 2.5f, AimSpread = 0.05f, RecoilUp = 4.5f, RecoilSide = 0.8f,
                NoiseRadius = 60f, ReloadTime = 2.6f, EmptyReloadTime = 3f, MuzzleScale = 1.6f,
                StatDamage = 1f, StatAccuracy = 0.95f, StatSilence = 0.05f, StatHandling = 0.35f },
            new WeaponDef { Id = "k98", Name = "Mauser 98k (zdobyczny)", Caliber = "7,92 mm Mauser", Prefab = "OB_Mauser98k", Slot = WeaponSlot.Rifle,
                Description = "Karabin odebrany SD. Te same naboje, trochę szybszy zamek.",
                Damage = 100f, FireRate = 62f, Magazine = 5, StartReserve = 10, BulletSpeed = 600f, HipSpread = 2.4f, AimSpread = 0.06f, RecoilUp = 4.3f, RecoilSide = 0.8f,
                NoiseRadius = 60f, ReloadTime = 2.4f, EmptyReloadTime = 2.8f, MuzzleScale = 1.6f,
                StatDamage = 1f, StatAccuracy = 0.9f, StatSilence = 0.05f, StatHandling = 0.45f },
            new WeaponDef { Id = "erma", Name = "Erma EMP (zdobyczna)", Caliber = "9 mm Parabellum", Prefab = "OB_Erma", Slot = WeaponSlot.Rifle,
                Description = "Pistolet maszynowy. Ogień ciągły z bliska — mało celny, bardzo głośny.",
                Damage = 30f, FireRate = 540f, Magazine = 32, StartReserve = 32, Automatic = true, BulletSpeed = 380f, HipSpread = 3f, AimSpread = 0.9f, RecoilUp = 1.1f, RecoilSide = 0.6f,
                NoiseRadius = 45f, ReloadTime = 2.2f, EmptyReloadTime = 2.6f, MuzzleScale = 1.2f,
                StatDamage = 0.55f, StatAccuracy = 0.35f, StatSilence = 0.15f, StatHandling = 0.8f },
        };

        public static WeaponDef SelectedPistol = Pistols[0];
        public static WeaponDef SelectedMelee = Melee[0];
        /// <summary>Null until chosen at the armoury.</summary>
        public static WeaponDef SelectedRifle;
        /// <summary>True once the start-of-game loadout was confirmed (survives checkpoint reloads).</summary>
        public static bool LoadoutChosen;

        public static event System.Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Changed = null;
            NewGame();
        }

        public static void Choose(WeaponDef def)
        {
            switch (def.Slot)
            {
                case WeaponSlot.Pistol: SelectedPistol = def; break;
                case WeaponSlot.Melee: SelectedMelee = def; break;
                case WeaponSlot.Rifle: SelectedRifle = def; break;
            }
            Changed?.Invoke();
        }

        public static void NewGame()
        {
            SelectedPistol = Pistols[0];
            SelectedMelee = Melee[0];
            SelectedRifle = null;
            LoadoutChosen = false;
        }
    }
}
