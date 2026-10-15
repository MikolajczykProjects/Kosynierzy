using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Trauma-based camera shake (rotation only — positional shake causes motion sickness).
    /// Anything can call AddTrauma(0..1); FirstPersonController applies Rotation every frame.
    /// Scaled by GameSettings.CameraShake, so 0 in the menu really means no shake.
    /// </summary>
    public static class CameraShake
    {
        const float MaxAngle = 4f;      // degrees at full trauma
        const float Frequency = 22f;
        const float Decay = 1.6f;       // trauma per second

        static float trauma;
        static float seed = 17.3f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => trauma = 0f;

        public static void AddTrauma(float amount) => trauma = Mathf.Clamp01(trauma + amount);

        /// <summary>Euler offset in degrees for this frame; call once per frame.</summary>
        public static Vector3 Evaluate(float deltaTime)
        {
            trauma = Mathf.Max(0f, trauma - Decay * deltaTime);
            float shake = trauma * trauma * GameSettings.CameraShake;
            if (shake <= 0f) return Vector3.zero;
            float t = Time.unscaledTime * Frequency;
            return new Vector3(
                (Mathf.PerlinNoise(seed, t) * 2f - 1f) * MaxAngle * shake,
                (Mathf.PerlinNoise(seed + 1f, t) * 2f - 1f) * MaxAngle * shake,
                (Mathf.PerlinNoise(seed + 2f, t) * 2f - 1f) * MaxAngle * 0.6f * shake);
        }
    }
}
