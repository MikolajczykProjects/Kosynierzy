using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Street lamps (OB_Latarnia from team D, or placeholder posts) at miasto_/hotel_/radio_latarnia_* points.
    /// At night (AtmosphereController.NightChanged) every lamp is switched on, by day off.
    /// Real prefab: only the Animator bool "Wlaczona" is driven (AC_Latarnia also switches the spot light "Swiatlo", 8 m,
    /// and flickers; the prefab defaults to ON, so it is set to OFF by day) — no extra light, no scaling.
    /// Placeholder post: scaled to a 3.5 m post with our own warm point light (range ≈ the AI's 6 m "lit" zone).
    /// Added to the scene by Mission.
    /// </summary>
    public class StreetLamps : MonoBehaviour
    {
        [SerializeField] float lightRange = 9f;
        [SerializeField] float lightIntensity = 2.2f;
        [SerializeField] Color lightColor = new(1f, 0.78f, 0.45f);

        // light == null → real prefab (animator-driven); otherwise our placeholder light.
        readonly List<(GameObject lamp, Light light)> lamps = new();
        bool settled;

        void Start()
        {
            bool real = Spawner.HasPrefab("OB_Latarnia");
            foreach (var prefix in new[] { "miasto_latarnia", "hotel_latarnia", "radio_latarnia" })
            foreach (var p in Points.Series(prefix))
            {
                var lamp = Spawner.Spawn("OB_Latarnia", p.position, p.rotation);
                if (real)
                {
                    lamps.Add((lamp, null));
                    continue;
                }
                // Placeholder post 3.5 m tall.
                lamp.transform.localScale = new Vector3(0.15f, 1.75f, 0.15f);
                lamp.transform.position = p.position + Vector3.up * 1.75f;
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
            settled = true;
        }

        void OnDestroy() => AtmosphereController.NightChanged -= SetOn;

        void SetOn(bool on)
        {
            foreach (var (lamp, light) in lamps)
            {
                if (lamp == null) continue;
                if (light != null) { light.enabled = on; continue; }

                var anim = lamp.GetComponentInChildren<Animator>();
                AnimParams.SetBool(anim, "Wlaczona", on);
                if (!settled && anim != null && anim.isActiveAndEnabled)
                {
                    // First call right after spawning: settle the controller now, otherwise the prefab's default
                    // (lit) state would show for a frame or two by day.
                    anim.Update(0f);
                    anim.Update(0f);
                }
            }
        }
    }
}
