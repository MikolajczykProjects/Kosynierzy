using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Marks where a character went down (added by Character on death; the body stays). SD and Schupo that see it start
    /// searching; civilians who see it panic and run to raise the alarm.
    /// </summary>
    [DisallowMultipleComponent]
    public class Body : MonoBehaviour
    {
        static readonly List<Body> all = new();

        public static IReadOnlyList<Body> All => all;

        public CharacterType Type { get; private set; }
        /// <summary>Killed by a silent takedown (no shot).</summary>
        public bool Silent { get; private set; }
        /// <summary>Somebody already reacted to this body.</summary>
        public bool Discovered { get; private set; }
        public float DeathTime { get; private set; }
        /// <summary>Point the NPCs look at (just above the ground where it fell).</summary>
        public Vector3 Position => transform.position + Vector3.up * 0.3f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        public void Init(CharacterType type, bool silent)
        {
            Type = type;
            Silent = silent;
            DeathTime = Time.time;
        }

        public void MarkDiscovered() => Discovered = true;

        void OnEnable()
        {
            if (!all.Contains(this)) all.Add(this);
        }

        void OnDisable() => all.Remove(this);
    }
}
