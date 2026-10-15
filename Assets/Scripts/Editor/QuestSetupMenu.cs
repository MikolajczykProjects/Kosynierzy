using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Kosynierzy.EditorTools
{
    /// <summary>
    /// Kosynierzy > Create Quest Test Setup: a 4-step sample quest line on the test arena
    /// (reach a point, take ammo from a crate, collect 3 documents, kill all enemies) so every
    /// objective type can be tried. The story team replaces these with real steps.
    /// </summary>
    static class QuestSetupMenu
    {
        [MenuItem("Kosynierzy/Create Quest Test Setup")]
        static void CreateQuestSetup()
        {
            if (Object.FindAnyObjectByType<QuestManager>() != null)
            {
                Selection.activeGameObject = Object.FindAnyObjectByType<QuestManager>().gameObject;
                Debug.Log("Kosynierzy: scene already has a QuestManager — selected it.");
                return;
            }

            var root = new GameObject("Quests");
            Undo.RegisterCreatedObjectUndo(root, "Create Quest Test Setup");

            // 1. Reach a point.
            var reach = new GameObject("Step1_ReachPoint");
            reach.transform.SetParent(root.transform);
            reach.transform.position = new Vector3(10f, 1.5f, 8f);
            var box = reach.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(4f, 3f, 4f);
            var step1 = reach.AddComponent<ReachLocationObjective>();
            SetText(step1, "Dotrzyj do punktu zbornego", "Spotkaj się z resztą obrońców przy skrzyniach.");

            // 2. Use the ammo crate (gives reserve ammo through a persistent UnityEvent).
            var crate = CreateProp("AmmoCrate", PrimitiveType.Cube, new Vector3(12f, 0.4f, 10f), new Vector3(1.2f, 0.8f, 0.8f), root.transform);
            var crateUse = crate.AddComponent<Interactable>();
            SetSerialized(crateUse, "prompt", "Weź amunicję");
            SetSerialized(crateUse, "singleUse", true);
            var weapon = Object.FindAnyObjectByType<PlayerWeapon>();
            if (weapon != null)
            {
                var so = new SerializedObject(crateUse);
                so.ApplyModifiedPropertiesWithoutUndo();
                UnityEventTools.AddIntPersistentListener(GetEvent(crateUse), weapon.AddReserveAmmo, 24);
            }
            var step2Go = new GameObject("Step2_TakeAmmo");
            step2Go.transform.SetParent(root.transform);
            var step2 = step2Go.AddComponent<InteractObjective>();
            SetText(step2, "Uzupełnij amunicję", "Weź naboje ze skrzyni (E).");
            SetSerialized(step2, "target", crateUse);
            SetSerialized(step2, "marker", crate.transform);

            // 3. Collect three documents.
            var step3Go = new GameObject("Step3_CollectDocuments");
            step3Go.transform.SetParent(root.transform);
            step3Go.transform.position = new Vector3(-8f, 0f, 4f);
            var step3 = step3Go.AddComponent<CollectObjective>();
            SetText(step3, "Zbierz meldunki", "Trzy meldunki leżą porozrzucane na placu.");
            Vector3[] docPositions = { new(-8f, 0.15f, 2f), new(-12f, 0.15f, 7f), new(-5f, 0.15f, 9f) };
            var docsProp = new SerializedObject(step3).FindProperty("items");
            var so3 = docsProp.serializedObject;
            docsProp.arraySize = docPositions.Length;
            for (int i = 0; i < docPositions.Length; i++)
            {
                var doc = CreateProp($"Document{i + 1}", PrimitiveType.Cube, docPositions[i], new Vector3(0.4f, 0.05f, 0.3f), step3Go.transform);
                // Bigger invisible trigger so a flat paper is easy to aim at.
                var pick = doc.AddComponent<BoxCollider>();
                pick.isTrigger = true;
                pick.size = new Vector3(2f, 12f, 2f);
                var item = doc.AddComponent<Interactable>();
                SetSerialized(item, "prompt", "Podnieś meldunek");
                SetSerialized(item, "singleUse", true);
                SetSerialized(item, "hideOnUse", true);
                docsProp.GetArrayElementAtIndex(i).objectReferenceValue = item;
            }
            so3.ApplyModifiedPropertiesWithoutUndo();

            // 4. Kill all enemies alive when the step starts.
            var step4Go = new GameObject("Step4_KillPatrol");
            step4Go.transform.SetParent(root.transform);
            step4Go.transform.position = new Vector3(0f, 0f, 18f);
            var step4 = step4Go.AddComponent<KillObjective>();
            SetText(step4, "Zlikwiduj patrol", "Niemiecki patrol zbliża się od północy.");

            var manager = root.AddComponent<QuestManager>();
            var mso = new SerializedObject(manager);
            var steps = mso.FindProperty("steps");
            QuestObjective[] all = { step1, step2, step3, step4 };
            steps.arraySize = all.Length;
            for (int i = 0; i < all.Length; i++) steps.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
            mso.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("Kosynierzy: quest test setup ready (4 steps).");
        }

        static GameObject CreateProp(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Transform parent)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
            return go;
        }

        static void SetText(QuestObjective step, string title, string description)
        {
            var so = new SerializedObject(step);
            so.FindProperty("title").stringValue = title;
            so.FindProperty("description").stringValue = description;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetSerialized(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            switch (value)
            {
                case string s: p.stringValue = s; break;
                case bool b: p.boolValue = b; break;
                case Object o: p.objectReferenceValue = o; break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static UnityEngine.Events.UnityEvent GetEvent(Interactable target) =>
            (UnityEngine.Events.UnityEvent)typeof(Interactable)
                .GetField("onInteract", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(target);
    }
}
