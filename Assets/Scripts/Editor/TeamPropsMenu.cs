using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Kosynierzy.EditorTools
{
    /// <summary>
    /// Kosynierzy > Place Team Props: static set dressing from team D's OB_* prefabs at the contract points, as scene
    /// objects under "Rekwizyty_OB" so they bake into the NavMesh (Kosynierzy > Bake NavMesh (all floors)) and give
    /// Add Hiding Spots solid art to find. Run it AFTER the map / placeholder map exists and BEFORE baking.
    ///
    /// Idempotent: a re-run deletes the old "Rekwizyty_OB" and builds it again. One Undo step; marks the scene dirty.
    /// A missing point or prefab is skipped; the single summary line in the console lists what was skipped and what found
    /// no free spot. Mission-spawned things (doors, history cards, cache, switchboard, notes, map table, list, microphone,
    /// SD car) are NOT placed here — the mission spawns those at runtime.
    ///
    /// Placement rules: cover props stand exactly on miasto_oslona_N; everything else is offset from its anchor point so
    /// the point itself (an NPC spot, a route point) stays free, never closer than a margin to any named point, never on
    /// the SD car's straight route legs, and only where nothing solid is in the way.
    /// </summary>
    static class TeamPropsMenu
    {
        const string RootName = "Rekwizyty_OB";
        const string PrefabFolder = "Assets/_Obiekty/Prefaby/";

        static readonly string[] CoverCycle = { "OB_Beczka", "OB_Skrzynia", "OB_Worki" };
        static readonly string[] StreetCycle =
        {
            "OB_Lawka", "OB_Kosz", "OB_Slup_Ogloszeniowy", "OB_Hydrant", "OB_Kiosk",
            "OB_Wozek_Reczny", "OB_Lawka", "OB_Kosz", "OB_Hydrant", "OB_Slup_Ogloszeniowy",
        };

        // State of one run.
        static Transform root;
        static int placed;
        static readonly List<string> blocked = new();
        static readonly List<string> missingPoints = new();
        static readonly List<string> missingPrefabs = new();
        static readonly Dictionary<string, GameObject> prefabs = new();
        static readonly Dictionary<string, Bounds> boundsCache = new();
        static List<Transform> carRoute = new();

        [MenuItem("Kosynierzy/Place Team Props")]
        static void PlaceTeamProps()
        {
            Points.Rebuild();
            placed = 0;
            blocked.Clear(); missingPoints.Clear(); missingPrefabs.Clear();
            prefabs.Clear(); boundsCache.Clear();

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Place Team Props");
            int group = Undo.GetCurrentGroup();

            RemoveOldRoots();
            var rootGo = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(rootGo, "Place Team Props");
            root = rootGo.transform;

            carRoute = Points.Series("miasto_auto_sd_trasa");
            if (Points.Has("miasto_zagajnik_auto")) carRoute.Add(Points.Get("miasto_zagajnik_auto"));

            PlaceCovers();
            PlaceStation();
            PlaceHotel();
            PlaceBookshop();
            PlacePhotographer();
            PlaceRadioStation();
            PlaceStreet();
            PlaceVehicles();

            if (placed == 0) Undo.DestroyObjectImmediate(rootGo);
            else Selection.activeGameObject = rootGo;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(group);
            root = null;

            string line = placed == 0
                ? "Kosynierzy: Place Team Props — nothing placed (no contract points in the scene yet?)"
                : $"Kosynierzy: Place Team Props — {placed} props under '{RootName}'";
            line += Describe("; no point", missingPoints) + Describe("; no prefab", missingPrefabs) + Describe("; no free spot", blocked);
            Debug.Log(line + ". Next: Bake NavMesh (all floors), Add Hiding Spots, save the scene.");
        }

        static string Describe(string label, List<string> names)
        {
            if (names.Count == 0) return "";
            int shown = Mathf.Min(names.Count, 6);
            string text = string.Join(", ", names.GetRange(0, shown));
            return $"{label} ({names.Count}): {text}{(names.Count > shown ? ", …" : "")}";
        }

        static void RemoveOldRoots()
        {
            var old = new List<GameObject>();
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (t.name == RootName && t.parent == null) old.Add(t.gameObject);
            foreach (var go in old)
                if (go != null) Undo.DestroyObjectImmediate(go);
        }

        // ------------------------------------------------------------------ the set dressing

        // Cover for crouching: exactly on miasto_oslona_1..12 (HidingSpot finds them as art and skips its bush).
        static void PlaceCovers()
        {
            for (int i = 1; i <= 12; i++)
            {
                if (!Point($"miasto_oslona_{i}", out var p)) continue;
                PlaceAt(CoverCycle[(i - 1) % CoverCycle.Length], p.position, Yaw(p));
            }
        }

        // Locomotive + wagon on the track beside the spawn: across the spawn heading (the tracks run east-west, the player
        // faces north), 6 m behind it (in front when that is blocked), nose along -right, the wagon coupled behind.
        static void PlaceStation()
        {
            if (!Point("spawn_dworzec", out var sp)) return;
            if (Prefab("OB_Lokomotywa") == null) return;
            Vector3 fwd = Flat(sp.forward);
            Vector3 along = -Flat(sp.right);
            Quaternion rot = Facing(along);
            var loco = PlaceFirst("OB_Lokomotywa", new[]
            {
                (sp.position - fwd * 6f, rot),
                (sp.position + fwd * 6f, rot),
            }, sp, 0.6f);
            if (loco == null || Prefab("OB_Wagon") == null) return;
            float gap = LocalBounds("OB_Lokomotywa").extents.z + LocalBounds("OB_Wagon").extents.z + 1f;
            PlaceFirst("OB_Wagon", new[] { (loco.transform.position - along * gap, rot) }, sp, 0.6f);
        }

        static void PlaceHotel()
        {
            // Reception counter in front of the receptionist (his point stays free), a desk lamp on it.
            if (Point("hotel_recepcja", out var rec))
            {
                var counter = PlaceFirst("OB_Lada_Recepcja", Offsets(rec, false, 0.85f, 1.2f, 1.6f), rec, 0.5f);
                if (counter != null) PlaceOn(counter, "OB_Lada_Recepcja", "OB_Lampa_Biurkowa", new Vector3(0.7f, 0f, 0f));
            }

            // The telephone on a small table in front of the phone spot.
            if (Point("hotel_telefon", out var tel))
            {
                var table = PlaceFirst("OB_Stol", Offsets(tel, false, 0.8f, 1.1f, -0.8f), tel, 0.5f);
                if (table != null) PlaceOn(table, "OB_Stol", "OB_Telefon", Vector3.zero);
            }

            // Sofa against a lobby wall.
            if (Point("hotel_kolumna", out var column))
                PlaceFirst("OB_Kanapa", WallThenRing("OB_Kanapa", column.position, -Flat(column.right), 14f, 0f, 2f, -2f), column, 1.0f);

            // Naujocks' room: bed against one wall, wardrobe against the other.
            if (Point("hotel_pokoj_mapa", out var mapPoint))
                PlaceFirst("OB_Lozko", WallThenRing("OB_Lozko", mapPoint.position, -Flat(mapPoint.right), 16f, 0f, 2f, -2f), mapPoint, 1.2f);
            if (Point("hotel_pokoj_lista", out var listPoint))
                PlaceFirst("OB_Szafa", WallThenRing("OB_Szafa", listPoint.position, Flat(listPoint.right), 16f, 0f, 2f, -2f), listPoint, 1.2f);
        }

        // Bookshop: three shelves along the walls near Zofia, a radio on a table against a wall.
        static void PlaceBookshop()
        {
            if (!Point("ksiegarnia_zofia", out var zofia)) return;
            Vector3 fwd = Flat(zofia.forward), right = Flat(zofia.right);
            foreach (var dir in new[] { -fwd, -right, right })
                PlaceFirst("OB_Regal_Ksiazki", WallThenRing("OB_Regal_Ksiazki", zofia.position, dir, 10f, 0f, 1.2f, -1.2f, 2.4f, -2.4f), zofia, 1.0f);

            var table = PlaceFirst("OB_Stol", WallThenRing("OB_Stol", zofia.position, right, 10f, 3f, -3f, 4.5f, -4.5f), zofia, 1.0f);
            if (table != null) PlaceOn(table, "OB_Stol", "OB_Radio_Odbiornik", Vector3.zero);
        }

        static void PlacePhotographer()
        {
            if (Point("fotograf_wlasciciel", out var owner))
                PlaceFirst("OB_Aparat_Studyjny", Offsets(owner, false, 1.1f, 1.5f, -1.1f), owner, 0.5f);

            if (Point("fotograf_ciemnia", out var darkroom))
            {
                var bench = PlaceFirst("OB_Stol", Offsets(darkroom, false, 0.9f, -0.9f, 1.3f), darkroom, 0.5f);
                if (bench != null) PlaceOn(bench, "OB_Stol", "OB_Lampa_Ciemnia", Vector3.zero);
            }
        }

        static void PlaceRadioStation()
        {
            // Control desk 1 m from the microphone spot, turned to face it, so the microphone stands in front of it.
            if (Point("radio_sterownia_mikrofon", out var mic))
                PlaceFirst("OB_Pulpit", Offsets(mic, true, 1.0f, -1.0f, 1.5f), mic, 0.5f);

            // Two equipment cabinets against the switchboard wall, left and right of where the board will hang.
            if (Point("radio_technika_kable", out var cables))
            {
                Vector3 fwd = Flat(cables.forward);
                PlaceFirst("OB_Szafa_Aparatura", AgainstWall("OB_Szafa_Aparatura", cables.position, fwd, 5f, -2.2f, -3.4f), cables, 0.9f);
                PlaceFirst("OB_Szafa_Aparatura", AgainstWall("OB_Szafa_Aparatura", cables.position, fwd, 5f, -4.6f, 2.2f, 3.4f, 4.6f), cables, 0.9f);
            }
        }

        // Street furniture 3–4 m beside the crowd points, turned toward the street, clear of every named point and of the car's route.
        static void PlaceStreet()
        {
            for (int i = 1; i <= StreetCycle.Length; i++)
            {
                if (!Point($"miasto_tlum_{i}", out var anchor)) continue;
                // Alternate the sides of the street (the placeholder map's ±3 m crowd points: odd → +x side, even → −x side);
                // the other side is tried when the first is blocked.
                float side = i % 2 == 1 ? 1f : -1f;
                PlaceFirst(StreetCycle[i - 1], Sideways(anchor, side), anchor, 1.2f, checkRoute: true);
            }
        }

        static void PlaceVehicles()
        {
            if (Point("miasto_rower", out var bike)) PlaceAt("OB_Rower", bike.position, Yaw(bike));
            if (Point("miasto_ciezarowka", out var truck)) PlaceAt("OB_Ciezarowka", truck.position, Yaw(truck));
        }

        // ------------------------------------------------------------------ candidate generators

        /// <summary>Spots along the anchor's forward axis (metres; negative = behind). faceAnchor: the prop turns toward the anchor, else it takes the anchor's yaw.</summary>
        static IEnumerable<(Vector3 pos, Quaternion rot)> Offsets(Transform anchor, bool faceAnchor, params float[] distances)
        {
            Vector3 fwd = Flat(anchor.forward);
            foreach (float d in distances)
                yield return (anchor.position + fwd * d, faceAnchor ? Facing(fwd * -Mathf.Sign(d)) : Yaw(anchor));
        }

        /// <summary>Spots to one side of the anchor (side = ±1 along its right axis), the prop facing back toward the anchor; the far side after the near one.</summary>
        static IEnumerable<(Vector3 pos, Quaternion rot)> Sideways(Transform anchor, float side)
        {
            Vector3 right = Flat(anchor.right);
            foreach (float s in new[] { side, -side })
            foreach (float d in new[] { 3.5f, 3f, 4f })
                yield return (anchor.position + right * (s * d), Facing(-right * s));
        }

        /// <summary>
        /// Against the first wall found along dir from the anchor (rays shifted sideways by each offset), the prop's back on
        /// the wall and its front (+Z) into the room. Walls are solid non-trigger colliders at 1 m height.
        /// </summary>
        static IEnumerable<(Vector3 pos, Quaternion rot)> AgainstWall(string prefab, Vector3 origin, Vector3 dir, float maxDistance, params float[] sideOffsets)
        {
            dir = Flat(dir);
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            float backDepth = -LocalBounds(prefab).min.z; // from the pivot to the back face
            foreach (float offset in sideOffsets)
            {
                if (!WallHit(origin + side * offset, dir, maxDistance, out Vector3 hit)) continue;
                var pos = new Vector3(hit.x, origin.y, hit.z) - dir * (backDepth + 0.03f);
                yield return (pos, Facing(-dir));
            }
        }

        /// <summary>AgainstWall first; when no wall is near, a ring around the origin (3.5 m, 4.5 m) facing it.</summary>
        static IEnumerable<(Vector3 pos, Quaternion rot)> WallThenRing(string prefab, Vector3 origin, Vector3 dir, float maxDistance, params float[] sideOffsets)
        {
            foreach (var c in AgainstWall(prefab, origin, dir, maxDistance, sideOffsets)) yield return c;
            float start = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            foreach (float radius in new[] { 3.5f, 4.5f })
            for (int k = 0; k < 8; k++)
            {
                Vector3 d = Quaternion.Euler(0f, start + k * 45f, 0f) * Vector3.forward;
                yield return (origin + d * radius, Facing(-d));
            }
        }

        // ------------------------------------------------------------------ placing

        static bool Point(string name, out Transform t)
        {
            t = null;
            if (!Points.Has(name))
            {
                if (!missingPoints.Contains(name)) missingPoints.Add(name);
                return false;
            }
            t = Points.Get(name);
            return t != null;
        }

        static GameObject Prefab(string name)
        {
            if (prefabs.TryGetValue(name, out var cached)) return cached;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + name + ".prefab");
            if (prefab == null && !missingPrefabs.Contains(name)) missingPrefabs.Add(name);
            prefabs[name] = prefab;
            return prefab;
        }

        /// <summary>Union of the prefab's solid colliders in its own space (a 1 m box when it has none).</summary>
        static Bounds LocalBounds(string name)
        {
            if (boundsCache.TryGetValue(name, out var cached)) return cached;
            var bounds = new Bounds(new Vector3(0f, 0.5f, 0f), Vector3.one);
            var prefab = Prefab(name);
            if (prefab != null && Spawner.TryLocalColliderBounds(prefab.transform, out var found)) bounds = found;
            boundsCache[name] = bounds;
            return bounds;
        }

        /// <summary>Unconditional placement (cover on its point, vehicles on theirs).</summary>
        static GameObject PlaceAt(string name, Vector3 pos, Quaternion rot) =>
            Prefab(name) == null ? null : Spawn(name, pos, rot);

        /// <summary>A small prop on top of another (phone on its table, lamp on the counter): the host's collider top at a local XZ offset.</summary>
        static GameObject PlaceOn(GameObject host, string hostName, string name, Vector3 localOffset)
        {
            if (Prefab(name) == null) return null;
            Vector3 world = host.transform.TransformPoint(new Vector3(localOffset.x, LocalBounds(hostName).max.y, localOffset.z));
            return Spawn(name, world, host.transform.rotation);
        }

        /// <summary>
        /// First candidate where the prop fits: no named point (except the anchor) within the margin of its footprint, off the
        /// car's route legs when asked, and nothing solid in the way (floor excluded). Counts one "no free spot" when none fits.
        /// </summary>
        static GameObject PlaceFirst(string name, IEnumerable<(Vector3 pos, Quaternion rot)> candidates, Transform anchor, float margin, bool checkRoute = false)
        {
            if (Prefab(name) == null) return null;
            foreach (var (pos, rot) in candidates)
            {
                Vector3 spot = SnapToGround(pos);
                if (!PointsClear(name, spot, rot, margin, anchor)) continue;
                if (checkRoute && NearCarRoute(name, spot, rot)) continue;
                if (!SpaceFree(name, spot, rot)) continue;
                return Spawn(name, spot, rot);
            }
            if (!blocked.Contains(name)) blocked.Add(name);
            return null;
        }

        static GameObject Spawn(string name, Vector3 pos, Quaternion rot)
        {
            var go = PrefabUtility.InstantiatePrefab(Prefab(name), root) as GameObject;
            if (go == null) return null;
            go.transform.SetPositionAndRotation(pos, rot);
            MarkStatic(go);
            placed++;
            Physics.SyncTransforms(); // the next fit test must see this prop's colliders
            return go;
        }

        // Navigation + occlusion + batching static (batching not for animated props). NavigationStatic is cast from its
        // number (8): the member is deprecated in recent editors, and the NavMeshSurface here collects colliders anyway.
        static void MarkStatic(GameObject go)
        {
            const StaticEditorFlags navigation = (StaticEditorFlags)8;
            bool animated = go.GetComponentInChildren<Animator>() != null;
            var flags = navigation | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic;
            if (!animated) flags |= StaticEditorFlags.BatchingStatic;
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
        }

        // ------------------------------------------------------------------ geometry tests

        static bool IsPlaced(Collider c) => root != null && c.transform.IsChildOf(root);

        static bool IsCharacter(Collider c) =>
            c.GetComponentInParent<Character>() != null || c.GetComponentInParent<FirstPersonController>() != null;

        /// <summary>The ground under pos: the surface nearest to the given height within 1 m (never another prop, never a table top when the point is on the floor).</summary>
        static Vector3 SnapToGround(Vector3 pos)
        {
            Physics.SyncTransforms();
            float bestError = float.MaxValue;
            float y = pos.y;
            foreach (var h in Physics.RaycastAll(pos + Vector3.up * 2.5f, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (IsPlaced(h.collider) || IsCharacter(h.collider)) continue;
                float error = Mathf.Abs(h.point.y - pos.y);
                if (error > 1f || error >= bestError) continue;
                bestError = error;
                y = h.point.y;
            }
            pos.y = y;
            return pos;
        }

        static bool WallHit(Vector3 from, Vector3 dir, float maxDistance, out Vector3 point)
        {
            point = default;
            float best = float.MaxValue;
            Physics.SyncTransforms();
            foreach (var h in Physics.RaycastAll(from + Vector3.up, dir, maxDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (IsPlaced(h.collider) || IsCharacter(h.collider) || h.distance >= best) continue;
                if (Mathf.Abs(h.normal.y) > 0.5f) continue; // a table top or a slope is not a wall
                best = h.distance;
                point = h.point;
            }
            return best < float.MaxValue;
        }

        /// <summary>True when no named point (the anchor excepted) lies within margin of the prop's footprint.</summary>
        static bool PointsClear(string name, Vector3 pos, Quaternion rot, float margin, Transform ignore)
        {
            var b = LocalBounds(name);
            Quaternion inverse = Quaternion.Inverse(rot);
            foreach (var pt in Points.All)
            {
                if (pt == null || pt == ignore || Mathf.Abs(pt.position.y - pos.y) > 3f) continue;
                Vector3 local = inverse * (pt.position - pos);
                if (Mathf.Abs(local.x - b.center.x) < b.extents.x + margin && Mathf.Abs(local.z - b.center.z) < b.extents.z + margin)
                    return false;
            }
            return true;
        }

        /// <summary>Nothing solid inside the prop's box, ignoring the lowest 15 cm (floor, kerb) and characters.</summary>
        static bool SpaceFree(string name, Vector3 pos, Quaternion rot)
        {
            var b = LocalBounds(name);
            const float lift = 0.15f;
            float lo = b.min.y + lift, hi = b.max.y;
            if (hi - lo < 0.06f) return true;
            var center = new Vector3(b.center.x, (lo + hi) * 0.5f, b.center.z);
            var half = new Vector3(Mathf.Max(0.05f, b.extents.x - 0.03f), (hi - lo) * 0.5f, Mathf.Max(0.05f, b.extents.z - 0.03f));
            Physics.SyncTransforms();
            foreach (var c in Physics.OverlapBox(pos + rot * center, half, rot, ~0, QueryTriggerInteraction.Ignore))
                if (!IsCharacter(c)) return false;
            return true;
        }

        /// <summary>The SD car drives straight legs between its route points through the world, ignoring colliders: keep them clear.</summary>
        static bool NearCarRoute(string name, Vector3 pos, Quaternion rot)
        {
            var b = LocalBounds(name);
            float limit = Mathf.Max(b.extents.x, b.extents.z) + 1.4f; // car half-width 0.95 + a margin
            for (int i = 0; i + 1 < carRoute.Count; i++)
                if (carRoute[i] != null && carRoute[i + 1] != null && SegmentDistance(pos, carRoute[i].position, carRoute[i + 1].position) < limit)
                    return true;
            return false;
        }

        static float SegmentDistance(Vector3 p, Vector3 a, Vector3 b)
        {
            var pp = new Vector2(p.x, p.z);
            var aa = new Vector2(a.x, a.z);
            var ab = new Vector2(b.x, b.z) - aa;
            float len2 = ab.sqrMagnitude;
            float t = len2 < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(pp - aa, ab) / len2);
            return Vector2.Distance(pp, aa + ab * t);
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-4f ? v.normalized : Vector3.forward;
        }

        static Quaternion Facing(Vector3 direction) => Quaternion.LookRotation(Flat(direction), Vector3.up);

        static Quaternion Yaw(Transform t) => Quaternion.Euler(0f, t.eulerAngles.y, 0f);
    }
}
