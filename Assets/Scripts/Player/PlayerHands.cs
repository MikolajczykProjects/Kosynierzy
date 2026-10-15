using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kosynierzy
{
    /// <summary>Hotbar slots (CS-style): 1 pistol, 2 rifle, 3 blade (Bayonet = any melee), 4 camera.</summary>
    public enum HandItem { Empty, Pistol, Camera, Bayonet, Rifle }

    /// <summary>
    /// What the player holds. Keys 1–4, mouse wheel / d-pad cycle, F = camera. Firearms come from the
    /// WeaponCatalog picks (loadout + armoury) and share one PlayerWeapon that switches definition and
    /// viewmodel; every weapon has a low-poly placeholder viewmodel with real sights (aim pose puts the
    /// sight line on the screen centre) until team D's OB_* prefab exists (its child "Lufa" = muzzle).
    /// Also: blade stab (melee + silent takedowns) and the 3D hand-over of received items. On the camera.
    /// </summary>
    public class PlayerHands : MonoBehaviour
    {
        [SerializeField] float swapLower = 0.14f;
        [SerializeField] float swapRaise = 0.22f;

        public static PlayerHands Instance { get; private set; }
        public HandItem Current { get; private set; } = HandItem.Pistol;
        public bool IsSwapping { get; private set; }
        public event Action<HandItem> Equipped;

        FirstPersonController player;
        PlayerWeapon weapon;
        PhotoCamera photo;
        Transform firearmHolder, cameraHolder, meleeHolder;
        Transform cameraModel;
        InputAction slot1, slot2, slot3, slot4, cycle, stabAction;
        float holderLower; // 0 = in hands, 1 = lowered out of view
        bool stabbing;
        Material steel, wood, skin, sleeve, black, chrome, leather, brass;

        struct FirearmModel { public Transform Root, Muzzle; public Vector3 Hip, Aim; }
        readonly Dictionary<string, FirearmModel> firearms = new();
        readonly Dictionary<string, Transform> blades = new();
        Transform activeBlade;
        WeaponDef activeFirearm;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        void Awake()
        {
            Instance = this;
            player = GetComponentInParent<FirstPersonController>();
            weapon = GetComponentInChildren<PlayerWeapon>();
            photo = GetComponent<PhotoCamera>();

            steel = Mat(new Color(0.16f, 0.16f, 0.17f), 0.55f);
            wood = Mat(new Color(0.36f, 0.22f, 0.12f), 0.25f);
            skin = Mat(new Color(0.78f, 0.6f, 0.48f), 0.15f);
            sleeve = Mat(new Color(0.23f, 0.22f, 0.18f), 0.05f);
            black = Mat(new Color(0.07f, 0.07f, 0.07f), 0.3f);
            chrome = Mat(new Color(0.72f, 0.72f, 0.7f), 0.8f);
            leather = Mat(new Color(0.12f, 0.1f, 0.09f), 0.1f);
            brass = Mat(new Color(0.78f, 0.6f, 0.25f), 0.6f);

            firearmHolder = Holder("Holder_Firearm");
            meleeHolder = Holder("Holder_Melee");
            BuildCamera();
            var oldCube = transform.Find("WeaponModel");
            if (oldCube != null) oldCube.gameObject.SetActive(false);

            slot1 = new InputAction("Slot1", InputActionType.Button, "<Keyboard>/1");
            slot2 = new InputAction("Slot2", InputActionType.Button, "<Keyboard>/2");
            slot3 = new InputAction("Slot3", InputActionType.Button, "<Keyboard>/3");
            slot4 = new InputAction("Slot4", InputActionType.Button, "<Keyboard>/4");
            cycle = new InputAction("Cycle", InputActionType.Value, "<Mouse>/scroll/y");
            cycle.AddBinding("<Gamepad>/dpad/x");
            stabAction = new InputAction("Stab", InputActionType.Button, "<Mouse>/leftButton");
            stabAction.AddBinding("<Gamepad>/rightTrigger");
        }

        InputAction[] Actions => new[] { slot1, slot2, slot3, slot4, cycle, stabAction };

        void OnEnable()
        {
            foreach (var a in Actions) a.Enable();
            Inventory.ItemReceived += OnItemReceived;
            WeaponCatalog.Changed += RefreshLoadout;
        }

        void OnDisable()
        {
            foreach (var a in Actions) a.Disable();
            Inventory.ItemReceived -= OnItemReceived;
            WeaponCatalog.Changed -= RefreshLoadout;
        }

        void OnDestroy()
        {
            foreach (var a in Actions) a.Dispose();
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            ShowCurrent();
            Equipped?.Invoke(Current);
        }

        void Update()
        {
            bool controls = player == null || player.enabled;
            if (controls && !GameState.IsPaused && !stabbing && !(photo != null && photo.IsRaised))
            {
                if (slot1.WasPressedThisFrame()) Equip(HandItem.Pistol);
                if (slot2.WasPressedThisFrame()) Equip(HandItem.Rifle);
                if (slot3.WasPressedThisFrame()) Equip(HandItem.Bayonet);
                if (slot4.WasPressedThisFrame()) Equip(HandItem.Camera);
                float c = cycle.ReadValue<float>();
                if (Mathf.Abs(c) > 0.1f && cycle.WasPressedThisFrame()) Cycle(c > 0 ? -1 : 1);
                if (Current == HandItem.Bayonet && Cursor.lockState == CursorLockMode.Locked && stabAction.WasPressedThisFrame()) PlayStab();

                var kb = Keyboard.current;
                if (kb != null && kb.fKey.wasPressedThisFrame && Current != HandItem.Camera)
                {
                    if (Owns(HandItem.Camera)) Equip(HandItem.Camera);
                    else Popup("Nie masz jeszcze aparatu");
                }
            }

            ApplyEnabled();
            AnimateHolders();
        }

        // ------------------------------------------------------------------ slots

        static readonly HandItem[] Order = { HandItem.Pistol, HandItem.Rifle, HandItem.Bayonet, HandItem.Camera };

        public bool Owns(HandItem item) => item switch
        {
            HandItem.Pistol => WeaponCatalog.SelectedPistol != null,
            HandItem.Rifle => WeaponCatalog.SelectedRifle != null,
            HandItem.Bayonet => WeaponCatalog.SelectedMelee != null,
            HandItem.Camera => Inventory.Has(Inventory.Camera),
            _ => true,
        };

        public static int SlotNumber(HandItem item) => item switch
        {
            HandItem.Pistol => 1, HandItem.Rifle => 2, HandItem.Bayonet => 3, HandItem.Camera => 4, _ => 0,
        };

        void Cycle(int dir)
        {
            int idx = Array.IndexOf(Order, Current);
            for (int i = 1; i <= Order.Length; i++)
            {
                var next = Order[((idx + dir * i) % Order.Length + Order.Length) % Order.Length];
                if (Owns(next)) { Equip(next); return; }
            }
        }

        public void Equip(HandItem item)
        {
            if (item == Current || IsSwapping || !Owns(item)) return;
            StartCoroutine(Swap(item));
        }

        IEnumerator Swap(HandItem item)
        {
            IsSwapping = true;
            for (float t = 0f; t < swapLower; t += Time.deltaTime) { holderLower = t / swapLower; yield return null; }
            holderLower = 1f;
            Current = item;
            ShowCurrent();
            Equipped?.Invoke(item);
            SfxBank.Play(SfxBank.Sfx.ReloadEnd);
            for (float t = 0f; t < swapRaise; t += Time.deltaTime) { holderLower = 1f - t / swapRaise; yield return null; }
            holderLower = 0f;
            IsSwapping = false;
        }

        /// <summary>Loadout changed (start screen / armoury) — rebuild what is in the hands.</summary>
        void RefreshLoadout()
        {
            if (!Owns(Current)) Current = HandItem.Pistol;
            activeFirearm = null;
            ShowCurrent();
            Equipped?.Invoke(Current);
        }

        void ShowCurrent()
        {
            if (Current == HandItem.Pistol || Current == HandItem.Rifle)
            {
                var def = Current == HandItem.Pistol ? WeaponCatalog.SelectedPistol : WeaponCatalog.SelectedRifle;
                if (def != null && def != activeFirearm && weapon != null)
                {
                    var m = GetFirearm(def);
                    foreach (var f in firearms.Values) f.Root.gameObject.SetActive(f.Root == m.Root);
                    weapon.ApplyDefinition(def, m.Root, m.Muzzle, m.Hip, m.Aim);
                    activeFirearm = def;
                }
            }
            var melee = WeaponCatalog.SelectedMelee;
            if (melee != null)
            {
                var blade = GetBlade(melee);
                foreach (var b in blades.Values) b.gameObject.SetActive(b == blade);
                activeBlade = blade;
            }
        }

        void ApplyEnabled()
        {
            bool controls = (player == null || player.enabled) && !IsSwapping && !stabbing;
            bool gun = controls && (Current == HandItem.Pistol || Current == HandItem.Rifle);
            if (weapon != null && weapon.enabled != gun) weapon.enabled = gun;
            bool cam = controls && Current == HandItem.Camera;
            if (photo != null && photo.enabled != cam) photo.enabled = cam;
        }

        void AnimateHolders()
        {
            float ease = holderLower * holderLower;
            SetHolder(firearmHolder, (Current == HandItem.Pistol || Current == HandItem.Rifle) && !stabbing, ease);
            SetHolder(cameraHolder, Current == HandItem.Camera && !stabbing, ease);
            SetHolder(meleeHolder, Current == HandItem.Bayonet || stabbing, stabbing ? 0f : ease);

            if (cameraModel != null)
            {
                bool raised = photo != null && photo.IsRaised;
                Vector3 target = raised ? new Vector3(0f, -0.02f, 0.06f) : new Vector3(0.11f, -0.19f, 0.33f);
                Quaternion rot = raised ? Quaternion.identity : Quaternion.Euler(8f, -12f, 4f);
                float k = 1f - Mathf.Exp(-14f * Time.deltaTime);
                cameraModel.localPosition = Vector3.Lerp(cameraModel.localPosition, target, k);
                cameraModel.localRotation = Quaternion.Slerp(cameraModel.localRotation, rot, k);
            }
        }

        static void SetHolder(Transform holder, bool active, float lower)
        {
            if (holder == null) return;
            if (holder.gameObject.activeSelf != active) holder.gameObject.SetActive(active);
            holder.localPosition = new Vector3(0f, -0.3f * lower, -0.05f * lower);
            holder.localRotation = Quaternion.Euler(35f * lower, 0f, 0f);
        }

        // ------------------------------------------------------------------ blade

        /// <summary>Blade thrust. Also used by silent takedowns (TakedownTarget calls this).</summary>
        public void PlayStab()
        {
            if (stabbing || activeBlade == null) return;
            StartCoroutine(Stab());
        }

        IEnumerator Stab()
        {
            stabbing = true;
            var def = WeaponCatalog.SelectedMelee;
            float thrust = def != null ? Mathf.Clamp(def.TakedownTime * 0.35f, 0.06f, 0.12f) : 0.09f;
            var rest = new Vector3(0.17f, -0.2f, 0.32f);
            var restRot = Quaternion.Euler(8f, -8f, -12f);
            var lunge = new Vector3(0.04f, -0.1f, 0.68f);
            var lungeRot = Quaternion.Euler(-6f, -2f, -4f);
            for (float t = 0f; t < thrust; t += Time.deltaTime)
            {
                float k = t / thrust;
                activeBlade.localPosition = Vector3.Lerp(rest, lunge, k * k);
                activeBlade.localRotation = Quaternion.Slerp(restRot, lungeRot, k);
                yield return null;
            }
            activeBlade.localPosition = lunge;
            CameraShake.AddTrauma(0.12f);
            MeleeHit(def);
            yield return new WaitForSeconds(0.12f);
            for (float t = 0f; t < 0.22f; t += Time.deltaTime)
            {
                float k = t / 0.22f;
                activeBlade.localPosition = Vector3.Lerp(lunge, rest, k);
                activeBlade.localRotation = Quaternion.Slerp(lungeRot, restRot, k);
                yield return null;
            }
            activeBlade.localPosition = rest;
            activeBlade.localRotation = restRot;
            stabbing = false;
        }

        void MeleeHit(WeaponDef def)
        {
            // Direct stabs only; takedowns kill their own target.
            if (Current != HandItem.Bayonet) return;
            float range = def != null ? def.MeleeRange : 1.8f;
            if (!Physics.SphereCast(transform.position, 0.25f, transform.forward, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Ignore)) return;
            if (player != null && hit.collider.transform.IsChildOf(player.transform)) return;
            var health = hit.collider.GetComponentInParent<Health>();
            if (health == null || health.IsDead) { VfxLibrary.PlayImpact(hit.point, hit.normal, ImpactKind.Ground); return; }
            var ch = hit.collider.GetComponentInParent<Character>();
            health.TakeDamage(999f, hit.point, -transform.forward);
            VfxLibrary.PlayImpact(hit.point, hit.normal, ImpactKind.Flesh);
            Noise.Emit(transform.position, 3f, NoiseKind.Other, player != null ? player.transform : transform);
            if (ch != null && ch.IsInnocent) MissionFail.Trigger(MissionFail.ShotInnocent);
        }

        // ------------------------------------------------------------------ item hand-over

        void OnItemReceived(Inventory.Item item)
        {
            if (item.Id == Inventory.Camera) StartCoroutine(HandOver(cameraModel, HandItem.Camera));
            else if (item.Slot == 2 && WeaponCatalog.SelectedRifle != null) StartCoroutine(HandOver(GetFirearm(WeaponCatalog.SelectedRifle).Root, HandItem.Rifle));
        }

        IEnumerator HandOver(Transform template, HandItem equipAfter)
        {
            if (template == null) yield break;
            var copy = Instantiate(template.gameObject, transform).transform;
            copy.gameObject.SetActive(true);
            foreach (var r in copy.GetComponentsInChildren<Renderer>()) if (r.name.StartsWith("Hand") || r.name.StartsWith("Sleeve")) r.enabled = false;
            Vector3 start = new Vector3(0f, -0.02f, 0.6f);
            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 0.5f);
                copy.localPosition = start;
                copy.localScale = Vector3.one * k;
                copy.localRotation = Quaternion.Euler(10f, 360f * (1f - k) + 25f * Mathf.Sin(t * 6f), 0f);
                yield return null;
            }
            for (float t = 0f; t < 0.9f; t += Time.unscaledDeltaTime)
            {
                copy.localRotation = Quaternion.Euler(10f + 4f * Mathf.Sin(t * 5f), 25f * Mathf.Sin(t * 3f), 0f);
                yield return null;
            }
            Vector3 from = copy.localPosition;
            Vector3 to = new Vector3(0.25f, -0.35f, 0.3f);
            for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.35f;
                copy.localPosition = Vector3.Lerp(from, to, k * k);
                copy.localScale = Vector3.one * (1f - 0.6f * k);
                yield return null;
            }
            Destroy(copy.gameObject);
            Equip(equipAfter);
        }

        // ------------------------------------------------------------------ models

        FirearmModel GetFirearm(WeaponDef def)
        {
            if (firearms.TryGetValue(def.Id, out var m)) return m;
            var prefab = Spawner.HasPrefab(def.Prefab) ? Resources.Load<PrefabRegistry>("PrefabRegistry").Find(def.Prefab) : null;
            if (prefab != null)
            {
                var root = Instantiate(prefab, firearmHolder).transform;
                root.name = def.Id;
                foreach (var c in root.GetComponentsInChildren<Collider>()) Destroy(c);
                var muzzle = FindDeep(root, "Lufa") ?? root;
                // Assumes the art's sights sit on the root's local +Y ≈ the muzzle height.
                float sightY = muzzle.localPosition.y + 0.02f;
                m = new FirearmModel { Root = root, Muzzle = muzzle, Hip = new Vector3(0.15f, -0.15f, 0.3f), Aim = new Vector3(0f, -sightY, 0.2f) };
            }
            else m = def.Slot == WeaponSlot.Rifle ? BuildRifle(def) : BuildPistol(def);
            firearms[def.Id] = m;
            return m;
        }

        FirearmModel BuildPistol(WeaponDef def)
        {
            var root = new GameObject(def.Id + "_Placeholder").transform;
            root.SetParent(firearmHolder, false);
            Transform muzzle = new GameObject("Lufa").transform;
            muzzle.SetParent(root, false);
            float sightTop;

            if (def.Id == "nagant")
            {
                Part(root, "Barrel", PrimitiveType.Cylinder, new Vector3(0f, 0.008f, 0.07f), new Vector3(0.018f, 0.06f, 0.018f), steel, new Vector3(90f, 0f, 0f));
                Part(root, "Frame", PrimitiveType.Cube, new Vector3(0f, -0.006f, -0.02f), new Vector3(0.028f, 0.045f, 0.07f), steel);
                Part(root, "Drum", PrimitiveType.Cylinder, new Vector3(0f, -0.004f, -0.005f), new Vector3(0.042f, 0.02f, 0.042f), steel, new Vector3(90f, 0f, 0f));
                Part(root, "Hammer", PrimitiveType.Cube, new Vector3(0f, 0.016f, -0.06f), new Vector3(0.008f, 0.02f, 0.012f), steel, new Vector3(-30f, 0f, 0f));
                Part(root, "Grip", PrimitiveType.Cube, new Vector3(0f, -0.07f, -0.07f), new Vector3(0.028f, 0.1f, 0.04f), wood, new Vector3(-20f, 0f, 0f));
                Part(root, "FrontSight", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0.125f), new Vector3(0.004f, 0.009f, 0.008f), steel);
                Part(root, "RearSightL", PrimitiveType.Cube, new Vector3(-0.005f, 0.0195f, -0.045f), new Vector3(0.005f, 0.008f, 0.008f), steel);
                Part(root, "RearSightR", PrimitiveType.Cube, new Vector3(0.005f, 0.0195f, -0.045f), new Vector3(0.005f, 0.008f, 0.008f), steel);
                muzzle.localPosition = new Vector3(0f, 0.008f, 0.13f);
                sightTop = 0.0235f;
            }
            else
            {
                float s = def.Id == "browning" ? 0.82f : 1f;
                Part(root, "Slide", PrimitiveType.Cube, new Vector3(0f, 0f, 0.02f) * s, new Vector3(0.032f, 0.036f, 0.2f) * s, steel);
                Part(root, "Frame", PrimitiveType.Cube, new Vector3(0f, -0.026f, 0f) * s, new Vector3(0.03f, 0.02f, 0.16f) * s, steel);
                Part(root, "Grip", PrimitiveType.Cube, new Vector3(0f, -0.083f, -0.058f) * s, new Vector3(0.03f, 0.11f, 0.045f) * s, def.Id == "browning" ? black : wood, new Vector3(-16f, 0f, 0f));
                Part(root, "TriggerGuard", PrimitiveType.Cube, new Vector3(0f, -0.048f, -0.005f) * s, new Vector3(0.008f, 0.025f, 0.04f) * s, steel);
                Part(root, "FrontSight", PrimitiveType.Cube, new Vector3(0f, 0.022f, 0.112f) * s, new Vector3(0.005f, 0.008f, 0.008f), steel);
                Part(root, "RearSightL", PrimitiveType.Cube, new Vector3(-0.0065f, 0.0225f, -0.072f) * s, new Vector3(0.006f, 0.009f, 0.008f), steel);
                Part(root, "RearSightR", PrimitiveType.Cube, new Vector3(0.0065f, 0.0225f, -0.072f) * s, new Vector3(0.006f, 0.009f, 0.008f), steel);
                muzzle.localPosition = new Vector3(0f, 0.004f, 0.125f) * s;
                sightTop = 0.0265f * s;
            }
            Hands(root, -0.06f);
            return new FirearmModel { Root = root, Muzzle = muzzle, Hip = new Vector3(0.15f, -0.14f, 0.3f), Aim = new Vector3(0f, -sightTop, 0.2f) };
        }

        FirearmModel BuildRifle(WeaponDef def)
        {
            var root = new GameObject(def.Id + "_Placeholder").transform;
            root.SetParent(firearmHolder, false);
            bool smg = def.Id == "erma";
            float barrelEnd = smg ? 0.32f : 0.62f;

            Part(root, "Stock", PrimitiveType.Cube, new Vector3(0f, -0.035f, -0.28f), new Vector3(0.045f, 0.075f, 0.42f), wood, new Vector3(4f, 0f, 0f));
            Part(root, "Forend", PrimitiveType.Cube, new Vector3(0f, -0.018f, smg ? 0.08f : 0.2f), new Vector3(0.04f, 0.04f, smg ? 0.2f : 0.42f), wood);
            Part(root, "Receiver", PrimitiveType.Cube, new Vector3(0f, 0.002f, -0.04f), new Vector3(0.036f, 0.04f, 0.2f), steel);
            Part(root, "Barrel", PrimitiveType.Cylinder, new Vector3(0f, 0.012f, barrelEnd * 0.5f + 0.02f), new Vector3(0.018f, barrelEnd * 0.5f, 0.018f), steel, new Vector3(90f, 0f, 0f));
            if (smg)
            {
                Part(root, "SideMag", PrimitiveType.Cube, new Vector3(-0.07f, -0.005f, -0.01f), new Vector3(0.11f, 0.03f, 0.026f), steel);
                Part(root, "Jacket", PrimitiveType.Cylinder, new Vector3(0f, 0.012f, 0.16f), new Vector3(0.03f, 0.13f, 0.03f), black, new Vector3(90f, 0f, 0f));
            }
            else
            {
                Part(root, "Bolt", PrimitiveType.Cylinder, new Vector3(0.035f, 0.004f, -0.08f), new Vector3(0.012f, 0.025f, 0.012f), steel, new Vector3(0f, 0f, 90f));
                Part(root, "BoltKnob", PrimitiveType.Sphere, new Vector3(0.06f, 0.004f, -0.08f), new Vector3(0.018f, 0.018f, 0.018f), steel);
            }
            Part(root, "FrontSight", PrimitiveType.Cube, new Vector3(0f, 0.03f, barrelEnd), new Vector3(0.004f, 0.012f, 0.008f), steel);
            Part(root, "RearSightL", PrimitiveType.Cube, new Vector3(-0.006f, 0.0305f, 0.02f), new Vector3(0.006f, 0.011f, 0.01f), steel);
            Part(root, "RearSightR", PrimitiveType.Cube, new Vector3(0.006f, 0.0305f, 0.02f), new Vector3(0.006f, 0.011f, 0.01f), steel);
            Part(root, "Trigger", PrimitiveType.Cube, new Vector3(0f, -0.05f, -0.06f), new Vector3(0.008f, 0.03f, 0.035f), steel);
            var muzzle = new GameObject("Lufa").transform;
            muzzle.SetParent(root, false);
            muzzle.localPosition = new Vector3(0f, 0.012f, barrelEnd + 0.01f);

            Part(root, "HandR", PrimitiveType.Cube, new Vector3(0.005f, -0.07f, -0.1f), new Vector3(0.058f, 0.085f, 0.085f), skin, new Vector3(-10f, 0f, 0f));
            Part(root, "HandL", PrimitiveType.Cube, new Vector3(-0.01f, -0.05f, smg ? 0.1f : 0.18f), new Vector3(0.06f, 0.06f, 0.09f), skin);
            Part(root, "SleeveR", PrimitiveType.Cube, new Vector3(0.03f, -0.12f, -0.27f), new Vector3(0.075f, 0.075f, 0.26f), sleeve, new Vector3(-8f, 0f, 0f));
            Part(root, "SleeveL", PrimitiveType.Cube, new Vector3(-0.06f, -0.12f, smg ? -0.06f : 0.0f), new Vector3(0.07f, 0.07f, 0.28f), sleeve, new Vector3(-20f, 25f, 0f));
            // Aim: sight line (≈ y 0.036) on the camera axis, rear sight 0.14 m from the eye.
            return new FirearmModel { Root = root, Muzzle = muzzle, Hip = new Vector3(0.13f, -0.16f, 0.24f), Aim = new Vector3(0f, -0.036f, 0.12f) };
        }

        void Hands(Transform root, float gripZ)
        {
            Part(root, "HandR", PrimitiveType.Cube, new Vector3(0.004f, -0.09f, gripZ), new Vector3(0.058f, 0.085f, 0.085f), skin, new Vector3(-16f, 0f, 0f));
            Part(root, "HandL", PrimitiveType.Cube, new Vector3(-0.03f, -0.1f, gripZ + 0.015f), new Vector3(0.045f, 0.07f, 0.08f), skin, new Vector3(-10f, 0f, 20f));
            Part(root, "SleeveR", PrimitiveType.Cube, new Vector3(0.015f, -0.13f, gripZ - 0.17f), new Vector3(0.075f, 0.075f, 0.26f), sleeve, new Vector3(-8f, 0f, 0f));
        }

        Transform GetBlade(WeaponDef def)
        {
            if (blades.TryGetValue(def.Id, out var b)) return b;
            var root = new GameObject(def.Id + "_Placeholder").transform;
            root.SetParent(meleeHolder, false);
            root.localPosition = new Vector3(0.17f, -0.2f, 0.32f);
            root.localRotation = Quaternion.Euler(8f, -8f, -12f);
            if (def.Id == "noz")
            {
                Part(root, "Handle", PrimitiveType.Cube, new Vector3(0f, 0f, -0.04f), new Vector3(0.022f, 0.028f, 0.1f), black);
                Part(root, "Bolster", PrimitiveType.Cube, new Vector3(0f, 0.002f, 0.012f), new Vector3(0.026f, 0.03f, 0.008f), chrome);
                Part(root, "Blade", PrimitiveType.Cube, new Vector3(0f, 0.004f, 0.068f), new Vector3(0.005f, 0.022f, 0.11f), chrome);
            }
            else
            {
                Part(root, "Handle", PrimitiveType.Cube, new Vector3(0f, 0f, -0.05f), new Vector3(0.028f, 0.034f, 0.11f), wood);
                Part(root, "Guard", PrimitiveType.Cube, new Vector3(0f, 0.004f, 0.008f), new Vector3(0.07f, 0.012f, 0.012f), steel);
                Part(root, "Blade", PrimitiveType.Cube, new Vector3(0f, 0.004f, 0.14f), new Vector3(0.007f, 0.026f, 0.26f), chrome);
            }
            Part(root, "HandR", PrimitiveType.Cube, new Vector3(0f, -0.005f, -0.05f), new Vector3(0.06f, 0.07f, 0.09f), skin);
            Part(root, "SleeveR", PrimitiveType.Cube, new Vector3(0.01f, -0.03f, -0.22f), new Vector3(0.075f, 0.075f, 0.26f), sleeve);
            blades[def.Id] = root;
            return root;
        }

        void BuildCamera()
        {
            cameraHolder = Holder("Holder_Camera");
            cameraModel = new GameObject("Aparat_Placeholder").transform;
            cameraModel.SetParent(cameraHolder, false);
            cameraModel.localPosition = new Vector3(0.11f, -0.19f, 0.33f);
            Part(cameraModel, "Body", PrimitiveType.Cube, Vector3.zero, new Vector3(0.135f, 0.07f, 0.034f), leather);
            Part(cameraModel, "TopPlate", PrimitiveType.Cube, new Vector3(0f, 0.041f, 0f), new Vector3(0.135f, 0.014f, 0.034f), chrome);
            Part(cameraModel, "Lens", PrimitiveType.Cylinder, new Vector3(0.018f, -0.004f, 0.03f), new Vector3(0.042f, 0.014f, 0.042f), black, new Vector3(90f, 0f, 0f));
            Part(cameraModel, "LensRing", PrimitiveType.Cylinder, new Vector3(0.018f, -0.004f, 0.045f), new Vector3(0.036f, 0.004f, 0.036f), chrome, new Vector3(90f, 0f, 0f));
            Part(cameraModel, "Dial", PrimitiveType.Cylinder, new Vector3(-0.045f, 0.052f, 0f), new Vector3(0.02f, 0.006f, 0.02f), chrome);
            Part(cameraModel, "Window", PrimitiveType.Cube, new Vector3(-0.045f, 0.025f, 0.018f), new Vector3(0.016f, 0.012f, 0.002f), black);
            Part(cameraModel, "HandR", PrimitiveType.Cube, new Vector3(0.085f, -0.01f, -0.01f), new Vector3(0.05f, 0.085f, 0.06f), skin);
            Part(cameraModel, "HandL", PrimitiveType.Cube, new Vector3(-0.085f, -0.01f, -0.01f), new Vector3(0.05f, 0.085f, 0.06f), skin);
            Part(cameraModel, "SleeveR", PrimitiveType.Cube, new Vector3(0.1f, -0.06f, -0.17f), new Vector3(0.07f, 0.07f, 0.24f), sleeve, new Vector3(-10f, -10f, 0f));
            Part(cameraModel, "SleeveL", PrimitiveType.Cube, new Vector3(-0.1f, -0.06f, -0.17f), new Vector3(0.07f, 0.07f, 0.24f), sleeve, new Vector3(-10f, 10f, 0f));
        }

        Transform Holder(string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(transform, false);
            return t;
        }

        static void Part(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 size, Material mat, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = size;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static Material Mat(Color c, float smooth)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            return m;
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        static void Popup(string text)
        {
            var fb = FindAnyObjectByType<CombatFeedback>();
            if (fb != null) fb.AddPopup(text, new Color(0.95f, 0.85f, 0.7f));
        }
    }
}
