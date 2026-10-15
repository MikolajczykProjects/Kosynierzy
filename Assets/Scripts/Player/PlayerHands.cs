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
        Renderer[] cameraRenderers;
        bool cameraHidden;
        InputAction slot1, slot2, slot3, slot4, cycle, stabAction;
        float holderLower; // 0 = in hands, 1 = lowered out of view
        bool stabbing;
        Material steel, wood, skin, sleeve, black, chrome, leather, brass;

        // Real (team D) viewmodels also carry the measured sight points (prefab root space) for the ADS self-check.
        struct FirearmModel
        {
            public Transform Root, Muzzle, Grip;
            public Vector3 Hip, Aim;
            public Vector3 SightFront, SightRear;
            public float RearmostZ;
            public bool HasSights;
        }
        readonly Dictionary<string, FirearmModel> firearms = new();
        readonly HashSet<string> aimChecked = new();
        Camera viewCam;
        ViewmodelRig cameraRig;

        // ---- real-prefab viewmodel constants (team D's OB_Pistolet_Vis / OB_Aparat; numbers from docs/PLAN_PACZKA_OBIEKTY.md §4 and the FBX data)
        const string RealCameraPrefab = "OB_Aparat";
        /// <summary>Where the pistol grip (child Uchwyt) sits at the hip, in camera space (where the placeholder grip was).</summary>
        static readonly Vector3 RealHipGrip = new Vector3(0.15f, -0.16f, 0.30f);
        /// <summary>Eye to rear sight at full ADS (the placeholder pistols use ≈ 0.13 m).</summary>
        const float EyeToRearSight = 0.13f;
        /// <summary>Metres the eye sits ABOVE the sight tops in ADS. 0 = sight line exactly on the camera axis (the spec).
        /// Vis caveat: its rear sight is a solid block as tall as the front post, so with 0 the post hides behind the block;
        /// 0.0015–0.002 makes the post tip show above the block (but breaks the 0.005 viewport check).</summary>
        const float AimEyeLift = 0f;
        /// <summary>Grip lean of a real pistol (top forward), degrees about X — the hand boxes follow it.</summary>
        const float RealGripLean = 16f;
        /// <summary>Sight z (front post centre, rear block centre) in prefab root space, measured from the FBX vertices
        /// (Assets/_Obiekty/Modele/OB_Pistolet_Vis.fbx, 15.10.2026). Sight TOP height is measured from the mesh at runtime.</summary>
        static readonly Dictionary<string, Vector2> SightZ = new() { ["OB_Pistolet_Vis"] = new Vector2(0.160f, -0.030f) };
        /// <summary>Parts that stick up near the sights but are not sights (hammer, trigger, magazine) and our own hands.</summary>
        static bool IsNotSightRenderer(string n) => n == "Kurek" || n == "Spust" || n == "Magazynek" || n.StartsWith("Hand") || n.StartsWith("Sleeve");
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
            viewCam = GetComponent<Camera>();
            if (viewCam == null) viewCam = GetComponentInParent<Camera>();

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
            if (photo == null) photo = GetComponent<PhotoCamera>();
            if (photo != null)
            {
                photo.PhotoTaken -= OnPhotoTaken; // never twice
                photo.PhotoTaken += OnPhotoTaken;
            }
        }

        void OnDisable()
        {
            foreach (var a in Actions) a.Disable();
            Inventory.ItemReceived -= OnItemReceived;
            WeaponCatalog.Changed -= RefreshLoadout;
            if (photo != null) photo.PhotoTaken -= OnPhotoTaken;
        }

        void OnDestroy()
        {
            if (photo != null) photo.PhotoTaken -= OnPhotoTaken;
            foreach (var a in Actions) a.Dispose();
            if (Instance == this) Instance = null;
        }

        /// <summary>Photo printed (PhotoCamera fires it one frame after the capture, so the flash is not in the picture).</summary>
        void OnPhotoTaken(Texture2D snapshot, string caption, bool counted)
        {
            if (cameraRig != null) cameraRig.PlayFlash();
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
                if (slot1.WasPressedThisFrame()) Toggle(HandItem.Pistol);
                if (slot2.WasPressedThisFrame()) Toggle(HandItem.Rifle);
                if (slot3.WasPressedThisFrame()) Toggle(HandItem.Bayonet);
                if (slot4.WasPressedThisFrame()) Toggle(HandItem.Camera);
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

        /// <summary>Slot key: same slot as the item in hand = holster (empty hands); otherwise draw it.</summary>
        void Toggle(HandItem item)
        {
            if (IsSwapping || !Owns(item)) return;
            Equip(item == Current ? HandItem.Empty : item);
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
                    if (m.HasSights && Debug.isDebugBuild) StartCoroutine(AimSelfCheck(def.Id, m));
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
                // Raised = looking through the viewfinder: the body would sit 0.06 m from the eye (the near plane is 0.05)
                // and fill the screen with black, so the model is hidden while the viewfinder UI is up.
                if (raised != cameraHidden && cameraRenderers != null)
                {
                    cameraHidden = raised;
                    foreach (var r in cameraRenderers)
                        if (r != null) r.enabled = !raised;
                }
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
            var prefab = FindTeamPrefab(def.Prefab);
            if (prefab != null) m = BuildRealFirearm(def, prefab);
            else m = def.Slot == WeaponSlot.Rifle ? BuildRifle(def) : BuildPistol(def);
            firearms[def.Id] = m;
            return m;
        }

        /// <summary>Team prefab from the registry, or null (missing registry / prefab → caller builds a placeholder).</summary>
        static GameObject FindTeamPrefab(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName) || !Spawner.HasPrefab(prefabName)) return null;
            var registry = Resources.Load<PrefabRegistry>("PrefabRegistry");
            return registry != null ? registry.Find(prefabName) : null;
        }

        /// <summary>
        /// Real team-D firearm (OB_Pistolet_Vis …): pivot in the grip, +Z forward, children Lufa (muzzle), Uchwyt (grip),
        /// Wyrzut (ejection port). Hip pose puts Uchwyt where the placeholder grip was; the ADS pose is derived from the
        /// real sights (top of the top-most non-hammer mesh = sight tops), our hands/sleeve are added around the grip.
        /// </summary>
        FirearmModel BuildRealFirearm(WeaponDef def, GameObject prefab)
        {
            var root = Instantiate(prefab, firearmHolder).transform;
            root.name = def.Id;
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;
            PrepareViewmodel(root, firearmHolder.gameObject.layer);

            var muzzle = FindDeep(root, "Lufa");
            if (muzzle == null)
            {
                Debug.LogWarning($"Kosynierzy: {def.Prefab} has no child 'Lufa' — muzzle = model root.");
                muzzle = root;
            }
            var grip = FindDeep(root, "Uchwyt");
            var rig = root.gameObject.AddComponent<ViewmodelRig>();
            rig.Setup(root);

            var m = new FirearmModel { Root = root, Muzzle = muzzle, Grip = grip, Hip = new Vector3(0.15f, -0.15f, 0.3f) };

            if (MeasureSights(root, out float sightTop, out float rearmostZ))
            {
                float frontZ, rearZ;
                if (SightZ.TryGetValue(def.Prefab, out var known)) { frontZ = known.x; rearZ = known.y; }
                else
                {
                    // Unknown model: front sight ≈ 1 cm behind the muzzle, rear sight ≈ 2 cm in front of the rearmost mesh point.
                    frontZ = root.InverseTransformPoint(muzzle.position).z - 0.01f;
                    rearZ = rearmostZ + 0.02f;
                }
                m.SightFront = new Vector3(0f, sightTop, frontZ);
                m.SightRear = new Vector3(0f, sightTop, rearZ);
                m.RearmostZ = rearmostZ;
                m.HasSights = true;
                // Root in camera space so that the sight tops (x 0, y sightTop) lie on the camera axis, rear sight EyeToRearSight away.
                m.Aim = new Vector3(0f, -sightTop - AimEyeLift, EyeToRearSight - rearZ);
            }
            else
            {
                Debug.LogWarning($"Kosynierzy: {def.Prefab} — no meshes to measure sights from; using the old muzzle-height guess for ADS.");
                float guessY = root.InverseTransformPoint(muzzle.position).y + 0.02f;
                m.Aim = new Vector3(0f, -guessY, 0.2f);
            }

            if (grip != null)
            {
                Vector3 gripLocal = root.InverseTransformPoint(grip.position);
                m.Hip = RealHipGrip - gripLocal; // root rotation is identity at the hip, so Uchwyt lands exactly on RealHipGrip
                if (def.Slot == WeaponSlot.Pistol) RealHands(root, gripLocal);
            }
            return m;
        }

        /// <summary>Viewmodel hygiene shared by every real model: no colliders, no shadows, one layer.</summary>
        static void PrepareViewmodel(Transform root, int layer)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            foreach (var c in root.GetComponentsInChildren<Collider>(true)) { c.enabled = false; Destroy(c); }
            foreach (var r in root.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>
        /// Top of the sights = highest point of the meshes that are not hammer/trigger/magazine/hands (the slide on the Vis:
        /// measured from the mesh bounds, so it follows a re-exported model), plus the rearmost z. Root space.
        /// </summary>
        static bool MeasureSights(Transform root, out float topY, out float rearmostZ)
        {
            topY = float.MinValue;
            rearmostZ = float.MaxValue;
            bool any = false;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null || IsNotSightRenderer(mf.name)) continue;
                Bounds b = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    Vector3 p = root.InverseTransformPoint(mf.transform.TransformPoint(corner));
                    if (p.y > topY) topY = p.y;
                    if (p.z < rearmostZ) rearmostZ = p.z;
                    any = true;
                }
            }
            return any;
        }

        /// <summary>The real pistol has no hands: skin boxes and a sleeve around the grip (same offsets as the placeholder's).</summary>
        void RealHands(Transform root, Vector3 grip)
        {
            Part(root, "HandR", PrimitiveType.Cube, grip + new Vector3(0.004f, -0.007f, -0.002f), new Vector3(0.058f, 0.085f, 0.085f), skin, new Vector3(RealGripLean, 0f, 0f));
            Part(root, "HandL", PrimitiveType.Cube, grip + new Vector3(-0.03f, -0.017f, 0.013f), new Vector3(0.045f, 0.07f, 0.08f), skin, new Vector3(-10f, 0f, 20f));
            Part(root, "SleeveR", PrimitiveType.Cube, grip + new Vector3(0.015f, -0.047f, -0.172f), new Vector3(0.075f, 0.075f, 0.26f), sleeve, new Vector3(-8f, 0f, 0f));
        }

        /// <summary>
        /// Editor/dev builds: once per weapon, when it is in the hands and the holder is up, project the sight tops with the
        /// camera at the ADS pose and log the viewport offsets (|x−0.5|,|y−0.5| must be &lt; 0.005), the distances, the depth of
        /// the rearmost mesh point (must exceed the camera near plane) and where the grip is at the hip.
        /// </summary>
        IEnumerator AimSelfCheck(string id, FirearmModel m)
        {
            if (viewCam == null || m.Root == null || !aimChecked.Add(id)) yield break;
            for (int i = 0; i < 600 && (IsSwapping || holderLower > 0.001f); i++) yield return null;
            yield return null;
            if (m.Root == null) yield break;

            Vector3 pos0 = m.Root.localPosition;
            Quaternion rot0 = m.Root.localRotation;
            m.Root.localRotation = Quaternion.identity;
            m.Root.localPosition = m.Aim;
            Vector3 front = viewCam.WorldToViewportPoint(m.Root.TransformPoint(m.SightFront));
            Vector3 rear = viewCam.WorldToViewportPoint(m.Root.TransformPoint(m.SightRear));
            float rearmostDepth = viewCam.transform.InverseTransformPoint(m.Root.TransformPoint(new Vector3(0f, m.SightRear.y, m.RearmostZ))).z;
            m.Root.localPosition = m.Hip;
            Vector3 gripAtHip = m.Grip != null ? viewCam.transform.InverseTransformPoint(m.Grip.position) : Vector3.zero;
            m.Root.localPosition = pos0;
            m.Root.localRotation = rot0;

            float worst = Mathf.Max(Mathf.Abs(front.x - 0.5f), Mathf.Abs(front.y - 0.5f), Mathf.Abs(rear.x - 0.5f), Mathf.Abs(rear.y - 0.5f));
            bool ok = worst < 0.005f && rearmostDepth > viewCam.nearClipPlane;
            string text = $"[Viewmodel] {id} ADS check: front sight vp=({front.x:F4},{front.y:F4}) dist={front.z:F3} m | rear sight vp=({rear.x:F4},{rear.y:F4}) dist={rear.z:F3} m"
                        + $" | worst offset {worst:F4} (limit 0.005) | rearmost mesh depth {rearmostDepth:F3} m (near clip {viewCam.nearClipPlane:F2})"
                        + $" | hip: Uchwyt in camera space ({gripAtHip.x:F3},{gripAtHip.y:F3},{gripAtHip.z:F3}) | eye lift {AimEyeLift * 1000f:F1} mm"
                        + (ok ? " => PASS" : " => FAIL");
            if (ok) Debug.Log(text); else Debug.LogWarning(text);
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
                Part(root, "RearSightL", PrimitiveType.Cube, new Vector3(-0.005f, 0.0205f, -0.045f), new Vector3(0.005f, 0.008f, 0.008f), steel);
                Part(root, "RearSightR", PrimitiveType.Cube, new Vector3(0.005f, 0.0205f, -0.045f), new Vector3(0.005f, 0.008f, 0.008f), steel);
                muzzle.localPosition = new Vector3(0f, 0.008f, 0.13f);
                sightTop = 0.0245f; // front blade top (0.02 + 0.0045) = rear post tops (0.0205 + 0.004)
            }
            else
            {
                float s = def.Id == "browning" ? 0.82f : 1f;
                // The sights are not scaled with the gun, so derive the rear posts from the front blade: both end at the
                // same height (frontTop) and the ADS pose puts exactly that height on the camera axis.
                float frontTop = 0.022f * s + 0.004f;
                float rearY = frontTop - 0.0045f;
                Part(root, "Slide", PrimitiveType.Cube, new Vector3(0f, 0f, 0.02f) * s, new Vector3(0.032f, 0.036f, 0.2f) * s, steel);
                Part(root, "Frame", PrimitiveType.Cube, new Vector3(0f, -0.026f, 0f) * s, new Vector3(0.03f, 0.02f, 0.16f) * s, steel);
                Part(root, "Grip", PrimitiveType.Cube, new Vector3(0f, -0.083f, -0.058f) * s, new Vector3(0.03f, 0.11f, 0.045f) * s, def.Id == "browning" ? black : wood, new Vector3(-16f, 0f, 0f));
                Part(root, "TriggerGuard", PrimitiveType.Cube, new Vector3(0f, -0.048f, -0.005f) * s, new Vector3(0.008f, 0.025f, 0.04f) * s, steel);
                Part(root, "FrontSight", PrimitiveType.Cube, new Vector3(0f, 0.022f, 0.112f) * s, new Vector3(0.005f, 0.008f, 0.008f), steel);
                Part(root, "RearSightL", PrimitiveType.Cube, new Vector3(-0.0065f * s, rearY, -0.072f * s), new Vector3(0.006f, 0.009f, 0.008f), steel);
                Part(root, "RearSightR", PrimitiveType.Cube, new Vector3(0.0065f * s, rearY, -0.072f * s), new Vector3(0.006f, 0.009f, 0.008f), steel);
                muzzle.localPosition = new Vector3(0f, 0.004f, 0.125f) * s;
                sightTop = frontTop;
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
            // Starts right behind the rear sight (z -0.065): at ADS the eye is 0.12 m behind the root, so a longer receiver
            // would cross the 0.05 m near plane and show a see-through hole at the bottom of the sight picture.
            Part(root, "Receiver", PrimitiveType.Cube, new Vector3(0f, 0.002f, 0f), new Vector3(0.036f, 0.04f, 0.13f), steel);
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
            // Sight tops sit at y 0.036 (the ADS pose lifts exactly that onto the camera axis); the blades reach down to the
            // barrel (top 0.021) and the receiver (top 0.022) so nothing floats.
            Part(root, "FrontSight", PrimitiveType.Cube, new Vector3(0f, 0.0285f, barrelEnd), new Vector3(0.004f, 0.015f, 0.008f), steel);
            Part(root, "RearSightL", PrimitiveType.Cube, new Vector3(-0.006f, 0.029f, 0.02f), new Vector3(0.006f, 0.014f, 0.01f), steel);
            Part(root, "RearSightR", PrimitiveType.Cube, new Vector3(0.006f, 0.029f, 0.02f), new Vector3(0.006f, 0.014f, 0.01f), steel);
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
            var realCamera = FindTeamPrefab(RealCameraPrefab);
            cameraModel = new GameObject(realCamera != null ? "Aparat" : "Aparat_Placeholder").transform;
            cameraModel.SetParent(cameraHolder, false);
            cameraModel.localPosition = new Vector3(0.11f, -0.19f, 0.33f);
            if (realCamera != null)
            {
                // OB_Aparat: pivot at the bottom centre, lens +Z (child Obiektyw/FX_FlashAparatu), body (korpus) centre at y 0.031.
                // Shifted so the body centre is the cameraModel origin — the placeholder body's origin — and our hands fit both.
                var real = Instantiate(realCamera, cameraModel).transform;
                real.name = RealCameraPrefab;
                real.localPosition = new Vector3(0f, -0.031f, 0f);
                real.localRotation = Quaternion.identity;
                real.localScale = Vector3.one;
                PrepareViewmodel(real, cameraHolder.gameObject.layer);
                cameraRig = real.gameObject.AddComponent<ViewmodelRig>();
                cameraRig.Setup(real);
            }
            else
            {
                Part(cameraModel, "Body", PrimitiveType.Cube, Vector3.zero, new Vector3(0.135f, 0.07f, 0.034f), leather);
                Part(cameraModel, "TopPlate", PrimitiveType.Cube, new Vector3(0f, 0.041f, 0f), new Vector3(0.135f, 0.014f, 0.034f), chrome);
                Part(cameraModel, "Lens", PrimitiveType.Cylinder, new Vector3(0.018f, -0.004f, 0.03f), new Vector3(0.042f, 0.014f, 0.042f), black, new Vector3(90f, 0f, 0f));
                Part(cameraModel, "LensRing", PrimitiveType.Cylinder, new Vector3(0.018f, -0.004f, 0.045f), new Vector3(0.036f, 0.004f, 0.036f), chrome, new Vector3(90f, 0f, 0f));
                Part(cameraModel, "Dial", PrimitiveType.Cylinder, new Vector3(-0.045f, 0.052f, 0f), new Vector3(0.02f, 0.006f, 0.02f), chrome);
                Part(cameraModel, "Window", PrimitiveType.Cube, new Vector3(-0.045f, 0.025f, 0.018f), new Vector3(0.016f, 0.012f, 0.002f), black);
            }
            Part(cameraModel, "HandR", PrimitiveType.Cube, new Vector3(0.085f, -0.01f, -0.01f), new Vector3(0.05f, 0.085f, 0.06f), skin);
            Part(cameraModel, "HandL", PrimitiveType.Cube, new Vector3(-0.085f, -0.01f, -0.01f), new Vector3(0.05f, 0.085f, 0.06f), skin);
            Part(cameraModel, "SleeveR", PrimitiveType.Cube, new Vector3(0.1f, -0.06f, -0.17f), new Vector3(0.07f, 0.07f, 0.24f), sleeve, new Vector3(-10f, -10f, 0f));
            Part(cameraModel, "SleeveL", PrimitiveType.Cube, new Vector3(-0.1f, -0.06f, -0.17f), new Vector3(0.07f, 0.07f, 0.24f), sleeve, new Vector3(-10f, 10f, 0f));
            // Hidden while the viewfinder is up (AnimateHolders) — but not the flash particles: they play exactly then.
            var hideWhenRaised = new List<Renderer>();
            foreach (var r in cameraModel.GetComponentsInChildren<Renderer>(true)) if (!(r is ParticleSystemRenderer)) hideWhenRaised.Add(r);
            cameraRenderers = hideWhenRaised.ToArray();
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
