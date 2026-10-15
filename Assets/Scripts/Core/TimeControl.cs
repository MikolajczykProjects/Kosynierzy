using System.Collections;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Hit-stop and slow-motion that never fight the pause menu. Durations are real seconds.
    /// Hit-stop is meant for LONE big moments (a kill), not every hit — repeated freezes read as lag.
    /// </summary>
    public class TimeControl : MonoBehaviour
    {
        static TimeControl instance;
        Coroutine running;
        float lastHitStop = -10f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        static TimeControl Instance
        {
            get
            {
                if (instance == null) instance = new GameObject("[TimeControl]").AddComponent<TimeControl>();
                return instance;
            }
        }

        /// <summary>Short freeze; skipped when another one happened within <paramref name="minGap"/> s.</summary>
        public static void HitStop(float duration = 0.07f, float scale = 0.05f, float minGap = 0.9f)
        {
            var tc = Instance;
            // Never cut short a running slow motion (e.g. the last enemy of an objective).
            if (tc.running != null || Time.unscaledTime - tc.lastHitStop < minGap) return;
            tc.lastHitStop = Time.unscaledTime;
            tc.Run(duration, scale, 0f);
        }

        /// <summary>Slow motion that eases back to normal speed over <paramref name="recover"/> s.</summary>
        public static void SlowMotion(float duration = 0.6f, float scale = 0.35f, float recover = 0.35f) =>
            Instance.Run(duration, scale, recover);

        void Run(float duration, float scale, float recover)
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Routine(duration, scale, recover));
        }

        IEnumerator Routine(float duration, float scale, float recover)
        {
            float end = Time.unscaledTime + duration;
            while (Time.unscaledTime < end)
            {
                if (!GameState.IsPaused) Time.timeScale = scale;
                yield return null;
            }
            float t = 0f;
            while (t < recover)
            {
                t += Time.unscaledDeltaTime;
                if (!GameState.IsPaused) Time.timeScale = Mathf.Lerp(scale, 1f, t / recover);
                yield return null;
            }
            if (!GameState.IsPaused) Time.timeScale = 1f;
            running = null;
        }

        void OnDestroy()
        {
            if (!GameState.IsPaused) Time.timeScale = 1f;
        }
    }
}
