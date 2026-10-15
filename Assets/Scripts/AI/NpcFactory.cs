using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace Kosynierzy
{
    /// <summary>
    /// Turns a spawned character (art prefab CH_* or Spawner's grey placeholder capsule) into a working NPC, adding
    /// only what is missing: NavMeshAgent snapped to the NavMesh (disabled when there is none nearby), body
    /// CapsuleCollider, one-hit Health that leaves the body, head Hitbox for soldiers, DeathEffect when the model has
    /// no "Trafiony" animation, Character, the AI for its type and the "?"/"!" indicator. Safe to call twice; also
    /// works in edit mode (test-setup menu). Placeholders get a feet pivot, a facing visor and a colour per type.
    /// </summary>
    public static class NpcFactory
    {
        public const float AgentRadius = 0.35f;
        public const float AgentHeight = 1.8f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly HashSet<string> warnedFields = new();
        static bool warnedNoNavMesh;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            warnedNoNavMesh = false;
            warnedFields.Clear();
        }

        /// <summary>Placeholder colour per type (also used for the test-setup materials).</summary>
        public static Color TintFor(CharacterType type) => type switch
        {
            CharacterType.Enemy => new Color(0.37f, 0.40f, 0.31f),    // SD grey-green
            CharacterType.Police => new Color(0.22f, 0.34f, 0.30f),   // Schupo dark green
            CharacterType.Civilian => new Color(0.55f, 0.43f, 0.32f), // brown coat
            CharacterType.Worker => new Color(0.30f, 0.42f, 0.56f),   // blue overalls
            _ => new Color(0.62f, 0.56f, 0.40f),                      // ally
        };

        public static GameObject Setup(GameObject go, CharacterType type)
        {
            if (go == null) return null;
            bool placeholder = NormalizePlaceholder(go);
            bool soldier = type == CharacterType.Enemy;

            SetupAgent(go);
            EnsureBodyCollider(go, placeholder, soldier);
            ConfigureOneHit(GetOrAdd<Health>(go));
            if (soldier) EnsureHeadHitbox(go);

            var animator = go.GetComponentInChildren<Animator>();
            if (animator != null) animator.applyRootMotion = false; // the NavMeshAgent moves the body
            var death = go.GetComponent<DeathEffect>();
            if (HasParameter(animator, "Trafiony"))
            {
                if (death != null) death.enabled = false;
            }
            else
            {
                if (death == null) death = go.AddComponent<DeathEffect>();
                SetField(death, "corpseLifetime", 1e6f); // the body stays on the ground
            }

            var rb = go.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            GetOrAdd<Character>(go).Type = type;
            switch (type)
            {
                case CharacterType.Enemy: GetOrAdd<EnemyController>(go); break;
                case CharacterType.Police: GetOrAdd<PoliceController>(go); break;
                case CharacterType.Civilian: GetOrAdd<CivilianController>(go); break;
                case CharacterType.Worker: GetOrAdd<WorkerController>(go); break;
            }
            if (type == CharacterType.Enemy || type == CharacterType.Police) GetOrAdd<SuspicionIndicator>(go);
            if (placeholder) Tint(go, TintFor(type));
            return go;
        }

        /// <summary>One hit = down: max health 1, the object is never destroyed (the body stays).</summary>
        public static void ConfigureOneHit(Health health)
        {
            if (health == null) return;
            SetField(health, "maxHealth", 1f);
            SetField(health, "destroyOnDeath", false);
            if (!health.IsDead && !Mathf.Approximately(health.Current, 1f)) health.Revive();
        }

        public static bool HasParameter(Animator animator, string parameter)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return false;
            foreach (var p in animator.parameters)
                if (p.name == parameter) return true;
            return false;
        }

        // ---------- Pieces ----------

        /// <summary>
        /// Spawner's placeholder is a scaled capsule primitive with its pivot at mid-height. Move the mesh to a "Body"
        /// child so the root has a feet pivot and unit scale (NavMeshAgent, colliders and DeathEffect need that).
        /// </summary>
        static bool NormalizePlaceholder(GameObject go)
        {
            if (go.transform.childCount > 0) return false;
            var filter = go.GetComponent<MeshFilter>();
            var renderer = go.GetComponent<MeshRenderer>();
            if (filter == null || renderer == null) return false;

            Vector3 scale = go.transform.localScale;
            float halfHeight = scale.y; // the primitive capsule is 2 units tall
            go.transform.localScale = Vector3.one;
            go.transform.position -= Vector3.up * halfHeight;

            var body = new GameObject("Body");
            body.layer = go.layer;
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0f, halfHeight, 0f);
            body.transform.localScale = scale;
            body.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            var bodyRenderer = body.AddComponent<MeshRenderer>();
            bodyRenderer.sharedMaterials = renderer.sharedMaterials;

            // A light visor on the face shows which way the placeholder looks.
            var visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Visor";
            visor.layer = go.layer;
            Kill(visor.GetComponent<Collider>());
            visor.transform.SetParent(go.transform, false);
            visor.transform.localPosition = new Vector3(0f, 1.55f, 0.25f);
            visor.transform.localScale = new Vector3(0.4f, 0.1f, 0.16f);

            renderer.enabled = false;
            Kill(renderer);
            Kill(filter);
            return true;
        }

        static void SetupAgent(GameObject go)
        {
            bool onMesh = NavMesh.SamplePosition(go.transform.position, out var hit, 3f, NavMesh.AllAreas);
            if (onMesh) go.transform.position = hit.position;
            else if (!warnedNoNavMesh)
            {
                warnedNoNavMesh = true;
                Debug.LogWarning($"Kosynierzy: brak NavMesh przy '{go.name}' ({go.transform.position}) — NPC bez NavMesh stoją w miejscu. Zbakuj NavMeshSurface na mapie.");
            }

            var agent = go.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                if (onMesh || !Application.isPlaying) agent = go.AddComponent<NavMeshAgent>();
                else
                {
                    // An enabled agent off the NavMesh logs an error when created; add it while the object is inactive.
                    bool wasActive = go.activeSelf;
                    go.SetActive(false);
                    agent = go.AddComponent<NavMeshAgent>();
                    agent.enabled = false;
                    go.SetActive(wasActive);
                }
            }

            agent.radius = AgentRadius;
            agent.height = AgentHeight;
            agent.baseOffset = 0f;
            agent.angularSpeed = 540f;
            agent.acceleration = 14f;
            agent.stoppingDistance = 0.3f;
            agent.autoBraking = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.avoidancePriority = Random.Range(40, 60);
            agent.enabled = onMesh;
            if (onMesh && Application.isPlaying && agent.isOnNavMesh) agent.Warp(hit.position);
        }

        static void EnsureBodyCollider(GameObject go, bool placeholder, bool soldier)
        {
            var capsule = go.GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                foreach (var c in go.GetComponentsInChildren<Collider>(true))
                    if (!c.isTrigger && c.GetComponent<Hitbox>() == null) return; // the art prefab brought its own
                capsule = go.AddComponent<CapsuleCollider>();
            }
            else if (!placeholder) return;

            capsule.isTrigger = false;
            capsule.direction = 1;
            capsule.radius = AgentRadius;
            // Soldiers: the body stops below the head so head shots reach the head hitbox.
            capsule.height = soldier ? 1.45f : AgentHeight;
            capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);
        }

        static void EnsureHeadHitbox(GameObject go)
        {
            if (go.GetComponentInChildren<Hitbox>(true) != null) return;
            var head = new GameObject("HeadHitbox");
            head.layer = go.layer;
            head.transform.SetParent(go.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            head.AddComponent<SphereCollider>().radius = 0.17f;
            head.AddComponent<Hitbox>();
        }

        static void Tint(GameObject go, Color color)
        {
            var body = go.transform.Find("Body");
            if (body == null || !body.TryGetComponent<Renderer>(out var r)) return;
            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            r.SetPropertyBlock(block);
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }

        /// <summary>Sets a private serialized field of a component we may not edit (Health, DeathEffect).</summary>
        static void SetField(object target, string field, object value)
        {
            var info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (info != null && info.FieldType.IsInstanceOfType(value))
            {
                info.SetValue(target, value);
                return;
            }
            if (warnedFields.Add(field))
                Debug.LogWarning($"Kosynierzy: NpcFactory nie znalazł pola '{field}' w {target.GetType().Name} — sprawdź nazwę.");
        }
    }
}
