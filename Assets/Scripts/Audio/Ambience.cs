using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Ambience loops (Resources/Ambience) with a 0.4 s crossfade. API: Ambience.SetZone("amb_..."). Auto mode picks the zone
    /// from the player position: city loop (peaceful before 19:30, war after) or the interior of the nearest door target
    /// (interiors sit at x &gt;= 900). Entering/leaving an interior plays the wooden door sound once.
    /// </summary>
    public sealed class Ambience : MonoBehaviour
    {
        static Ambience instance;
        AudioSource a, b, active;
        string zone = "";
        float fade = 0.4f, t = 1f, checkAt;
        const float Volume = 0.5f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (instance != null) { instance.zone = ""; return; }
            var go = new GameObject("Ambience");
            instance = go.AddComponent<Ambience>();
            DontDestroyOnLoad(go);
        }

        public static string Zone => instance != null ? instance.zone : "";

        public static void SetZone(string id)
        {
            if (instance == null) Bootstrap();
            instance.Switch(id);
        }

        void Awake() { a = NewSource(); b = NewSource(); active = a; }

        AudioSource NewSource()
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.loop = true; s.playOnAwake = false; s.volume = 0f; s.spatialBlend = 0f;
            return s;
        }

        void Switch(string id)
        {
            if (id == zone) return;
            bool wasInterior = zone != "" && !zone.StartsWith("amb_miasto");
            bool isInterior = id != "" && !id.StartsWith("amb_miasto");
            if (zone != "" && wasInterior != isInterior)
            {
                var door = Resources.Load<AudioClip>("Sfx/sfx_drzwi_drewno");
                if (door != null) AudioSource.PlayClipAtPoint(door, Camera.main != null ? Camera.main.transform.position : Vector3.zero, 0.8f);
            }
            zone = id;
            var next = active == a ? b : a;
            t = 0f;
            if (!string.IsNullOrEmpty(id))
            {
                var clip = Resources.Load<AudioClip>("Ambience/" + id);
                if (clip != null) { next.clip = clip; next.volume = 0f; next.Play(); }
                else Debug.LogWarning("Ambience: brak klipu Ambience/" + id);
            }
            active = next;
        }

        static readonly (string point, string zone, float radius)[] Interiors =
        {
            ("spawn_hotel_wejscie", "amb_hotel_hol_wnetrze", 60f),
            ("spawn_ksiegarnia", "amb_ksiegarnia_wnetrze", 60f),
            ("spawn_fotograf", "amb_fotograf_ciemnia", 60f),
        };

        string AutoZone(Vector3 p)
        {
            foreach (var (point, z, r) in Interiors)
                if (Points.Has(point) && (Points.Position(point) - p).sqrMagnitude < r * r) return z;
            if (Points.Has("radio_piwnica_drzwi") && Vector3.Distance(Points.Position("radio_piwnica_drzwi"), p) < 8f && p.y < -1f) return "amb_radiostacja_piwnica";
            if (Points.Has("radio_technika_kable") && Vector3.Distance(Points.Position("radio_technika_kable"), p) < 10f) return "amb_radiostacja_technika";
            return GameClock.Hour >= 19.5f ? "amb_miasto_wojna_1939" : "amb_miasto_gliwice_1939";
        }

        void Update()
        {
            if (Time.unscaledTime >= checkAt)
            {
                checkAt = Time.unscaledTime + 0.4f;
                var pl = FindAnyObjectByType<FirstPersonController>();
                Switch(pl != null ? AutoZone(pl.transform.position) : "");
            }
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / fade);
            var other = active == a ? b : a;
            active.volume = Volume * t;
            other.volume = Volume * (1f - t);
            if (t >= 1f && other.isPlaying) other.Stop();
        }
    }
}
