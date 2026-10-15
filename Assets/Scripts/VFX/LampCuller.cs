using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// The map has 413 point lights (MapaLampa) that MapaSwiatlo switches on all at once from 19:30.
    /// This keeps only the nearest few realtime and never lets any of them cast shadows.
    /// Runs every 0.4 s, so it also overrides MapaSwiatlo re-enabling them.
    /// </summary>
    public class LampCuller : MonoBehaviour
    {
        const int MaxLit = 8;
        const float MaxDistance = 60f;

        readonly List<(Light light, float dist)> candidates = new();
        MapaLampa[] lamps;
        float next;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<LampCuller>() == null) new GameObject("LampCuller").AddComponent<LampCuller>();
        }

        void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + 0.4f;
            if (lamps == null) lamps = FindObjectsByType<MapaLampa>();
            var cam = Camera.main;
            if (cam == null || lamps.Length == 0) return;
            bool night = MapaSwiatlo.Instance != null && MapaSwiatlo.Instance.godzina >= 19.5f;
            Vector3 p = cam.transform.position;

            candidates.Clear();
            foreach (var lamp in lamps)
            {
                if (lamp == null || !lamp.noc) continue;
                var l = lamp.GetComponent<Light>();
                if (l == null) continue;
                l.shadows = LightShadows.None;
                if (!night) { l.enabled = false; continue; }
                float d = (lamp.transform.position - p).sqrMagnitude;
                candidates.Add((l, d));
            }
            if (!night) return;
            candidates.Sort((a, b) => a.dist.CompareTo(b.dist));
            for (int i = 0; i < candidates.Count; i++)
                candidates[i].light.enabled = i < MaxLit && candidates[i].dist < MaxDistance * MaxDistance;
        }
    }
}
