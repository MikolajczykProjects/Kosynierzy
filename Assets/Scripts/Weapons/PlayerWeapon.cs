using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kosynierzy
{
    /// <summary>
    /// The player's gun. Fires fast simulated bullets through <see cref="ProjectileSystem"/> from the camera
    /// centre (tracer starts at the muzzle). Fire LMB / RT, aim RMB / LT, reload R / X.
    /// Feel: view recoil (rotation only), spread bloom, ADS zoom, viewmodel kick, sway, bob,
    /// procedural reload and sprint poses, muzzle flash light + particles, camera trauma.
    /// The viewmodel is any child transform — the art team's gun model goes there.
    /// </summary>
    public class PlayerWeapon : MonoBehaviour
    {
        [Header("Bullet")]
        [SerializeField] float damage = 34f;
        [SerializeField] float bulletSpeed = 320f;
        [SerializeField] float bulletGravityScale = 0.5f;
        [SerializeField] float range = 300f;
        [SerializeField] float impulse = 4f;
        [SerializeField] Color tracerColor = new(1f, 0.8f, 0.45f, 1f);

        [Header("Fire")]
        [Tooltip("Shots per minute.")]
        [SerializeField] float fireRate = 420f;
        [Tooltip("Hold to keep firing; off = one shot per click.")]
        [SerializeField] bool automatic;
        [Tooltip("Team doc: LMB fires only while aiming (RMB).")]
        [SerializeField] bool requireAimToFire = true;
        [Tooltip("Radius in metres in which enemies hear the shot.")]
        [SerializeField] float shotNoiseRadius = 30f;
        [Tooltip("Spread in degrees: hip fire / aiming.")]
        [SerializeField] float hipSpread = 1.4f;
        [SerializeField] float aimSpread = 0.15f;
        [SerializeField] float movingSpreadPerSpeed = 0.25f;
        [SerializeField] float bloomPerShot = 0.5f;
        [SerializeField] float bloomRecovery = 4f;

        [Header("Recoil (degrees)")]
        [SerializeField] float recoilUp = 1.6f;
        [SerializeField] float recoilSide = 0.45f;
        [SerializeField] float aimRecoilMultiplier = 0.6f;
        [SerializeField] float shotTrauma = 0.07f;

        [Header("Ammo")]
        [SerializeField] int magazineSize = 8;
        [SerializeField] int reserveAmmo = 0;   // team doc: Vis wz. 35 starts with one magazine; +8 from the cache
        [SerializeField] float reloadTime = 1.5f;
        [SerializeField] float emptyReloadTime = 2f;

        [Header("Aim down sights")]
        [SerializeField] float aimFovMultiplier = 0.72f;
        [SerializeField] float aimSensitivityMultiplier = 0.65f;
        [SerializeField] float aimSpeed = 9f;

        // Muzzle flash size multiplier by view: hip / fully aimed (see Fire).
        const float HipFlashScale = 0.8f;
        const float AdsFlashScale = 0.4f;

        [Header("Viewmodel")]
        [Tooltip("Weapon model root; kicked, swayed and posed procedurally. Optional.")]
        [SerializeField] Transform viewModel;
        [SerializeField] Vector3 aimPosition = new(0f, -0.13f, 0.32f);
        [SerializeField] Vector3 sprintPosOffset = new(-0.05f, -0.07f, -0.05f);
        [SerializeField] Vector3 sprintRotOffset = new(38f, 12f, -10f); // muzzle down, close to the body
        [SerializeField] float kickBack = 0.07f;
        [SerializeField] float kickRotate = 5f;
        [SerializeField] float swayAmount = 1.6f;
        [SerializeField] float swayMax = 6f;
        [Tooltip("Local muzzle point; when empty the front of the viewmodel is used.")]
        [SerializeField] Transform muzzle;

        InputAction fireAction, aimAction, reloadAction;
        Camera cam;
        FirstPersonController player;
        Light flashLight;
        // Real team-D viewmodel glue (Animator triggers, ejection port, ADS hammer); null on placeholder models.
        ViewmodelRig rig;
        /// <summary>Real Vis: the last shot's slide animation (0.16 s) finishes before the reload animation starts.</summary>
        const float LastShotReloadAnimDelay = 0.17f;

        float nextFireTime, bloom, aimBlend;
        float reloadStart, reloadDuration;
        bool reloadWasEmpty;
        Vector3 restPos;
        Quaternion restRot;
        Vector3 kickPos, kickPosVel;
        Vector3 kickRot, kickRotVel;
        Vector2 sway;
        float flashUntil;

        public int AmmoInMagazine { get; private set; }
        public int ReserveAmmo => reserveAmmo;
        public int MagazineSize => magazineSize;
        public bool IsReloading { get; private set; }
        public float ReloadProgress => IsReloading ? Mathf.Clamp01((Time.time - reloadStart) / reloadDuration) : 0f;
        public bool IsAiming { get; private set; }
        public float CurrentSpread { get; private set; }

        public event Action Fired;
        public event Action DryFired;
        public event Action ReloadStarted;
        public event Action ReloadFinished;
        public event Action<int> AmmoAdded;

        void Awake()
        {
            requireAimToFire = false; // playtest 15.10: LMB must fire from the hip; scenes saved with true are overridden
            cam = GetComponentInParent<Camera>();
            player =GetComponentInParent<FirstPersonController>();
            AmmoInMagazine = magazineSize;
            if (viewModel != null)
            {
                restPos = viewModel.localPosition;
                restRot = viewModel.localRotation;
            }

            fireAction = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
            fireAction.AddBinding("<Gamepad>/rightTrigger");
            aimAction = new InputAction("Aim", InputActionType.Button, "<Mouse>/rightButton");
            aimAction.AddBinding("<Gamepad>/leftTrigger");
            reloadAction = new InputAction("Reload", InputActionType.Button, "<Keyboard>/r");
            reloadAction.AddBinding("<Gamepad>/buttonWest");

            var lightGo = new GameObject("MuzzleFlashLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0.2f, -0.1f, 0.9f);
            flashLight = lightGo.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.range = 8f;
            flashLight.intensity = 6f;
            flashLight.color = new Color(1f, 0.7f, 0.35f);
            flashLight.shadows = LightShadows.None;
            flashLight.enabled = false;
        }

        void OnEnable()
        {
            fireAction.Enable();
            aimAction.Enable();
            reloadAction.Enable();
        }

        void OnDisable()
        {
            fireAction.Disable();
            aimAction.Disable();
            reloadAction.Disable();
            IsReloading = false;
            IsAiming = false;
            if (player != null)
            {
                player.AimFovMultiplier = 1f;
                player.AimSensitivityMultiplier = 1f;
                player.SprintBlocked = false;
            }
        }

        void OnDestroy()
        {
            fireAction.Dispose();
            aimAction.Dispose();
            reloadAction.Dispose();
        }

        void Update()
        {
            if (GameState.IsPaused) return;
            float dt = Time.deltaTime;
            bool sprinting = player != null && (player.IsSprinting || player.IsSliding);
            bool inputActive = Cursor.lockState == CursorLockMode.Locked;

            if (IsReloading && Time.time - reloadStart >= reloadDuration) FinishReload();

            IsAiming = inputActive && aimAction.IsPressed() && !IsReloading && !(player != null && player.IsSprinting);
            aimBlend = Mathf.MoveTowards(aimBlend, IsAiming ? 1f : 0f, aimSpeed * dt);
            if (player != null)
            {
                float a = Mathf.SmoothStep(0f, 1f, aimBlend);
                player.AimFovMultiplier = Mathf.Lerp(1f, aimFovMultiplier, a);
                player.AimSensitivityMultiplier = Mathf.Lerp(1f, aimSensitivityMultiplier, a);
                player.SprintBlocked = IsAiming;
            }

            if (inputActive)
            {
                if (reloadAction.WasPressedThisFrame()) TryReload();

                bool wantsFire = automatic ? fireAction.IsPressed() : fireAction.WasPressedThisFrame();
                if (wantsFire && !IsReloading && !sprinting && (IsAiming || !requireAimToFire) && Time.time >= nextFireTime)
                {
                    if (AmmoInMagazine > 0) Fire();
                    else if (fireAction.WasPressedThisFrame())
                    {
                        DryFired?.Invoke();
                        TryReload();
                    }
                }
            }

            bloom = Mathf.MoveTowards(bloom, 0f, bloomRecovery * dt);
            float moving = player != null ? player.CurrentSpeed * movingSpreadPerSpeed : 0f;
            CurrentSpread = Mathf.Lerp(hipSpread, aimSpread, aimBlend) + bloom + moving * (1f - aimBlend * 0.7f);

            if (flashLight.enabled && Time.time > flashUntil) flashLight.enabled = false;
            AnimateViewModel(dt, sprinting);
        }

        void Fire()
        {
            nextFireTime = Time.time + 60f / fireRate;
            AmmoInMagazine--;

            Transform origin = cam != null ? cam.transform : transform;
            Vector2 offset = UnityEngine.Random.insideUnitCircle * CurrentSpread;
            Vector3 dir = Quaternion.AngleAxis(offset.x, origin.up) * Quaternion.AngleAxis(offset.y, origin.right) * origin.forward;
            Vector3 muzzlePos = MuzzlePosition();

            ProjectileSystem.Fire(new ShotInfo
            {
                Origin = origin.position,
                Direction = dir,
                VisualOrigin = muzzlePos,
                Speed = bulletSpeed,
                Damage = damage,
                GravityScale = bulletGravityScale,
                MaxDistance = range,
                Impulse = impulse,
                Owner = player != null ? player.transform : transform.root,
                FromPlayer = true,
                TracerColor = tracerColor,
            });

            bloom += bloomPerShot;
            float recoilMul = Mathf.Lerp(1f, aimRecoilMultiplier, aimBlend);
            if (player != null)
                player.AddRecoil(recoilUp * recoilMul * UnityEngine.Random.Range(0.85f, 1.15f),
                                 recoilSide * recoilMul * UnityEngine.Random.Range(-1f, 1f));
            CameraShake.AddTrauma(shotTrauma);
            Noise.Emit(origin.position, shotNoiseRadius, NoiseKind.Gunshot, transform.root);

            kickPosVel += new Vector3(0f, 0.15f, -kickBack * 30f) * recoilMul;
            kickRotVel += new Vector3(-kickRotate * 45f, UnityEngine.Random.Range(-1f, 1f) * kickRotate * 12f, 0f) * recoilMul;

            flashLight.enabled = true;
            flashUntil = Time.time + 0.045f;
            // The muzzle is only 0.3–0.75 m from the eye: at the authored size a pistol flash covers ~60 % of the screen
            // height at ADS (measured analytically, docs/WS4_gameplay_report.md), so the flash shrinks while aiming.
            float viewScale = Mathf.Lerp(HipFlashScale, AdsFlashScale, aimBlend);
            VfxLibrary.PlayMuzzleFlash(muzzlePos, origin.rotation, muzzleScale * viewScale);
            // Revolvers keep their casings in the drum; rifles (muzzleScale >= 1.3) throw the long casing.
            if (Definition == null || Definition.EjectsCasings)
                VfxLibrary.PlayShellEject(EjectionOrigin(muzzlePos, origin.rotation), origin.rotation, player != null ? player.Velocity : Vector3.zero, muzzleScale);

            // Real model: hammer + slide clip (Animator trigger "Strzal"). Our flash/casings above stay the single source.
            if (rig != null) rig.OnShot();

            Fired?.Invoke();
            if (AmmoInMagazine == 0) StartReload(LastShotReloadAnimDelay);
        }

        /// <summary>
        /// ShellCasings puts a casing 0.11 m behind, 2 cm right and 1.5 cm above the point it is given (in the rotation's axes).
        /// With a real model that has an ejection port (child "Wyrzut") the point is shifted so the casing leaves the port;
        /// otherwise it is the muzzle, as before.
        /// </summary>
        Vector3 EjectionOrigin(Vector3 muzzlePos, Quaternion rot)
        {
            if (rig == null || rig.Ejector == null) return muzzlePos;
            return rig.Ejector.position + rot * new Vector3(-0.02f, -0.015f, 0.11f);
        }

        Vector3 MuzzlePosition()
        {
            if (muzzle != null) return muzzle.position;
            if (viewModel != null) return viewModel.position + viewModel.forward * (viewModel.lossyScale.z * 0.5f + 0.02f);
            return cam != null ? cam.transform.position + cam.transform.forward * 0.5f : transform.position;
        }

        public void TryReload() => StartReload(0f);

        /// <param name="animDelay">Seconds before the reload CLIP starts on a real model (the logical reload timer starts now).</param>
        void StartReload(float animDelay)
        {
            if (IsReloading || reserveAmmo <= 0 || AmmoInMagazine >= magazineSize) return;
            IsReloading = true;
            reloadWasEmpty = AmmoInMagazine == 0;
            reloadDuration = reloadWasEmpty ? emptyReloadTime : reloadTime;
            reloadStart = Time.time;
            if (rig != null) rig.OnReloadStart(animDelay); // Animator trigger "Przeladuj" (Vis clip 1.8 s)
            ReloadStarted?.Invoke();
        }

        void FinishReload()
        {
            int taken = Mathf.Min(magazineSize - AmmoInMagazine, reserveAmmo);
            AmmoInMagazine += taken;
            reserveAmmo -= taken;
            IsReloading = false;
            ReloadFinished?.Invoke();
        }

        // Ammo kept per weapon so switching pistol <-> rifle doesn't refill anything.
        readonly System.Collections.Generic.Dictionary<string, (int mag, int reserve)> ammoById = new();
        float muzzleScale = 1f;

        public WeaponDef Definition { get; private set; }
        public event Action<WeaponDef> DefinitionChanged;

        /// <summary>
        /// Switch to another firearm from the catalog (loadout / armoury / hotbar): stats, ammo state,
        /// viewmodel and poses. The first time a weapon is used it starts with a full magazine + StartReserve.
        /// </summary>
        public void ApplyDefinition(WeaponDef def, Transform model, Transform muzzlePoint, Vector3 hipPosition, Vector3 aimPos)
        {
            if (Definition != null) ammoById[Definition.Id] = (AmmoInMagazine, reserveAmmo);
            Definition = def;
            damage = def.Damage; fireRate = def.FireRate; bulletSpeed = def.BulletSpeed; automatic = def.Automatic;
            hipSpread = def.HipSpread; aimSpread = def.AimSpread; recoilUp = def.RecoilUp; recoilSide = def.RecoilSide;
            reloadTime = def.ReloadTime; emptyReloadTime = def.EmptyReloadTime; shotNoiseRadius = def.NoiseRadius;
            magazineSize = def.Magazine; muzzleScale = def.MuzzleScale;
            ProceduralSfx.ShotProfile = def.Id; // placeholder gunshot that sounds like this weapon (SfxBank clips override it)
            if (ammoById.TryGetValue(def.Id, out var a)) { AmmoInMagazine = a.mag; reserveAmmo = a.reserve; }
            else { AmmoInMagazine = def.Magazine; reserveAmmo = def.StartReserve; }
            IsReloading = false;
            bloom = 0f;
            SetViewModel(model, muzzlePoint, hipPosition, aimPos);
            DefinitionChanged?.Invoke(def);
        }

        /// <summary>
        /// Swap in a viewmodel built at runtime (PlayerHands): hip pose, aim pose that puts the sights on the
        /// camera axis, and the muzzle point for tracers/flash.
        /// </summary>
        public void SetViewModel(Transform model, Transform muzzlePoint, Vector3 hipPosition, Vector3 aimPos)
        {
            viewModel = model;
            muzzle = muzzlePoint;
            rig = model != null ? model.GetComponent<ViewmodelRig>() : null;
            aimPosition = aimPos;
            restPos = hipPosition;
            restRot = Quaternion.identity;
            model.localPosition = hipPosition;
            model.localRotation = Quaternion.identity;
            // Poses tuned for the pistol viewmodel (override older values saved in the scene).
            sprintPosOffset = new Vector3(-0.05f, -0.07f, -0.05f);
            sprintRotOffset = new Vector3(38f, 12f, -10f);
            aimSpeed = 9f;
            aimFovMultiplier = 0.72f;
        }

        public void AddReserveAmmo(int amount)
        {
            if (amount <= 0) return;
            reserveAmmo += amount;
            AmmoAdded?.Invoke(amount);
        }

        void AnimateViewModel(float dt, bool sprinting)
        {
            if (viewModel == null) return;
            if (rig != null) rig.SetAim(aimBlend); // real model: flattens the cocked hammer while the sights are in use

            // Springs for kick (position and rotation).
            kickPosVel += (-kickPos * 260f - kickPosVel * 22f) * dt;
            kickPos += kickPosVel * dt;
            kickRotVel += (-kickRot * 220f - kickRotVel * 20f) * dt;
            kickRot += kickRotVel * dt;

            // Sway lags behind the look direction.
            Vector2 look = player != null ? player.LookDelta : Vector2.zero;
            Vector2 targetSway = Vector2.ClampMagnitude(-look * swayAmount, swayMax) * (1f - aimBlend * 0.8f);
            sway = Vector2.Lerp(sway, targetSway, 1f - Mathf.Exp(-10f * dt));

            // Walk bob on the weapon (separate from camera bob).
            Vector3 bob = Vector3.zero;
            if (player != null && player.IsGrounded && player.CurrentSpeed > 0.5f)
            {
                float t = Time.time * (player.IsSprinting ? 11f : 8f);
                float amp = Mathf.Clamp01(player.CurrentSpeed / 7f) * 0.012f * (1f - aimBlend * 0.85f);
                bob = new Vector3(Mathf.Cos(t * 0.5f) * amp, Mathf.Abs(Mathf.Sin(t * 0.5f)) * amp, 0f);
            }

            float a = Mathf.SmoothStep(0f, 1f, aimBlend);
            Vector3 pos = Vector3.Lerp(restPos, aimPosition, a) + bob + kickPos;
            Vector3 rotOffset = new Vector3(sway.y, sway.x, sway.x * 0.5f) + kickRot;

            float sprintPose = sprinting && !IsReloading ? 1f : 0f;
            sprintBlend = Mathf.Lerp(sprintBlend, sprintPose, 1f - Mathf.Exp(-9f * dt));
            pos += sprintPosOffset * sprintBlend;
            rotOffset += sprintRotOffset * sprintBlend;

            if (IsReloading)
            {
                // Dip down, roll the gun, come back up.
                float p = ReloadProgress;
                float dip = Mathf.Sin(p * Mathf.PI);
                pos += new Vector3(0f, -0.12f, -0.03f) * dip;
                rotOffset += new Vector3(25f, 0f, -35f) * dip;
            }

            viewModel.localPosition = pos;
            viewModel.localRotation = restRot * Quaternion.Euler(rotOffset);
        }

        float sprintBlend;
    }
}
