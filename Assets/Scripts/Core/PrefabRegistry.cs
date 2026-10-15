using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Name → prefab table for the team's CH_* (characters) and OB_* (objects) prefabs.
    /// Lives at Resources/PrefabRegistry; refreshed by menu Kosynierzy/Refresh Prefab Registry,
    /// which scans Assets/_Postacie and Assets/_Obiekty.
    /// </summary>
    [CreateAssetMenu(menuName = "Kosynierzy/Prefab Registry")]
    public class PrefabRegistry : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public string Name;
            public GameObject Prefab;
        }

        public List<Entry> Entries = new();

        Dictionary<string, GameObject> lookup;

        public GameObject Find(string name)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, GameObject>();
                foreach (var e in Entries) if (e.Prefab != null) lookup[e.Name] = e.Prefab;
            }
            return lookup.TryGetValue(name, out var p) ? p : null;
        }
    }
}
