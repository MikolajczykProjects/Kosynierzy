using System;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Mission failure (team doc: one hit = end; shooting a civilian, policeman or station worker = end).
    /// Trigger(message) freezes the game and shows the failure screen with "Spróbuj od punktu kontrolnego".
    /// </summary>
    public static class MissionFail
    {
        public const string PlayerHit = "Agent »Kruk« zdemaskowany. Prowokacja przeszła bez świadków.";
        public const string ShotInnocent = "Polski agent nie strzela do cywilów. Właśnie dałeś im prawdziwy dowód.";

        public static bool IsFailed { get; private set; }
        /// <summary>Distinct civilians the player has hurt; the 3rd one fails the mission.</summary>
        public static int InnocentHits;
        public static string Message { get; private set; }
        public static event Action<string> Failed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            IsFailed = false;
            InnocentHits = 0;
            Message = null;
            Failed = null;
        }

        public static void Trigger(string message)
        {
            if (IsFailed) return;
            IsFailed = true;
            Message = message;
            Debug.Log($"Kosynierzy: PORAŻKA — {message}");
            Failed?.Invoke(message);
        }

        /// <summary>Called by the checkpoint reload.</summary>
        public static void Clear()
        {
            IsFailed = false;
            Message = null;
            InnocentHits = 0;
        }
    }
}
