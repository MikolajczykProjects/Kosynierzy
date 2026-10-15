using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Kosynierzy
{
    /// <summary>
    /// Base for NPC behaviours: NavMeshAgent movement (every destination sampled onto the NavMesh, throttled re-pathing,
    /// a detour when no progress is made for 1.5 s, arrival that also accepts unreachable targets), turning, roaming a
    /// route or around home, line-of-sight checks, path-based hearing, body sighting and rate-limited barks.
    /// The Character on the same object calls NotifyDown and disables the brain when the NPC goes down.
    /// Without a usable NavMesh the NPC simply stands (every move counts as arrived).
    /// </summary>
    public abstract class NpcBrain : MonoBehaviour
    {
        const float RepathInterval = 0.25f;
        const float StuckTime = 1.5f;
        static readonly Dictionary<string, float> nextGlobalBark = new();

        protected NavMeshAgent agent;
        protected Character character;
        protected Vector3 homePosition;
        protected Quaternion homeRotation;

        readonly RaycastHit[] hits = new RaycastHit[16];
        readonly HashSet<Body> seenBodies = new();
        NavMeshPath path;

        Vector3 requestedPoint, sampledDestination;
        bool hasRequest, resendAfterDetour;
        float requestTime, nextRepathTime, nextBarkTime;
        Vector3 progressPosition;
        float progressTime, lastMoveCall, detourUntil, nextBodyCheck;
        int stuckCount;

        Vector3 roamTarget;
        bool roamHasTarget, roamWaiting;
        float roamWaitUntil;
        int roamIndex = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => nextGlobalBark.Clear();

        /// <summary>Type given to a Character this brain has to add itself (brain added by hand).</summary>
        protected abstract CharacterType DefaultType { get; }

        public bool IsDown => character != null && character.IsDown;

        /// <summary>Eye position for line-of-sight checks.</summary>
        protected virtual Vector3 Eye => transform.position + Vector3.up * 1.6f;

        protected virtual void Awake()
        {
            character = GetComponent<Character>();
            if (character == null)
            {
                character = gameObject.AddComponent<Character>();
                character.Type = DefaultType;
            }
            agent = GetComponent<NavMeshAgent>();
            if (agent != null) agent.avoidancePriority = Random.Range(35, 65); // fewer deadlocks in narrow streets
            homePosition = transform.position;
            homeRotation = transform.rotation;
            nextBodyCheck = Time.time + Random.Range(0f, 0.5f);
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

            // Progress watchdog: restart the clock when movement (re)starts after a pause.
            if (Time.time - lastMoveCall > 0.3f) ResetProgress();
            lastMoveCall = Time.time;
            if (Time.time < detourUntil) return;
            if (CheckStuck()) return;

            bool busy = agent.hasPath || agent.pathPending;
            float moved = hasRequest ? FlatDistance(requestedPoint, point) : float.MaxValue;
            if (!resendAfterDetour)
            {
                if (moved < 0.6f && (busy || Time.time < requestTime + 1f)) return; // same target (idle agent: retry 1x/s)
                if (busy && moved < 3f && Time.time < nextRepathTime) return;       // moving target: repath at most 4x/s
            }
            resendAfterDetour = false;
            nextRepathTime = Time.time + RepathInterval;

            if (moved >= 0.6f)
            {
                requestTime = Time.time;
                stuckCount = 0;
            }
            requestedPoint = point;
            hasRequest = true;
            sampledDestination = SampleNavMesh(point, 3f);
            agent.SetDestination(sampledDestination);
        }

        /// <summary>No progress for 1.5 s while moving: walk to a random nearby NavMesh point for a moment.</summary>
        bool CheckStuck()
        {
            if (!hasRequest || agent.pathPending)
            {
                ResetProgress();
                return false;
            }
            if ((transform.position - progressPosition).sqrMagnitude > 0.09f)
            {
                ResetProgress();
                return false;
            }
            if (Time.time - progressTime < StuckTime) return false;

            stuckCount++;
            ResetProgress();
            detourUntil = Time.time + 1.2f;
            agent.SetDestination(RandomNavPoint(transform.position, 3f));
            resendAfterDetour = true; // after the detour the next MoveTo re-sends the real target
            return true;
        }

        void ResetProgress()
        {
            progressPosition = transform.position;
            progressTime = Time.time;
        }

        /// <summary>Within tolerance of the point, or the agent got as close as the NavMesh allows (or gave up).</summary>
        protected bool Arrived(Vector3 point, float tolerance = 0.6f)
        {
            if (FlatDistanceTo(point) <= tolerance) return true;
            if (!AgentReady) return true; // cannot move: treat as there instead of getting stuck
            if (stuckCount >= 3) return true; // three detours did not help
            if (!hasRequest || FlatDistance(requestedPoint, point) > 0.6f) return false;
            if (FlatDistanceTo(sampledDestination) <= agent.stoppingDistance + 0.4f) return true; // nearest NavMesh point
            if (agent.pathPending) return false;
            if (agent.pathStatus == NavMeshPathStatus.PathInvalid) return true;
            if (Time.time < requestTime + 0.5f) return false;
            return agent.hasPath && agent.remainingDistance <= agent.stoppingDistance + 0.2f; // end of a partial path
        }

        protected void Halt()
        {
            hasRequest = false;
            resendAfterDetour = false;
            stuckCount = 0;
            detourUntil = 0f;
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

        protected static Vector3 SampleNavMesh(Vector3 point, float maxDistance) =>
            NavMesh.SamplePosition(point, out var hit, maxDistance, NavMesh.AllAreas) ? hit.position : point;

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

        /// <summary>
        /// A quiet sound is heard only when its walking path is short enough (no hearing through thick walls or from
        /// another floor). Without a NavMesh the straight distance counts.
        /// </summary>
        protected bool HeardAlongPath(Vector3 position, float radius)
        {
            if (!AgentReady) return true;
            path ??= new NavMeshPath();
            if (!NavMesh.SamplePosition(position, out var hit, 2f, NavMesh.AllAreas)) return false;
            if (!NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path)) return false;
            if (path.status != NavMeshPathStatus.PathComplete) return false;
            var corners = path.corners;
            float length = 0f;
            for (int i = 1; i < corners.Length; i++) length += Vector3.Distance(corners[i - 1], corners[i]);
            return length <= radius * 1.4f;
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
            return SampleNavMesh(points[roamIndex], 3f);
        }

        // ---------- Sight ----------

        /// <summary>No solid collider between the points except this NPC, the player and other people.</summary>
        protected bool IsClear(Vector3 from, Vector3 to) => FirstBlocker(from, to, true) == null;

        /// <summary>Nearest solid collider between the points, ignoring this NPC and the player (and people if asked).</summary>
        protected Collider FirstBlocker(Vector3 from, Vector3 to, bool ignoreCharacters)
        {
            Vector3 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.01f) return null;
            int count = Physics.RaycastNonAlloc(from, delta / length, hits, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Collider best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var col = hits[i].collider;
                if (col.transform.IsChildOf(transform) || PlayerTarget.IsPlayerCollider(col.transform)) continue;
                if (ignoreCharacters && col.GetComponentInParent<Character>() != null) continue;
                if (hits[i].distance < bestDistance)
                {
                    bestDistance = hits[i].distance;
                    best = col;
                }
            }
            return best;
        }

        /// <summary>
        /// Every 0.5 s: the first body (not seen before by this NPC) inside the cone, range and line of sight.
        /// Range is cut to darkRange for bodies outside lamp light at night.
        /// </summary>
        protected Body SpotBody(float range, float darkRange, float fullAngle)
        {
            if (Time.time < nextBodyCheck) return null;
            nextBodyCheck = Time.time + 0.5f;
            var bodies = Body.All;
            if (bodies.Count == 0) return null;
            Vector3 eye = Eye;
            for (int i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];
                if (body == null || body.transform == transform || seenBodies.Contains(body)) continue;
                Vector3 target = body.Position;
                float r = LightZones.IsLit(target) ? range : darkRange;
                Vector3 to = target - eye;
                if (to.sqrMagnitude > r * r) continue;
                Vector3 flat = new(to.x, 0f, to.z);
                if (flat.sqrMagnitude > 0.01f && Vector3.Angle(transform.forward, flat) > fullAngle * 0.5f) continue;
                if (!IsClear(eye, target)) continue;
                seenBodies.Add(body);
                return body;
            }
            return null;
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

        /// <summary>Bark the preferred set when the dialogue file has it, otherwise the fallback.</summary>
        protected void Bark(string preferred, string fallback) => Bark(Dialog.Exists(preferred) ? preferred : fallback);
    }
}
