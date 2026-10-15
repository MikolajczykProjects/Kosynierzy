using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kosynierzy
{
    /// <summary>
    /// Background music with a crossfade between two AudioSources. API: GameMusic.Play("mus_...", fade), GameMusic.Stop(fade).
    /// Clips live in Resources/Music. Auto-picks: menu scene → menu theme, game → calm exploration, detection Searching/Alarmed
    /// → tension / action (6 s hold so it does not flicker), mission fail → game-over theme. Mission steps may call Play() too;
    /// an explicit Play() pauses the auto-picker for 20 s.
    /// </summary>
    public sealed class GameMusic : MonoBehaviour
    {
        static GameMusic instance;
        AudioSource a, b;
        AudioSource active;
        string currentId = "";
        float fadeTime = 1.5f, fadeLeft;
        float holdUntil, manualUntil;
        const float Volume = 0.55f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (instance == null)
            {
                var go = new GameObject("GameMusic");
                instance = go.AddComponent<GameMusic>();
                DontDestroyOnLoad(go);
            }
            instance.manualUntil = 0f;
            instance.AutoPick(true);
        }

        public static string Current => instance != null ? instance.currentId : "";

        public static void Play(string id, float fade = 1.5f, float hold = 20f)
        {
            if (instance == null) Bootstrap();
            instance.manualUntil = Time.unscaledTime + hold;
            instance.Switch(id, fade);
        }

        public static void Stop(float fade = 1.5f)
        {
            if (instance == null) return;
            instance.manualUntil = Time.unscaledTime + 20f;
            instance.Switch("", fade);
        }

        void Awake()
        {
            a = NewSource(); b = NewSource(); active = a;
            MissionFail.Failed += OnFail;
        }

        void OnDestroy() { MissionFail.Failed -= OnFail; }

        AudioSource NewSource()
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.loop = true; s.playOnAwake = false; s.volume = 0f; s.spatialBlend = 0f;
            return s;
        }

        void OnFail(string _) { holdUntil = 0f; manualUntil = 0f; Switch("mus_porazka_game_over", 0.8f); manualUntil = Time.unscaledTime + 600f; }

        void Switch(string id, float fade)
        {
            if (id == currentId) return;
            currentId = id;
            fadeTime = Mathf.Max(0.05f, fade); fadeLeft = fadeTime;
            var next = active == a ? b : a;
            if (!string.IsNullOrEmpty(id))
            {
                var clip = Resources.Load<AudioClip>("Music/" + id);
                if (clip == null) { Debug.LogWarning("GameMusic: brak klipu Music/" + id); }
                else { next.clip = clip; next.volume = 0f; next.Play(); }
            }
            else next.clip = null;
            active = next;
        }

        void AutoPick(bool force)
        {
            if (!force && Time.unscaledTime < manualUntil) return;
            string want;
            if (SceneManager.GetActiveScene().name.ToLowerInvariant().Contains("menu")) want = "mus_menu_main_theme";
            else
            {
                var st = Detection.PlayerStatus;
                if (st == AwarenessLevel.Alarmed) { want = "mus_akcja_strzelanina"; holdUntil = Time.unscaledTime + 6f; }
                else if (st == AwarenessLevel.Searching) { want = "mus_skradanie_napiecie"; holdUntil = Time.unscaledTime + 6f; }
                else if (Time.unscaledTime < holdUntil && currentId.StartsWith("mus_ak") || Time.unscaledTime < holdUntil && currentId == "mus_skradanie_napiecie") want = currentId;
                else want = "mus_eksploracja_spokojna";
            }
            Switch(want, 1.5f);
        }

        void Update()
        {
            AutoPick(false);
            float k = fadeLeft > 0f ? 1f - Mathf.Clamp01((fadeLeft -= Time.unscaledDeltaTime) / fadeTime) : 1f;
            var other = active == a ? b : a;
            active.volume = Volume * (string.IsNullOrEmpty(currentId) ? 0f : k);
            other.volume = Mathf.Max(0f, other.volume - Volume * Time.unscaledDeltaTime / fadeTime);
            if (other.volume <= 0.001f && other.isPlaying) other.Stop();
        }
    }
}
