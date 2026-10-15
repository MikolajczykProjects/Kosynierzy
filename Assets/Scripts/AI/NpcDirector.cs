using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// Spawns the town's NPCs by contract names at Start (Spawner gives placeholders while art is missing) and is the
    /// mission's handle on them: AlarmAll (act 4), SetGroupActive by point prefix (e.g. "radio_" before act 3),
    /// FreeWorkers (act 3), Spawned(prefab). Missing points are skipped with one summary log. One per scene
    /// (menu Kosynierzy > Add NPC Director). Also creates the default ForbiddenZones.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public class NpcDirector : MonoBehaviour
    {
        struct Record
        {
            public string Point;
            public GameObject Go;
        }

        static readonly string[] CivilianPrefabs = { "CH_Cywil_M1", "CH_Cywil_M2", "CH_Cywil_M3", "CH_Cywil_K1", "CH_Cywil_K2", "CH_Cywil_K3" };
        static readonly GameObject[] None = new GameObject[0];

        [SerializeField] bool spawnOnStart = true;
        [Tooltip("Create sphere ForbiddenZones (hotel room 6 m, radio grounds 25 m) when the scene has no hand-made zones.")]
        [SerializeField] bool createDefaultZones = true;
        [Tooltip("Create HidingSpots (placeholder bushes where no art stands) at miasto_zagajnik_krzaki and miasto_oslona_* not covered yet.")]
        [SerializeField] bool createDefaultHidingSpots = true;
        [Tooltip("A city SD walks to the next miasto_patrol_sd point when it is closer than this; otherwise it wanders around its own point.")]
        [SerializeField] float maxPatrolLeg = 40f;
        [SerializeField] float cityWanderRadius = 6f;
        [Tooltip("Radio guards (radio_sd_*) wander this little around their posts.")]
        [SerializeField] float radioGuardWander = 3f;
        [Tooltip("Parent spawned NPCs under this object (tidy hierarchy).")]
        [SerializeField] bool parentToDirector = true;

        readonly List<Record> records = new();
        readonly Dictionary<string, List<GameObject>> byPrefab = new();
        bool spawned;

        public static NpcDirector Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning($"Kosynierzy: więcej niż jeden NpcDirector w scenie — używam '{name}'.", this);
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (spawnOnStart) SpawnAll();
            if (createDefaultZones) ForbiddenZone.EnsureDefaults();
            if (createDefaultHidingSpots)
            {
                int made = HidingSpot.EnsureDefaults(transform);
                if (made > 0) Debug.Log($"Kosynierzy: NpcDirector — {made} kryjówek (krzaki / osłony) przy punktach mapy.");
            }
        }

        // ---------- Mission API ----------

        /// <summary>Every active SD goes into Alarm heading for this position (act 4).</summary>
        public void AlarmAll(Vector3 lastKnownPlayerPos)
        {
            EnsureSpawned();
            var soldiers = new List<EnemyController>(EnemyController.All);
            foreach (var e in soldiers) e.RaiseAlarm(lastKnownPlayerPos, false);
        }

        /// <summary>Enables/disables the NPCs spawned at points whose name starts with the prefix (e.g. "radio_").</summary>
        public void SetGroupActive(string pointPrefix, bool active)
        {
            EnsureSpawned();
            int count = 0;
            foreach (var r in records)
            {
                if (r.Go == null || !r.Point.StartsWith(pointPrefix, System.StringComparison.Ordinal)) continue;
                r.Go.SetActive(active);
                count++;
            }
            if (count == 0) Debug.Log($"Kosynierzy: NpcDirector.SetGroupActive(\"{pointPrefix}\") — brak NPC na punktach z tym prefiksem.");
        }

        /// <summary>Station workers lower their hands and walk to the nearest exit (act 3).</summary>
        public void FreeWorkers()
        {
            EnsureSpawned();
            foreach (var w in FindObjectsByType<WorkerController>(FindObjectsInactive.Include)) w.SetFreed();
        }

        /// <summary>Live NPCs spawned under this prefab name (empty when none).</summary>
        public IReadOnlyList<GameObject> Spawned(string prefabName)
        {
            EnsureSpawned();
            if (!byPrefab.TryGetValue(prefabName, out var list)) return None;
            list.RemoveAll(g => g == null);
            return list;
        }

        // ---------- Spawning ----------

        void EnsureSpawned()
        {
            if (!spawned && spawnOnStart) SpawnAll();
        }

        /// <summary>Spawns everyone once (called at Start, or earlier by the first API call).</summary>
        public void SpawnAll()
        {
            if (spawned) return;
            spawned = true;
            var missing = new List<string>();
            int soldiers = 0, police = 0, civilians = 0, workers = 0;

            var city = Points.Series("miasto_patrol_sd");
            for (int i = 0; i < city.Count; i++)
            {
                var enemy = SpawnAt("CH_SD", city[i], CharacterType.Enemy).GetComponent<EnemyController>();
                soldiers++;
                if (enemy == null) continue;
                var next = city[(i + 1) % city.Count];
                if (city.Count > 1 && Flat(city[i].position, next.position) <= maxPatrolLeg)
                    enemy.SetRoute(new List<Vector3> { next.position, city[i].position });
                else enemy.SetWander(cityWanderRadius);
            }
            if (city.Count == 0) missing.Add("miasto_patrol_sd_*");

            var hotel = Points.Series("hotel_patrol");
            for (int i = 0; i < hotel.Count; i++)
            {
                var enemy = SpawnAt("CH_SD", hotel[i], CharacterType.Enemy).GetComponent<EnemyController>();
                soldiers++;
                if (enemy == null) continue;
                if (hotel.Count > 1)
                {
                    // Start toward the other point(s), end at its own: the two guards walk opposite ways.
                    var route = new List<Vector3>();
                    for (int k = 1; k <= hotel.Count; k++) route.Add(hotel[(i + k) % hotel.Count].position);
                    enemy.SetRoute(route);
                }
                else enemy.SetWander(2f);
            }
            if (hotel.Count == 0) missing.Add("hotel_patrol_*");

            var radio = Points.Series("radio_sd");
            foreach (var p in radio)
            {
                var enemy = SpawnAt("CH_SD", p, CharacterType.Enemy).GetComponent<EnemyController>();
                soldiers++;
                if (enemy != null) enemy.SetWander(radioGuardWander);
            }
            if (radio.Count == 0) missing.Add("radio_sd_*");

            var posts = Points.Series("miasto_schupo");
            foreach (var p in posts)
            {
                SpawnAt("CH_Schupo", p, CharacterType.Police);
                police++;
            }
            if (posts.Count == 0) missing.Add("miasto_schupo_*");

            var crowd = Points.Series("miasto_tlum");
            var crowdPositions = new List<Vector3>();
            foreach (var p in crowd) crowdPositions.Add(p.position);
            for (int i = 0; i < crowd.Count; i++)
            {
                var civilian = SpawnAt(CivilianPrefabs[i % CivilianPrefabs.Length], crowd[i], CharacterType.Civilian)
                    .GetComponent<CivilianController>();
                civilians++;
                if (civilian != null) civilian.SetWanderPoints(crowdPositions);
            }
            if (crowd.Count == 0) missing.Add("miasto_tlum_*");

            var staff = Points.Series("radio_piwnica_pracownik");
            foreach (var p in staff)
            {
                SpawnAt("CH_Pracownik", p, CharacterType.Worker);
                workers++;
            }
            if (staff.Count == 0) missing.Add("radio_piwnica_pracownik_*");

            string gaps = missing.Count > 0 ? $" Brak punktów: {string.Join(", ", missing)}." : "";
            Debug.Log($"Kosynierzy: NpcDirector — SD {soldiers}, Schupo {police}, cywile {civilians}, pracownicy {workers}.{gaps}");
        }

        GameObject SpawnAt(string prefabName, Transform point, CharacterType type)
        {
            var go = NpcFactory.Setup(Spawner.Spawn(prefabName, point.name), type);
            if (parentToDirector) go.transform.SetParent(transform, true);
            records.Add(new Record { Point = point.name, Go = go });
            if (!byPrefab.TryGetValue(prefabName, out var list)) byPrefab[prefabName] = list = new List<GameObject>();
            list.Add(go);
            return go;
        }

        static float Flat(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
