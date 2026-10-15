using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kosynierzy
{
    /// <summary>
    /// Street-lamp light pools for the AI after dark. Lamps stand at points miasto_latarnia_*, hotel_latarnia_* and
    /// radio_latarnia_*; each lights a 6 m circle, only at night. Night comes from AtmosphereController.IsNight (the
    /// state its NightChanged event reports); SetNight(bool) overrides it for tests, ClearNightOverride() undoes that.
    /// </summary>
    public static class LightZones
    {
        public const float LitRadius = 6f;
        static readonly string[] Prefixes = { "miasto_latarnia", "hotel_latarnia", "radio_latarnia" };
        static readonly List<Vector3> lamps = new();
        static bool built;
        static float nextRetry;
        static bool hasOverride, overrideNight;
        static int playerFrame = -1;
        static bool playerLit;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            lamps.Clear();
            built = false;
            nextRetry = 0f;
            hasOverride = false;
            playerFrame = -1;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => built = false;

        public static bool IsNight => hasOverride ? overrideNight : AtmosphereController.IsNight;

        /// <summary>Force day/night for the AI (tests, scenes without an AtmosphereController).</summary>
        public static void SetNight(bool night)
        {
            hasOverride = true;
            overrideNight = night;
        }

        public static void ClearNightOverride() => hasOverride = false;

        /// <summary>Call after the map is (re)instantiated so lamp points are read again.</summary>
        public static void Rebuild() => built = false;

        public static int LampCount
        {
            get
            {
                Build();
                return lamps.Count;
            }
        }

        /// <summary>By day everything is lit; at night only within 6 m of a lamp.</summary>
        public static bool IsLit(Vector3 position)
        {
            if (!IsNight) return true;
            Build();
            const float r2 = LitRadius * LitRadius;
            foreach (var lamp in lamps)
            {
                float dx = position.x - lamp.x, dz = position.z - lamp.z;
                if (dx * dx + dz * dz <= r2 && Mathf.Abs(position.y - lamp.y) < 5f) return true;
            }
            return false;
        }

        /// <summary>IsLit(player feet), computed once per frame.</summary>
        public static bool PlayerLit
        {
            get
            {
                if (playerFrame != Time.frameCount)
                {
                    playerFrame = Time.frameCount;
                    playerLit = IsLit(PlayerTarget.Feet);
                }
                return playerLit;
            }
        }

        static void Build()
        {
            // No lamps yet (map not loaded?) — look again every few seconds.
            if (built && (lamps.Count > 0 || Time.unscaledTime < nextRetry)) return;
            built = true;
            nextRetry = Time.unscaledTime + 5f;
            lamps.Clear();
            foreach (var prefix in Prefixes)
                foreach (var t in Points.Series(prefix))
                    lamps.Add(t.position);
        }
    }
}
