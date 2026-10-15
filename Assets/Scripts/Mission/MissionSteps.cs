using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kosynierzy
{
    /// <summary>
    /// One step of the main mission, built in code from contract names (points, prefabs, dialogue ids)
    /// so the story runs before the map/art exist. Base class: title for the HUD, optional marker point,
    /// Begin / Tick / End, and Done.
    /// </summary>
    public abstract class MissionStep
    {
        public string Title;
        /// <summary>Point name the HUD marker points at (null = no marker).</summary>
        public string MarkerPoint;
        public bool Done { get; protected set; }
        /// <summary>Progress text for the HUD, e.g. "2 / 3" or "0:45".</summary>
        public virtual string Progress => null;
        public virtual Vector3? MarkerPosition => MarkerPoint != null && Points.Has(MarkerPoint) ? Points.Position(MarkerPoint) : null;

        public virtual void Begin() { }
        public virtual void Tick() { }
        public virtual void End() { }
        /// <summary>Dev key F1 is about to complete this step without the player doing it: grant what it would have given.</summary>
        public virtual void Skip() { }

        protected static FirstPersonController Player => UnityEngine.Object.FindAnyObjectByType<FirstPersonController>();

        /// <summary>
        /// Position/rotation of a map point; a missing point falls back (once logged) to 3 m in front of the player,
        /// so a step is never blocked by a wrong name.
        /// </summary>
        protected static void Resolve(string point, out Vector3 pos, out Quaternion rot)
        {
            if (Points.Has(point)) { pos = Points.Position(point); rot = Points.Rotation(point); return; }
            var p = Player;
            if (p != null)
            {
                pos = p.transform.position + Vector3.ProjectOnPlane(p.transform.forward, Vector3.up).normalized * 3f;
                rot = Quaternion.LookRotation(-Vector3.ProjectOnPlane(p.transform.forward, Vector3.up).normalized, Vector3.up);
            }
            else { pos = Vector3.zero; rot = Quaternion.identity; }
            Points.Get(point); // logs the warning once
        }

        /// <summary>Horizontal distance (+ at most 8 m of height difference) — points sit at door/floor height, interiors have storeys.</summary>
        protected static bool Near(Vector3 a, Vector3 b, float radius)
        {
            var d = a - b;
            return Mathf.Abs(d.y) < 8f && new Vector2(d.x, d.z).sqrMagnitude <= radius * radius;
        }

        /// <summary>
        /// Story characters (Zofia, the receptionist, the railwayman, "Sowa") are bystanders too: a player bullet that hits
        /// them fails the mission like any civilian (Character + one-hit Health, type Ally). No-op when already a Character.
        /// </summary>
        public static void MakeInnocent(GameObject go)
        {
            if (go == null || go.GetComponent<Character>() != null) return;
            go.AddComponent<Character>().Type = CharacterType.Ally;
        }

        // Story characters without their own model borrow a look (Spawner has the same idea for CH_Sowa).
        static readonly Dictionary<string, string> LookAlike = new() { { "CH_Zofia", "CH_Cywil_K1" } };

        /// <summary>Spawns a story character as its real CH_* model (or a look-alike), idle, innocent, turning to the player in dialogues.</summary>
        public static GameObject SpawnStoryNpc(string character, Vector3 pos, Quaternion rot)
        {
            string prefab = !Spawner.HasPrefab(character) && LookAlike.TryGetValue(character, out var alt) && Spawner.HasPrefab(alt) ? alt : character;
            var go = Spawner.Spawn(prefab, pos, rot);
            go.name = character; // Dialog finds speakers by "CH_<speaker>"
            MakeInnocent(go);
            if (go.GetComponent<StoryNpcFacing>() == null) go.AddComponent<StoryNpcFacing>();
            return go;
        }

        // Explicit null check: GetComponent returns a fake-null object in the editor, so "??" would not add.
        protected static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        /// <summary>Spawns a prefab (or placeholder) at a point and makes it usable with E.</summary>
        protected static Interactable SpawnInteractable(string prefab, string point, string prompt, Action onUse, bool singleUse = true)
        {
            Resolve(point, out var pos, out var rot);
            var go = Spawner.Spawn(prefab, pos, rot);
            var it = GetOrAdd<Interactable>(go);
            if (go.GetComponentInChildren<Collider>() == null) go.AddComponent<BoxCollider>();
            it.SetPrompt(prompt);
            it.SetSingleUse(singleUse);
            it.Interacted += _ => onUse?.Invoke();
            return it;
        }
    }

    /// <summary>Plays a dialogue; done when it ends. Optional onBegin runs when the dialogue starts.</summary>
    public class DialogStep : MissionStep
    {
        readonly string id;
        readonly Action onBegin;
        public DialogStep(string id, string title = null, Action onBegin = null) { this.id = id; Title = title; this.onBegin = onBegin; }

        public override void Begin()
        {
            onBegin?.Invoke();
            Dialog.Play(id, () => Done = true);
        }
    }

    /// <summary>Calls an action once and finishes (time of day, spawn, give item…).</summary>
    public class ActionStep : MissionStep
    {
        readonly Action action;
        public ActionStep(Action action) { this.action = action; }
        public override void Begin() { action?.Invoke(); Done = true; }
    }

    /// <summary>Reach a point (within radius).</summary>
    public class ReachStep : MissionStep
    {
        readonly string point;
        readonly float radius;
        public ReachStep(string point, string title, float radius = 3f) { this.point = point; Title = title; MarkerPoint = point; this.radius = radius; }

        public override void Tick()
        {
            if (!Points.Has(point)) { Points.Get(point); Done = true; return; } // wrong name must never block the story
            var p = Player;
            if (p != null && Near(p.transform.position, Points.Position(point), radius)) Done = true;
        }

        public override string Progress
        {
            get
            {
                var p = Player;
                return p == null || !Points.Has(point) ? null : $"{Mathf.RoundToInt(Vector3.Distance(p.transform.position, Points.Position(point)))} m";
            }
        }
    }

    /// <summary>Talk to a character placed at a point (E — Porozmawiaj), dialogue, then done.</summary>
    public class TalkStep : MissionStep
    {
        readonly string character, point, dialog;
        readonly Action after;
        GameObject spawned;
        Interactable talk;
        bool afterRun;

        public TalkStep(string character, string point, string dialog, string title, Action after = null)
        {
            this.character = character; this.point = point; this.dialog = dialog; Title = title; MarkerPoint = point; this.after = after;
        }

        public override void Begin()
        {
            spawned = Spawner.Find(character);
            if (spawned == null) { Resolve(point, out var pos, out var rot); spawned = SpawnStoryNpc(character, pos, rot); }
            else if (spawned.GetComponent<StoryNpcFacing>() == null) spawned.AddComponent<StoryNpcFacing>();
            MakeInnocent(spawned);
            talk = GetOrAdd<Interactable>(spawned);
            talk.SetPrompt("Porozmawiaj");
            talk.SetSingleUse(true);
            talk.Interacted += _ => Dialog.Play(dialog, () => { RunAfter(); Done = true; });
        }

        // Once only: the dialogue end and the F1 skip must not both hand out the item (the camera from Zofia).
        void RunAfter()
        {
            if (afterRun) return;
            afterRun = true;
            after?.Invoke();
        }

        public override void Skip()
        {
            RunAfter();
            if (talk != null) talk.enabled = false;
        }
    }

    /// <summary>Use something at a point with E (door, cache, note…).</summary>
    public class InteractStep : MissionStep
    {
        readonly string prefab, point, prompt, dialogAfter;
        readonly Action after;

        public InteractStep(string prefab, string point, string prompt, string title, string dialogAfter = null, Action after = null)
        {
            this.prefab = prefab; this.point = point; this.prompt = prompt; Title = title; MarkerPoint = point;
            this.dialogAfter = dialogAfter; this.after = after;
        }

        /// <summary>
        /// The used object gets out of the way (a door that swings open, a cache emptied…). A real OB_Drzwi (Animator bool
        /// "Otwarte") is opened and KEPT open with its colliders off; a model that only has the trigger "Otworz" plays it;
        /// anything else (placeholder, no usable parameter) is hidden.
        /// </summary>
        public bool HideOnUse;
        Interactable spawned;

        public override void Begin() => spawned = SpawnInteractable(prefab, point, prompt, () =>
        {
            if (HideOnUse && spawned != null)
            {
                var anim = spawned.GetComponentInChildren<Animator>();
                bool opened = AnimParams.SetBool(anim, "Otwarte", true) || AnimParams.SetTrigger(anim, "Otworz");
                if (opened) foreach (var c in spawned.GetComponentsInChildren<Collider>()) c.enabled = false;
                else spawned.gameObject.SetActive(false);
            }
            after?.Invoke();
            if (dialogAfter != null) Dialog.Play(dialogAfter, () => Done = true);
            else Done = true;
        });
    }

    /// <summary>Reach ANY of several points — lets the player choose the approach (gate or fence hole).</summary>
    public class ReachAnyStep : MissionStep
    {
        readonly string[] points;
        readonly float radius;

        public ReachAnyStep(string title, float radius, params string[] points)
        {
            Title = title; this.radius = radius; this.points = points; MarkerPoint = points[0];
        }

        public override void Tick()
        {
            var p = Player;
            if (p == null) return;
            bool any = false;
            foreach (var name in points)
            {
                if (!Points.Has(name)) continue;
                any = true;
                if (Near(p.transform.position, Points.Position(name), radius)) { Done = true; return; }
            }
            if (!any) { Debug.LogWarning($"Kosynierzy: żaden z punktów kroku '{Title}' nie istnieje — pomijam."); Done = true; }
        }
    }

    /// <summary>
    /// Photograph evidence placed at points (Evidence components on props or map objects). With team D's prefabs the
    /// evidence is a real prop (target ids: "mapa" → OB_Mapa_Stol, "lista" → OB_Biurko + OB_Lista on it, "honiok" → OB_Kartka
    /// on the ground, "konserwa" → the SD car OB_Auto_SD parked at the point); the Evidence component sits on the prop's
    /// root so its own colliders never block the photo ray, and its focus point is the prop's "CelZdjecia" child.
    /// A missing prefab falls back to the grey plate.
    /// </summary>
    public class PhotoStep : MissionStep
    {
        public struct Target { public string Point, Id, Name, Thought; }
        readonly Target[] targets;
        readonly List<Evidence> spawned = new();
        int taken;
        GameObject parkedCar;
        float nextDoor;

        public PhotoStep(string title, params Target[] targets) { Title = title; this.targets = targets; MarkerPoint = targets[0].Point; }

        public override string Progress => $"{taken} / {targets.Length}";

        public override void Begin()
        {
            foreach (var t in targets)
            {
                var go = BuildProp(t);
                if (go == null) go = BuildPlate(t); // explicit null check: no ?? on Unity objects
                var ev = go.AddComponent<Evidence>();
                ev.Setup(t.Id, t.Name, t.Thought);
                ev.Photographed += OnPhoto;
                spawned.Add(ev);
            }
        }

        /// <summary>Grey plate at table / wall height — the placeholder path when the prop prefab is missing.</summary>
        static GameObject BuildPlate(Target t)
        {
            Resolve(t.Point, out var ppos, out var prot);
            var go = Spawner.CreatePlaceholder("Dowod_" + t.Id, ppos, prot);
            go.transform.localScale = new Vector3(0.6f, 0.05f, 0.45f);
            go.transform.position = ppos + Vector3.up * 0.9f; // sits on a table / wall height
            return go;
        }

        /// <summary>The real prop for this target (root = what carries Evidence), or null when a prefab is missing.</summary>
        GameObject BuildProp(Target t)
        {
            Resolve(t.Point, out Vector3 pos, out Quaternion rot);
            switch (t.Id)
            {
                case "mapa":
                    return Spawner.HasPrefab("OB_Mapa_Stol") ? Spawner.Spawn("OB_Mapa_Stol", pos, rot) : null;

                case "lista":
                {
                    if (!Spawner.HasPrefab("OB_Biurko") || !Spawner.HasPrefab("OB_Lista")) return null;
                    var desk = Spawner.Spawn("OB_Biurko", pos, rot);
                    // The sheet lies on the desktop: top of the desk's solid collider (≈ 0.77 m), at the desk's centre.
                    float top = pos.y + 0.77f;
                    var col = desk.GetComponentInChildren<Collider>();
                    if (col != null)
                    {
                        Physics.SyncTransforms();
                        top = col.bounds.max.y;
                    }
                    var sheet = Spawner.Spawn("OB_Lista", new Vector3(pos.x, top, pos.z), rot);
                    sheet.transform.SetParent(desk.transform, true);
                    return desk;
                }

                case "honiok":
                    return Spawner.HasPrefab("OB_Kartka") ? Spawner.Spawn("OB_Kartka", pos, rot) : null;

                case "konserwa":
                {
                    if (!Spawner.HasPrefab("OB_Auto_SD")) return null;
                    // The car that was tailed in act 2 ends its route at the grove: photograph that one (no second car
                    // standing inside it). Resumed from a checkpoint (no such car) → park a new one at the point.
                    var existing = Spawner.Find("OB_Auto_SD");
                    if (existing != null && Vector3.Distance(existing.transform.position, pos) < 8f) parkedCar = existing;
                    else parkedCar = Spawner.Spawn("OB_Auto_SD", pos, rot);
                    AnimParams.SetFloat(parkedCar, "Speed", 0f);
                    AnimParams.SetBool(parkedCar, "Swiatla", GameClock.IsNight);
                    AnimParams.SetTrigger(parkedCar, "OtworzDrzwi");
                    nextDoor = Time.time + 8f;
                    return parkedCar;
                }
            }
            return null;
        }

        public override void Tick()
        {
            // The driver's door of the parked SD car (trigger OtworzDrzwi, closes again by itself): swing it open again
            // now and then while the player is near, so the photo target keeps moving a little.
            if (parkedCar == null || Time.time < nextDoor) return;
            var p = Player;
            if (p != null && Vector3.Distance(p.transform.position, parkedCar.transform.position) < 30f)
            {
                AnimParams.SetTrigger(parkedCar, "OtworzDrzwi");
                nextDoor = Time.time + 10f;
            }
        }

        void OnPhoto(Evidence e)
        {
            taken++;
            if (taken >= targets.Length) Done = true;
            else MarkerPoint = Array.Find(targets, t => !spawned.Exists(s => s.Id == t.Id && s.IsPhotographed)).Point;
        }
    }

    /// <summary>Crouch inside a radius around a point for a while (eavesdropping), then dialogues.</summary>
    public class CrouchListenStep : MissionStep
    {
        readonly string point;
        readonly float radius, seconds;
        readonly string[] dialogs;
        float held;
        bool playing;

        public CrouchListenStep(string point, string title, float radius, float seconds, params string[] dialogs)
        {
            this.point = point; Title = title; MarkerPoint = point; this.radius = radius; this.seconds = seconds; this.dialogs = dialogs;
        }

        public override string Progress => held > 0f && !playing ? $"podsłuch {Mathf.RoundToInt(held / seconds * 100f)}%" : "kucnij (C) w pobliżu";

        public override void Tick()
        {
            if (playing) return;
            if (!Points.Has(point)) { Points.Get(point); PlayNext(0); return; } // no such point: skip the crouch, keep the dialogues
            var p = Player;
            bool ok = p != null && p.IsCrouching && Near(p.transform.position, Points.Position(point), radius);
            held = ok ? held + Time.deltaTime : Mathf.Max(0f, held - Time.deltaTime * 0.5f);
            if (held >= seconds) PlayNext(0);
        }

        void PlayNext(int i)
        {
            playing = true;
            if (i >= dialogs.Length) { Done = true; return; }
            Dialog.Play(dialogs[i], () => PlayNext(i + 1), false);
        }
    }

    /// <summary>
    /// Tail the SD car along route points: it drives on; closer than minDistance or farther than maxDistance = fail.
    /// </summary>
    public class FollowCarStep : MissionStep
    {
        readonly string prefab, routePrefix;
        readonly float speed, minDistance, maxDistance;
        List<Transform> route;
        GameObject car;
        Animator carAnim;
        bool lightsOn;
        int index;
        float graceUntil;

        /// <summary>Optional last stop after the route (the grove, where the next step photographs the same car).</summary>
        public string ParkPoint;

        public FollowCarStep(string prefab, string routePrefix, string title, float speed = 3.6f, float minDistance = 8f, float maxDistance = 40f)
        {
            this.prefab = prefab; this.routePrefix = routePrefix; Title = title; this.speed = speed;
            this.minDistance = minDistance; this.maxDistance = maxDistance;
        }

        public override Vector3? MarkerPosition => car != null ? car.transform.position : null;

        public override string Progress
        {
            get
            {
                if (car == null || Player == null) return null;
                float d = Vector3.Distance(car.transform.position, Player.transform.position);
                string warn = d > maxDistance * 0.8f ? " — za daleko!" : d < minDistance * 1.3f ? " — za blisko!" : "";
                return $"{Mathf.RoundToInt(d)} m{warn}";
            }
        }

        public override void Begin()
        {
            route = Points.Series(routePrefix);
            if (route.Count == 0) { Debug.LogWarning($"Kosynierzy: brak trasy '{routePrefix}_*' — pomijam śledzenie."); Done = true; return; }
            car = Spawner.Spawn(prefab, route[0].position, route[0].rotation);
            if (!Spawner.HasPrefab(prefab))
            {
                // Placeholder: the default 0.6 m cube cannot be tailed from 40 m. A car-sized block (still grey) can.
                car.transform.localScale = new Vector3(2f, 1.4f, 4.2f);
                car.transform.position += Vector3.up * 0.4f;
            }
            if (!string.IsNullOrEmpty(ParkPoint) && Points.Has(ParkPoint))
            {
                var park = Points.Get(ParkPoint);
                if (park != null && Vector3.Distance(park.position, route[route.Count - 1].position) > 1f) route.Add(park);
            }
            index = 1;
            graceUntil = Time.time + 5f;

            // OB_Auto_SD (AC_Auto_SD): float Speed spins the wheels, bool Swiatla = headlights. Writes are guarded.
            carAnim = AnimParams.Of(car);
            lightsOn = GameClock.IsNight;
            AnimParams.SetBool(carAnim, "Swiatla", lightsOn);
            AnimParams.SetFloat(carAnim, "Speed", speed);
        }

        public override void End() => AnimParams.SetFloat(carAnim, "Speed", 0f);

        const float LongLeg = 300f;
        List<Vector3> path = new();
        bool jumping;

        // Corners of a NavMesh path (streets) from a to b; straight line when there is no usable NavMesh.
        static List<Vector3> LegPath(Vector3 a, Vector3 b)
        {
            var list = new List<Vector3>();
            if (UnityEngine.AI.NavMesh.SamplePosition(a, out var ha, 15f, UnityEngine.AI.NavMesh.AllAreas) &&
                UnityEngine.AI.NavMesh.SamplePosition(b, out var hb, 15f, UnityEngine.AI.NavMesh.AllAreas))
            {
                var nav = new UnityEngine.AI.NavMeshPath();
                if (UnityEngine.AI.NavMesh.CalculatePath(ha.position, hb.position, UnityEngine.AI.NavMesh.AllAreas, nav) &&
                    nav.status == UnityEngine.AI.NavMeshPathStatus.PathComplete && nav.corners.Length > 1)
                {
                    for (int i = 1; i < nav.corners.Length; i++) list.Add(nav.corners[i]);
                    return list;
                }
            }
            list.Add(b);
            return list;
        }

        void JumpLeg(Vector3 to)
        {
            jumping = true;
            var p = Player;
            Vector3 dir = Vector3.ProjectOnPlane(to - car.transform.position, Vector3.up).normalized;
            Vector3 back = to - dir * 18f; // the player ends 18 m behind the car: inside the 8–40 m window
            ScreenFade.Transition(() =>
            {
                if (car != null) { car.transform.position = to; if (dir.sqrMagnitude > 0.01f) car.transform.rotation = Quaternion.LookRotation(dir); }
                if (p != null)
                {
                    if (UnityEngine.AI.NavMesh.SamplePosition(back, out var h, 20f, UnityEngine.AI.NavMesh.AllAreas)) back = h.position;
                    p.Teleport(back + Vector3.up * 0.05f, Quaternion.LookRotation(dir.sqrMagnitude > 0.01f ? dir : Vector3.forward).eulerAngles.y);
                }
                index++;
                path.Clear();
                graceUntil = Time.time + 5f;
                jumping = false;
            });
        }

        public override void Tick()
        {
            if (car == null || Done) return;
            if (index >= route.Count) { AnimParams.SetFloat(carAnim, "Speed", 0f); Done = true; return; }
            // Legs longer than LongLeg (the real map's route jumps ~900 m across the city) are a cut: fade, car and player
            // are moved together to the end of the leg. Otherwise the straight line runs through buildings and the tail fails.
            if (path.Count == 0 && !jumping)
            {
                Vector3 from = car.transform.position, to = route[index].position;
                if (Vector3.ProjectOnPlane(to - from, Vector3.up).magnitude > LongLeg) { JumpLeg(to); return; }
                path = LegPath(from, to);
            }
            if (jumping) return;
            Vector3 target = path.Count > 0 ? path[0] : route[index].position;
            Vector3 pos = car.transform.position;
            Vector3 flat = Vector3.ProjectOnPlane(target - pos, Vector3.up);
            if (flat.magnitude < 0.5f)
            {
                if (path.Count > 0) path.RemoveAt(0);
                if (path.Count == 0) index++;
                return;
            }
            car.transform.rotation = Quaternion.Slerp(car.transform.rotation, Quaternion.LookRotation(flat), 4f * Time.deltaTime);
            car.transform.position = Vector3.MoveTowards(pos, new Vector3(target.x, pos.y, target.z), speed * Time.deltaTime);

            // Wheels follow the speed the car really has this frame; headlights follow the clock (night from 19:30).
            AnimParams.SetFloat(carAnim, "Speed", Mathf.Min(speed, flat.magnitude / Mathf.Max(Time.deltaTime, 0.0001f)));
            if (GameClock.IsNight != lightsOn)
            {
                lightsOn = GameClock.IsNight;
                AnimParams.SetBool(carAnim, "Swiatla", lightsOn);
            }

            var p = Player;
            if (p == null || Time.time < graceUntil) return;
            float d = Vector3.Distance(p.transform.position, car.transform.position);
            if (d > maxDistance) MissionFail.Trigger("Zgubiłeś auto SD. Nikt nie wie, dokąd pojechali.");
            else if (d < minDistance) MissionFail.Trigger("Podszedłeś za blisko — SD cię rozpoznało.");
        }
    }

    /// <summary>
    /// Act 3: 90 s to cut the right one of three cables (hint on Gruber's note). Wrong cut: spark, −15 s.
    /// Real OB_Lacznica: its children Kabel_1 / Kabel_2 / Kabel_3 (czerwony / czarny / biały) are the interactables; a cut
    /// plays Animator trigger "Przetnij" on the cable and Play() on FX_Iskra_n, the right cut switches the board's bool
    /// "Zasilanie" off (and the storm microphone's "Nadaje"). Missing prefab: three coloured cylinders as before.
    /// </summary>
    public class CableStep : MissionStep
    {
        // Same order as the prefab's Kabel_1..3 (and the placeholder cylinders).
        static readonly string[] Colours = { "czerwony", "czarny", "biały" };
        readonly string panelPoint, notePoint, successDialog;
        readonly float duration;
        float endTime;
        float solvedRemaining = -1f;
        int correct;
        GameObject panel;
        bool realPanel;
        readonly List<GameObject> spawned = new();

        /// <summary>Seconds left on the countdown; frozen at the moment the right cable is cut (the HUD reads this).</summary>
        public float Remaining => solvedRemaining >= 0f ? solvedRemaining : Mathf.Max(0f, endTime - Time.time);
        public float Duration => duration;

        public CableStep(string panelPoint, string notePoint, string successDialog, string title, float duration = 90f)
        {
            this.panelPoint = panelPoint; this.notePoint = notePoint; this.successDialog = successDialog; Title = title;
            MarkerPoint = panelPoint; this.duration = duration;
        }

        public override string Progress => $"{Mathf.CeilToInt(Remaining)} s";

        public override void Begin()
        {
            endTime = Time.time + duration;
            correct = UnityEngine.Random.Range(0, 3);
            Mission.SetMicrophone(true); // the broadcast is running until the right line is cut (also after a checkpoint resume)

            Resolve(panelPoint, out var basePos, out var baseRot);
            PanelFrame(basePos, baseRot, out Vector3 panelPos, out Quaternion panelRot);
            realPanel = Spawner.HasPrefab("OB_Lacznica");
            panel = Spawner.Spawn("OB_Lacznica", panelPos, panelRot);
            spawned.Add(panel);

            Color[] tints = { new(0.8f, 0.15f, 0.12f), new(0.07f, 0.07f, 0.08f), new(0.92f, 0.92f, 0.9f) };
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                Transform real = realPanel ? panel.transform.Find($"Kabel_{i + 1}") : null;
                if (real != null)
                {
                    // Real cable: thin (Ø 3.6 cm, 0.53 m long) and without a collider in the prefab. A trigger box that
                    // sticks 13 cm out of the board's front, so the crosshair ray meets it before the board's own collider.
                    var cableGo = real.gameObject;
                    var box = cableGo.GetComponent<BoxCollider>();
                    if (box == null) box = cableGo.AddComponent<BoxCollider>();
                    box.isTrigger = true;
                    box.center = new Vector3(0f, 0f, 0.08f);
                    box.size = new Vector3(0.1f, 0.56f, 0.2f);
                    var realIt = GetOrAdd<Interactable>(cableGo);
                    realIt.SetPrompt($"Przetnij {Colours[i]} kabel");
                    realIt.SetSingleUse(true);
                    realIt.Interacted += _ => Cut(idx, cableGo, box);
                    continue;
                }

                var cable = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cable.name = $"Kabel_{Colours[i]}";
                cable.transform.position = panelPos + panelRot * new Vector3(-0.4f + 0.4f * i, 1.2f, 0.35f);
                cable.transform.localScale = new Vector3(0.08f, 0.35f, 0.08f);
                var r = cable.GetComponent<Renderer>();
                r.material.SetColor("_BaseColor", tints[i]);
                var it = cable.AddComponent<Interactable>();
                it.SetPrompt($"Przetnij {Colours[i]} kabel");
                it.SetSingleUse(true);
                it.Interacted += _ => Cut(idx, cable, null);
                spawned.Add(cable);
            }

            var note = SpawnInteractable("OB_Kartka", notePoint, "Przeczytaj kartkę Grubera", () =>
                HistoryCardView.ShowText("Kartka Grubera", $"Wzmacniacz mikrofonu burzowego — linia oznaczona kolorem: {Colours[correct].ToUpperInvariant()}."),
                singleUse: false);
            spawned.Add(note.gameObject);
        }

        /// <summary>
        /// The point (contract: "przy ścianie łącznicy, patrzy na ścianę") stands in front of the wall and faces it.
        /// OB_Lacznica has its pivot in the middle of its back at floor level and its front on +Z, so the back is snapped to
        /// the wall found 3 m ahead (1 m ahead when there is none) and the front turned toward the player.
        /// </summary>
        static void PanelFrame(Vector3 basePos, Quaternion pointRot, out Vector3 pos, out Quaternion rot)
        {
            Vector3 fwd = Vector3.ProjectOnPlane(pointRot * Vector3.forward, Vector3.up);
            fwd = fwd.sqrMagnitude > 0.0001f ? fwd.normalized : Vector3.forward;
            rot = Quaternion.LookRotation(-fwd, Vector3.up);
            pos = basePos + fwd;

            float best = float.MaxValue;
            foreach (var h in Physics.RaycastAll(basePos + Vector3.up, fwd, 3f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.collider.GetComponentInParent<Character>() != null || h.collider.GetComponentInParent<FirstPersonController>() != null) continue;
                if (h.distance >= best) continue;
                best = h.distance;
                pos = new Vector3(h.point.x, basePos.y, h.point.z) - fwd * 0.01f;
            }
        }

        void Cut(int idx, GameObject cable, Collider cableCollider)
        {
            if (realPanel && cableCollider != null)
            {
                cableCollider.enabled = false; // a cut cable is no longer a target
                AnimParams.SetTrigger(cable, "Przetnij");
                var spark = panel != null ? panel.transform.Find($"FX_Iskra_{idx + 1}") : null;
                var ps = spark != null ? spark.GetComponent<ParticleSystem>() : null;
                if (ps != null) ps.Play();
            }
            VfxLibrary.PlayImpact(cable.transform.position, Vector3.up, ImpactKind.Ground);
            CameraShake.AddTrauma(0.2f);
            if (idx == correct)
            {
                solvedRemaining = Remaining; // before endTime is parked: float.MaxValue would show as a garbage countdown
                PowerOff();
                Dialog.Play(successDialog, () => Done = true, false);
                endTime = float.MaxValue;
            }
            else
            {
                endTime -= 15f;
                if (!realPanel || cableCollider == null) cable.transform.localScale = new Vector3(0.08f, 0.15f, 0.08f);
            }
        }

        // Board lamp off (AC_Lacznica bool Zasilanie) and the storm microphone silent (AC_Mikrofon bool Nadaje).
        void PowerOff()
        {
            if (panel != null) AnimParams.SetBool(panel.GetComponent<Animator>(), "Zasilanie", false);
            Mission.SetMicrophone(false);
        }

        public override void End() => PowerOff(); // also on the F1 skip

        public override void Tick()
        {
            if (!Done && endTime < float.MaxValue && Time.time >= endTime)
                MissionFail.Trigger("Komunikat poszedł w eter w całości. Prowokacja się udała.");
        }
    }
}
