using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Cached facts about the player for the AI: feet, eyes (camera), chest, stance, lamp light, forbidden zones.
    /// Finds the FirstPersonController lazily (at most once per second while it is missing).
    /// </summary>
    public static class PlayerTarget
    {
        static FirstPersonController player;
        static Camera cam;
        static Health health;
        static float nextSearch;
        static int zoneFrame = -1;
        static bool inZone;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            player = null;
            cam = null;
            health = null;
            nextSearch = 0f;
            zoneFrame = -1;
            inZone = false;
        }

        public static bool Find()
        {
            if (player != null) return true;
            if (Time.unscaledTime < nextSearch) return false;
            nextSearch = Time.unscaledTime + 1f;
            player = Object.FindAnyObjectByType<FirstPersonController>();
            if (player == null) return false;
            cam = player.GetComponentInChildren<Camera>(true);
            health = player.GetComponent<Health>();
            return true;
        }

        public static FirstPersonController Controller => Find() ? player : null;
        public static Transform Transform => Find() ? player.transform : null;
        public static Camera Camera => Find() ? cam : null;

        public static Vector3 Feet => Find() ? player.transform.position : Vector3.zero;

        public static Vector3 Eye
        {
            get
            {
                if (!Find()) return Vector3.zero;
                if (cam != null) return cam.transform.position;
                return player.transform.position + Vector3.up * (player.IsCrouching ? 1.0f : 1.6f);
            }
        }

        /// <summary>Where enemies aim.</summary>
        public static Vector3 Chest => Feet + Vector3.up * (IsCrouching ? 0.7f : 1.25f);

        public static bool IsCrouching => Find() && player.IsCrouching;
        public static bool IsSprinting => Find() && (player.IsSprinting || player.IsSliding);

        /// <summary>Player dead or mission already failed — the AI stops shooting.</summary>
        public static bool IsGone
        {
            get
            {
                if (MissionFail.IsFailed) return true;
                if (!Find()) return true;
                if (health == null) health = player.GetComponent<Health>();
                return health != null && health.IsDead;
            }
        }

        public static bool InForbiddenZone
        {
            get
            {
                if (zoneFrame != Time.frameCount)
                {
                    zoneFrame = Time.frameCount;
                    inZone = Find() && ForbiddenZone.IsInside(player.transform.position);
                }
                return inZone;
            }
        }

        /// <summary>The collider belongs to the player.</summary>
        public static bool IsPlayerCollider(Transform t) => t != null && Find() && t.IsChildOf(player.transform);

        /// <summary>A Noise source is the player (PlayerWeapon passes transform.root, which may be a parent of the player).</summary>
        public static bool IsPlayerSource(Transform t) =>
            t != null && Find() && (t.IsChildOf(player.transform) || player.transform.IsChildOf(t));
    }
}
