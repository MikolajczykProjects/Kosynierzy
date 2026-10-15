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
            QuestStep, QuestComplete, Pickup, EnemyShot, BulletImpact, BulletWhizz
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

        public static void Play(Sfx id) => instance?.PlayInternal(id, null);
        public static void PlayAt(Sfx id, Vector3 position) => instance?.PlayInternal(id, position);

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
            // No clip from the sound team yet → synthesised placeholder.
            var fallback = ProceduralSfx.Get(id);
            if (fallback != null) Emit(fallback, position, FallbackVolume(id), 0.06f);
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
            Sfx.Footstep => 0.35f,
            Sfx.Shot => 0.9f,
            Sfx.EnemyShot => 1f,
            Sfx.QuestStep or Sfx.QuestComplete => 0.5f,
            _ => 0.7f,
        };
    }
}
