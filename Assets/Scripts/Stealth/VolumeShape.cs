using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Box or sphere volume read from a BoxCollider / SphereCollider and tested with math instead of physics.
    /// At runtime TryRead disables that collider, so player raycasts (E interaction, photos) never hit big zone triggers.
    /// </summary>
    public struct VolumeShape
    {
        Transform owner;
        bool isSphere;
        Vector3 center, halfSize;
        float radius;

        public bool IsValid => owner != null;

        public static bool TryRead(Component component, out VolumeShape shape)
        {
            shape = default;
            if (component.TryGetComponent<BoxCollider>(out var box))
            {
                shape = new VolumeShape { owner = component.transform, center = box.center, halfSize = box.size * 0.5f };
                if (Application.isPlaying) box.enabled = false;
                return true;
            }
            if (component.TryGetComponent<SphereCollider>(out var sphere))
            {
                shape = new VolumeShape { owner = component.transform, isSphere = true, center = sphere.center, radius = sphere.radius };
                if (Application.isPlaying) sphere.enabled = false;
                return true;
            }
            return false;
        }

        public bool Contains(Vector3 point)
        {
            if (owner == null) return false;
            Vector3 local = owner.InverseTransformPoint(point) - center;
            if (isSphere) return local.sqrMagnitude <= radius * radius;
            return Mathf.Abs(local.x) <= halfSize.x && Mathf.Abs(local.y) <= halfSize.y && Mathf.Abs(local.z) <= halfSize.z;
        }
    }
}
