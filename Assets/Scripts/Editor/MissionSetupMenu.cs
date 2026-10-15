using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Kosynierzy.EditorTools
{
    /// <summary>
    /// Kosynierzy > Setup Mission: wires the story into the scene — Mission object, photo camera and noise
    /// on the player, one-hit Health (team rule), Vis wz. 35 ammo (8 + 0). Also
    /// Kosynierzy > Refresh Prefab Registry: scans Assets/_Postacie and Assets/_Obiekty for CH_* / OB_* prefabs.
    /// </summary>
    static class MissionSetupMenu
    {
        const string RegistryPath = "Assets/Resources/PrefabRegistry.asset";

        [MenuItem("Kosynierzy/Setup Mission")]
        static void Setup()
        {
            var fps = Object.FindAnyObjectByType<FirstPersonController>();
            if (fps == null) { Debug.LogWarning("Kosynierzy: no player — run Create First-Person Player first."); return; }
            var cam = fps.GetComponentInChildren<Camera>();

            if (fps.GetComponent<PlayerNoise>() == null) Undo.AddComponent<PlayerNoise>(fps.gameObject);
            if (cam != null && cam.GetComponent<PhotoCamera>() == null) Undo.AddComponent<PhotoCamera>(cam.gameObject);
            if (cam != null && cam.GetComponent<PlayerHands>() == null) Undo.AddComponent<PlayerHands>(cam.gameObject);

            var health = fps.GetComponent<Health>();
            if (health == null) health = Undo.AddComponent<Health>(fps.gameObject);
            var hso = new SerializedObject(health);
            hso.FindProperty("maxHealth").floatValue = 1f;
            hso.FindProperty("destroyOnDeath").boolValue = false;
            hso.ApplyModifiedPropertiesWithoutUndo();
            if (fps.GetComponent<PlayerRespawn>() == null) Undo.AddComponent<PlayerRespawn>(fps.gameObject);

            var weapon = Object.FindAnyObjectByType<PlayerWeapon>();
            if (weapon != null)
            {
                var wso = new SerializedObject(weapon);
                wso.FindProperty("magazineSize").intValue = 8;
                wso.FindProperty("reserveAmmo").intValue = 0;
                wso.FindProperty("requireAimToFire").boolValue = true;
                wso.ApplyModifiedPropertiesWithoutUndo();
            }

            if (Object.FindAnyObjectByType<Mission>() == null)
            {
                var go = new GameObject("Mission");
                Undo.RegisterCreatedObjectUndo(go, "Add Mission");
                go.AddComponent<Mission>();
            }

            RefreshRegistry();
            EditorSceneManager.MarkSceneDirty(fps.gameObject.scene);
            Debug.Log("Kosynierzy: mission wired (Mission, photo camera F, player noise, one-hit health, Vis 8+0).");
        }

        [MenuItem("Kosynierzy/Refresh Prefab Registry")]
        static void RefreshRegistry()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            var reg = AssetDatabase.LoadAssetAtPath<PrefabRegistry>(RegistryPath);
            if (reg == null)
            {
                reg = ScriptableObject.CreateInstance<PrefabRegistry>();
                AssetDatabase.CreateAsset(reg, RegistryPath);
            }

            var folders = new List<string>();
            foreach (var f in new[] { "Assets/_Postacie", "Assets/_Obiekty", "Assets/_Mapa" })
                if (AssetDatabase.IsValidFolder(f)) folders.Add(f);

            reg.Entries.Clear();
            if (folders.Count > 0)
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Prefab", folders.ToArray()))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null) continue;
                    if (!prefab.name.StartsWith("CH_") && !prefab.name.StartsWith("OB_") && !prefab.name.StartsWith("Mapa_")) continue;
                    reg.Entries.Add(new PrefabRegistry.Entry { Name = prefab.name, Prefab = prefab });
                }
            }
            EditorUtility.SetDirty(reg);
            AssetDatabase.SaveAssets();
            Debug.Log($"Kosynierzy: prefab registry — {reg.Entries.Count} prefabs (CH_/OB_/Mapa_) from {folders.Count} team folders.");
        }
    }
}
