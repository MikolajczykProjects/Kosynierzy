using System;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Player-facing settings shared by gameplay code and the settings menu. Persisted in PlayerPrefs.
    /// Gameplay reads the values every frame (or listens to Changed); the menu writes them and calls Save().
    /// </summary>
    public static class GameSettings
    {
        const string Prefix = "kosynierzy.";

        public static float MouseSensitivity = 1f;   // multiplier on the controller's base sensitivity
        public static bool InvertY;
        public static float FieldOfView = 80f;       // vertical FOV in degrees
        public static bool HeadBob = true;
        public static float CameraShake = 1f;        // 0 = off
        public static float MasterVolume = 1f;
        /// <summary>"Krew (16+)" checkbox: blood instead of dust on hits. Off by default (team rule: no gore).</summary>
        public static bool MatureContent;

        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Changed = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Load()
        {
            MouseSensitivity = PlayerPrefs.GetFloat(Prefix + "sens", 1f);
            InvertY = PlayerPrefs.GetInt(Prefix + "invertY", 0) == 1;
            FieldOfView = PlayerPrefs.GetFloat(Prefix + "fov", 80f);
            HeadBob = PlayerPrefs.GetInt(Prefix + "headBob", 1) == 1;
            CameraShake = PlayerPrefs.GetFloat(Prefix + "shake", 1f);
            MasterVolume = PlayerPrefs.GetFloat(Prefix + "volume", 1f);
            MatureContent = PlayerPrefs.GetInt(Prefix + "mature", 0) == 1;
            AudioListener.volume = MasterVolume;
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat(Prefix + "sens", MouseSensitivity);
            PlayerPrefs.SetInt(Prefix + "invertY", InvertY ? 1 : 0);
            PlayerPrefs.SetFloat(Prefix + "fov", FieldOfView);
            PlayerPrefs.SetInt(Prefix + "headBob", HeadBob ? 1 : 0);
            PlayerPrefs.SetFloat(Prefix + "shake", CameraShake);
            PlayerPrefs.SetFloat(Prefix + "volume", MasterVolume);
            PlayerPrefs.SetInt(Prefix + "mature", MatureContent ? 1 : 0);
            PlayerPrefs.Save();
            AudioListener.volume = MasterVolume;
            Changed?.Invoke();
        }
    }

    /// <summary>Global pause state; the pause menu toggles it, gameplay input checks IsPaused.</summary>
    public static class GameState
    {
        public static bool IsPaused { get; private set; }
        public static event Action<bool> PauseChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            IsPaused = false;
            PauseChanged = null;
        }

        public static void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;
            PauseChanged?.Invoke(paused);
        }

        public static void TogglePause() => SetPaused(!IsPaused);
    }
}
