using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace Kosynierzy
{
    /// <summary>
    /// Transitions and short cutscenes. Act title cards (fade + act / place / hour), long-trip transitions (objective farther than
    /// 300 m → fade, title card, teleport to a NavMesh point &lt;= 100 m from the objective facing it), skippable camera pans at key
    /// beats (Space skips), shootout start (shake + action music), and proximity/step triggers for the dialogue scenes that
    /// nothing else plays. API for other code and the admin panel: Cutscene.Pan / ActTitle / Travel / Shootout / PlayScene.
    /// </summary>
    public sealed class Cutscene : MonoBehaviour
    {
        static Cutscene instance;

        string cardTop = "", cardMain = "", cardSub = "";
        float cardUntil = -1f, cardFadeIn;
        bool bars;
        bool skip;
        int lastIndex = -2;
        float travelCheckAt = -1f, nextTick, carBarkAt;
        readonly HashSet<string> fired = new();
        Mission hooked;
        bool shootoutDone;
        readonly PlayerControlLock locker = new();
        int locks;
        void Lock() { if (locks++ == 0) locker.Lock(); }
        void Unlock() { if (locks > 0 && --locks == 0) locker.Unlock(); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (instance != null) return;
            var go = new GameObject("Cutscene");
            instance = go.AddComponent<Cutscene>();
            DontDestroyOnLoad(go);
        }

        public static bool Busy => instance != null && (instance.cardUntil > Time.unscaledTime || instance.bars);

        // ------------------------------------------------------------------ API

        /// <summary>Fade to black, show "act / place / hour", fade back in.</summary>
        public static void ActTitle(string act, string place, float hour)
        {
            if (instance == null) return;
            int h = Mathf.FloorToInt(hour), m = Mathf.RoundToInt((hour - h) * 60f);
            instance.ShowCard(act, place, h + ":" + m.ToString("00"), 2.4f);
        }

        /// <summary>Camera turns to look at a world point for a few seconds; player locked, Space skips.</summary>
        public static void Pan(Vector3 target, float seconds = 3.5f, string caption = null)
        {
            if (instance != null) instance.StartCoroutine(instance.PanRoutine(target, seconds, caption));
        }

        public static void Pan(string point, float seconds = 3.5f, string caption = null)
        {
            if (Points.Has(point)) Pan(Points.Position(point) + Vector3.up * 1.4f, seconds, caption);
        }

        /// <summary>Long trip: fade, title card, teleport near the target on the NavMesh, face it, fade in.</summary>
        public static void Travel(Vector3 target, string label)
        {
            if (instance == null) return;
            var p = Object.FindAnyObjectByType<FirstPersonController>();
            if (p == null) return;
            Vector3 dir = target - p.transform.position; dir.y = 0f;
            Vector3 cand = target - dir.normalized * 60f;
            Vector3 pos = target;
            if (NavMesh.SamplePosition(cand, out var hit, 60f, NavMesh.AllAreas) && Vector3.Distance(hit.position, target) <= 100f) pos = hit.position;
            else if (NavMesh.SamplePosition(target, out hit, 100f, NavMesh.AllAreas)) pos = hit.position;
            Vector3 face = target - pos; face.y = 0f;
            float yaw = face.sqrMagnitude > 0.01f ? Quaternion.LookRotation(face).eulerAngles.y : p.transform.eulerAngles.y;
            instance.ShowCard("Dalej", label, GameClock.Text, 2.2f, () => p.Teleport(pos + Vector3.up * 0.05f, yaw));
        }

        public static void Shootout()
        {
            CameraShake.AddTrauma(0.9f);
            GameMusic.Play("mus_akcja_strzelanina", 0.4f);
        }

        /// <summary>Plays any dialogue scene without freezing the player (used by the admin panel and the hooks).</summary>
        public static void PlayScene(string id)
        {
            if (Dialog.Exists(id)) Dialog.Play(id, null, false);
        }

        // ------------------------------------------------------------------ title card

        void ShowCard(string top, string main, string sub, float seconds, System.Action whileBlack = null)
        {
            if (cardUntil > Time.unscaledTime) return;
            cardTop = top; cardMain = main; cardSub = sub;
            cardFadeIn = Time.unscaledTime + 0.4f;
            cardUntil = Time.unscaledTime + seconds + 1.0f;
            Lock();
            ScreenFade.Transition(() => { whileBlack?.Invoke(); }, 0.4f, seconds, 0.6f);
            StartCoroutine(UnlockAfter(seconds + 1.1f));
        }

        IEnumerator UnlockAfter(float s)
        {
            yield return new WaitForSecondsRealtime(s);
            Unlock();
        }

        void OnGUI()
        {
            float k = Screen.height / 1080f;
            if (cardUntil > Time.unscaledTime && ScreenFade.Alpha > 0.6f)
            {
                GUI.depth = -2000;
                var st = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
                st.normal.textColor = new Color(0.93f, 0.92f, 0.88f, 0.7f);
                st.fontSize = Mathf.RoundToInt(16 * k);
                GUI.Label(new Rect(0, Screen.height * 0.5f - 90 * k, Screen.width, 28 * k), cardTop.ToUpperInvariant(), st);
                st.normal.textColor = new Color(0.93f, 0.92f, 0.88f, 1f);
                st.fontSize = Mathf.RoundToInt(54 * k);
                GUI.Label(new Rect(0, Screen.height * 0.5f - 60 * k, Screen.width, 80 * k), cardMain, st);
                st.normal.textColor = new Color(0.89f, 0.66f, 0.23f, 1f);
                st.fontSize = Mathf.RoundToInt(24 * k);
                GUI.Label(new Rect(0, Screen.height * 0.5f + 28 * k, Screen.width, 34 * k), cardSub, st);
            }
            if (bars)
            {
                GUI.depth = -500;
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, 90 * k), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0, Screen.height - 90 * k, Screen.width, 90 * k), Texture2D.whiteTexture);
                GUI.color = new Color(1f, 1f, 1f, 0.6f);
                var st = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontSize = Mathf.RoundToInt(14 * k) };
                st.normal.textColor = Color.white;
                GUI.Label(new Rect(0, Screen.height - 70 * k, Screen.width - 40 * k, 30 * k), "Spacja — pomiń", st);
                GUI.color = Color.white;
            }
        }

        // ------------------------------------------------------------------ camera pan

        IEnumerator PanRoutine(Vector3 target, float seconds, string caption)
        {
            if (bars) yield break;
            var cam = Camera.main;
            if (cam == null) yield break;
            bars = true; skip = false;
            Lock();
            var startRot = cam.transform.rotation;
            var startPos = cam.transform.position;
            var endRot = Quaternion.LookRotation((target - startPos).sqrMagnitude > 0.01f ? target - startPos : cam.transform.forward);
            for (float t = 0f; t < seconds && !skip; t += Time.unscaledDeltaTime)
            {
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / (seconds * 0.45f)));
                cam.transform.rotation = Quaternion.Slerp(startRot, endRot, e);
                yield return null;
            }
            bars = false;
            Unlock();
        }

        // ------------------------------------------------------------------ triggers

        struct Trigger { public string Point; public float Radius; public string Dialog, After; public bool Pan; }

        static readonly Trigger[] Triggers =
        {
            new Trigger { Point = "ksiegarnia_zofia", Radius = 9f, Dialog = null, Pan = true },
            new Trigger { Point = "fotograf_wlasciciel", Radius = 8f, Dialog = "pob_fotograf_przed" },
            new Trigger { Point = "fotograf_gestapo", Radius = 6f, Dialog = "pob_fotograf_gestapo", After = "pob_fotograf_przed" },
            new Trigger { Point = "fotograf_ciemnia", Radius = 6f, Dialog = "pob_fotograf_po", After = "pob_fotograf_gestapo" },
            new Trigger { Point = "hotel_recepcja", Radius = 14f, Dialog = "a1_hotel_goscie" },
            new Trigger { Point = "miasto_skrytka_most", Radius = 14f, Dialog = "a2_most" },
            new Trigger { Point = "spawn_radio_tyl", Radius = 25f, Dialog = "a3_wejscie" },
            new Trigger { Point = "radio_sd_3", Radius = 16f, Dialog = "a3_sd_udaja" },
            new Trigger { Point = "radio_technika_kable", Radius = 7f, Dialog = "a3_mysl_kable" },
        };

        readonly HashSet<string> finished = new();

        void OnEnable() => Dialog.Finished += OnDialogFinished;
        void OnDisable() { Dialog.Finished -= OnDialogFinished; if (locks > 0) { locks = 0; locker.Unlock(); } }
        void OnDialogFinished(string id) => finished.Add(id);

        void Update()
        {
            if (bars && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) skip = true;
            if (Time.unscaledTime < nextTick) return;
            nextTick = Time.unscaledTime + 0.3f;

            var m = Mission.Instance;
            var pl = FindAnyObjectByType<FirstPersonController>();
            if (m == null || pl == null) return;
            if (hooked != m) { hooked = m; m.ActStarted += OnAct; if (!string.IsNullOrEmpty(m.CurrentActName)) OnAct(m.CurrentActName); lastIndex = -2; fired.Clear(); finished.Clear(); shootoutDone = false; }
            if (Dialog.IsPlaying || Busy || MissionFail.IsFailed) return;
            Vector3 pp = pl.transform.position;

            // G3 long trips: decided 1.5 s after a step starts
            if (m.CurrentIndex != lastIndex)
            {
                lastIndex = m.CurrentIndex;
                travelCheckAt = Time.unscaledTime + 1.5f;
                string title = m.Current != null ? m.Current.Title : "";
                if (title.StartsWith("Uciekaj")) { PlayOnce("a4_ucieczka"); Shootout(); }
                else if (title.StartsWith("Sfotografuj kartkę przy ciele")) PlayOnce("a4_radio_spiker");
            }
            if (travelCheckAt > 0f && Time.unscaledTime >= travelCheckAt)
            {
                travelCheckAt = -1f;
                if (m.Current != null && m.Current.MarkerPosition.HasValue && pp.x < 3000f)
                {
                    var mp = m.Current.MarkerPosition.Value;
                    if (mp.x < 3000f && Vector3.Distance(pp, mp) > 300f) Travel(mp, m.Current.Title);
                }
            }

            // car-tailing barks
            if (m.Current != null && m.Current.Title != null && m.Current.Title.StartsWith("Śledź auto") && Time.unscaledTime > carBarkAt)
            {
                var car = Spawner.Find("OB_Auto_SD");
                if (car != null)
                {
                    float d = Vector3.Distance(pp, car.transform.position);
                    if (d > 33f) { PlayScene("a2_auto_daleko"); carBarkAt = Time.unscaledTime + 25f; }
                    else if (d < 10f) { PlayScene("a2_auto_blisko"); carBarkAt = Time.unscaledTime + 25f; }
                }
            }

            // shootout beat: first time anyone is fully alarmed
            if (!shootoutDone && Detection.PlayerStatus == AwarenessLevel.Alarmed) { shootoutDone = true; Shootout(); }

            // proximity triggers
            foreach (var t in Triggers)
            {
                string key = t.Point + t.Dialog;
                if (fired.Contains(key) || !Points.Has(t.Point)) continue;
                if (t.After != null && !finished.Contains(t.After)) continue;
                if ((Points.Position(t.Point) - pp).sqrMagnitude > t.Radius * t.Radius) continue;
                fired.Add(key);
                if (t.Pan) { Pan(t.Point, 3f); continue; }
                PlayScene(t.Dialog);
                break;
            }
            // SD car first sight
            if (!fired.Contains("carpan"))
            {
                var car = Spawner.Find("OB_Auto_SD");
                if (car != null && Vector3.Distance(pp, car.transform.position) < 22f && m.CurrentIndex >= 0)
                { fired.Add("carpan"); Pan(car.transform.position + Vector3.up * 1f, 2.8f); }
            }
        }

        void PlayOnce(string id)
        {
            if (fired.Contains(id)) return;
            fired.Add(id);
            PlayScene(id);
        }

        void OnAct(string name)
        {
            string place = name.Contains("—") ? name.Substring(name.IndexOf('—') + 1).Trim() : name;
            string act = name.Contains("—") ? name.Substring(0, name.IndexOf('—')).Trim() : name;
            ActTitle(act, place, GameClock.Hour);
        }
    }
}
