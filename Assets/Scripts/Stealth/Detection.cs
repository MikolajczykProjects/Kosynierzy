using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>How aware an NPC (or, for PlayerStatus, the worst NPC) is of the player.</summary>
    public enum AwarenessLevel { Unaware, Suspicious, Searching, Alarmed }

    /// <summary>
    /// Hitman-style detection state for the HUD: which SD / Schupo are noticing the player right now (meter and level),
    /// the player's overall status, whether they are hidden and how exposed they are (light, stance, movement).
    /// Fed by every WatcherBrain (SD, Schupo); refreshed once per frame (DetectionRunner, or lazily by any getter).
    /// </summary>
    public static class Detection
    {
        public struct Observer
        {
            public Transform Transform;
            public float Meter01;
            public AwarenessLevel Level;
            public bool IsPolice;
        }

        static readonly List<WatcherBrain> watchers = new();
        static readonly List<Observer> observers = new();
        static int refreshedFrame = -1;
        static AwarenessLevel status;
        static bool hidden, inHidingSpot;
        static float visibility;
        static DetectionRunner runner;

        /// <summary>SD/Schupo currently noticing the player (Meter01 > 0 or Level > Unaware).</summary>
        public static IReadOnlyList<Observer> Observers
        {
            get
            {
                Refresh();
                return observers;
            }
        }

        /// <summary>Highest awareness level among all watching NPCs.</summary>
        public static AwarenessLevel PlayerStatus
        {
            get
            {
                Refresh();
                return status;
            }
        }

        /// <summary>The player is crouched in a hiding spot, or crouched in darkness (night, outside lamp light).</summary>
        public static bool PlayerHidden
        {
            get
            {
                Refresh();
                return hidden;
            }
        }

        /// <summary>Crouched inside a HidingSpot: NPCs cannot see the player (unless alarmed and very close).</summary>
        public static bool InHidingSpot
        {
            get
            {
                Refresh();
                return inHidingSpot;
            }
        }

        /// <summary>0 = invisible … 1 = fully exposed (light, stance, movement).</summary>
        public static float Visibility01
        {
            get
            {
                Refresh();
                return visibility;
            }
        }

        public static event System.Action<AwarenessLevel> StatusChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            watchers.Clear();
            observers.Clear();
            refreshedFrame = -1;
            status = AwarenessLevel.Unaware;
            hidden = inHidingSpot = false;
            visibility = 0f;
            runner = null;
            StatusChanged = null;
        }

        public static void Register(WatcherBrain watcher)
        {
            if (watcher != null && !watchers.Contains(watcher)) watchers.Add(watcher);
            if (runner == null && Application.isPlaying)
                runner = new GameObject("[Detection]").AddComponent<DetectionRunner>();
        }

        public static void Unregister(WatcherBrain watcher) => watchers.Remove(watcher);

        /// <summary>Recomputes everything once per frame (extra calls in the same frame do nothing).</summary>
        public static void Refresh()
        {
            if (refreshedFrame == Time.frameCount) return;
            refreshedFrame = Time.frameCount;

            bool player = PlayerTarget.Find() && !MissionFail.IsFailed;
            bool crouched = player && PlayerTarget.IsCrouching;
            inHidingSpot = crouched && HidingSpot.IsInside(PlayerTarget.Feet);
            hidden = inHidingSpot || (crouched && LightZones.IsNight && !LightZones.PlayerLit);
            visibility = player ? ComputeVisibility() : 0f;

            observers.Clear();
            var top = AwarenessLevel.Unaware;
            for (int i = watchers.Count - 1; i >= 0; i--)
            {
                var w = watchers[i];
                if (w == null)
                {
                    watchers.RemoveAt(i);
                    continue;
                }
                if (w.IsDown || !w.isActiveAndEnabled) continue;
                var level = w.Awareness;
                float meter = Mathf.Clamp01(w.Suspicion);
                if (level > top) top = level;
                if (meter > 0.001f || level > AwarenessLevel.Unaware)
                    observers.Add(new Observer { Transform = w.transform, Meter01 = meter, Level = level, IsPolice = w.IsPolice });
            }

            if (top != status)
            {
                status = top;
                StatusChanged?.Invoke(top);
            }
        }

        static float ComputeVisibility()
        {
            if (inHidingSpot) return 0.05f;
            float light = !LightZones.IsNight ? 1f : LightZones.PlayerLit ? 0.8f : 0.3f;
            float stance = PlayerTarget.IsCrouching ? 0.55f : 1f;
            float motion = PlayerTarget.IsSprinting ? 1.25f : PlayerTarget.Speed > 0.5f ? 1f : 0.8f;
            return Mathf.Clamp01(light * stance * motion);
        }
    }
}
