using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Placeholder HUD drawn with IMGUI (no prefabs needed). Shows: dynamic crosshair (opens with spread),
    /// hit marker with punch (gold headshot, red kill), kill/streak popups, damage direction arc,
    /// damage flash + low-health pulse, ammo (low-ammo warning), reload bar, stamina bar, player HP,
    /// current quest step (punches on change) with objective marker + distance, [E] prompt, banners.
    /// The UI team's real HUD can bind to the same public members (PlayerWeapon, FirstPersonController,
    /// Health, QuestManager, PlayerInteractor, CombatFeedback).
    /// </summary>
    [RequireComponent(typeof(CombatFeedback))]
    public class CombatHud : MonoBehaviour
    {
        [SerializeField] float stepBannerTime = 3f;
        [SerializeField] Color crosshairColor = new(1f, 1f, 1f, 0.85f);
        [SerializeField] Color markerColor = new(1f, 0.82f, 0.25f, 1f);

        CombatFeedback fb;
        PlayerWeapon weapon;
        PlayerInteractor interactor;
        FirstPersonController player;
        Health playerHealth;
        Camera cam;
        float bannerUntil;
        string bannerText;
        GUIStyle label, small, big, popup;
        Texture2D white, vignette;

        void Start()
        {
            fb = GetComponent<CombatFeedback>();
            white = Texture2D.whiteTexture;
            vignette = MakeVignette(128);
            weapon = Object.FindAnyObjectByType<PlayerWeapon>();
            interactor = Object.FindAnyObjectByType<PlayerInteractor>();
            player = Object.FindAnyObjectByType<FirstPersonController>();
            if (player != null)
            {
                playerHealth = player.GetComponent<Health>();
                cam = player.GetComponentInChildren<Camera>();
            }
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.StepStarted += OnStepStarted;
                QuestManager.Instance.StepCompleted += OnStepCompleted;
                QuestManager.Instance.AllStepsCompleted += OnAllCompleted;
                if (QuestManager.Instance.Current != null) OnStepStarted(QuestManager.Instance.Current);
            }
        }

        void OnDestroy()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.StepStarted -= OnStepStarted;
                QuestManager.Instance.StepCompleted -= OnStepCompleted;
                QuestManager.Instance.AllStepsCompleted -= OnAllCompleted;
            }
        }

        void OnStepStarted(QuestObjective step) => Banner($"NOWE ZADANIE: {step.Title}");
        void OnStepCompleted(QuestObjective step) => Banner($"WYKONANO: {step.Title}");
        void OnAllCompleted() => Banner("MISJA ZAKOŃCZONA");

        void Banner(string text)
        {
            bannerText = text;
            bannerUntil = Time.unscaledTime + stepBannerTime;
        }

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout) return;
            // The uGUI menus/boards sit under IMGUI, so step aside while they are open.
            if (GameState.IsPaused || HistoryCardView.IsOpen || MissionFail.IsFailed || Epilogue.IsPlaying) return;
            // Lay out in a 1080-unit-high virtual canvas scaled to the screen.
            float scale = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale, h = 1080f;
            float now = Time.unscaledTime;

            label ??= new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, wordWrap = true };
            small ??= new GUIStyle(label) { fontSize = 20, fontStyle = FontStyle.Normal };
            big ??= new GUIStyle(label) { fontSize = 40 };
            popup ??= new GUIStyle(label) { fontSize = 30, wordWrap = false };

            DrawScreenEffects(w, h, now);
            DrawObjectiveMarker(scale, w, h);
            DrawCrosshair(w * 0.5f, h * 0.5f, now);
            DrawDamageDirection(w * 0.5f, h * 0.5f, now);
            DrawPopups(w, h, now);
            DrawQuest(now);

            if (now < bannerUntil)
            {
                float age = stepBannerTime - (bannerUntil - now);
                float punch = 1f + 0.25f * Mathf.Exp(-age * 8f);
                big.fontSize = Mathf.RoundToInt(40 * punch);
                Shadowed(new Rect(0f, h * 0.2f, w, 70f), bannerText, big, TextAnchor.MiddleCenter);
            }

            if (interactor != null && interactor.CurrentTarget != null)
                Shadowed(new Rect(0f, h * 0.5f + 70f, w, 40f), $"E — {interactor.CurrentTarget.Prompt}", label, TextAnchor.MiddleCenter);

            DrawWeaponAndVitals(w, h);
        }

        void DrawScreenEffects(float w, float h, float now)
        {
            float dmg = Mathf.Clamp01(1f - (now - fb.LastDamageTime) / 0.4f);
            float pulse = fb.LowHealth ? 0.35f + 0.25f * Mathf.Sin(now * 7f) : 0f;
            float a = Mathf.Max(dmg * 0.7f, pulse);
            if (a > 0.01f)
            {
                GUI.color = new Color(0.65f, 0f, 0f, a);
                GUI.DrawTexture(new Rect(0f, 0f, w, h), vignette);
                GUI.color = Color.white;
            }
        }

        void DrawCrosshair(float cx, float cy, float now)
        {
            bool aiming = weapon != null && weapon.IsAiming;
            float spread = weapon != null ? weapon.CurrentSpread : 1f;
            float gap = 5f + spread * 9f;
            const float len = 10f, t = 2f;
            GUI.color = new Color(crosshairColor.r, crosshairColor.g, crosshairColor.b, aiming ? 0.35f : crosshairColor.a);
            GUI.DrawTexture(new Rect(cx - gap - len, cy - t * 0.5f, len, t), white);
            GUI.DrawTexture(new Rect(cx + gap, cy - t * 0.5f, len, t), white);
            GUI.DrawTexture(new Rect(cx - t * 0.5f, cy - gap - len, t, len), white);
            GUI.DrawTexture(new Rect(cx - t * 0.5f, cy + gap, t, len), white);
            GUI.DrawTexture(new Rect(cx - 1.5f, cy - 1.5f, 3f, 3f), white);

            // Hit marker: diagonal ticks that punch out and fade.
            float age = now - fb.LastHitTime;
            float dur = fb.LastHitKilled ? 0.45f : 0.2f;
            if (age < dur)
            {
                float k = 1f - age / dur;
                float size = (fb.LastHitKilled ? 16f : 10f) * (1f + 0.6f * Mathf.Exp(-age * 25f));
                Color c = fb.LastHitKilled ? CombatFeedback.Blood : fb.LastHitHeadshot ? CombatFeedback.Gold : Color.white;
                GUI.color = new Color(c.r, c.g, c.b, k);
                float thick = fb.LastHitKilled ? 4f : 3f;
                for (int i = 0; i < 4; i++)
                {
                    float ang = 45f + 90f * i;
                    var pivot = new Vector2(cx, cy);
                    var m = GUI.matrix;
                    GUIUtility.RotateAroundPivot(ang, pivot * GUI.matrix.m00);
                    GUI.DrawTexture(new Rect(cx + 8f, cy - thick * 0.5f, size, thick), white);
                    GUI.matrix = m;
                }
            }
            GUI.color = Color.white;
        }

        void DrawDamageDirection(float cx, float cy, float now)
        {
            float age = now - fb.LastDamageTime;
            if (age > 1.2f || cam == null) return;
            Vector3 toAttacker = fb.LastDamageDirection;
            Vector3 local = cam.transform.InverseTransformDirection(toAttacker);
            float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            GUI.color = new Color(1f, 0.15f, 0.1f, Mathf.Clamp01(1f - age / 1.2f) * 0.85f);
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, new Vector2(cx, cy) * GUI.matrix.m00);
            GUI.DrawTexture(new Rect(cx - 45f, cy - 150f, 90f, 10f), white);
            GUI.DrawTexture(new Rect(cx - 25f, cy - 162f, 50f, 8f), white);
            GUI.matrix = m;
            GUI.color = Color.white;
        }

        void DrawPopups(float w, float h, float now)
        {
            float y = h * 0.5f + 110f;
            for (int i = fb.Popups.Count - 1; i >= 0; i--)
            {
                var p = fb.Popups[i];
                float age = now - p.Born;
                float alpha = Mathf.Clamp01(1.6f - age) ;
                float punch = 1f + 0.5f * Mathf.Exp(-age * 12f);
                popup.fontSize = Mathf.RoundToInt(28 * p.Scale * punch);
                var c = p.Color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                var style = popup;
                style.normal.textColor = c;
                Shadowed(new Rect(0f, y - age * 18f, w, 44f), p.Text, style, TextAnchor.MiddleCenter);
                y += 40f;
            }
            popup.normal.textColor = Color.white;
            GUI.color = Color.white;
        }

        void DrawQuest(float now)
        {
            var mission = Mission.Instance;
            if (mission != null)
            {
                DrawMission(mission, now);
                return;
            }
            var quests = QuestManager.Instance;
            if (quests == null || quests.Current == null) return;
            var step = quests.Current;
            float punch = 1f + 0.2f * Mathf.Exp(-(now - fb.QuestPunchTime) * 6f);
            label.fontSize = Mathf.RoundToInt(26 * punch);
            string progress = step.ProgressText != null ? $"  ({step.ProgressText})" : "";
            Shadowed(new Rect(30f, 30f, 680f, 44f), $"ZADANIE: {step.Title}{progress}", label, TextAnchor.UpperLeft);
            label.fontSize = 26;
            if (!string.IsNullOrEmpty(step.Description))
                Shadowed(new Rect(30f, 74f, 620f, 90f), step.Description, small, TextAnchor.UpperLeft);
        }

        void DrawMission(Mission mission, float now)
        {
            float w = Screen.width / (Screen.height / 1080f);
            // Top-left: objective. Top-right: clock + collections.
            var step = mission.Current;
            if (step != null && !string.IsNullOrEmpty(step.Title))
            {
                float punch = 1f + 0.2f * Mathf.Exp(-(now - fb.QuestPunchTime) * 6f);
                small.fontSize = 18;
                Shadowed(new Rect(30f, 22f, 680f, 28f), mission.CurrentActName.ToUpperInvariant(), small, TextAnchor.UpperLeft);
                label.fontSize = Mathf.RoundToInt(26 * punch);
                Shadowed(new Rect(30f, 48f, 760f, 70f), step.Title, label, TextAnchor.UpperLeft);
                label.fontSize = 26;
                small.fontSize = 20;
                if (step.Progress != null) Shadowed(new Rect(30f, 118f, 600f, 30f), step.Progress, small, TextAnchor.UpperLeft);
            }

            big.fontSize = 40;
            Shadowed(new Rect(w - 330f, 20f, 300f, 50f), GameClock.Text, big, TextAnchor.UpperRight);
            Shadowed(new Rect(w - 430f, 72f, 400f, 30f),
                $"Dowody {GameProgress.Photos.Count}/{GameProgress.EvidenceTotal}   Karty historii {GameProgress.HistoryCards.Count}/{GameProgress.HistoryCardTotal}",
                small, TextAnchor.UpperRight);

            if (step is CableStep cable && !cable.Done)
            {
                float left = cable.Remaining / cable.Duration;
                DrawBar(new Rect(w * 0.5f - 300f, 24f, 600f, 22f), left, left < 0.25f ? CombatFeedback.Blood : new Color(1f, 0.8f, 0.3f));
                Shadowed(new Rect(w * 0.5f - 300f, 48f, 600f, 36f), $"KOMUNIKAT NA ANTENIE — {Mathf.CeilToInt(cable.Remaining)} s", label, TextAnchor.MiddleCenter);
            }
        }

        void DrawWeaponAndVitals(float w, float h)
        {
            if (weapon != null)
            {
                bool low = weapon.AmmoInMagazine <= Mathf.Max(1, weapon.MagazineSize / 4);
                string ammo = weapon.IsReloading ? "PRZEŁADOWANIE" : $"{weapon.AmmoInMagazine} / {weapon.ReserveAmmo}";
                label.normal.textColor = low && !weapon.IsReloading ? CombatFeedback.Blood : Color.white;
                Shadowed(new Rect(w - 380f, h - 95f, 350f, 50f), ammo, label, TextAnchor.MiddleRight);
                label.normal.textColor = Color.white;
                if (low && !weapon.IsReloading && weapon.ReserveAmmo > 0)
                    Shadowed(new Rect(w - 380f, h - 130f, 350f, 36f), "[R] przeładuj", small, TextAnchor.MiddleRight);
                if (weapon.IsReloading)
                    DrawBar(new Rect(w * 0.5f - 60f, h * 0.5f + 40f, 120f, 6f), weapon.ReloadProgress, Color.white);
            }

            if (player != null && player.StaminaNormalized < 0.99f)
            {
                Color sc = player.IsExhausted ? CombatFeedback.Blood : new Color(0.85f, 0.85f, 0.7f);
                DrawBar(new Rect(30f, h - 40f, 320f, 8f), player.StaminaNormalized, sc);
            }

            if (playerHealth != null)
            {
                DrawBar(new Rect(30f, h - 70f, 320f, 22f), playerHealth.Normalized, new Color(0.8f, 0.15f, 0.12f));
                Shadowed(new Rect(30f, h - 110f, 320f, 36f), $"HP {Mathf.CeilToInt(playerHealth.Current)}", label, TextAnchor.MiddleLeft);
                if (playerHealth.IsDead)
                    Shadowed(new Rect(0f, h * 0.4f, w, 60f), "NIE ŻYJESZ — powrót do punktu kontrolnego…", big, TextAnchor.MiddleCenter);
            }
        }

        void DrawObjectiveMarker(float scale, float w, float h)
        {
            if (cam == null) return;
            Vector3 markerWorld;
            if (Mission.Instance != null)
            {
                var ms = Mission.Instance.Current;
                var pos = ms?.MarkerPosition;
                if (pos == null) return;
                markerWorld = pos.Value;
            }
            else
            {
                var step = QuestManager.Instance != null ? QuestManager.Instance.Current : null;
                if (step == null || !step.ShowMarker) return;
                markerWorld = step.MarkerPosition;
            }

            Vector3 target = markerWorld + Vector3.up * 1.5f;
            Vector3 sp = cam.WorldToScreenPoint(target);
            bool behind = sp.z < 0f;
            // Screen pixels (origin bottom-left) -> virtual GUI units (origin top-left).
            float x = sp.x / scale, y = h - sp.y / scale;
            if (behind) { x = w - x; y = h - 60f; }
            const float margin = 50f;
            x = Mathf.Clamp(x, margin, w - margin);
            y = Mathf.Clamp(y, margin + 140f, h - margin - 80f);

            float bob = Mathf.Sin(Time.unscaledTime * 3f) * 3f;
            GUI.color = markerColor;
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(45f, new Vector2(x, y + bob) * GUI.matrix.m00);
            GUI.DrawTexture(new Rect(x - 9f, y + bob - 9f, 18f, 18f), white);
            GUI.matrix = m;
            GUI.color = Color.white;
            int meters = Mathf.RoundToInt(Vector3.Distance(cam.transform.position, markerWorld));
            Shadowed(new Rect(x - 60f, y + 16f, 120f, 30f), $"{meters} m", small, TextAnchor.UpperCenter);
        }

        void DrawBar(Rect r, float fill, Color color)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(r, white);
            GUI.color = color;
            GUI.DrawTexture(new Rect(r.x + 2f, r.y + 2f, (r.width - 4f) * Mathf.Clamp01(fill), r.height - 4f), white);
            GUI.color = Color.white;
        }

        static void Shadowed(Rect r, string text, GUIStyle style, TextAnchor anchor)
        {
            style.alignment = anchor;
            var c = GUI.color;
            var tc = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.8f * c.a);
            GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), text, style);
            style.normal.textColor = tc;
            GUI.Label(r, text, style);
        }

        static Texture2D MakeVignette(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float c = (size - 1) * 0.5f;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - c) / c, dy = (y - c) / c;
                float d = Mathf.Clamp01((Mathf.Sqrt(dx * dx + dy * dy) - 0.55f) / 0.6f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(d * d * 255));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }
    }
}
