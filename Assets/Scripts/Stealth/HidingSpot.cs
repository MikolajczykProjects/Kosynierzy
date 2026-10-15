using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Hiding volume (bush, low wall, crate). While the player is inside AND crouched, Detection.PlayerHidden is true and
    /// SD / Schupo cannot see them (unless already alarmed and very close). The Box/SphereCollider on this object is the
    /// volume (a 1.2 m sphere is added when there is none); it is read once and disabled, so it never blocks raycasts.
    /// EnsureDefaults() puts spots at miasto_zagajnik_krzaki and miasto_oslona_1..12, with a placeholder bush
    /// where no art stands at the point.
    /// </summary>
    [DisallowMultipleComponent]
    public class HidingSpot : MonoBehaviour
    {
        const int IgnoreRaycastLayer = 2;
        static readonly List<HidingSpot> active = new();
        static readonly Color LeafDark = new(0.25f, 0.36f, 0.17f);
        static readonly Color LeafLight = new(0.33f, 0.45f, 0.21f);

        VolumeShape shape;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();

        public static int Count => active.Count;

        void Awake()
        {
            if (!VolumeShape.TryRead(this, out shape))
            {
                var sphere = gameObject.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = 1.2f;
                VolumeShape.TryRead(this, out shape);
            }
            if (gameObject.layer == 0) gameObject.layer = IgnoreRaycastLayer;
        }

        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        public bool Contains(Vector3 point) => shape.Contains(point);

        public static bool IsInside(Vector3 point)
        {
            for (int i = 0; i < active.Count; i++)
                if (active[i].Contains(point + Vector3.up * 0.3f)) return true;
            return false;
        }

        /// <summary>Spots at the contract points that no existing spot covers yet. Returns how many were made.</summary>
        public static int EnsureDefaults(Transform parent = null)
        {
            int made = 0;
            foreach (var point in DefaultPoints())
            {
                if (IsInside(point.position)) continue;
                Create($"HidingSpot_{point.name}", point.position, point.rotation, parent);
                made++;
            }
            return made;
        }

        /// <summary>miasto_zagajnik_krzaki and miasto_oslona_1..12 that exist on the map.</summary>
        public static List<Transform> DefaultPoints()
        {
            var list = new List<Transform>();
            if (Points.Has("miasto_zagajnik_krzaki")) list.Add(Points.Get("miasto_zagajnik_krzaki"));
            list.AddRange(Points.Series("miasto_oslona"));
            return list;
        }

        /// <summary>
        /// Hiding spot at a ground point. Without art at the point (no solid collider 0.2–1.2 m above it) a
        /// placeholder bush is added (no collider: the player crouches inside it).
        /// </summary>
        public static HidingSpot Create(string spotName, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            Physics.SyncTransforms();
            bool hasArt = HasSolidNear(position, rotation);
            var go = new GameObject(spotName) { layer = IgnoreRaycastLayer };
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            var sphere = go.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.center = new Vector3(0f, 0.6f, 0f);
            sphere.radius = hasArt ? 1.6f : 1.2f;
            if (!hasArt) BuildBush(go.transform);
            return go.AddComponent<HidingSpot>();
        }

        static bool HasSolidNear(Vector3 position, Quaternion rotation)
        {
            var found = Physics.OverlapBox(position + Vector3.up * 0.7f, new Vector3(0.9f, 0.5f, 0.9f), rotation, ~0, QueryTriggerInteraction.Ignore);
            foreach (var c in found)
            {
                if (c.GetComponentInParent<Character>() != null) continue;
                if (c.GetComponentInParent<FirstPersonController>() != null) continue;
                return true;
            }
            return false;
        }

        static void BuildBush(Transform parent)
        {
            Leaf(parent, new Vector3(0f, 0.5f, 0f), new Vector3(1.7f, 1.0f, 1.4f), LeafDark);
            Leaf(parent, new Vector3(0.45f, 0.75f, 0.2f), new Vector3(1.0f, 0.8f, 1.0f), LeafLight);
            Leaf(parent, new Vector3(-0.5f, 0.65f, -0.15f), new Vector3(0.9f, 0.75f, 0.9f), LeafLight);
        }

        static void Leaf(Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
        {
            var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaf.name = "Bush";
            var col = leaf.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
            leaf.layer = parent.gameObject.layer;
            leaf.transform.SetParent(parent, false);
            leaf.transform.localPosition = localPosition;
            leaf.transform.localScale = localScale;
            leaf.AddComponent<PlaceholderTint>().Color = color;
        }

        void OnDrawGizmos()
        {
            var sphere = GetComponent<SphereCollider>();
            if (sphere == null) return;
            Gizmos.color = new Color(0.3f, 0.8f, 0.3f, 0.2f);
            float scale = Mathf.Max(transform.lossyScale.x, Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));
            Gizmos.DrawSphere(transform.TransformPoint(sphere.center), sphere.radius * scale);
        }
    }
}
