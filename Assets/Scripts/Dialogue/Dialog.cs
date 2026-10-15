using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kosynierzy
{
    /// <summary>
    /// Plays dialogues from Resources/dialogi.json (team A), format:
    /// { "a1_zofia": { "linie": [ { "kto":"zofia", "tekst":"…", "napis":"…", "mysl":false, "audio":"a1_zofia_01" } ] } }
    /// Audio from Resources/Audio/&lt;audio&gt;; without a clip the line shows for 0.06 s per character (min 2 s).
    /// Space/E skips to the next line. The player stands still; the speaker's Animator gets Rozmowa = true.
    /// Usage: Dialog.Play("a1_zofia", () => …). Missing dialogue = short placeholder line, never a crash.
    /// </summary>
    public class Dialog : MonoBehaviour
    {
        public class Line
        {
            [JsonProperty("kto")] public string Who;
            [JsonProperty("tekst")] public string Text;
            [JsonProperty("napis")] public string Subtitle;
            [JsonProperty("mysl")] public bool Thought;
            [JsonProperty("audio")] public string Audio;
        }

        class Entry
        {
            [JsonProperty("linie")] public List<Line> Lines;
        }

        static Dialog instance;
        static Dictionary<string, Entry> data;

        AudioSource voice;
        InputAction nextAction;
        Coroutine running;

        public static bool IsPlaying => instance != null && instance.running != null;
        public static Line CurrentLine { get; private set; }
        /// <summary>Raised when a dialogue finishes (id) — quests can listen.</summary>
        public static event Action<string> Finished;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
            data = null;
            CurrentLine = null;
            Finished = null;
        }

        static Dialog Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameObject("[Dialog]").AddComponent<Dialog>();
                    instance.gameObject.AddComponent<DialogUI>();
                }
                return instance;
            }
        }

        public static bool Exists(string id)
        {
            Load();
            return data.ContainsKey(id);
        }

        /// <summary>Lines of a dialogue (e.g. barks "bark_sd_alarm" — pick one at random), or null.</summary>
        public static IReadOnlyList<Line> GetLines(string id)
        {
            Load();
            return data.TryGetValue(id, out var e) ? e.Lines : null;
        }

        /// <summary>Plays one random line of a bark set as 3D audio at a character (no subtitles freeze).</summary>
        /// <summary>Team A writes "Audio/a1_intro_01" (new package) or "a1_intro_01" (old); both map to Resources/Audio.</summary>
        public static string AudioPath(string audio) => audio.StartsWith("Audio/") ? audio : "Audio/" + audio;

        public static void Bark(string id, Vector3 position)
        {
            var lines = GetLines(id);
            if (lines == null || lines.Count == 0) return;
            var line = lines[UnityEngine.Random.Range(0, lines.Count)];
            var clip = string.IsNullOrEmpty(line.Audio) ? null : Resources.Load<AudioClip>(AudioPath(line.Audio));
            if (clip != null) AudioSource.PlayClipAtPoint(clip, position);
            BarkShown?.Invoke(line, position);
        }

        /// <summary>For a floating subtitle above the speaker.</summary>
        public static event Action<Line, Vector3> BarkShown;

        public static void Play(string id, Action onEnd = null, bool freezePlayer = true)
        {
            var d = Instance;
            if (d.running != null) d.StopCoroutine(d.running);
            d.running = d.StartCoroutine(d.Run(id, onEnd, freezePlayer));
        }

        static void Load()
        {
            if (data != null) return;
            data = new Dictionary<string, Entry>();
            var asset = Resources.Load<TextAsset>("dialogi");
            if (asset == null)
            {
                Debug.LogWarning("Kosynierzy: brak Resources/dialogi.json — dialogi będą zaślepkami.");
                return;
            }
            try
            {
                data = JsonConvert.DeserializeObject<Dictionary<string, Entry>>(asset.text) ?? data;
            }
            catch (Exception e)
            {
                Debug.LogError($"Kosynierzy: błąd w dialogi.json: {e.Message}");
            }
        }

        void Awake()
        {
            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0f;
            nextAction = new InputAction("DialogNext", InputActionType.Button, "<Keyboard>/space");
            nextAction.AddBinding("<Keyboard>/e");
            nextAction.AddBinding("<Gamepad>/buttonSouth");
            nextAction.Enable();
        }

        void OnDestroy() => nextAction?.Dispose();

        IEnumerator Run(string id, Action onEnd, bool freezePlayer)
        {
            Load();
            List<Line> lines;
            if (data.TryGetValue(id, out var entry) && entry.Lines != null && entry.Lines.Count > 0) lines = entry.Lines;
            else
            {
                Debug.LogWarning($"Kosynierzy: brak dialogu '{id}' — zaślepka.");
                lines = new List<Line> { new() { Who = "", Subtitle = $"[dialog: {id}]" } };
            }

            var player = UnityEngine.Object.FindAnyObjectByType<FirstPersonController>();
            var weapon = UnityEngine.Object.FindAnyObjectByType<PlayerWeapon>();
            if (freezePlayer)
            {
                if (player) player.enabled = false;
                if (weapon) weapon.enabled = false;
            }

            Animator speakerAnim = null;
            foreach (var line in lines)
            {
                CurrentLine = line;
                SetTalking(ref speakerAnim, line.Who);

                var clip = string.IsNullOrEmpty(line.Audio) ? null : Resources.Load<AudioClip>(AudioPath(line.Audio));
                string shown = string.IsNullOrEmpty(line.Subtitle) ? line.Text : line.Subtitle;
                float duration = clip != null ? clip.length + 0.3f : Mathf.Max(2f, (shown?.Length ?? 0) * 0.06f);
                if (clip != null) voice.PlayOneShot(clip);

                float end = Time.unscaledTime + duration;
                yield return null; // ignore the key press that started the dialogue
                while (Time.unscaledTime < end)
                {
                    if (!GameState.IsPaused && nextAction.WasPressedThisFrame()) break;
                    yield return null;
                }
                voice.Stop();
            }

            if (speakerAnim != null) SetBool(speakerAnim, "Rozmowa", false);
            CurrentLine = null;
            if (freezePlayer)
            {
                if (player) player.enabled = true;
                if (weapon) weapon.enabled = true;
            }
            running = null;
            Finished?.Invoke(id);
            onEnd?.Invoke();
        }

        static void SetTalking(ref Animator current, string who)
        {
            if (current != null) SetBool(current, "Rozmowa", false);
            current = null;
            if (string.IsNullOrEmpty(who) || who == "kruk") return;
            // Speaker id "zofia" -> prefab instance "CH_Zofia".
            string prefab = "CH_" + char.ToUpperInvariant(who[0]) + who.Substring(1);
            var go = Spawner.Find(prefab);
            if (go == null) return;
            current = go.GetComponentInChildren<Animator>();
            if (current != null) SetBool(current, "Rozmowa", true);
        }

        public static void SetBool(Animator a, string param, bool value)
        {
            foreach (var p in a.parameters)
                if (p.name == param && p.type == AnimatorControllerParameterType.Bool) { a.SetBool(param, value); return; }
        }
    }
}
