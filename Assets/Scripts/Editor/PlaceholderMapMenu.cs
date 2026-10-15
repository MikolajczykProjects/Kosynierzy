using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Kosynierzy.EditorTools
{
    /// <summary>
    /// Kosynierzy > Create Placeholder Map: a grey-box stand-in for Mapa_Gliwice.prefab (team A) with every
    /// contract point the mission uses under "Punkty", interiors far away at x ≥ 1000 (as the team doc says),
    /// simple blocks for buildings and a 111 m radio tower landmark — so the whole story can be played
    /// before the real map lands. Delete "Mapa_Testowa" when the real map is in the scene.
    /// </summary>
    static class PlaceholderMapMenu
    {
        static Transform points;
        static Transform blocks;
        static Material blockMat, floorMat, towerMat;

        [MenuItem("Kosynierzy/Create Placeholder Map")]
        static void Create()
        {
            var old = GameObject.Find("Mapa_Testowa");
            if (old != null) Undo.DestroyObjectImmediate(old);
            var oldQuests = GameObject.Find("Quests");
            if (oldQuests != null) Undo.DestroyObjectImmediate(oldQuests); // replaced by Mission

            var root = new GameObject("Mapa_Testowa");
            Undo.RegisterCreatedObjectUndo(root, "Create Placeholder Map");
            points = new GameObject("Punkty").transform;
            points.SetParent(root.transform);
            blocks = new GameObject("Bryly").transform;
            blocks.SetParent(root.transform);

            blockMat = Mat("PH_Block", new Color(0.62f, 0.56f, 0.5f));
            floorMat = Mat("PH_Floor", new Color(0.42f, 0.4f, 0.36f));
            towerMat = Mat("PH_Tower", new Color(0.45f, 0.32f, 0.2f));

            // Big ground for the city (the old 50 m test "Ground" stays where it is).
            var ground = GameObject.Find("Ground");
            if (ground != null) ground.transform.localScale = new Vector3(50f, 1f, 50f); // 500 x 500 m

            // ---- City (street runs along Z through x = 0, north = +Z) --------------------------------
            P("spawn_dworzec", 0, -120, 0);
            Block(new Vector3(0, 4, -135), new Vector3(40, 8, 12)); // station
            P("miasto_kolejarz", 8, -114, 180);

            Building("miasto_drzwi_ksiegarnia", -12, -80, 90);
            Building("miasto_drzwi_hotel", 12, -40, -90, big: true);
            Building("miasto_drzwi_fotograf", -12, -20, 90);
            P("miasto_skrytka_most", 25, 5, 0);
            Block(new Vector3(25, 0.5f, 0), new Vector3(6, 1, 14)); // bridge

            string[] route = { "miasto_auto_sd_trasa_1", "miasto_auto_sd_trasa_2", "miasto_auto_sd_trasa_3", "miasto_auto_sd_trasa_4", "miasto_auto_sd_trasa_5" };
            Vector3[] routePos = { new(4, 0, -32), new(4, 0, 10), new(0, 0, 55), new(-20, 0, 95), new(-32, 0, 135) };
            for (int i = 0; i < route.Length; i++) P(route[i], routePos[i].x, routePos[i].z, 0);

            P("miasto_zagajnik_krzaki", -46, 128, 45);
            P("miasto_zagajnik_auto", -34, 138, 0);
            for (int i = 0; i < 6; i++) Block(new Vector3(-52 + i * 2.5f, 0.6f, 124 + (i % 2) * 3), new Vector3(1.5f, 1.2f, 1.5f)); // bushes

            P("miasto_dziura_plot", 45, 150, 90);
            P("miasto_drzwi_radio", 62, 128, 0);
            Block(new Vector3(48, 1.2f, 160), new Vector3(0.3f, 2.4f, 40)); // fence
            P("miasto_stodola", 150, 40, -90);
            Block(new Vector3(156, 3, 40), new Vector3(10, 6, 14));
            P("miasto_rower", 30, 140, 90);

            Vector3[] cards = { new(-6, 0, -100), new(16, 0, -60), new(-8, 0, -5), new(10, 0, 40), new(-26, 0, 110), new(120, 0, 60) };
            for (int i = 0; i < cards.Length; i++) P($"miasto_karta_{i + 1}", cards[i].x, cards[i].z, 0);

            for (int i = 0; i < 6; i++) P($"miasto_patrol_sd_{i + 1}", (i % 2 == 0 ? 6 : -6), -60 + i * 25, 0);
            for (int i = 0; i < 3; i++) P($"miasto_schupo_{i + 1}", -4, -90 + i * 45, 180);
            for (int i = 0; i < 10; i++) P($"miasto_tlum_{i + 1}", (i % 2 == 0 ? 3 : -3), -110 + i * 15, 0);
            for (int i = 0; i < 20; i++) P($"miasto_latarnia_{i + 1}", (i % 2 == 0 ? 7 : -7), -120 + i * 13, 0);
            for (int i = 0; i < 12; i++) P($"miasto_oslona_{i + 1}", (i % 2 == 0 ? 5 : -5), -70 + i * 15, 0);

            // ---- Radio station grounds (north-east) -----------------------------------------------------
            P("spawn_radio_tyl", 52, 160, 90);
            P("spawn_radio_brama", 66, 134, 0);
            P("radio_drzwi_wyjscie", 66, 140, 180);
            Block(new Vector3(82, 3, 168), new Vector3(16, 6, 10)); // technical building
            P("radio_technika_kable", 76, 162, 0);
            P("radio_kartka_grubera", 79, 162, 0);
            P("radio_sterownia_mikrofon", 86, 162, 0);
            P("radio_piwnica_drzwi", 92, 162, 0);
            for (int i = 0; i < 3; i++) P($"radio_piwnica_pracownik_{i + 1}", 95 + i, 158, 180);
            for (int i = 0; i < 5; i++) P($"radio_sd_{i + 1}", 60 + i * 7, 150 + (i % 2) * 6, 180);
            P("radio_cialo_honiok", 70, 145, 90);
            P("radio_kartka_honiok", 71.2f, 145.6f, 0);
            for (int i = 0; i < 4; i++) P($"radio_latarnia_{i + 1}", 60 + i * 10, 142, 0);
            var tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tower.name = "Wieza_111m";
            tower.transform.SetParent(blocks);
            tower.transform.position = new Vector3(100, 55.5f, 178);
            tower.transform.localScale = new Vector3(3f, 55.5f, 3f);
            tower.GetComponent<Renderer>().sharedMaterial = towerMat;

            // ---- Interiors far away (x >= 1000) ----------------------------------------------------------
            Floor(new Vector3(1000, 0, 10), 30);
            P("spawn_hotel_wejscie", 1000, 0, 0);
            P("hotel_drzwi_wyjscie", 1000, -3, 180);
            P("hotel_recepcja", 1005, 5, -90);
            P("hotel_telefon", 993, 9, 90);
            P("hotel_kolumna", 995, 8, 0);
            Block(new Vector3(995, 2, 8.8f), new Vector3(0.8f, 4, 0.8f)); // column
            P("hotel_pokoj_drzwi", 1000, 14, 0);
            P("hotel_pokoj_mapa", 998, 20, 0);
            P("hotel_pokoj_lista", 1003, 20, 0);
            Block(new Vector3(998, 0.4f, 20), new Vector3(1.6f, 0.8f, 1f)); // desk
            Block(new Vector3(1003, 0.4f, 20), new Vector3(1.6f, 0.8f, 1f));
            P("hotel_patrol_1", 1008, 6, 0);
            P("hotel_patrol_2", 1008, 18, 180);
            for (int i = 0; i < 3; i++) P($"hotel_latarnia_{i + 1}", 994 + i * 6, 12, 0);

            Floor(new Vector3(1100, 0, 5), 16);
            P("spawn_ksiegarnia", 1100, 0, 0);
            P("ksiegarnia_drzwi_wyjscie", 1100, -3, 180);
            P("ksiegarnia_zofia", 1100, 6, 180);

            Floor(new Vector3(1200, 0, 5), 16);
            P("spawn_fotograf", 1200, 0, 0);
            P("fotograf_drzwi_wyjscie", 1200, -3, 180);
            P("fotograf_wlasciciel", 1202, 6, 180);
            P("fotograf_gestapo", 1197, 6, 180);
            P("fotograf_ciemnia", 1200, 10, 0);

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            Debug.Log($"Kosynierzy: placeholder map with {points.childCount} points. Bake the NavMesh again (Create Enemy Test Setup).");
        }

        static void P(string name, float x, float z, float yaw) => P(name, x, 0f, z, yaw);

        static void P(string name, float x, float y, float z, float yaw)
        {
            var t = new GameObject(name).transform;
            t.SetParent(points);
            t.SetPositionAndRotation(new Vector3(x, y, z), Quaternion.Euler(0f, yaw, 0f));
        }

        // A building block behind a street door; the door point faces the street (yaw).
        static void Building(string doorPoint, float x, float z, float yaw, bool big = false)
        {
            P(doorPoint, x, z, yaw);
            var rot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 size = big ? new Vector3(20, 12, 16) : new Vector3(12, 8, 10);
            Vector3 center = new Vector3(x, 0, z) - rot * Vector3.forward * (size.z * 0.5f + 0.3f) + Vector3.up * size.y * 0.5f;
            var b = Block(center, size);
            b.transform.rotation = rot;
        }

        static GameObject Block(Vector3 center, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Bryla";
            go.transform.SetParent(blocks);
            go.transform.position = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = blockMat;
            return go;
        }

        static void Floor(Vector3 center, float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Podloga_Wnetrza";
            go.transform.SetParent(blocks);
            go.transform.position = center - Vector3.up * 0.5f;
            go.transform.localScale = new Vector3(size, 1f, size);
            go.GetComponent<Renderer>().sharedMaterial = floorMat;
        }

        static Material Mat(string name, Color c)
        {
            string path = $"Assets/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }
    }
}
