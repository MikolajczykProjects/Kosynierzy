using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Guarded Animator parameter writes for the team's OB_* / CH_* prefabs: a missing parameter, a missing controller or
    /// an inactive Animator never throws or logs — the call just returns false. Parameter lists are cached per controller
    /// (Animator.parameters allocates), so this is safe to call every frame.
    /// </summary>
    public static class AnimParams
    {
        static readonly Dictionary<RuntimeAnimatorController, Dictionary<string, AnimatorControllerParameterType>> cache = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => cache.Clear();

        /// <summary>The Animator on this object, else the first one below it (null when there is none).</summary>
        public static Animator Of(GameObject go) => go != null ? go.GetComponentInChildren<Animator>() : null;

        public static bool Has(Animator animator, string name, AnimatorControllerParameterType type)
        {
            if (animator == null || !animator.isActiveAndEnabled) return false;
            var controller = animator.runtimeAnimatorController;
            if (controller == null) return false;
            if (!cache.TryGetValue(controller, out var map))
            {
                var list = animator.parameters;
                if (list.Length == 0) return false; // not initialised yet or no parameters: do not cache
                map = new Dictionary<string, AnimatorControllerParameterType>(list.Length);
                foreach (var p in list) map[p.name] = p.type;
                cache[controller] = map;
            }
            return map.TryGetValue(name, out var found) && found == type;
        }

        public static bool SetBool(Animator animator, string name, bool value)
        {
            if (!Has(animator, name, AnimatorControllerParameterType.Bool)) return false;
            animator.SetBool(name, value);
            return true;
        }

        public static bool SetFloat(Animator animator, string name, float value)
        {
            if (!Has(animator, name, AnimatorControllerParameterType.Float)) return false;
            animator.SetFloat(name, value);
            return true;
        }

        public static bool SetTrigger(Animator animator, string name)
        {
            if (!Has(animator, name, AnimatorControllerParameterType.Trigger)) return false;
            animator.SetTrigger(name);
            return true;
        }

        // Convenience overloads: the Animator is looked up on the object or below it.
        public static bool SetBool(GameObject go, string name, bool value) => SetBool(Of(go), name, value);
        public static bool SetFloat(GameObject go, string name, float value) => SetFloat(Of(go), name, value);
        public static bool SetTrigger(GameObject go, string name) => SetTrigger(Of(go), name);
    }
}
