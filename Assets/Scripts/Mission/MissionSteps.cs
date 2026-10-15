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

        protected static FirstPersonController Player => UnityEngine.Object.FindAnyObjectByType<FirstPersonController>();

        // Explicit null check: GetComponent returns a fake-null object in the editor, so "??" would not add.
        protected static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        /// <summary>Spawns a prefab (or placeholder) at a point and makes it usable with E.</summary>
        protected static Interactable SpawnInteractable(string prefab, string point, string prompt, Action onUse, bool singleUse = true)
        {
            var go = Spawner.Spawn(prefab, point);
            var it = GetOrAdd<Interactable>(go);
            if (go.GetComponentInChildren<Collider>() == null) go.AddComponent<BoxCollider>();
            it.SetPrompt(prompt);
            it.SetSingleUse(singleUse);
            it.Interacted += _ => onUse?.Invoke();
            return it;
        }
    }

    /// <summary>Plays a dialogue; done when it ends.</summary>
    public class DialogStep : MissionStep
    {
        readonly string id;
        public DialogStep(string id, string title = null) { this.id = id; Title = title; }
        public override void Begin() => Dialog.Play(id, () => Done = true);
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
            var p = Player;
            if (p != null && Vector3.Distance(p.transform.position, Points.Position(point)) <= radius) Done = true;
        }

        public override string Progress
        {
            get
            {
                var p = Player;
                return p == null ? null : $"{Mathf.RoundToInt(Vector3.Distance(p.transform.position, Points.Position(point)))} m";
            }
        }
    }

    /// <summary>Talk to a character placed at a point (E — Porozmawiaj), dialogue, then done.</summary>
    public class TalkStep : MissionStep
    {
        readonly string character, point, dialog;
        readonly Action after;
        GameObject spawned;

        public TalkStep(string character, string point, string dialog, string title, Action after = null)
        {
            this.character = character; this.point = point; this.dialog = dialog; Title = title; MarkerPoint = point; this.after = after;
        }

        public override void Begin()
        {
            spawned = Spawner.Find(character);
            if (spawned == null) spawned = Spawner.Spawn(character, point);
            var it = GetOrAdd<Interactable>(spawned);
            it.SetPrompt("Porozmawiaj");
            it.SetSingleUse(true);
            it.Interacted += _ => Dialog.Play(dialog, () => { after?.Invoke(); Done = true; });
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

        public override void Begin() => SpawnInteractable(prefab, point, prompt, () =>
        {
            after?.Invoke();
            if (dialogAfter != null) Dialog.Play(dialogAfter, () => Done = true);
            else Done = true;
        });
    }

    /// <summary>Photograph evidence placed at points (Evidence components on placeholders or map objects).</summary>
    public class PhotoStep : MissionStep
    {
        public struct Target { public string Point, Id, Name, Thought; }
        readonly Target[] targets;
        readonly List<Evidence> spawned = new();
        int taken;

        public PhotoStep(string title, params Target[] targets) { Title = title; this.targets = targets; MarkerPoint = targets[0].Point; }

        public override string Progress => $"{taken} / {targets.Length}";

        public override void Begin()
        {
            foreach (var t in targets)
            {
                var go = Spawner.CreatePlaceholder("Dowod_" + t.Id, Points.Position(t.Point), Points.Rotation(t.Point));
                go.transform.localScale = new Vector3(0.6f, 0.05f, 0.45f);
                go.transform.position = Points.Position(t.Point) + Vector3.up * 0.9f; // sits on a table / wall height
                var ev = go.AddComponent<Evidence>();
                ev.Setup(t.Id, t.Name, t.Thought);
                ev.Photographed += OnPhoto;
                spawned.Add(ev);
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
            var p = Player;
            bool ok = p != null && p.IsCrouching && Vector3.Distance(p.transform.position, Points.Position(point)) <= radius;
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
        int index;
        float graceUntil;

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
            index = 1;
            graceUntil = Time.time + 5f;
        }

        public override void Tick()
        {
            if (car == null || Done) return;
            if (index >= route.Count) { Done = true; return; }
            Vector3 target = route[index].position;
            Vector3 pos = car.transform.position;
            Vector3 flat = Vector3.ProjectOnPlane(target - pos, Vector3.up);
            if (flat.magnitude < 0.5f) { index++; return; }
            car.transform.rotation = Quaternion.Slerp(car.transform.rotation, Quaternion.LookRotation(flat), 4f * Time.deltaTime);
            car.transform.position = Vector3.MoveTowards(pos, new Vector3(target.x, pos.y, target.z), speed * Time.deltaTime);

            var p = Player;
            if (p == null || Time.time < graceUntil) return;
            float d = Vector3.Distance(p.transform.position, car.transform.position);
            if (d > maxDistance) MissionFail.Trigger("Zgubiłeś auto SD. Nikt nie wie, dokąd pojechali.");
            else if (d < minDistance) MissionFail.Trigger("Podszedłeś za blisko — SD cię rozpoznało.");
        }
    }

    /// <summary>Act 3: 90 s to cut the right one of three cables (hint on Gruber's note). Wrong cut: spark, −15 s.</summary>
    public class CableStep : MissionStep
    {
        static readonly string[] Colours = { "czerwony", "zielony", "niebieski" };
        readonly string panelPoint, notePoint, successDialog;
        readonly float duration;
        float endTime;
        int correct;
        readonly List<GameObject> spawned = new();

        public float Remaining => Mathf.Max(0f, endTime - Time.time);
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
            Vector3 basePos = Points.Position(panelPoint);
            Quaternion rot = Points.Rotation(panelPoint);
            spawned.Add(Spawner.Spawn("OB_Lacznica", basePos, rot));

            Color[] tints = { new(0.8f, 0.15f, 0.12f), new(0.2f, 0.65f, 0.25f), new(0.2f, 0.35f, 0.85f) };
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var cable = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cable.name = $"Kabel_{Colours[i]}";
                cable.transform.position = basePos + rot * new Vector3(-0.4f + 0.4f * i, 1.2f, -0.35f);
                cable.transform.localScale = new Vector3(0.08f, 0.35f, 0.08f);
                var r = cable.GetComponent<Renderer>();
                r.material.SetColor("_BaseColor", tints[i]);
                var it = cable.AddComponent<Interactable>();
                it.SetPrompt($"Przetnij {Colours[i]} kabel");
                it.SetSingleUse(true);
                it.Interacted += _ => Cut(idx, cable);
                spawned.Add(cable);
            }

            var note = SpawnInteractable("OB_Kartka", notePoint, "Przeczytaj kartkę Grubera", () =>
                HistoryCardView.ShowText("Kartka Grubera", $"Wzmacniacz mikrofonu burzowego — linia oznaczona kolorem: {Colours[correct].ToUpperInvariant()}."),
                singleUse: false);
            spawned.Add(note.gameObject);
        }

        void Cut(int idx, GameObject cable)
        {
            VfxLibrary.PlayImpact(cable.transform.position, Vector3.up, ImpactKind.Ground);
            CameraShake.AddTrauma(0.2f);
            if (idx == correct)
            {
                Dialog.Play(successDialog, () => Done = true, false);
                endTime = float.MaxValue;
            }
            else
            {
                endTime -= 15f;
                cable.transform.localScale = new Vector3(0.08f, 0.15f, 0.08f);
            }
        }

        public override void Tick()
        {
            if (!Done && endTime < float.MaxValue && Time.time >= endTime)
                MissionFail.Trigger("Komunikat poszedł w eter w całości. Prowokacja się udała.");
        }
    }
}
