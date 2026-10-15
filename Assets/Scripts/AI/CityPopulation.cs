using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Kosynierzy
{
    /// <summary>
    /// Extra street life on top of the contract NPCs: more civilians, SD and Schupo spread over the city, all
    /// culled by distance from the player (inactive beyond CullOut, woken inside CullIn) so FPS stays fine.
    /// Added by NpcDirector.SpawnAll. Contract / scripted NPCs are never culled here.
    /// </summary>
    public class CityPopulation : MonoBehaviour
    {
        const float CullOut = 130f, CullIn = 105f;
        const int ExtraCivilians = 48, ExtraSoldiers = 8, ExtraPolice = 4;
        const int MaxActive = 40; // hard cap of simultaneously active extras

        readonly List<GameObject> extras = new();
        float nextCull;

        public void Populate(IList<Transform> crowdPoints, IList<Transform> patrolPoints, string[] civilianPrefabs)
        {
            if (crowdPoints.Count == 0) return;
            var crowdPositions = new List<Vector3>();
            foreach (var p in crowdPoints) crowdPositions.Add(p.position);

            int made = 0;
            for (int i = 0; i < ExtraCivilians; i++)
            {
                var origin = crowdPoints[i % crowdPoints.Count].position;
                if (!Sample(origin, 9f, out var pos)) continue;
                var go = Spawn(civilianPrefabs[i % civilianPrefabs.Length], pos, CharacterType.Civilian);
                if (go == null) continue;
                var civ = go.GetComponent<CivilianController>();
                if (civ != null) civ.SetWanderPoints(crowdPositions);
                made++;
                // Groups of two walking the same loop: a partner next to every 3rd civilian.
                if (i % 3 == 0 && Sample(pos, 2f, out var buddyPos))
                {
                    var buddy = Spawn(civilianPrefabs[(i + 1) % civilianPrefabs.Length], buddyPos, CharacterType.Civilian);
                    if (buddy != null)
                    {
                        var bc = buddy.GetComponent<CivilianController>();
                        if (bc != null) bc.SetWanderPoints(crowdPositions);
                    }
                }
            }

            int soldiers = 0, police = 0;
            if (patrolPoints.Count > 1)
            {
                for (int i = 0; i < ExtraSoldiers; i++)
                {
                    var a = patrolPoints[i % patrolPoints.Count].position;
                    var b = patrolPoints[(i * 2 + 1) % patrolPoints.Count].position;
                    if (!Sample(a, 4f, out var pos)) continue;
                    var go = Spawn("CH_SD", pos, CharacterType.Enemy);
                    if (go == null) continue;
                    var enemy = go.GetComponent<EnemyController>();
                    if (enemy != null)
                    {
                        if ((a - b).magnitude <= 60f) enemy.SetRoute(new List<Vector3> { b, a });
                        else enemy.SetWander(8f);
                    }
                    soldiers++;
                }
                for (int i = 0; i < ExtraPolice; i++)
                {
                    var a = patrolPoints[(i * 3 + 2) % patrolPoints.Count].position;
                    if (!Sample(a, 5f, out var pos)) continue;
                    var go = Spawn("CH_Schupo", pos, CharacterType.Police);
                    if (go == null) continue;
                    police++;
                }
            }
            Debug.Log($"Kosynierzy: CityPopulation — +{made} cywili (z parami), +{soldiers} SD, +{police} Schupo; odcinanie {CullIn:0}/{CullOut:0} m, limit aktywnych {MaxActive}.");
        }

        GameObject Spawn(string prefab, Vector3 pos, CharacterType type)
        {
            var raw = Spawner.Spawn(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            if (raw == null) return null;
            var go = NpcFactory.Setup(raw, type);
            go.transform.SetParent(transform, true);
            extras.Add(go);
            return go;
        }

        static bool Sample(Vector3 origin, float radius, out Vector3 result)
        {
            for (int tries = 0; tries < 6; tries++)
            {
                var p = origin + new Vector3(Random.Range(-radius, radius), 0f, Random.Range(-radius, radius));
                if (NavMesh.SamplePosition(p, out var hit, 3f, NavMesh.AllAreas)) { result = hit.position; return true; }
            }
            result = origin;
            return false;
        }

        void Update()
        {
            if (Time.unscaledTime < nextCull) return;
            nextCull = Time.unscaledTime + 0.5f;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 pp = cam.transform.position;

            int active = 0;
            for (int i = 0; i < extras.Count; i++)
            {
                var go = extras[i];
                if (go == null) continue;
                var d = go.transform.position - pp; d.y = 0f;
                float dist = d.magnitude;
                bool on = go.activeSelf;
                if (on && dist > CullOut) { go.SetActive(false); on = false; }
                else if (!on && dist < CullIn && active < MaxActive) { go.SetActive(true); on = true; }
                if (on) active++;
            }
        }
    }
}
