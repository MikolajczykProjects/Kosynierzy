using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// One place for the sound team to drop clips: every gameplay event already calls Play(...).
    /// Empty slots are silent. 2D sounds for the player's own actions and feedback;
    /// positional sounds (enemy shots, impacts) use PlayAt.
    /// </summary>
    public class SfxBank : MonoBehaviour
    {
        public enum Sfx
        {
            Shot, DryFire, ReloadStart, ReloadEnd, Hit, Headshot, Kill, MultiKill,
            PlayerHurt, LowHealthHeartbeat, Footstep, Jump, Land, Slide,
            QuestStep, QuestComplete, Pickup, EnemyShot, BulletImpact, BulletWhizz, Shutter
        }

        [System.Serializable]
        public struct Entry
        {
            public Sfx Id;
            public AudioClip[] Clips;
            [Range(0f, 1f)] public float Volume;
            [Range(0f, 0.3f)] public float PitchJitter;
        }

        [SerializeField] Entry[] entries = new Entry[0];
        [SerializeField] int voices = 12;

        static SfxBank instance;
        AudioSource[] sources;
        int next;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        void Awake()
        {
            instance = this;
            sources = new AudioSource[voices];
            for (int i = 0; i < voices; i++)
            {
                var src = new GameObject($"Voice{i}").AddComponent<AudioSource>();
                src.transform.SetParent(transform, false);
                src.playOnAwake = false;
                sources[i] = src;
            }
        }

        // Explicit null checks: "?." would call into a destroyed bank (fake null) right after a scene reload.
        public static void Play(Sfx id)
        {
            if (instance != null) instance.PlayInternal(id, null);
        }

        public static void PlayAt(Sfx id, Vector3 position)
        {
            if (instance != null) instance.PlayInternal(id, position);
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        void PlayInternal(Sfx id, Vector3? position)
        {
            foreach (var e in entries)
            {
                if (e.Id != id || e.Clips == null || e.Clips.Length == 0) continue;
                var clip = e.Clips[Random.Range(0, e.Clips.Length)];
                if (clip == null) break;
                Emit(clip, position, e.Volume <= 0f ? 1f : e.Volume, e.PitchJitter);
                return;
            }
            // Clips shipped in Resources/Sfx/<name> (team A) come before the synthesised placeholder.
            string res = ResourceName(id);
            if (res != null)
            {
                if (!resourceClips.TryGetValue(res, out var rc)) resourceClips[res] = rc = Resources.Load<AudioClip>("Sfx/" + res);
                if (rc != null && id == Sfx.Footstep) { PlayStepSlice(rc); return; }
                if (rc != null) { Emit(rc, position, id == Sfx.Footstep ? 0.55f : 0.9f, 0.05f); return; }
            }
            // No clip from the sound team yet → synthesised placeholder.
            var fallback = ProceduralSfx.Get(id);
            if (fallback != null) Emit(fallback, position, FallbackVolume(id), 0.06f);
        }

        readonly System.Collections.Generic.Dictionary<string, AudioClip> resourceClips = new();
        AudioSource stepSource;
        float stepStopAt;

        // The shipped footstep files are long walking recordings: one stride = a short random slice of it.
        void PlayStepSlice(AudioClip clip)
        {
            if (stepSource == null)
            {
                stepSource = new GameObject("StepVoice").AddComponent<AudioSource>();
                stepSource.transform.SetParent(transform, false);
                stepSource.playOnAwake = false;
                stepSource.spatialBlend = 0f;
                stepSource.volume = 0.55f;
            }
            const float slice = 0.4f;
            stepSource.clip = clip;
            stepSource.time = Random.Range(0f, Mathf.Max(0f, clip.length - slice - 0.05f));
            stepSource.pitch = 1f + Random.Range(-0.05f, 0.05f);
            stepSource.Play();
            stepStopAt = Time.unscaledTime + slice;
        }

        void Update()
        {
            if (stepSource != null && stepSource.isPlaying && Time.unscaledTime >= stepStopAt) stepSource.Stop();
        }

        /// <summary>Resources/Sfx clip for an event; footsteps depend on the floor (interiors stand at x >= 3000).</summary>
        static string ResourceName(Sfx id)
        {
            if (id == Sfx.Shutter) return "sfx_aparat_leica_migawka";
            if (id != Sfx.Footstep) return null;
            var p = FindPlayerPosition();
            return p.x >= 3000f ? "sfx_kroki_drewno" : "sfx_kroki_bruk";
        }

        static Vector3 FindPlayerPosition()
        {
            var cam = Camera.main;
            return cam != null ? cam.transform.position : Vector3.zero;
        }

        void Emit(AudioClip clip, Vector3? position, float volume, float jitter)
        {
            var src = sources[next];
            next = (next + 1) % sources.Length;
            src.transform.position = position ?? transform.position;
            src.spatialBlend = position.HasValue ? 1f : 0f;
            src.minDistance = 3f;
            src.maxDistance = 120f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.pitch = 1f + Random.Range(-jitter, jitter);
            src.PlayOneShot(clip, volume);
        }

        static float FallbackVolume(Sfx id) => id switch
        {
            // Clips are normalised to peaks of 0.5–0.95 (ProceduralSfx), so these volumes decide the mix:
            // the player's shot ≈ -6 dBFS, an SD rifle ≈ -2.5 dBFS at 3 m (and quieter with distance), steps ≈ -20 dBFS.
            Sfx.Footstep => 0.35f,
            Sfx.Shot => 0.6f,
            Sfx.EnemyShot => 0.8f,
            Sfx.QuestStep or Sfx.QuestComplete => 0.5f,
            _ => 0.7f,
        };
    }
}
