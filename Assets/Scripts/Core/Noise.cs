using System;
using UnityEngine;

namespace Kosynierzy
{
    public enum NoiseKind { Footstep, Run, Land, Gunshot, Other }

    /// <summary>
    /// Sound events the AI can hear. Emit(position, radius) — every listener within the radius reacts.
    /// Team doc radii: walk 4 m, run 12 m, shot 30 m; sneaking is quieter.
    /// </summary>
    public static class Noise
    {
        public static event Action<Vector3, float, NoiseKind, Transform> Emitted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Emitted = null;

        public static void Emit(Vector3 position, float radius, NoiseKind kind, Transform source) =>
            Emitted?.Invoke(position, radius, kind, source);
    }

}
