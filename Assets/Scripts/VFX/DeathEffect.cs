using System.Collections;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// On death the visible parts are moved to a separate "corpse" that topples away from the last hit,
    /// kicks up a dust burst and sinks out after a while — independent of when Health destroys the enemy.
    /// Works with any child models from the art team.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class DeathEffect : MonoBehaviour
    {
        [SerializeField] float toppleTime = 0.45f;
        [SerializeField] float corpseLifetime = 8f;
        [SerializeField] float sinkTime = 1.5f;

        Health health;
        Vector3 lastHitDir = Vector3.forward;

        void Awake() => health = GetComponent<Health>();

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        // Health passes the normal facing the shooter; the body falls the other way.
        void OnDamaged(float amount, Vector3 point, Vector3 normal)
        {
            Vector3 d = Vector3.ProjectOnPlane(-normal, Vector3.up);
            if (d.sqrMagnitude > 0.01f) lastHitDir = d.normalized;
        }

        void OnDied(Health _)
        {
            var corpse = new GameObject($"{name}_Corpse");
            corpse.transform.SetPositionAndRotation(transform.position, transform.rotation);
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.GetComponentInChildren<Renderer>() == null) continue;
                foreach (var col in child.GetComponentsInChildren<Collider>()) Destroy(col);
                child.SetParent(corpse.transform, true);
            }
            VfxLibrary.PlayDeath(transform.position + Vector3.up * 0.05f);
            corpse.AddComponent<CorpseMotion>().Begin(lastHitDir, toppleTime, corpseLifetime, sinkTime);
        }
    }

    /// <summary>Runs the topple and sink on the detached corpse (the enemy object is already gone).</summary>
    public class CorpseMotion : MonoBehaviour
    {
        public void Begin(Vector3 fallDir, float toppleTime, float lifetime, float sinkTime) =>
            StartCoroutine(Run(fallDir, toppleTime, lifetime, sinkTime));

        IEnumerator Run(Vector3 fallDir, float toppleTime, float lifetime, float sinkTime)
        {
            Vector3 axis = Vector3.Cross(Vector3.up, fallDir).normalized;
            if (axis.sqrMagnitude < 0.01f) axis = transform.right;
            Quaternion start = transform.rotation;
            Quaternion end = Quaternion.AngleAxis(88f, axis) * start;
            Vector3 startPos = transform.position;

            for (float t = 0f; t < toppleTime; t += Time.deltaTime)
            {
                float k = t / toppleTime;
                k = k * k; // accelerate like a falling body
                transform.rotation = Quaternion.Slerp(start, end, k);
                yield return null;
            }
            transform.rotation = end;
            CameraShake.AddTrauma(0.05f);

            yield return new WaitForSeconds(lifetime);
            for (float t = 0f; t < sinkTime; t += Time.deltaTime)
            {
                transform.position = startPos + Vector3.down * (t / sinkTime * 0.6f);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
