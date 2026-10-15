using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kosynierzy
{
    /// <summary>
    /// Checkpoint at the start of EVERY mission step (player feedback: "not everywhere are checkpoints"), with
    /// the player's position, so a failure costs at most one step. Reload = reload the scene and resume at the
    /// saved step with the progress snapshot. Historical mode has no checkpoints.
    /// F8 saves now, F9 loads (dev / presentation shortcuts, handled by Mission).
    /// </summary>
    public static class Checkpoints
    {
        public static int SavedStep { get; private set; } = -1;
        /// <summary>Step to resume after a reload; consumed by Mission.Start.</summary>
        public static int ResumeStep = -1;

        public static bool HasPosition { get; private set; }
        public static Vector3 SavedPosition { get; private set; }
        public static float SavedYaw { get; private set; }

        static bool savedCamera;
        static readonly List<string> savedPhotos = new();
        static readonly List<int> savedCards = new();

        // Enter Play Mode Options are set to "no domain reload" in this project: statics survive a stop/play, so a
        // checkpoint from the previous play session would otherwise offer "Spróbuj od punktu kontrolnego" in a new game.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => NewGame();

        public static void Save(int step)
        {
            SavedStep = step;
            savedCamera = GameProgress.HasCamera;
            savedPhotos.Clear(); savedPhotos.AddRange(GameProgress.Photos);
            savedCards.Clear(); savedCards.AddRange(GameProgress.HistoryCards);
            var p = Object.FindAnyObjectByType<FirstPersonController>();
            HasPosition = p != null;
            if (p != null)
            {
                SavedPosition = p.transform.position;
                SavedYaw = p.transform.eulerAngles.y;
            }
        }

        /// <summary>Replaces the saved position (the act-start teleport was still pending when the step was saved).</summary>
        public static void OverridePosition(Vector3 position, float yaw)
        {
            HasPosition = true;
            SavedPosition = position;
            SavedYaw = yaw;
        }

        public static void RestoreProgress()
        {
            GameProgress.HasCamera = savedCamera;
            GameProgress.Photos.Clear(); foreach (var ph in savedPhotos) GameProgress.Photos.Add(ph);
            GameProgress.HistoryCards.Clear(); foreach (var c in savedCards) GameProgress.HistoryCards.Add(c);
        }

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
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public static void NewGame()
        {
            SavedStep = -1;
            ResumeStep = -1;
            HasPosition = false;
            savedPhotos.Clear();
            savedCards.Clear();
            savedCamera = false;
        }
    }
}
