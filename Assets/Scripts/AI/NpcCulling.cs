using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Kosynierzy
{
    /// <summary>
    /// Distance culling for every NPC (contract and extra): beyond CullOut from the player the brains, NavMeshAgent
    /// and Animator are switched off; inside CullIn they come back. Alerted watchers and panicking civilians are never
    /// culled. Ticks 2.5x per second. Also re-seats agents that started off the NavMesh (retry every ~3 s).
    /// Created by NpcDirector.Start.
    /// </summary>
    public class NpcCulling : MonoBehaviour
    {
        const float CullOut = 80f, CullIn = 70f, TickInterval = 0.4f;
        const int RetryEvery = 8; // ticks

        static readonly Dictionary<Character, NpcBrain[]> brainsOf = new();
        static NpcCulling instance;
        float nextTick;
        int tickCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            brainsOf.Clear();
            instance = null;
        }

        void Awake() => instance = this;

        /// <summary>Wakes every culled NPC now (e.g. before a global alarm).</summary>
        public static void WakeAll()
        {
            var list = Character.All;
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null && list[i].Culled) Resume(list[i]);
        }

        void Update()
        {
            if (Time.unscaledTime < nextTick) return;
            nextTick = Time.unscaledTime + TickInterval;
            tickCount++;

            Vector3 pp;
            if (PlayerTarget.Find()) pp = PlayerTarget.Feet;
            else
            {
                var cam = Camera.main;
                if (cam == null) return;
                pp = cam.transform.position;
            }

            var list = Character.All;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var c = list[i];
                if (c == null || c.IsDown) continue;
                float dx = c.transform.position.x - pp.x, dz = c.transform.position.z - pp.z;
                float sqr = dx * dx + dz * dz;
                if (c.Culled)
                {
                    if (sqr < CullIn * CullIn) Resume(c);
                    continue;
                }
                if (sqr > CullOut * CullOut && !MustStayAwake(c)) Suspend(c);
                else if ((tickCount + i) % RetryEvery == 0) TrySeat(c);
            }
        }

        static NpcBrain[] Brains(Character c)
        {
            if (!brainsOf.TryGetValue(c, out var brains) || brains == null)
                brainsOf[c] = brains = c.GetComponents<NpcBrain>();
            return brains;
        }

        static bool MustStayAwake(Character c)
        {
            foreach (var b in Brains(c))
            {
                if (b is WatcherBrain w && (w.Awareness != AwarenessLevel.Unaware || w.Suspicion > 0.02f)) return true;
                if (b is CivilianController civ && civ.IsPanicking) return true;
            }
            return false;
        }

        static void Suspend(Character c)
        {
            c.Culled = true;
            foreach (var b in Brains(c)) if (b != null) b.enabled = false;
            if (c.Agent != null && c.Agent.enabled) c.Agent.enabled = false;
            if (c.Anim != null) c.Anim.enabled = false;
        }

        static void Resume(Character c)
        {
            c.Culled = false;
            if (c.IsDown) return;
            if (c.Agent != null)
            {
                c.Agent.enabled = true;
                if (!c.Agent.isOnNavMesh) TrySeat(c);
            }
            foreach (var b in Brains(c)) if (b != null) b.enabled = true;
            if (c.Anim != null) c.Anim.enabled = true;
        }

        /// <summary>Agent off the NavMesh (spawned before the bake, or disabled by NpcFactory): put it on the nearest point.</summary>
        static void TrySeat(Character c)
        {
            var agent = c.Agent;
            if (agent == null || c.IsDown) return;
            if (agent.enabled && agent.isOnNavMesh) return;
            if (!NavMesh.SamplePosition(c.transform.position, out var hit, 6f, NavMesh.AllAreas)) return;
            if (!agent.enabled)
            {
                c.transform.position = hit.position;
                agent.enabled = true;
                return;
            }
            agent.Warp(hit.position);
        }
    }
}
