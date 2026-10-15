using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kosynierzy
{
    /// <summary>
    /// Developer/test panel (hackathon build): ` or F2 toggles. Jump to any mission step, teleport to any named point,
    /// give weapons, change the hour, alarm/calm NPCs, kill the nearest enemy, god mode, infinite ammo, fail screen.
    /// IMGUI on purpose: zero setup, works in the editor and in builds.
    /// </summary>
    public sealed class AdminPanel : MonoBehaviour
    {
        static AdminPanel instance;
        bool open;
        int tab;
        Vector2 scroll;
        float fps;
        bool god, infiniteAmmo;
        readonly List<string> pointNames = new();
        GUIStyle box, btn, label, tabOn;
        Texture2D bg;
        string status = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (instance != null) return;
            var go = new GameObject("AdminPanel");
            instance = go.AddComponent<AdminPanel>();
            DontDestroyOnLoad(go);
        }

        void Update()
        {
            fps = Mathf.Lerp(fps, 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime), 0.05f);
            var kb = Keyboard.current;
            if (kb != null && (kb.backquoteKey.wasPressedThisFrame || kb.f2Key.wasPressedThisFrame)) Toggle();
            if (open && Cursor.lockState != CursorLockMode.None) PlayerControlLock.SetCursorFree(true);
            if (infiniteAmmo) FillAmmo();
            if (god) KeepAlive();
        }

        void Toggle()
        {
            open = !open;
            PlayerControlLock.SetCursorFree(open);
            if (open) RefreshPoints();
        }

        void RefreshPoints()
        {
            pointNames.Clear();
            var root = GameObject.Find("Punkty");
            if (root == null) return;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t != root.transform) pointNames.Add(t.name);
            pointNames.Sort();
        }

        FirstPersonController Player => FindAnyObjectByType<FirstPersonController>();

        void FillAmmo()
        {
            var w = FindAnyObjectByType<PlayerWeapon>();
            if (w == null) return;
            var f = typeof(PlayerWeapon).GetField("reserveAmmo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null && w.ReserveAmmo < 30) f.SetValue(w, 60);
        }

        void KeepAlive()
        {
            var p = Player;
            if (p == null) return;
            var h = p.GetComponentInChildren<Health>();
            if (h == null) h = p.GetComponentInParent<Health>();
            if (h != null && h.Current < h.Max) h.Heal(h.Max);
        }

        void OnGUI()
        {
            if (!open) { GUI.Label(new Rect(Screen.width - 150, 4, 146, 20), "FPS " + Mathf.RoundToInt(fps) + "   [`] admin", Style(ref label, 12, TextAnchor.MiddleRight)); return; }
            if (bg == null) { bg = new Texture2D(1, 1); bg.SetPixel(0, 0, new Color(0.05f, 0.055f, 0.065f, 0.94f)); bg.Apply(); }
            float s = Screen.height / 1080f;
            float w = Mathf.Min(Screen.width - 20, 640 * s + 120), h = Screen.height - 40;
            var r = new Rect(20, 20, w, h);
            GUI.DrawTexture(r, bg);
            GUILayout.BeginArea(new Rect(r.x + 14, r.y + 10, r.width - 28, r.height - 20));
            GUILayout.BeginHorizontal();
            string[] names = { "Misja", "Teleport", "Ułatwienia", "Świat" };
            for (int i = 0; i < names.Length; i++)
                if (GUILayout.Button(names[i], i == tab ? Style(ref tabOn, 15, TextAnchor.MiddleCenter, true) : Style(ref btn, 15, TextAnchor.MiddleCenter), GUILayout.Height(30))) { tab = i; scroll = Vector2.zero; }
            GUILayout.EndHorizontal();
            GUILayout.Label(status, Style(ref label, 13, TextAnchor.MiddleLeft));
            scroll = GUILayout.BeginScrollView(scroll);
            switch (tab) { case 0: TabMission(); break; case 1: TabTeleport(); break; case 2: TabCheats(); break; default: TabWorld(); break; }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        GUIStyle Style(ref GUIStyle st, int size, TextAnchor a, bool on = false)
        {
            if (st == null)
            {
                st = new GUIStyle(GUI.skin.button) { fontSize = size, alignment = a };
                st.normal.textColor = on ? new Color(0.05f, 0.06f, 0.07f) : new Color(0.93f, 0.92f, 0.88f);
                st.hover.textColor = st.normal.textColor;
                if (on) { var t = new Texture2D(1, 1); t.SetPixel(0, 0, new Color(0.93f, 0.92f, 0.88f)); t.Apply(); st.normal.background = t; st.hover.background = t; }
                if (st == label) { st.normal.background = null; }
            }
            return st;
        }

        bool B(string text) => GUILayout.Button(text, Style(ref btn, 14, TextAnchor.MiddleLeft), GUILayout.Height(28));

        void TabMission()
        {
            var m = Mission.Instance;
            if (m == null) { GUILayout.Label("Brak misji w scenie", Style(ref label, 14, TextAnchor.MiddleLeft)); return; }
            GUILayout.Label("Teraz: " + m.CurrentIndex + "  " + (m.Current != null ? m.Current.Title : "-"), Style(ref label, 14, TextAnchor.MiddleLeft));
            for (int i = 0; i < m.DebugStepCount; i++)
                if (B(i + "   " + m.DebugStepTitle(i))) { status = "Skok do kroku " + i; m.DebugJumpTo(i); open = false; PlayerControlLock.SetCursorFree(false); }
        }

        void TabTeleport()
        {
            var m = Mission.Instance;
            if (m != null && m.Current != null && m.Current.MarkerPosition.HasValue && B("Aktualny cel: " + m.Current.Title)) Go(m.Current.MarkerPosition.Value);
            foreach (var n in pointNames)
                if (B(n)) Go(Points.Position(n));
        }

        void Go(Vector3 pos)
        {
            var p = Player;
            if (p != null) p.Teleport(pos + Vector3.up * 0.1f, p.transform.eulerAngles.y);
            status = "Teleport: " + pos;
        }

        void TabCheats()
        {
            god = GUILayout.Toggle(god, " Tryb Boga (leczy gracza)", Style(ref label, 14, TextAnchor.MiddleLeft));
            infiniteAmmo = GUILayout.Toggle(infiniteAmmo, " Nieskończona amunicja", Style(ref label, 14, TextAnchor.MiddleLeft));
            if (B("Daj aparat")) Inventory.GiveCamera();
            if (B("Daj Vis wz. 35")) Inventory.GiveWeapon(WeaponCatalog.Pistols[0]);
            foreach (var r in WeaponCatalog.Rifles)
                if (B("Daj karabin: " + r.Name)) { WeaponCatalog.Choose(r); Inventory.GiveWeapon(r); }
            if (B("Zabij najbliższego wroga")) KillNearest();
        }

        void KillNearest()
        {
            var p = Player;
            if (p == null) return;
            Health best = null; float bd = 1e9f;
            foreach (var e in FindObjectsByType<EnemyController>())
            {
                var hp = e.GetComponentInChildren<Health>();
                if (hp == null) hp = e.GetComponentInParent<Health>();
                if (hp == null || hp.IsDead) continue;
                float d = (e.transform.position - p.transform.position).sqrMagnitude;
                if (d < bd) { bd = d; best = hp; }
            }
            if (best != null) { best.TakeDamage(9999f); status = "Zabity wróg"; } else status = "Brak wrogów";
        }

        void TabWorld()
        {
            GUILayout.Label("Godzina w grze: " + GameClock.Text + "   NPC: " + FindObjectsByType<WatcherBrain>().Length + "   FPS " + Mathf.RoundToInt(fps), Style(ref label, 14, TextAnchor.MiddleLeft));
            if (B("15:30")) GameClock.Set(15.5f);
            if (B("19:00")) GameClock.Set(19f);
            if (B("20:00")) GameClock.Set(20f);
            if (B("Alarm: wszyscy SD")) { var d = NpcDirector.Instance; var p = Player; if (d != null && p != null) d.AlarmAll(p.transform.position); }
            if (B("Ekran porażki")) MissionFail.Trigger(MissionFail.PlayerHit);
            if (B("Od ostatniego checkpointu")) Checkpoints.Reload();
            if (B("Zamknij panel")) Toggle();
        }
    }
}
