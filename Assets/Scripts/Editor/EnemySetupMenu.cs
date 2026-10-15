using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Kosynierzy.EditorTools
{
    /// <summary>
    /// NPC, NavMesh and stealth menus.
    /// Kosynierzy > Create Enemy Test Setup: bakes the NavMesh and places placeholder NPCs built by NpcFactory under
    /// "NPC_Test": an SD guard, a patrolling SD, a wandering SD inside a test ForbiddenZone, a Schupo, three civilians
    /// and two hiding bushes. Safe to run again (rebuilds NPC_Test; removes the old Enemy_Test instances).
    /// Kosynierzy > Bake NavMesh (all floors): one NavMeshSurface collecting every physics collider in the scene (town
    /// and interiors at x ≥ 1000); triggers, NPCs and the player are switched off for the bake.
    /// Kosynierzy > Add Hiding Spots: hiding spots at miasto_zagajnik_krzaki / miasto_oslona_* (bushes where no art).
    /// Kosynierzy > Add NPC Director: adds the NpcDirector that spawns the town's NPCs at the map points.
    /// </summary>
    static class EnemySetupMenu
    {
        const string MaterialsFolder = "Assets/Materials";
        const string OldEnemyPrefabPath = "Assets/Prefabs/Enemy_Test.prefab";
        const string TestRootName = "NPC_Test";
        const string HidingRootName = "HidingSpots";
        static readonly Vector3[] TestBushes = { new(-3f, 0f, 8f), new(7f, 0f, 11f), new(-9f, 0f, 17f) };

        [MenuItem("Kosynierzy/Create Enemy Test Setup")]
        static void CreateSetup()
        {
            EnsureFolder(MaterialsFolder);
            var ground = EnsureGround();
            RemoveOldTestEnemies();
            var old = GameObject.Find(TestRootName);
            if (old != null) Undo.DestroyObjectImmediate(old);
            BakeNavMesh(ground); // before placing NPCs, so their colliders never cut the mesh

            var root = new GameObject(TestRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create NPC Test Setup");
            var facePlayer = Quaternion.Euler(0f, 180f, 0f); // -Z, toward the player at the origin

            Place(root, "CH_SD", CharacterType.Enemy, new Vector3(-6f, 0f, 14f), facePlayer);

            var patrol = Place(root, "CH_SD", CharacterType.Enemy, new Vector3(5f, 0f, 18f), facePlayer);
            SetWaypoints(patrol.GetComponent<EnemyController>(), root, new Vector3(10f, 0f, 18f), new Vector3(1f, 0f, 18f));

            var wanderer = Place(root, "CH_SD", CharacterType.Enemy, new Vector3(0f, 0f, 22f), facePlayer);
            SetFloat(wanderer.GetComponent<EnemyController>(), "wanderRadius", 4f);
            CreateZone(root, new Vector3(0f, 2f, 22f), new Vector3(10f, 4f, 8f));

            var schupo = Place(root, "CH_Schupo", CharacterType.Police, new Vector3(-12f, 0f, 6f), Quaternion.Euler(0f, 90f, 0f));
            SetFloat(schupo.GetComponent<PoliceController>(), "wanderRadius", 4f);

            Place(root, "CH_Cywil_M1", CharacterType.Civilian, new Vector3(8f, 0f, 4f), Quaternion.identity);
            Place(root, "CH_Cywil_K1", CharacterType.Civilian, new Vector3(12f, 0f, 9f), Quaternion.Euler(0f, -90f, 0f));
            Place(root, "CH_Cywil_M2", CharacterType.Civilian, new Vector3(-10f, 0f, -6f), Quaternion.Euler(0f, 45f, 0f));

            for (int i = 0; i < 2; i++) HidingSpot.Create($"HidingSpot_Test_{i + 1}", TestBushes[i], Quaternion.identity, root.transform);

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("Kosynierzy: NPC test setup ready — 3 SD (guard, patrol, wanderer in a forbidden zone), Schupo, 3 civilians, 2 bushes. " +
                      "Play: crouch in a bush to hide, sneak behind an SD and press E for a silent takedown.");
        }

        [MenuItem("Kosynierzy/Bake NavMesh (all floors)")]
        static void BakeAllFloors()
        {
            BakeNavMesh(GameObject.Find("Ground"));
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        [MenuItem("Kosynierzy/Add Hiding Spots")]
        static void AddHidingSpots()
        {
            Points.Rebuild();
            var old = GameObject.Find(HidingRootName);
            if (old != null) Undo.DestroyObjectImmediate(old);
            var root = new GameObject(HidingRootName);
            Undo.RegisterCreatedObjectUndo(root, "Add Hiding Spots");

            int made = 0;
            foreach (var p in HidingSpot.DefaultPoints())
            {
                HidingSpot.Create($"HidingSpot_{p.name}", p.position, p.rotation, root.transform);
                made++;
            }
            bool testArena = made == 0;
            if (testArena)
                foreach (var position in TestBushes)
                {
                    HidingSpot.Create("HidingSpot_Test", position, Quaternion.identity, root.transform);
                    made++;
                }

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log(testArena
                ? $"Kosynierzy: no miasto_zagajnik_krzaki / miasto_oslona_* points — placed {made} test bushes in the arena."
                : $"Kosynierzy: {made} hiding spots at the map points (bushes where no art stands).");
        }

        [MenuItem("Kosynierzy/Add NPC Director")]
        static void AddDirector()
        {
            var existing = Object.FindAnyObjectByType<NpcDirector>(FindObjectsInactive.Include);
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("Kosynierzy: the scene already has an NpcDirector — selected it.");
                return;
            }
            var go = new GameObject("NpcDirector");
            Undo.RegisterCreatedObjectUndo(go, "Add NPC Director");
            go.AddComponent<NpcDirector>();
            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(go.scene);
            Debug.Log("Kosynierzy: NpcDirector added — on Play it spawns SD / Schupo / civilians / workers at the map points.");
        }

        // ---------- NPCs ----------

        static GameObject Place(GameObject parent, string name, CharacterType type, Vector3 position, Quaternion rotation)
        {
            // Same shape as Spawner's placeholder, but with a saved material so it survives a scene reload.
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            go.transform.SetPositionAndRotation(position + Vector3.up * 0.9f, rotation);
            go.GetComponent<MeshRenderer>().sharedMaterial = GetOrCreateMaterial(type);
            go = NpcFactory.Setup(go, type, true);
            go.transform.SetParent(parent.transform, true);
            return go;
        }

        static void SetWaypoints(EnemyController enemy, GameObject parent, params Vector3[] positions)
        {
            if (enemy == null) return;
            var so = new SerializedObject(enemy);
            var list = so.FindProperty("waypoints");
            if (list == null) return;
            list.arraySize = positions.Length;
            for (int i = 0; i < positions.Length; i++)
            {
                var p = new GameObject($"Waypoint_{(char)('A' + i)}").transform;
                p.SetParent(parent.transform, false);
                p.position = positions[i];
                list.GetArrayElementAtIndex(i).objectReferenceValue = p;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(Object component, string field, float value)
        {
            if (component == null) return;
            var so = new SerializedObject(component);
            var prop = so.FindProperty(field);
            if (prop == null) return;
            prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreateZone(GameObject parent, Vector3 center, Vector3 size)
        {
            var zone = new GameObject("ForbiddenZone_Test") { layer = 2 }; // Ignore Raycast
            zone.transform.SetParent(parent.transform, false);
            zone.transform.position = center;
            zone.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            zone.AddComponent<ForbiddenZone>();
        }

        static void RemoveOldTestEnemies()
        {
            int removed = 0;
            foreach (var enemy in Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include))
            {
                if (enemy == null) continue;
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(enemy.gameObject) != OldEnemyPrefabPath) continue;
                var instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(enemy.gameObject);
                Undo.DestroyObjectImmediate(instanceRoot != null ? instanceRoot : enemy.gameObject);
                removed++;
            }
            var waypoints = GameObject.Find("Waypoints");
            if (waypoints != null && waypoints.transform.childCount > 0 &&
                waypoints.transform.GetChild(0).name.StartsWith("EnemyWaypoint_"))
                Undo.DestroyObjectImmediate(waypoints);
            if (removed > 0) Debug.Log($"Kosynierzy: removed {removed} old Enemy_Test instance(s) — replaced by NPC_Test.");
        }

        static Material GetOrCreateMaterial(CharacterType type)
        {
            string path = $"{MaterialsFolder}/NPC_{type}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("Kosynierzy: URP Lit shader not found — is URP the active render pipeline?");
                shader = Shader.Find("Standard");
            }
            mat = new Material(shader);
            var color = NpcFactory.TintFor(type);
            mat.SetColor("_BaseColor", color);
            mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // ---------- Arena and NavMesh ----------

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static GameObject EnsureGround()
        {
            var ground = GameObject.Find("Ground");
            if (ground != null) return ground;

            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(5f, 1f, 5f); // 50 x 50 m
            Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
            return ground;
        }

        /// <summary>
        /// Bakes one NavMesh for the whole scene (town and interiors far away, e.g. x ≥ 1000) from physics colliders.
        /// Reuses the scene's NavMeshSurface, else adds one to preferredHost (or a new "NavMesh" object).
        /// Trigger colliders, NPCs and the player are switched off during the bake so they never shape the mesh.
        /// </summary>
        static void BakeNavMesh(GameObject preferredHost)
        {
            var surface = Object.FindAnyObjectByType<NavMeshSurface>(FindObjectsInactive.Exclude);
            if (surface == null)
            {
                var host = preferredHost != null ? preferredHost : GameObject.Find("NavMesh");
                if (host == null)
                {
                    host = new GameObject("NavMesh");
                    Undo.RegisterCreatedObjectUndo(host, "Create NavMesh");
                }
                surface = Undo.AddComponent<NavMeshSurface>(host);
            }
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~((1 << 2) | (1 << 5)); // not Ignore Raycast (zones, hiding spots) nor UI
            surface.ignoreNavMeshAgent = true;

            var switchedOff = new List<Collider>();
            foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude))
            {
                if (c == null || !c.enabled) continue;
                bool skip = c.isTrigger
                            || c.GetComponentInParent<Character>() != null
                            || c.GetComponentInParent<FirstPersonController>() != null;
                if (!skip) continue;
                c.enabled = false;
                switchedOff.Add(c);
            }

            var oldData = surface.navMeshData;
            string oldPath = oldData != null ? AssetDatabase.GetAssetPath(oldData) : null;
            try
            {
                surface.BuildNavMesh();
            }
            finally
            {
                foreach (var c in switchedOff)
                    if (c != null) c.enabled = true;
            }

            if (surface.navMeshData == null || surface.navMeshData == oldData)
            {
                Debug.LogError("Kosynierzy: NavMesh bake failed — check that the floors have colliders.");
                return;
            }

            // BuildNavMesh() keeps the data in memory only; save it next to the scene so it survives a reload.
            var scene = surface.gameObject.scene;
            string scenePath = scene.path;
            string folder = string.IsNullOrEmpty(scenePath) ? "Assets/Scenes" : Path.GetDirectoryName(scenePath).Replace('\\', '/');
            string sceneName = string.IsNullOrEmpty(scenePath) ? "Untitled" : Path.GetFileNameWithoutExtension(scenePath);
            string assetPath = $"{folder}/{sceneName}_NavMesh.asset";
            EnsureFolder(folder);

            if (!string.IsNullOrEmpty(oldPath)) AssetDatabase.DeleteAsset(oldPath);
            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null) AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(surface.navMeshData, assetPath);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(surface);
            Debug.Log($"Kosynierzy: NavMesh baked on '{surface.name}' for the whole scene ({switchedOff.Count} trigger/NPC/player colliders ignored) → {assetPath}");
        }
    }
}
