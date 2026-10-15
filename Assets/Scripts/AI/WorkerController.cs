using UnityEngine;
using UnityEngine.AI;

namespace Kosynierzy
{
    /// <summary>
    /// Radio-station worker held in the basement: stands with Animator "RecePodniesione" = true until SetFreed()
    /// (mission act 3), then lowers the hands and walks to the nearest exit point (radio_piwnica_drzwi,
    /// radio_drzwi_wyjscie, spawn_radio_tyl, spawn_radio_brama) and idles there. Shooting a worker fails the mission.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class WorkerController : NpcBrain
    {
        static readonly string[] ExitPoints = { "radio_piwnica_drzwi", "radio_drzwi_wyjscie", "spawn_radio_tyl", "spawn_radio_brama" };
        static readonly int HandsUpId = Animator.StringToHash("RecePodniesione");

        [SerializeField] float walkSpeed = 1.5f;
        [Tooltip("Exit points farther than this are ignored (they belong to another interior).")]
        [SerializeField] float maxExitDistance = 80f;

        bool hasExit;
        Vector3 exitPoint;

        public bool IsFreed { get; private set; }
        protected override CharacterType DefaultType => CharacterType.Worker;

        void Update()
        {
            if (IsDown) return;
            character.SetAnimBool(HandsUpId, !IsFreed); // re-applied: SetActive resets Animator parameters
            if (!IsFreed || !hasExit)
            {
                Halt();
                return;
            }
            if (Arrived(exitPoint, 1.2f))
            {
                Halt();
                hasExit = false;
                return;
            }
            MoveTo(exitPoint, walkSpeed);
        }

        /// <summary>Hands down, walk to the nearest exit point.</summary>
        public void SetFreed()
        {
            if (IsFreed || IsDown) return;
            IsFreed = true;
            hasExit = FindExit(out exitPoint);
        }

        /// <summary>Hands down, walk to the given position.</summary>
        public void SetFreed(Vector3 destination)
        {
            if (IsDown) return;
            IsFreed = true;
            exitPoint = destination;
            hasExit = true;
        }

        bool FindExit(out Vector3 point)
        {
            point = transform.position;
            bool found = false;
            float best = maxExitDistance;
            foreach (var pointName in ExitPoints)
            {
                if (!Points.Has(pointName)) continue;
                Vector3 p = Points.Position(pointName);
                float d = FlatDistanceTo(p);
                if (d >= best) continue;
                best = d;
                point = p;
                found = true;
            }
            return found;
        }
    }
}
