using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Kosynierzy.EditorTools
{
    /// <summary>
    /// NPC test arena and director menus.
    /// Kosynierzy > Create Enemy Test Setup: bakes a NavMesh on "Ground" (physics colliders, saved next to the scene)
    /// and places placeholder NPCs built by NpcFactory under "NPC_Test": an SD guard, a patrolling SD, a wandering SD
    /// inside a test ForbiddenZone, a Schupo and three civilians. Safe to run again (rebuilds NPC_Test; removes the
    /// old Enemy_Test instances from the previous version of this menu).
    /// Kosynierzy > Add NPC Director: adds the NpcDirector that spawns the town's NPCs at the map points.
    /// </summary>
    static class EnemySetupMenu
    {
        const string MaterialsFolder = "Assets/Materials";
        const string OldEnemyPrefabPath = "Assets/Prefabs/Enemy_Test.prefab";
        const string TestRootName = "NPC_Test";

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

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("Kosynierzy: NPC test setup ready — 3 SD (guard, patrol, wanderer in a forbidden zone), Schupo, 3 civilians. " +
                      "Press Play: walk into a cone to fill '?', sprint or shoot to raise the alarm.");
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
            go = NpcFactory.Setup(go, type);
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

        static void BakeNavMesh(GameObject ground)
        {
            var surface = ground.GetComponent<NavMeshSurface>();
            if (surface == null) surface = Undo.AddComponent<NavMeshSurface>(ground);
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.ignoreNavMeshAgent = true; // NPCs never carve the mesh

            // The player's CharacterController would otherwise cut a hole at the origin.
            var player = Object.FindAnyObjectByType<FirstPersonController>();
            if (player != null)
            {
                var modifier = player.GetComponent<NavMeshModifier>();
                if (modifier == null) modifier = Undo.AddComponent<NavMeshModifier>(player.gameObject);
                modifier.ignoreFromBuild = true;
                EditorUtility.SetDirty(modifier);
            }

            var oldData = surface.navMeshData;
            string oldPath = oldData != null ? AssetDatabase.GetAssetPath(oldData) : null;

            surface.BuildNavMesh();
            if (surface.navMeshData == null || surface.navMeshData == oldData)
            {
                Debug.LogError("Kosynierzy: NavMesh bake failed — check that Ground has a collider.");
                return;
            }

            // BuildNavMesh() keeps the data in memory only; save it next to the scene so it survives a reload.
            string scenePath = ground.scene.path;
            string folder = string.IsNullOrEmpty(scenePath) ? "Assets/Scenes" : Path.GetDirectoryName(scenePath).Replace('\\', '/');
            string sceneName = string.IsNullOrEmpty(scenePath) ? "Untitled" : Path.GetFileNameWithoutExtension(scenePath);
            string assetPath = $"{folder}/{sceneName}_NavMesh.asset";
            EnsureFolder(folder);

            if (!string.IsNullOrEmpty(oldPath)) AssetDatabase.DeleteAsset(oldPath);
            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null) AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(surface.navMeshData, assetPath);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(surface);
        }
    }
}
