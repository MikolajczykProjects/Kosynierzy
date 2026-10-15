using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Kosynierzy
{
    /// <summary>
    /// Base for NPC behaviours: NavMeshAgent movement (throttled re-pathing; arrival also accepts targets the agent
    /// cannot fully reach), turning, roaming a route or around home, random NavMesh points and rate-limited barks.
    /// The Character on the same object calls NotifyDown and disables the brain when the NPC goes down.
    /// Without a usable NavMesh the NPC simply stands (every move counts as arrived).
    /// </summary>
    public abstract class NpcBrain : MonoBehaviour
    {
        const float RepathInterval = 0.25f;
        static readonly Dictionary<string, float> nextGlobalBark = new();

        protected NavMeshAgent agent;
        protected Character character;
        protected Vector3 homePosition;
        protected Quaternion homeRotation;

        Vector3 requestedPoint, sampledDestination;
        bool hasRequest;
        float requestTime, nextRepathTime, nextBarkTime;

        Vector3 roamTarget;
        bool roamHasTarget, roamWaiting;
        float roamWaitUntil;
        int roamIndex = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => nextGlobalBark.Clear();

        /// <summary>Type given to a Character this brain has to add itself (brain added by hand).</summary>
        protected abstract CharacterType DefaultType { get; }

        public bool IsDown => character != null && character.IsDown;

        protected virtual void Awake()
        {
            character = GetComponent<Character>();
            if (character == null)
            {
                character = gameObject.AddComponent<Character>();
                character.Type = DefaultType;
            }
            agent = GetComponent<NavMeshAgent>();
            homePosition = transform.position;
            homeRotation = transform.rotation;
        }

        /// <summary>Called by Character right before the brain is disabled.</summary>
        public virtual void NotifyDown() { }

        protected bool AgentReady => agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;

        protected static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        protected float FlatDistanceTo(Vector3 point) => FlatDistance(transform.position, point);

        // ---------- Movement ----------

        protected void MoveTo(Vector3 point, float speed)
        {
            if (!AgentReady) return;
            agent.speed = speed;
            if (agent.isStopped) agent.isStopped = false;

            bool busy = agent.hasPath || agent.pathPending;
            float moved = hasRequest ? FlatDistance(requestedPoint, point) : float.MaxValue;
            if (moved < 0.6f && (busy || Time.time < requestTime + 1f)) return; // same target (idle agent: retry 1x/s)
            if (busy && moved < 3f && Time.time < nextRepathTime) return;       // moving target: repath at most 4x/s
            nextRepathTime = Time.time + RepathInterval;

            if (moved >= 0.6f) requestTime = Time.time;
            requestedPoint = point;
            hasRequest = true;
            sampledDestination = NavMesh.SamplePosition(point, out var hit, 3f, NavMesh.AllAreas) ? hit.position : point;
            agent.SetDestination(sampledDestination);
        }

        /// <summary>Within tolerance of the point, or the agent got as close as the NavMesh allows (or is stuck).</summary>
        protected bool Arrived(Vector3 point, float tolerance = 0.6f)
        {
            if (FlatDistanceTo(point) <= tolerance) return true;
            if (!AgentReady) return true; // cannot move: treat as there instead of getting stuck
            if (!hasRequest || FlatDistance(requestedPoint, point) > 0.6f) return false;
            if (FlatDistanceTo(sampledDestination) <= agent.stoppingDistance + 0.4f) return true; // nearest NavMesh point
            if (agent.pathPending) return false;
            if (agent.pathStatus == NavMeshPathStatus.PathInvalid) return true;
            if (Time.time < requestTime + 0.5f) return false;
            if (agent.hasPath && agent.remainingDistance <= agent.stoppingDistance + 0.2f) return true; // end of a partial path
            return Time.time > requestTime + 1.5f && agent.velocity.sqrMagnitude < 0.01f;                // stuck
        }

        protected void Halt()
        {
            hasRequest = false;
            if (!AgentReady || agent.isStopped) return;
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        protected void FaceTowards(Vector3 point, float degreesPerSecond = 360f)
        {
            Vector3 d = point - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return;
            TurnTo(Quaternion.LookRotation(d, Vector3.up), degreesPerSecond);
        }

        protected void TurnTo(Quaternion rotation, float degreesPerSecond)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, degreesPerSecond * Time.deltaTime);
        }

        protected bool IsFacing(Vector3 point, float maxAngle)
        {
            Vector3 d = point - transform.position;
            d.y = 0f;
            return d.sqrMagnitude < 0.01f || Vector3.Angle(transform.forward, d) <= maxAngle;
        }

        protected static Vector3 RandomNavPoint(Vector3 center, float radius)
        {
            for (int i = 0; i < 6; i++)
            {
                Vector2 r = Random.insideUnitCircle * radius;
                if (NavMesh.SamplePosition(center + new Vector3(r.x, 0f, r.y), out var hit, 2f, NavMesh.AllAreas))
                    return hit.position;
            }
            return center;
        }

        // ---------- Roaming ----------

        /// <summary>
        /// One frame of walking a route (in order, or random stops preferring nearby ones) or random walks within
        /// radius of home, waiting at each stop. No points and radius 0 = stand at home facing the start direction.
        /// </summary>
        protected void Roam(IReadOnlyList<Vector3> points, bool randomOrder, float radius, float speed, Vector2 wait)
        {
            bool hasPoints = points != null && points.Count > 0;
            if (!hasPoints && radius <= 0.01f)
            {
                if (!Arrived(homePosition)) MoveTo(homePosition, speed);
                else
                {
                    Halt();
                    TurnTo(homeRotation, 120f);
                }
                return;
            }

            if (!roamHasTarget || (roamWaiting && Time.time >= roamWaitUntil))
            {
                roamHasTarget = true;
                roamWaiting = false;
                roamTarget = NextRoamPoint(points, randomOrder, radius);
            }
            if (roamWaiting) return;

            if (Arrived(roamTarget))
            {
                Halt();
                roamWaiting = true;
                roamWaitUntil = Time.time + Random.Range(wait.x, wait.y);
                return;
            }
            MoveTo(roamTarget, speed);
        }

        /// <summary>Forget the current stop (route changed, back from a chase).</summary>
        protected void ResetRoam()
        {
            roamHasTarget = false;
            roamWaiting = false;
        }

        protected void ResetRoamIndex() => roamIndex = -1;

        Vector3 NextRoamPoint(IReadOnlyList<Vector3> points, bool randomOrder, float radius)
        {
            if (points == null || points.Count == 0) return RandomNavPoint(homePosition, radius);
            if (randomOrder && points.Count > 1)
            {
                int pick = -1;
                for (int tries = 0; tries < 8 && pick < 0; tries++)
                {
                    int i = Random.Range(0, points.Count);
                    if (i == roamIndex) continue;
                    if (tries >= 5 || FlatDistanceTo(points[i]) <= 60f) pick = i;
                }
                roamIndex = pick >= 0 ? pick : (roamIndex + 1) % points.Count;
            }
            else roamIndex = (roamIndex + 1) % points.Count;
            return points[roamIndex];
        }

        // ---------- Barks ----------

        /// <summary>Random line of a bark set (Dialog.Bark); max one per NPC every 5 s and one per id every 1.5 s.</summary>
        protected void Bark(string id)
        {
            if (Time.time < nextBarkTime) return;
            if (nextGlobalBark.TryGetValue(id, out float next) && Time.time < next) return;
            nextBarkTime = Time.time + 5f;
            nextGlobalBark[id] = Time.time + 1.5f;
            Dialog.Bark(id, transform.position + Vector3.up * 1.7f);
        }
    }
}
