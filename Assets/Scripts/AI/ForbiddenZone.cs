using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Area where SD and Schupo fill their "?" meter 3x faster (Naujocks' room, the radio grounds). The Box/SphereCollider
    /// on this object is the area (a BoxCollider is added when there is none); it is read once and disabled at runtime,
    /// so player raycasts (E, photos) pass through. IsInside(point) checks every enabled zone.
    /// EnsureDefaults() creates sphere zones at hotel_pokoj_mapa (6 m) and around the radio_sd_* centroid (25 m)
    /// when the scene has no hand-made zones.
    /// </summary>
    [DisallowMultipleComponent]
    public class ForbiddenZone : MonoBehaviour
    {
        const int IgnoreRaycastLayer = 2;
        static readonly List<ForbiddenZone> active = new();

        VolumeShape shape;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();

        public static int Count => active.Count;

        void Awake()
        {
            if (!VolumeShape.TryRead(this, out shape))
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
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
                if (active[i].Contains(point)) return true;
            return false;
        }

        /// <summary>Creates the default zones from map points unless the scene already has zones.</summary>
        public static void EnsureDefaults()
        {
            if (FindAnyObjectByType<ForbiddenZone>(FindObjectsInactive.Include) != null) return;

            if (Points.Has("hotel_pokoj_mapa"))
                CreateSphere("ForbiddenZone_hotel_pokoj", Points.Position("hotel_pokoj_mapa"), 6f);

            var radio = Points.Series("radio_sd");
            if (radio.Count > 0)
            {
                Vector3 center = Vector3.zero;
                foreach (var t in radio) center += t.position;
                CreateSphere("ForbiddenZone_radio", center / radio.Count, 25f);
            }
        }

        public static ForbiddenZone CreateSphere(string zoneName, Vector3 center, float radius)
        {
            var go = new GameObject(zoneName) { layer = IgnoreRaycastLayer };
            go.transform.position = center;
            go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            var sphere = go.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = radius;
            return go.AddComponent<ForbiddenZone>();
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.25f, 0.2f, 0.12f);
            if (TryGetComponent<BoxCollider>(out var box))
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.matrix = Matrix4x4.identity;
            }
            else if (TryGetComponent<SphereCollider>(out var sphere))
            {
                float scale = Mathf.Max(transform.lossyScale.x, Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));
                Gizmos.DrawSphere(transform.TransformPoint(sphere.center), sphere.radius * scale);
            }
        }
    }
}
