using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Kosynierzy.EditorTools
{
    /// <summary>
    /// One-click scene setup: Kosynierzy > Create First-Person Player.
    /// Builds a Player with CharacterController, moves the Main Camera to eye height inside it,
    /// and adds a test ground with a few boxes when the scene has no ground yet.
    /// </summary>
    static class PlayerSetupMenu
    {
        const float EyeHeight = 1.6f;

        [MenuItem("Kosynierzy/Create First-Person Player")]
        static void CreatePlayer()
        {
            var existing = Object.FindAnyObjectByType<FirstPersonController>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("Kosynierzy: scene already has a first-person player — selected it.");
                return;
            }

            var player = new GameObject("Player");
            Undo.RegisterCreatedObjectUndo(player, "Create First-Person Player");
            player.transform.position = Vector3.zero;

            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);

            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                Undo.RegisterCreatedObjectUndo(camGo, "Create Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            Undo.SetTransformParent(cam.transform, player.transform, "Parent Camera");
            cam.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
            cam.transform.localRotation = Quaternion.identity;
            cam.nearClipPlane = 0.05f;

            var fps = player.AddComponent<FirstPersonController>();
            var so = new SerializedObject(fps);
            so.FindProperty("cameraPivot").objectReferenceValue = cam.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (GameObject.Find("Ground") == null) CreateTestArena();

            Selection.activeGameObject = player;
            EditorSceneManager.MarkSceneDirty(player.scene);
            Debug.Log("Kosynierzy: first-person player created. Press Play, WASD to walk, Shift to sprint, mouse to look.");
        }

        [MenuItem("Kosynierzy/Add Weapon And HUD")]
        static void AddWeaponAndHud()
        {
            var fps = Object.FindAnyObjectByType<FirstPersonController>();
            if (fps == null)
            {
                CreatePlayer();
                fps = Object.FindAnyObjectByType<FirstPersonController>();
            }
            var cam = fps.GetComponentInChildren<Camera>();

            if (cam.GetComponent<PlayerWeapon>() == null)
            {
                // Placeholder gun; the art team's model replaces this child.
                var model = GameObject.CreatePrimitive(PrimitiveType.Cube);
                model.name = "WeaponModel";
                Object.DestroyImmediate(model.GetComponent<Collider>());
                Undo.RegisterCreatedObjectUndo(model, "Create Weapon Model");
                model.transform.SetParent(cam.transform, false);
                model.transform.localPosition = new Vector3(0.22f, -0.2f, 0.45f);
                model.transform.localScale = new Vector3(0.07f, 0.09f, 0.55f);

                var weapon = Undo.AddComponent<PlayerWeapon>(cam.gameObject);
                var so = new SerializedObject(weapon);
                so.FindProperty("viewModel").objectReferenceValue = model.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (cam.GetComponent<PlayerInteractor>() == null) Undo.AddComponent<PlayerInteractor>(cam.gameObject);

            if (fps.GetComponent<Health>() == null)
            {
                var health = Undo.AddComponent<Health>(fps.gameObject);
                var hso = new SerializedObject(health);
                hso.FindProperty("destroyOnDeath").boolValue = false;
                hso.ApplyModifiedPropertiesWithoutUndo();
            }
            if (fps.GetComponent<PlayerRespawn>() == null) Undo.AddComponent<PlayerRespawn>(fps.gameObject);

            if (Object.FindAnyObjectByType<CombatHud>() == null)
            {
                var hud = new GameObject("HUD");
                Undo.RegisterCreatedObjectUndo(hud, "Create HUD");
                hud.AddComponent<CombatHud>();
            }

            EditorSceneManager.MarkSceneDirty(fps.gameObject.scene);
            Debug.Log("Kosynierzy: weapon and HUD ready. LMB fires, R reloads.");
        }

        static void CreateTestArena()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(5f, 1f, 5f); // 50 x 50 m
            Undo.RegisterCreatedObjectUndo(ground, "Create Ground");

            // A few boxes so walking and sprinting can be felt against something.
            Vector3[] positions = { new(4f, 0.5f, 6f), new(-5f, 1f, 9f), new(8f, 0.75f, -4f), new(-7f, 0.5f, -6f) };
            float[] sizes = { 1f, 2f, 1.5f, 1f };
            for (int i = 0; i < positions.Length; i++)
            {
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = $"TestBox{i + 1}";
                box.transform.position = positions[i];
                box.transform.localScale = Vector3.one * sizes[i];
                Undo.RegisterCreatedObjectUndo(box, "Create Test Box");
            }
        }
    }
}
