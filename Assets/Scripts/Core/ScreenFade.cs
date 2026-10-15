using System;
using System.Collections;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>Full-screen black fade used by doors, checkpoints and the epilogue.</summary>
    public class ScreenFade : MonoBehaviour
    {
        static ScreenFade instance;
        float alpha;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        static ScreenFade Instance => instance != null ? instance : instance = new GameObject("[ScreenFade]").AddComponent<ScreenFade>();

        public static float Alpha => instance != null ? instance.alpha : 0f;

        /// <summary>Fade to black, run the action while black, fade back in. Real-time seconds.</summary>
        public static void Transition(Action whileBlack, float fadeOut = 0.3f, float hold = 0.1f, float fadeIn = 0.35f) =>
            Instance.StartCoroutine(Instance.Run(whileBlack, fadeOut, hold, fadeIn));

        public static void SetAlpha(float a) => Instance.alpha = a;

        IEnumerator Run(Action whileBlack, float fadeOut, float hold, float fadeIn)
        {
            for (float t = 0f; t < fadeOut; t += Time.unscaledDeltaTime) { alpha = t / fadeOut; yield return null; }
            alpha = 1f;
            whileBlack?.Invoke();
            yield return new WaitForSecondsRealtime(hold);
            for (float t = 0f; t < fadeIn; t += Time.unscaledDeltaTime) { alpha = 1f - t / fadeIn; yield return null; }
            alpha = 0f;
        }

        void OnGUI()
        {
            if (alpha <= 0f) return;
            GUI.depth = -1000;
            GUI.color = new Color(0f, 0f, 0f, alpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
