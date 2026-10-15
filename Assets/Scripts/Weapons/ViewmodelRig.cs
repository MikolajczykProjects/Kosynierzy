using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Runtime glue on a REAL (team D) first-person viewmodel prefab such as OB_Pistolet_Vis or OB_Aparat. Added by
    /// PlayerHands, used by PlayerWeapon. Everything is optional: a missing Animator, parameter or child is skipped
    /// (never throws, never logs per frame).
    ///  - guarded Animator triggers (Strzal / Przeladuj) — a parameter is written only when the controller really has
    ///    a parameter of that name and type;
    ///  - the ejection port (child "Wyrzut") for the casing origin;
    ///  - ADS hammer flattening: the cocked Vis hammer (child "Kurek") is 6 mm higher than the sights and sits right
    ///    behind the rear sight, so it would hide the sight picture; while aiming it is squashed below the slide top;
    ///  - D's particle prefabs: the muzzle flash and casing prefabs are switched OFF (our VfxLibrary is the single
    ///    source of those effects), the camera flash plays on request (PlayFlash).
    /// </summary>
    public class ViewmodelRig : MonoBehaviour
    {
        public const string TriggerShot = "Strzal";
        public const string TriggerReload = "Przeladuj";

        /// <summary>Vertical scale of the hammer when fully aimed (1 = untouched). Set 1 to keep the art as authored.</summary>
        public const float HammerAimScaleDefault = 0.2f;
        /// <summary>Camera flash particle size multiplier (the prefab is 0.6 m, the lens is 0.1-0.4 m from the eye).</summary>
        public const float FlashSizeScale = 0.5f;

        public Animator Animator { get; private set; }
        public Transform Ejector { get; private set; }
        public Transform Hammer { get; private set; }
        public float HammerAimScale { get; set; } = HammerAimScaleDefault;

        ParticleSystem flash;
        float aim;
        float pendingReloadAt = -1f;
        List<AnimatorControllerParameter> parameters;

        /// <summary>Find the parts by their contract names under <paramref name="root"/> and silence D's muzzle FX.</summary>
        public void Setup(Transform root)
        {
            Animator = root.GetComponentInChildren<Animator>(true);
            if (Animator != null)
            {
                Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                Animator.updateMode = AnimatorUpdateMode.Normal;
            }
            Ejector = FindDeep(root, "Wyrzut");
            Hammer = FindDeep(root, "Kurek");

            // One source of muzzle flash / casings: ours. D's FX instances stay inactive.
            DisableChild(root, "FX_BlyskLufy");
            DisableChild(root, "FX_Luska");

            var flashT = FindDeep(root, "FX_FlashAparatu");
            foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.playOnAwake = false;
                if (ps.gameObject.activeInHierarchy) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if (flashT != null)
            {
                flash = flashT.GetComponent<ParticleSystem>();
                if (flash != null)
                {
                    var main = flash.main;
                    main.startSizeMultiplier *= FlashSizeScale;
                }
            }
        }

        /// <summary>0 = hip, 1 = fully aimed (PlayerWeapon passes its aim blend every frame).</summary>
        public void SetAim(float value) => aim = Mathf.Clamp01(value);

        /// <summary>Shot fired: hammer + slide animation.</summary>
        public void OnShot() => SetTrigger(TriggerShot);

        /// <summary>Reload started; <paramref name="delay"/> lets the last shot's slide animation finish first.</summary>
        public void OnReloadStart(float delay)
        {
            if (delay <= 0f)
            {
                pendingReloadAt = -1f;
                SetTrigger(TriggerReload);
            }
            else pendingReloadAt = Time.time + delay;
        }

        /// <summary>Camera item: flash from the lens (child Obiektyw/FX_FlashAparatu).</summary>
        public void PlayFlash()
        {
            if (flash == null) return;
            if (!flash.gameObject.activeInHierarchy) return;
            flash.Clear(true);
            flash.Play(true);
        }

        /// <summary>Guarded trigger write. False when nothing was written.</summary>
        public bool SetTrigger(string parameter)
        {
            var a = Animator;
            if (a == null || !a.isActiveAndEnabled || !a.isInitialized || a.runtimeAnimatorController == null) return false;
            if (parameters == null) parameters = new List<AnimatorControllerParameter>(a.parameters);
            foreach (var p in parameters)
            {
                if (p.name == parameter && p.type == AnimatorControllerParameterType.Trigger)
                {
                    a.SetTrigger(parameter);
                    return true;
                }
            }
            return false;
        }

        /// <summary>Uncached check, for callers that hold only an Animator.</summary>
        public static bool HasParameter(Animator a, string parameter, AnimatorControllerParameterType type)
        {
            if (a == null || !a.isInitialized || a.runtimeAnimatorController == null) return false;
            foreach (var p in a.parameters) if (p.name == parameter && p.type == type) return true;
            return false;
        }

        void Update()
        {
            if (pendingReloadAt >= 0f && Time.time >= pendingReloadAt)
            {
                pendingReloadAt = -1f;
                SetTrigger(TriggerReload);
            }
        }

        // After the Animator: the clips never animate the hammer's scale.
        void LateUpdate()
        {
            if (Hammer == null || HammerAimScale >= 1f) return;
            float k = Mathf.SmoothStep(0f, 1f, aim);
            Hammer.localScale = new Vector3(1f, Mathf.Lerp(1f, HammerAimScale, k), 1f);
        }

        void OnDisable() => pendingReloadAt = -1f;

        static void DisableChild(Transform root, string childName)
        {
            var t = FindDeep(root, childName);
            if (t != null) t.gameObject.SetActive(false);
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
    }
}
