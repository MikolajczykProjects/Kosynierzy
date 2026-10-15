using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Street lamps (OB_Latarnia from team D, or placeholder posts) at miasto_/hotel_/radio_latarnia_* points.
    /// At night (AtmosphereController.NightChanged) each gets Animator bool "Wlaczona" = true and a warm
    /// point light (6 m — the same radius the AI uses for "lit" zones). Added to the scene by Mission.
    /// </summary>
    public class StreetLamps : MonoBehaviour
    {
        [SerializeField] float lightRange = 9f;
        [SerializeField] float lightIntensity = 2.2f;
        [SerializeField] Color lightColor = new(1f, 0.78f, 0.45f);

        readonly List<(GameObject lamp, Light light)> lamps = new();

        void Start()
        {
            foreach (var prefix in new[] { "miasto_latarnia", "hotel_latarnia", "radio_latarnia" })
            foreach (var p in Points.Series(prefix))
            {
                var lamp = Spawner.Spawn("OB_Latarnia", p.position, p.rotation);
                if (!Spawner.HasPrefab("OB_Latarnia"))
                {
                    // Placeholder post 3.5 m tall.
                    lamp.transform.localScale = new Vector3(0.15f, 1.75f, 0.15f);
                    lamp.transform.position = p.position + Vector3.up * 1.75f;
                }
                var lightGo = new GameObject("LampLight");
                lightGo.transform.position = p.position + Vector3.up * 3.4f;
                lightGo.transform.SetParent(lamp.transform, true);
                var l = lightGo.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = lightRange;
                l.intensity = lightIntensity;
                l.color = lightColor;
                l.shadows = LightShadows.None;
                l.enabled = false;
                lamps.Add((lamp, l));
            }
            AtmosphereController.NightChanged += SetOn;
            SetOn(AtmosphereController.IsNight);
        }

        void OnDestroy() => AtmosphereController.NightChanged -= SetOn;

        void SetOn(bool on)
        {
            foreach (var (lamp, light) in lamps)
            {
                if (lamp == null) continue;
                light.enabled = on;
                var anim = lamp.GetComponentInChildren<Animator>();
                if (anim != null) Dialog.SetBool(anim, "Wlaczona", on);
            }
        }
    }
}
