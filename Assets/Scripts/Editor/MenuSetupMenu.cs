using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace Kosynierzy.EditorTools
{
    /// <summary>
    /// Kosynierzy > Create Pause And Settings Menu: "Menus" object (PauseMenu + SettingsMenu) and exactly one
    /// Input System EventSystem in the open scene. Kosynierzy > Create Main Menu Scene: Assets/_Gra/Sceny/Menu.unity
    /// with MainMenu, camera and EventSystem, and both Menu and Gra in Build Settings. Both idempotent.
    /// </summary>
    static class MenuSetupMenu
    {
        const string MenuScenePath = "Assets/_Gra/Sceny/Menu.unity";
        const string GameSceneName = "Gra";

        [MenuItem("Kosynierzy/Create Pause And Settings Menu")]
        static void CreatePauseMenu()
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Create Pause And Settings Menu");
            int group = Undo.GetCurrentGroup();

            var pause = Object.FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);
            if (pause == null)
            {
                var created = new GameObject("Menus");
                Undo.RegisterCreatedObjectUndo(created, "Create Menus");
                pause = created.AddComponent<PauseMenu>();
            }
            var settings = pause.GetComponent<SettingsMenu>();
            if (settings == null) settings = Undo.AddComponent<SettingsMenu>(pause.gameObject);
            LinkSettings(pause, settings);

            EnsureSingleEventSystem(pause.gameObject.scene);

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(pause.gameObject.scene);
            Selection.activeGameObject = pause.gameObject;
            Debug.Log("Kosynierzy: pause + settings menu ready (Esc / Start). Subscribe to PauseMenu.RestartFromCheckpointRequested " +
                      "for the checkpoint button. Save the scene (Ctrl+S).");
        }

        [MenuItem("Kosynierzy/Create Main Menu Scene")]
        static void CreateMainMenuScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var previousActive = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(MenuScenePath);
            bool openedHere = false;
            try
            {
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath) != null)
                        scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Additive);
                    else
                    {
                        EnsureFolder("Assets/_Gra");
                        EnsureFolder("Assets/_Gra/Sceny");
                        scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                    }
                    openedHere = true;
                }

                SceneManager.SetActiveScene(scene);
                PopulateMenuScene(scene);
                if (!EditorSceneManager.SaveScene(scene, MenuScenePath))
                    Debug.LogError($"Kosynierzy: could not save {MenuScenePath}.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Kosynierzy: main menu scene setup failed ({e.Message}). Save or close untitled scenes and try again.");
            }
            finally
            {
                if (previousActive.IsValid() && previousActive.isLoaded && previousActive != scene)
                    SceneManager.SetActiveScene(previousActive);
                if (openedHere && scene.IsValid() && scene.isLoaded && SceneManager.sceneCount > 1)
                    EditorSceneManager.CloseScene(scene, true);
            }

            AddScenesToBuildSettings();
            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath);
            if (asset != null) EditorGUIUtility.PingObject(asset);
        }

        static void PopulateMenuScene(Scene scene)
        {
            MainMenu menu = null;
            Camera cam = null;
            EventSystem eventSystem = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (menu == null) menu = root.GetComponentInChildren<MainMenu>(true);
                if (cam == null) cam = root.GetComponentInChildren<Camera>(true);
                if (eventSystem == null) eventSystem = root.GetComponentInChildren<EventSystem>(true);
            }

            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.07f, 0.06f, 0.05f, 1f);
                camGo.AddComponent<AudioListener>();
            }

            if (eventSystem == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<InputSystemUIInputModule>();
            }

            if (menu == null)
            {
                var menuGo = new GameObject("MainMenu");
                menu = menuGo.AddComponent<MainMenu>();
            }
            var settings = menu.GetComponent<SettingsMenu>();
            if (settings == null) settings = menu.gameObject.AddComponent<SettingsMenu>();
            var so = new SerializedObject(menu);
            so.FindProperty("settingsMenu").objectReferenceValue = settings;
            so.FindProperty("gameSceneName").stringValue = GameSceneName;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
        }

        static void AddScenesToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath) != null)
            {
                // Menu goes first so a build starts in it.
                scenes.RemoveAll(s => s.path == MenuScenePath);
                scenes.Insert(0, new EditorBuildSettingsScene(MenuScenePath, true));
            }

            string gamePath = FindScenePath(GameSceneName);
            if (gamePath == null)
            {
                Debug.LogWarning($"Kosynierzy: no scene named '{GameSceneName}' yet; run this menu again once it exists " +
                                 "(Nowa gra loads it by name).");
            }
            else
            {
                int index = scenes.FindIndex(s => s.path == gamePath);
                if (index < 0) scenes.Add(new EditorBuildSettingsScene(gamePath, true));
                else scenes[index] = new EditorBuildSettingsScene(gamePath, true);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"Kosynierzy: Build Settings scenes: {string.Join(", ", scenes.ConvertAll(s => s.path))}.");
        }

        static string FindScenePath(string sceneName)
        {
            string fallback = null;
            foreach (var guid in AssetDatabase.FindAssets(sceneName + " t:Scene"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) != sceneName) continue;
                if (path.StartsWith("Assets/_Gra/")) return path;
                fallback ??= path;
            }
            return fallback;
        }

        static void LinkSettings(PauseMenu pause, SettingsMenu settings)
        {
            var so = new SerializedObject(pause);
            so.FindProperty("settingsMenu").objectReferenceValue = settings;
            so.ApplyModifiedProperties();
        }

        /// <summary>Keeps one EventSystem (preferring an Input System one), removes the rest, swaps any legacy module.</summary>
        static void EnsureSingleEventSystem(Scene scene)
        {
            var all = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
            EventSystem keep = null;
            foreach (var es in all)
                if (es.GetComponent<InputSystemUIInputModule>() != null) { keep = es; break; }
            if (keep == null && all.Length > 0) keep = all[0];

            foreach (var es in all)
            {
                if (es == keep) continue;
                var go = es.gameObject;
                if (IsEventSystemOnly(go)) Undo.DestroyObjectImmediate(go);
                else
                {
                    foreach (var module in go.GetComponents<BaseInputModule>()) Undo.DestroyObjectImmediate(module);
                    Undo.DestroyObjectImmediate(es);
                }
            }

            if (keep == null)
            {
                var go = new GameObject("EventSystem");
                Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
                if (go.scene != scene) SceneManager.MoveGameObjectToScene(go, scene);
                keep = go.AddComponent<EventSystem>();
            }

            var legacy = keep.GetComponent<StandaloneInputModule>();
            if (legacy != null) Undo.DestroyObjectImmediate(legacy);
            if (keep.GetComponent<InputSystemUIInputModule>() == null) Undo.AddComponent<InputSystemUIInputModule>(keep.gameObject);
        }

        static bool IsEventSystemOnly(GameObject go)
        {
            if (go.transform.childCount > 0) return false;
            foreach (var c in go.GetComponents<Component>())
                if (!(c is Transform || c is EventSystem || c is BaseInputModule)) return false;
            return true;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
