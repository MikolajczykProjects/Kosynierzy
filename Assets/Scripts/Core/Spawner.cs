using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Spawns team prefabs by contract name (CH_*, OB_*) at named map points. A missing prefab becomes
    /// a grey placeholder (capsule for characters, cube for objects) with the same name, so the game
    /// keeps working before the art arrives. Spawned instances can be looked up by name.
    /// </summary>
    public static class Spawner
    {
        static PrefabRegistry registry;
        static readonly Dictionary<string, List<GameObject>> spawned = new();
        static readonly HashSet<string> warned = new();
        static Material placeholderMat;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            registry = null;
            spawned.Clear();
            warned.Clear();
        }

        static PrefabRegistry Registry => registry != null ? registry : registry = Resources.Load<PrefabRegistry>("PrefabRegistry");

        public static bool HasPrefab(string prefabName) => Registry != null && Registry.Find(prefabName) != null;

        public static GameObject Spawn(string prefabName, string pointName) =>
            Spawn(prefabName, Points.Position(pointName), Points.Rotation(pointName));

        public static GameObject Spawn(string prefabName, Vector3 position, Quaternion rotation)
        {
            var prefab = Registry != null ? Registry.Find(prefabName) : null;
            GameObject go;
            if (prefab != null)
            {
                go = Object.Instantiate(prefab, position, rotation);
                go.name = prefabName;
            }
            else
            {
                if (warned.Add(prefabName)) Debug.LogWarning($"Kosynierzy: brak prefabu '{prefabName}' — wstawiam zaślepkę.");
                go = CreatePlaceholder(prefabName, position, rotation);
            }
            if (!spawned.TryGetValue(prefabName, out var list)) spawned[prefabName] = list = new List<GameObject>();
            list.Add(go);
            return go;
        }

        /// <summary>First live instance spawned under this name (or found in the scene).</summary>
        public static GameObject Find(string prefabName)
        {
            if (spawned.TryGetValue(prefabName, out var list))
            {
                list.RemoveAll(g => g == null);
                if (list.Count > 0) return list[0];
            }
            return GameObject.Find(prefabName);
        }

        public static GameObject CreatePlaceholder(string name, Vector3 position, Quaternion rotation)
        {
            bool character = name.StartsWith("CH_");
            var go = GameObject.CreatePrimitive(character ? PrimitiveType.Capsule : PrimitiveType.Cube);
            go.name = name;
            if (character)
            {
                go.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
                position += Vector3.up * 0.9f;
            }
            else
            {
                go.transform.localScale = Vector3.one * 0.6f;
                position += Vector3.up * 0.3f;
            }
            go.transform.SetPositionAndRotation(position, rotation);
            if (placeholderMat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                placeholderMat = new Material(shader != null ? shader : Shader.Find("Standard"));
                placeholderMat.SetColor("_BaseColor", new Color(0.55f, 0.55f, 0.55f));
            }
            go.GetComponent<Renderer>().sharedMaterial = placeholderMat;
            return go;
        }
    }
}
