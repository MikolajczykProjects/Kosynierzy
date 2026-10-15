using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Kosynierzy
{
    /// <summary>
    /// Spawns team prefabs by contract name (CH_*, OB_*) at named map points. A missing prefab becomes
    /// a grey placeholder (capsule for characters, cube for objects) with the same name, so the game
    /// keeps working before the art arrives. Spawned instances can be looked up by name.
    /// Real OB_* props with a solid collider get a carving NavMeshObstacle so NPCs path around them
    /// (doors, cards, pickups, vehicles and tiny items excluded).
    /// </summary>
    public static class Spawner
    {
        static PrefabRegistry registry;
        static readonly Dictionary<string, List<GameObject>> spawned = new();
        static readonly HashSet<string> warned = new();
        static Material placeholderMat;

        /// <summary>
        /// OB_* prefabs that never get a NavMeshObstacle: doors, cards, pickups and notes, hand items, and anything that
        /// moves (carving a moving obstacle is expensive and leaves holes behind).
        /// </summary>
        static readonly HashSet<string> NoCarve = new()
        {
            "OB_Drzwi", "OB_Plot_Dziura", "OB_Karta_Historii", "OB_Skrytka", "OB_Skrzynia_Bron", "OB_Amunicja", "OB_Film",
            "OB_Kartka", "OB_Lista", "OB_Plakat", "OB_Aparat", "OB_Pistolet_Vis", "OB_Telefon",
            "OB_Auto_SD", "OB_Ciezarowka", "OB_Rower",
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            registry = null;
            placeholderMat = null;
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
                AddCarveObstacle(go, prefabName);
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

        /// <summary>
        /// Static props (barrel, table, lamp post, microphone…) carve a hole in the NavMesh at runtime. The box is the union
        /// of the prop's solid colliders in its own local space; props with a footprint below 1.2 m2 or lower than 0.5 m are skipped.
        /// </summary>
        const float MinCarveFootprint = 1.2f; // m2; only big props carve

        static void AddCarveObstacle(GameObject go, string prefabName)
        {
            if (!prefabName.StartsWith("OB_") || NoCarve.Contains(prefabName)) return;
            if (go.GetComponent<NavMeshObstacle>() != null) return;
            if (!TryLocalColliderBounds(go.transform, out var b)) return;
            if (b.size.x * b.size.z < MinCarveFootprint || b.size.y < 0.5f) return; // small props: no carving (each one re-carves the NavMesh tiles = lag)

            var obstacle = go.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = b.center;
            obstacle.size = b.size;
            obstacle.carveOnlyStationary = true;
            obstacle.carving = true;
        }

        /// <summary>Union of the enabled, non-trigger colliders under <paramref name="root"/>, in root-local space (also used by the editor menu on prefab assets).</summary>
        public static bool TryLocalColliderBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            foreach (var c in root.GetComponentsInChildren<Collider>())
            {
                if (!c.enabled || c.isTrigger) continue;
                Vector3 center, half;
                bool inColliderSpace = true;
                if (c is BoxCollider box) { center = box.center; half = box.size * 0.5f; }
                else if (c is SphereCollider sphere) { center = sphere.center; half = Vector3.one * sphere.radius; }
                else if (c is CapsuleCollider cap)
                {
                    center = cap.center;
                    float h = Mathf.Max(cap.height * 0.5f, cap.radius);
                    half = cap.direction == 0 ? new Vector3(h, cap.radius, cap.radius)
                         : cap.direction == 1 ? new Vector3(cap.radius, h, cap.radius)
                         : new Vector3(cap.radius, cap.radius, h);
                }
                else
                {
                    // Mesh and other colliders: world AABB corners (slightly generous when the prop is turned).
                    center = c.bounds.center;
                    half = c.bounds.extents;
                    inColliderSpace = false;
                }

                for (int i = 0; i < 8; i++)
                {
                    var corner = center + new Vector3((i & 1) == 0 ? -half.x : half.x, (i & 2) == 0 ? -half.y : half.y, (i & 4) == 0 ? -half.z : half.z);
                    Vector3 world = inColliderSpace ? c.transform.TransformPoint(corner) : corner;
                    Vector3 local = root.InverseTransformPoint(world);
                    if (!any) { bounds = new Bounds(local, Vector3.zero); any = true; }
                    else bounds.Encapsulate(local);
                }
            }
            return any;
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
