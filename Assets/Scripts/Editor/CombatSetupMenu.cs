using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Kosynierzy.EditorTools
{
    /// <summary>
    /// Kosynierzy > Upgrade Combat Setup: adds the VFX library, combat feedback and sound bank to the scene,
    /// applies the tuned weapon values and upgrades the enemy prefab (head hitbox, death effect).
    /// Safe to run again.
    /// </summary>
    static class CombatSetupMenu
    {
        const string EnemyPrefabPath = "Assets/Prefabs/Enemy_Test.prefab";

        [MenuItem("Kosynierzy/Upgrade Combat Setup")]
        static void Upgrade()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            var vfx = Object.FindAnyObjectByType<VfxLibrary>();
            if (vfx == null)
            {
                var go = new GameObject("VfxLibrary");
                Undo.RegisterCreatedObjectUndo(go, "Add VFX Library");
                vfx = go.AddComponent<VfxLibrary>();
            }
            // Both shaders are only reached by Shader.Find at runtime: referencing them here keeps them in builds.
            var vso = new SerializedObject(vfx);
            AssignShader(vso, "particleShader", "Universal Render Pipeline/Particles/Unlit");
            AssignShader(vso, "litShader", "Universal Render Pipeline/Lit");
            vso.ApplyModifiedPropertiesWithoutUndo();

            var hud = Object.FindAnyObjectByType<CombatHud>();
            if (hud == null)
            {
                var go = new GameObject("HUD");
                Undo.RegisterCreatedObjectUndo(go, "Create HUD");
                hud = go.AddComponent<CombatHud>();
            }
            if (hud.GetComponent<CombatFeedback>() == null) Undo.AddComponent<CombatFeedback>(hud.gameObject);
            var bank = hud.GetComponent<SfxBank>();
            if (bank == null) bank = Undo.AddComponent<SfxBank>(hud.gameObject);
            FillSfxSlots(bank);

            var weapon = Object.FindAnyObjectByType<PlayerWeapon>();
            if (weapon != null)
            {
                var wso = new SerializedObject(weapon);
                wso.FindProperty("damage").floatValue = 34f;
                wso.FindProperty("fireRate").floatValue = 420f;
                wso.FindProperty("reloadTime").floatValue = 1.5f;
                wso.ApplyModifiedPropertiesWithoutUndo();
            }

            UpgradeEnemyPrefab();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Kosynierzy: combat upgraded (VFX library, feedback, sound bank, enemy head hitbox + death effect).");
        }

        static void AssignShader(SerializedObject so, string field, string shaderName)
        {
            var prop = so.FindProperty(field);
            var shader = Shader.Find(shaderName);
            if (prop == null || shader == null)
            {
                Debug.LogWarning($"Kosynierzy: could not assign '{field}' on the VfxLibrary (field or shader '{shaderName}' not found).");
                return;
            }
            prop.objectReferenceValue = shader;
        }

        static void FillSfxSlots(SfxBank bank)
        {
            var so = new SerializedObject(bank);
            var entries = so.FindProperty("entries");
            var ids = System.Enum.GetValues(typeof(SfxBank.Sfx));
            if (entries.arraySize == ids.Length) return;
            entries.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++)
            {
                var e = entries.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("Id").enumValueIndex = i;
                e.FindPropertyRelative("Volume").floatValue = 1f;
                e.FindPropertyRelative("PitchJitter").floatValue = 0.06f;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void UpgradeEnemyPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath) == null)
            {
                Debug.LogWarning("Kosynierzy: no enemy prefab yet — run Create Enemy Test Setup first.");
                return;
            }
            var root = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
            try
            {
                // Body collider stops below the head so head shots reach the head hitbox.
                var capsule = root.GetComponent<CapsuleCollider>();
                if (capsule != null)
                {
                    capsule.height = 1.4f;
                    capsule.center = new Vector3(0f, 0.7f, 0f);
                }

                var head = root.transform.Find("HeadHitbox");
                if (head == null)
                {
                    head = new GameObject("HeadHitbox").transform;
                    head.SetParent(root.transform, false);
                    head.localPosition = new Vector3(0f, 1.6f, 0f);
                    var sphere = head.gameObject.AddComponent<SphereCollider>();
                    sphere.radius = 0.22f;
                    head.gameObject.AddComponent<Hitbox>();
                }

                if (root.GetComponent<DeathEffect>() == null) root.AddComponent<DeathEffect>();
                PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
