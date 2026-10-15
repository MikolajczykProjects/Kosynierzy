using System;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// In-game clock moved by mission steps (15:30 → 20:10), not by real time. Lighting listens to HourChanged.
    /// </summary>
    public static class GameClock
    {
        public static float Hour { get; private set; } = 15.5f;
        public static bool IsNight => Hour >= 19.5f;
        public static event Action<float> HourChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Hour = 15.5f;
            HourChanged = null;
        }

        public static void Set(float hour)
        {
            Hour = hour;
            HourChanged?.Invoke(hour);
        }

        public static string Text
        {
            get
            {
                int h = Mathf.FloorToInt(Hour);
                int m = Mathf.RoundToInt((Hour - h) * 60f);
                if (m == 60) { h++; m = 0; }
                return $"{h:00}:{m:00}";
            }
        }
    }
}
