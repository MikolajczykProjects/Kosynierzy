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
