using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kosynierzy
{
    /// <summary>
    /// Checkpoints at the start of each act (team doc). Reload = reload the scene and resume the mission
    /// at the saved step with the progress snapshot taken then. Historical mode has no checkpoints.
    /// F8 saves the current step, F9 loads (dev / presentation shortcuts, handled by Mission).
    /// </summary>
    public static class Checkpoints
    {
        public static int SavedStep { get; private set; } = -1;
        /// <summary>Step to resume after a reload; consumed by Mission.Start.</summary>
        public static int ResumeStep = -1;

        static bool savedCamera;
        static readonly List<string> savedPhotos = new();
        static readonly List<int> savedCards = new();
        static int savedAmmo = -1;

        public static void Save(int step)
        {
            SavedStep = step;
            savedCamera = GameProgress.HasCamera;
            savedPhotos.Clear(); savedPhotos.AddRange(GameProgress.Photos);
            savedCards.Clear(); savedCards.AddRange(GameProgress.HistoryCards);
            var w = Object.FindAnyObjectByType<PlayerWeapon>();
            savedAmmo = w != null ? w.AmmoInMagazine + w.ReserveAmmo : -1;
            Debug.Log($"Kosynierzy: punkt kontrolny zapisany (krok {step}).");
        }

        public static void RestoreProgress()
        {
            GameProgress.HasCamera = savedCamera;
            GameProgress.Photos.Clear(); foreach (var p in savedPhotos) GameProgress.Photos.Add(p);
            GameProgress.HistoryCards.Clear(); foreach (var c in savedCards) GameProgress.HistoryCards.Add(c);
        }

        public static int SavedAmmo => savedAmmo;

        /// <summary>Reload the scene at the last checkpoint (or from the start when none).</summary>
        public static void Reload()
        {
            ResumeStep = SavedStep;
            Restart();
        }

        public static void Restart()
        {
            MissionFail.Clear();
            GameState.SetPaused(false);
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0
                ? SceneManager.GetActiveScene().name : SceneManager.GetActiveScene().path);
        }

        public static void NewGame()
        {
            SavedStep = -1;
            ResumeStep = -1;
            savedPhotos.Clear();
            savedCards.Clear();
            savedCamera = false;
            savedAmmo = -1;
        }
    }
}
