using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Kosynierzy.EditorTools
{
    /// <summary>
    /// One-click fixes for scene data that went stale after the code changed (found by the WS4 gameplay audit, see
    /// docs/WS4_gameplay_report.md). Serialized scene values override C# field initialisers, so changing a default in code
    /// never reaches an object that was saved earlier. All items are idempotent and undoable (Ctrl+Z); save the scene after.
    ///   Kosynierzy > Sync Player Speeds With Team Doc  — walk 2.5 / sneak 2.0 / run 5.5 m/s on the Player (scene had 4 / 2.4 / 7)
    ///   Kosynierzy > Assign VFX Shaders                — VfxLibrary.particleShader + litShader, so builds keep them
    ///   Kosynierzy > Remove Test NPCs                  — the "NPC_Test" group (3 SD, Schupo, 3 civilians, a test ForbiddenZone)
    /// </summary>
    static class SceneFixMenu
    {
        [MenuItem("Kosynierzy/Sync Player Speeds With Team Doc")]
        static void SyncPlayerSpeeds()
        {
            var fps = Object.FindAnyObjectByType<FirstPersonController>();
            if (fps == null)
            {
                Debug.LogWarning("Kosynierzy: no first-person player in the open scene.");
                return;
            }

            var so = new SerializedObject(fps);
            Undo.RecordObject(fps, "Sync player speeds");
            string report = "";
            report += SetFloat(so, "walkSpeed", 2.5f);
            report += SetFloat(so, "sprintSpeed", 5.5f);
            report += SetFloat(so, "crouchSpeed", 2.0f);
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(fps.gameObject.scene);
            Debug.Log("Kosynierzy: player speeds (team doc: walk 2.5 / sneak 2.0 / run 5.5 m/s) —" + report + " Save the scene (Ctrl+S).");
        }

        static string SetFloat(SerializedObject so, string field, float value)
        {
            var p = so.FindProperty(field);
            if (p == null) return $" {field}: field not found;";
            float before = p.floatValue;
            p.floatValue = value;
            return $" {field} {before} -> {value};";
        }

        [MenuItem("Kosynierzy/Assign VFX Shaders")]
        static void AssignVfxShaders()
        {
            var vfx = Object.FindAnyObjectByType<VfxLibrary>();
            if (vfx == null)
            {
                Debug.LogWarning("Kosynierzy: no VfxLibrary in the open scene — run Kosynierzy > Upgrade Combat Setup first.");
                return;
            }

            var so = new SerializedObject(vfx);
            Undo.RecordObject(vfx, "Assign VFX shaders");
            bool ok = Assign(so, "particleShader", "Universal Render Pipeline/Particles/Unlit");
            ok &= Assign(so, "litShader", "Universal Render Pipeline/Lit");
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(vfx.gameObject.scene);
            Debug.Log(ok
                ? "Kosynierzy: VfxLibrary shaders assigned (particleShader = Particles/Unlit, litShader = Lit). Save the scene."
                : "Kosynierzy: VfxLibrary shaders only partly assigned — see the warnings above.");
        }

        static bool Assign(SerializedObject so, string field, string shaderName)
        {
            var prop = so.FindProperty(field);
            var shader = Shader.Find(shaderName);
            if (prop == null || shader == null)
            {
                Debug.LogWarning($"Kosynierzy: could not assign '{field}' (field or shader '{shaderName}' not found).");
                return false;
            }
            prop.objectReferenceValue = shader;
            return true;
        }

        [MenuItem("Kosynierzy/Remove Test NPCs")]
        static void RemoveTestNpcs()
        {
            // NPC_Test comes from Kosynierzy > Create Enemy Test Setup: 3 SD + 1 Schupo + 3 civilians + waypoints + a test
            // ForbiddenZone. Their saved values are stale (fillFar 0.35 instead of 0.18 …) and the leftover test zone makes
            // ForbiddenZone.EnsureDefaults() skip the real zones (Naujocks' room, the radio grounds).
            var group = GameObject.Find("NPC_Test");
            if (group == null)
            {
                Debug.Log("Kosynierzy: no NPC_Test group in the open scene — nothing to remove.");
                return;
            }
            int children = group.transform.childCount;
            var scene = group.scene;
            Undo.DestroyObjectImmediate(group);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Kosynierzy: removed NPC_Test with {children} children. The mission's NpcDirector spawns the real NPCs at the contract points. Save the scene.");
        }
    }
}
