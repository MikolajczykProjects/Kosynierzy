using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// The "juice" hub: listens to combat, movement and quest events and turns them into feedback —
    /// hit markers, kill/headshot/streak popups, hit-stop on a lone kill, slow motion when an objective
    /// falls, camera trauma, damage direction, low-health heartbeat, footsteps and every SfxBank cue.
    /// CombatHud draws the visual state exposed here.
    /// </summary>
    public class CombatFeedback : MonoBehaviour
    {
        public struct Popup
        {
            public string Text;
            public Color Color;
            public float Born;
            public float Scale;
        }

        [SerializeField] float streakWindow = 3f;
        [SerializeField] float footstepDistance = 2.1f;
        [SerializeField] float lowHealthThreshold = 0.35f;

        public readonly List<Popup> Popups = new();
        public float LastHitTime { get; private set; } = -10f;
        public bool LastHitHeadshot { get; private set; }
        public bool LastHitKilled { get; private set; }
        public float LastDamageTime { get; private set; } = -10f;
        public Vector3 LastDamageDirection { get; private set; }
        public float QuestPunchTime { get; private set; } = -10f;
        public int Streak { get; private set; }
        public bool LowHealth => playerHealth != null && !playerHealth.IsDead && playerHealth.Normalized < lowHealthThreshold;

        FirstPersonController player;
        PlayerWeapon weapon;
        Health playerHealth;
        float lastKillTime = -10f, stepDistance, lastHeartbeat;
        Vector3 lastPos;

        public static readonly Color Gold = new(1f, 0.82f, 0.25f);
        public static readonly Color Blood = new(1f, 0.3f, 0.25f);

        void Start()
        {
            player = Object.FindAnyObjectByType<FirstPersonController>();
            weapon = Object.FindAnyObjectByType<PlayerWeapon>();
            if (player != null)
            {
                playerHealth = player.GetComponent<Health>();
                lastPos = player.transform.position;
                player.Jumped += OnJumped;
                player.Landed += OnLanded;
                player.SlideStarted += OnSlide;
            }
            if (playerHealth != null) playerHealth.Damaged += OnPlayerDamaged;
            if (weapon != null)
            {
                weapon.Fired += OnFired;
                weapon.DryFired += OnDryFired;
                weapon.ReloadStarted += OnReloadStarted;
                weapon.ReloadFinished += OnReloadFinished;
                weapon.AmmoAdded += OnAmmoAdded;
            }
            ProjectileSystem.AnyHit += OnBulletHit;
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.StepStarted += OnStepStarted;
                QuestManager.Instance.StepCompleted += OnStepCompleted;
                QuestManager.Instance.AllStepsCompleted += OnAllCompleted;
            }
        }

        void OnDestroy()
        {
            if (player != null)
            {
                player.Jumped -= OnJumped;
                player.Landed -= OnLanded;
                player.SlideStarted -= OnSlide;
            }
            if (playerHealth != null) playerHealth.Damaged -= OnPlayerDamaged;
            if (weapon != null)
            {
                weapon.Fired -= OnFired;
                weapon.DryFired -= OnDryFired;
                weapon.ReloadStarted -= OnReloadStarted;
                weapon.ReloadFinished -= OnReloadFinished;
                weapon.AmmoAdded -= OnAmmoAdded;
            }
            ProjectileSystem.AnyHit -= OnBulletHit;
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.StepStarted -= OnStepStarted;
                QuestManager.Instance.StepCompleted -= OnStepCompleted;
                QuestManager.Instance.AllStepsCompleted -= OnAllCompleted;
            }
        }

        public void AddPopup(string text, Color color, float scale = 1f)
        {
            Popups.Add(new Popup { Text = text, Color = color, Born = Time.unscaledTime, Scale = scale });
            if (Popups.Count > 5) Popups.RemoveAt(0);
        }

        void Update()
        {
            Popups.RemoveAll(p => Time.unscaledTime - p.Born > 1.6f);
            if (Time.time - lastKillTime > streakWindow) Streak = 0;

            if (player != null && !GameState.IsPaused)
            {
                Vector3 pos = player.transform.position;
                if (player.IsGrounded && !player.IsSliding)
                {
                    stepDistance += Vector3.ProjectOnPlane(pos - lastPos, Vector3.up).magnitude;
                    float stride = footstepDistance * (player.IsSprinting ? 1.25f : player.IsCrouching ? 0.7f : 1f);
                    if (stepDistance >= stride)
                    {
                        stepDistance = 0f;
                        SfxBank.Play(SfxBank.Sfx.Footstep);
                    }
                }
                lastPos = pos;
            }

            if (LowHealth && Time.unscaledTime - lastHeartbeat > 0.9f)
            {
                lastHeartbeat = Time.unscaledTime;
                SfxBank.Play(SfxBank.Sfx.LowHealthHeartbeat);
            }
        }

        void OnBulletHit(HitResult hit)
        {
            if (!hit.FromPlayer || hit.Health == null) return;
            LastHitTime = Time.unscaledTime;
            LastHitHeadshot = hit.Headshot;
            LastHitKilled = hit.Killed;

            if (!hit.Killed)
            {
                SfxBank.Play(hit.Headshot ? SfxBank.Sfx.Headshot : SfxBank.Sfx.Hit);
                return;
            }

            Streak = Time.time - lastKillTime <= streakWindow ? Streak + 1 : 1;
            lastKillTime = Time.time;

            if (hit.Headshot) AddPopup("STRZAŁ W GŁOWĘ", Gold, 1.15f);
            else AddPopup("WRÓG WYELIMINOWANY", Color.white);
            if (Streak == 2) AddPopup("PODWÓJNE!", Gold, 1.3f);
            else if (Streak == 3) AddPopup("POTRÓJNE!", Gold, 1.45f);
            else if (Streak >= 4) AddPopup($"SERIA x{Streak}!", Blood, 1.6f);

            SfxBank.Play(Streak >= 2 ? SfxBank.Sfx.MultiKill : SfxBank.Sfx.Kill);
            CameraShake.AddTrauma(hit.Headshot ? 0.2f : 0.14f);
            TimeControl.HitStop(hit.Headshot ? 0.09f : 0.06f);
        }

        void OnPlayerDamaged(float amount, Vector3 point, Vector3 normal)
        {
            LastDamageTime = Time.unscaledTime;
            LastDamageDirection = normal;
            CameraShake.AddTrauma(Mathf.Clamp(amount * 0.025f, 0.15f, 0.45f));
            SfxBank.Play(SfxBank.Sfx.PlayerHurt);
        }

        void OnFired() => SfxBank.Play(SfxBank.Sfx.Shot);
        void OnDryFired() => SfxBank.Play(SfxBank.Sfx.DryFire);
        void OnReloadStarted() => SfxBank.Play(SfxBank.Sfx.ReloadStart);
        void OnReloadFinished() => SfxBank.Play(SfxBank.Sfx.ReloadEnd);
        void OnJumped() => SfxBank.Play(SfxBank.Sfx.Jump);
        void OnLanded(float speed) => SfxBank.Play(SfxBank.Sfx.Land);
        void OnSlide() => SfxBank.Play(SfxBank.Sfx.Slide);

        void OnAmmoAdded(int amount)
        {
            AddPopup($"+{amount} NABOI", new Color(0.75f, 0.95f, 0.6f));
            SfxBank.Play(SfxBank.Sfx.Pickup);
        }

        void OnStepStarted(QuestObjective step) => QuestPunchTime = Time.unscaledTime;

        void OnStepCompleted(QuestObjective step)
        {
            QuestPunchTime = Time.unscaledTime;
            SfxBank.Play(SfxBank.Sfx.QuestStep);
            // The last enemy of a fight gets the cinematic moment.
            if (step is KillObjective) TimeControl.SlowMotion(0.7f, 0.3f, 0.4f);
        }

        void OnAllCompleted()
        {
            SfxBank.Play(SfxBank.Sfx.QuestComplete);
            TimeControl.SlowMotion(1f, 0.4f, 0.6f);
        }
    }
}
