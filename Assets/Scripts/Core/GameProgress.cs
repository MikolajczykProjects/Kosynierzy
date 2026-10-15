using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// What the player has gathered this run: camera, evidence photos, history cards.
    /// Plain statics so HUD, mission and checkpoints can read them; reset on a new game.
    /// </summary>
    public static class GameProgress
    {
        public const int EvidenceTotal = 6;
        public const int HistoryCardTotal = 6;

        public static bool HasCamera;
        public static readonly HashSet<string> Photos = new();
        public static readonly HashSet<int> HistoryCards = new();

        public static event Action<string> PhotoTaken;
        public static event Action<int> HistoryCardFound;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            PhotoTaken = null;
            HistoryCardFound = null;
        }

        public static void NewGame()
        {
            HasCamera = false;
            Photos.Clear();
            HistoryCards.Clear();
        }

        public static void AddPhoto(string id)
        {
            if (Photos.Add(id)) PhotoTaken?.Invoke(id);
        }

        public static void AddHistoryCard(int index)
        {
            if (HistoryCards.Add(index)) HistoryCardFound?.Invoke(index);
        }
    }
}
