using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kosynierzy
{
    /// <summary>
    /// Named map points from the team contract (empty GameObjects under an object called "Punkty",
    /// placed by the map author). Lookup by name, e.g. Points.Get("hotel_pokoj_mapa").
    /// A missing point never crashes: it logs one warning and returns Vector3.zero / null.
    /// The +Z of a point is the direction a character/object faces.
    /// </summary>
    public static class Points
    {
        static readonly Dictionary<string, Transform> map = new();
        static readonly HashSet<string> warned = new();
        static bool built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            map.Clear();
            warned.Clear();
            built = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => built = false;

        /// <summary>Rebuild after the map prefab is (re)instantiated.</summary>
        public static void Rebuild()
        {
            map.Clear();
            built = true;
            foreach (var root in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (root.name != "Punkty") continue;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t != root && !map.ContainsKey(t.name)) map.Add(t.name, t);
            }
        }

        public static bool Has(string name)
        {
            if (!built) Rebuild();
            return map.ContainsKey(name);
        }

        public static Transform Get(string name)
        {
            if (!built) Rebuild();
            if (map.TryGetValue(name, out var t) && t != null) return t;
            if (warned.Add(name)) Debug.LogWarning($"Kosynierzy: brak punktu '{name}' na mapie (Punkty) — używam (0,0,0).");
            return null;
        }

        public static Vector3 Position(string name)
        {
            var t = Get(name);
            return t != null ? t.position : Vector3.zero;
        }

        public static Quaternion Rotation(string name)
        {
            var t = Get(name);
            return t != null ? t.rotation : Quaternion.identity;
        }

        /// <summary>All existing points prefix_1, prefix_2, … in order (stops at the first gap).</summary>
        public static List<Transform> Series(string prefix, int max = 50)
        {
            if (!built) Rebuild();
            var list = new List<Transform>();
            for (int i = 1; i <= max; i++)
            {
                if (!map.TryGetValue($"{prefix}_{i}", out var t)) break;
                list.Add(t);
            }
            return list;
        }

        public static IEnumerable<Transform> All
        {
            get
            {
                if (!built) Rebuild();
                return map.Values;
            }
        }
    }
}
