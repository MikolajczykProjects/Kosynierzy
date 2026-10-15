using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kosynierzy
{
    /// <summary>
    /// The main mission "Babcia umarła" (FABULA.md / ROLA-B KROK 6) as an ordered list of steps built in code
    /// from contract names. Acts start with a checkpoint, a spawn point and the clock hour. Also places the
    /// doors between the city and interiors, the history cards and the side content (cache, railwayman).
    /// Dev keys: F1 next step, F8 save checkpoint, F9 load checkpoint.
    /// </summary>
    public class Mission : MonoBehaviour
    {
        struct Act
        {
            public int Step;
            public string Spawn;
            public float Hour;
            public string Name;
        }

        public static Mission Instance { get; private set; }

        readonly List<MissionStep> steps = new();
        readonly List<Act> acts = new();
        InputAction skipAction, saveAction, loadAction;

        public MissionStep Current { get; private set; }
        public int CurrentIndex { get; private set; } = -1;
        public string CurrentActName { get; private set; } = "";
        public bool Finished { get; private set; }

        /// <summary>Side goals for the HUD's objectives panel ("Opcjonalne").</summary>
        public class OptionalTask
        {
            public string Title;
            public Func<bool> IsDone;
            public Func<string> Progress;
        }

        public readonly List<OptionalTask> Optional = new();
        bool cacheLooted, railwaymanTalked, workersFreed;

        public event Action<MissionStep> StepStarted;
        public event Action<MissionStep> StepCompleted;
        public event Action<string> ActStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        void Awake()
        {
            Instance = this;
            skipAction = new InputAction("DevSkip", InputActionType.Button, "<Keyboard>/f1");
            saveAction = new InputAction("DevSave", InputActionType.Button, "<Keyboard>/f8");
            loadAction = new InputAction("DevLoad", InputActionType.Button, "<Keyboard>/f9");
            skipAction.Enable(); saveAction.Enable(); loadAction.Enable();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            skipAction.Dispose(); saveAction.Dispose(); loadAction.Dispose();
        }

        void Start()
        {
            BuildStory();
            BuildDoors();
            BuildSideContent();
            if (FindAnyObjectByType<StreetLamps>() == null) gameObject.AddComponent<StreetLamps>();
            if (NpcDirector.Instance != null) NpcDirector.Instance.SpawnAll();

            int start = 0;
            if (Checkpoints.ResumeStep >= 0)
            {
                start = Checkpoints.ResumeStep;
                Checkpoints.ResumeStep = -1;
                Checkpoints.RestoreProgress();
                Inventory.NewGame(GameProgress.HasCamera);
            }
            else
            {
                GameProgress.NewGame();
                WeaponCatalog.NewGame();
                Inventory.NewGame(false);
            }
            // Radio NPCs appear in act 3 (index of the act-3 start step or later → active).
            int act3 = acts.Count > 2 ? acts[2].Step : int.MaxValue;
            SetRadioGuards(start >= act3);

            // New game: pick the loadout first (CS2-style screen), then the story starts.
            if (!WeaponCatalog.LoadoutChosen)
            {
                int s = start;
                LoadoutScreen.Open(LoadoutMode.Start, () => { Inventory.NewGame(GameProgress.HasCamera); StartStep(s, true); });
            }
            else StartStep(start, true);
        }

        /// <summary>Act 3: a weapons crate at the back entrance opens the rifle choice (optional).</summary>
        void SpawnArmoury()
        {
            if (!Points.Has("spawn_radio_tyl") || armouryUsed) return;
            var p = Points.Get("spawn_radio_tyl");
            var go = Spawner.Spawn("OB_Skrzynia_Bron", p.position + p.right * 2.5f + p.forward * 1.5f, p.rotation);
            var it = go.GetComponent<Interactable>();
            if (it == null) it = go.AddComponent<Interactable>();
            if (go.GetComponentInChildren<Collider>() == null) go.AddComponent<BoxCollider>();
            it.SetPrompt("Zbrojownia — wybierz karabin");
            it.SetSingleUse(true);
            it.Interacted += _ => LoadoutScreen.Open(LoadoutMode.Armoury, () =>
            {
                armouryUsed = true;
                if (WeaponCatalog.SelectedRifle != null) Inventory.GiveWeapon(WeaponCatalog.SelectedRifle);
            });
        }

        bool armouryUsed;

        // ------------------------------------------------------------------ story

        void BuildStory()
        {
            // AKT 1 (15:30) Dworzec i hotel
            AddAct("Akt 1 — Dworzec i hotel", "spawn_dworzec", 15.5f);
            steps.Add(new DialogStep("a1_intro", "Gliwice, 31 sierpnia 1939"));
            steps.Add(new ReachStep("miasto_drzwi_ksiegarnia", "Idź do księgarni pani Zofii na Ringu"));
            steps.Add(new TalkStep("CH_Zofia", "ksiegarnia_zofia", "a1_zofia", "Porozmawiaj z panią Zofią",
                () => Inventory.GiveCamera()));
            steps.Add(new ReachStep("miasto_drzwi_hotel", "Idź do hotelu Haus Oberschlesien"));
            steps.Add(new TalkStep("CH_Recepcjonista", "hotel_recepcja", "a1_recepcja", "Zagadaj recepcjonistę"));
            steps.Add(new InteractStep("OB_Drzwi", "hotel_pokoj_drzwi", "Otwórz", "Wejdź do pokoju Naujocksa") { HideOnUse = true });
            steps.Add(new PhotoStep("Sfotografuj mapę i listę (F)",
                new PhotoStep.Target { Point = "hotel_pokoj_mapa", Id = "mapa", Name = "Mapa z zaznaczoną radiostacją", Thought = "a1_mysl_mapa" },
                new PhotoStep.Target { Point = "hotel_pokoj_lista", Id = "lista", Name = "Lista „polskich ubrań”", Thought = "a1_mysl_lista" }));
            steps.Add(new CrouchListenStep("hotel_kolumna", "Podsłuchaj telefon w holu — kucnij przy kolumnie (C)", 3f, 2.5f,
                "a1_telefon", "a1_mysl_haslo"));

            // AKT 2 (16:00–19:45) Przez miasto
            AddAct("Akt 2 — Przez miasto", "miasto_drzwi_hotel", 16f);
            steps.Add(new DialogStep("a2_start", "Śledź auto SD"));
            steps.Add(new FollowCarStep("OB_Auto_SD", "miasto_auto_sd_trasa", "Śledź auto SD — nie bliżej niż 8 m, nie dalej niż 40 m"));
            steps.Add(new ActionStep(() => SetHour(19f)));
            steps.Add(new DialogStep("a2_zmrok", "Zapada zmrok"));
            steps.Add(new CrouchListenStep("miasto_zagajnik_krzaki", "Ukryj się w krzakach w zagajniku (C)", 3f, 2f, "a2_zagajnik"));
            steps.Add(new PhotoStep("Sfotografuj auto z „konserwą”",
                new PhotoStep.Target { Point = "miasto_zagajnik_auto", Id = "konserwa", Name = "Auto SD z nieprzytomnym mężczyzną", Thought = "a2_mysl_konserwa" }));

            // AKT 3 (20:00) Radiostacja
            AddAct("Akt 3 — Radiostacja", "miasto_dziura_plot", 20f);
            // Two approaches: quietly through the fence hole (doors network) or through the guarded gate.
            steps.Add(new ReachAnyStep("Wejdź na teren radiostacji — po cichu dziurą w płocie albo bramą", 4f,
                "spawn_radio_tyl", "spawn_radio_brama") { MarkerPoint = "miasto_dziura_plot" });
            steps.Add(new ActionStep(() => { SetHour(20f); SpawnWorkersDoor(); SetRadioGuards(true); SpawnArmoury(); }));
            steps.Add(new DialogStep("a3_komunikat", "SD nadaje przez mikrofon burzowy!"));
            steps.Add(new CableStep("radio_technika_kable", "radio_kartka_grubera", "a3_sygnal_urwany",
                "Przetnij właściwą linię wzmacniacza (kartka Grubera)"));

            // AKT 4 (20:10) Konserwa i ucieczka
            AddAct("Akt 4 — Konserwa i ucieczka", "radio_drzwi_wyjscie", 20.17f);
            steps.Add(new ActionStep(SpawnHoniok));
            steps.Add(new PhotoStep("Sfotografuj kartkę przy ciele",
                new PhotoStep.Target { Point = "radio_kartka_honiok", Id = "honiok", Name = "Kartka „Honiok, Franz. Konserve”", Thought = "a4_mysl_honiok" }));
            steps.Add(new DialogStep("a4_alarm", "Alarm!"));
            steps.Add(new ActionStep(AlarmAll));
            steps.Add(new ReachStep("miasto_stodola", "Uciekaj na wschód do stodoły kuriera", 4f));
            steps.Add(new TalkStep("CH_Sowa", "miasto_stodola", "a4_sowa", "Przekaż film kurierowi „Sowie”"));
            steps.Add(new ActionStep(() => Epilogue.Play()));
        }

        void AddAct(string name, string spawn, float hour) =>
            acts.Add(new Act { Step = steps.Count, Spawn = spawn, Hour = hour, Name = name });

        // ------------------------------------------------------------------ world content

        void BuildDoors()
        {
            Door("miasto_drzwi_hotel", "spawn_hotel_wejscie", "Wejdź do hotelu");
            Door("hotel_drzwi_wyjscie", "miasto_drzwi_hotel", "Wyjdź na ulicę", exitInFront: true);
            Door("miasto_drzwi_ksiegarnia", "spawn_ksiegarnia", "Wejdź do księgarni");
            Door("ksiegarnia_drzwi_wyjscie", "miasto_drzwi_ksiegarnia", "Wyjdź na ulicę", exitInFront: true);
            Door("miasto_drzwi_fotograf", "spawn_fotograf", "Wejdź do zakładu fotograficznego");
            Door("fotograf_drzwi_wyjscie", "miasto_drzwi_fotograf", "Wyjdź na ulicę", exitInFront: true);
            Door("miasto_drzwi_radio", "spawn_radio_brama", "Wejdź na teren radiostacji");
            Door("radio_drzwi_wyjscie", "miasto_drzwi_radio", "Wyjdź", exitInFront: true);
            Door("miasto_dziura_plot", "spawn_radio_tyl", "Przeciśnij się przez dziurę w płocie", prefab: "OB_Plot_Dziura");
        }

        void Door(string doorPoint, string target, string prompt, bool exitInFront = false, string prefab = "OB_Drzwi")
        {
            if (!Points.Has(doorPoint) || !Points.Has(target)) return;
            var go = Spawner.Spawn(prefab, doorPoint);
            var it = go.GetComponent<Interactable>();
            if (it == null) it = go.AddComponent<Interactable>();
            if (go.GetComponentInChildren<Collider>() == null) go.AddComponent<BoxCollider>();
            var door = go.GetComponent<DoorTeleport>();
            if (door == null) door = go.AddComponent<DoorTeleport>();
            Transform overrideTarget = null;
            if (exitInFront)
            {
                // Step out 1.5 m in front of the city door, facing away from it.
                var t = Points.Get(target);
                var marker = new GameObject($"Exit_{target}").transform;
                marker.SetPositionAndRotation(t.position + t.forward * 1.5f, t.rotation);
                overrideTarget = marker;
            }
            door.Setup(target, prompt, overrideTarget);
        }

        void BuildSideContent()
        {
            Optional.Add(new OptionalTask { Title = "Karty historii", IsDone = () => GameProgress.HistoryCards.Count >= GameProgress.HistoryCardTotal,
                Progress = () => $"{GameProgress.HistoryCards.Count}/{GameProgress.HistoryCardTotal}" });
            if (Points.Has("miasto_skrytka_most"))
                Optional.Add(new OptionalTask { Title = "Skrytka pod mostem (+8 naboi)", IsDone = () => cacheLooted });
            if (Points.Has("miasto_kolejarz"))
                Optional.Add(new OptionalTask { Title = "Kolejarz — rozkład transportów", IsDone = () => railwaymanTalked });
            if (Points.Has("radio_piwnica_drzwi"))
                Optional.Add(new OptionalTask { Title = "Uwolnij pracowników stacji (akt 3)", IsDone = () => workersFreed });
            Optional.Add(new OptionalTask { Title = "Zbrojownia — karabin (akt 3)", IsDone = () => WeaponCatalog.SelectedRifle != null });

            // 6 history cards.
            for (int i = 1; i <= GameProgress.HistoryCardTotal; i++)
            {
                string p = $"miasto_karta_{i}";
                if (!Points.Has(p)) continue;
                int idx = i;
                var go = Spawner.Spawn("OB_Karta_Historii", p);
                var it = go.GetComponent<Interactable>();
                if (it == null) it = go.AddComponent<Interactable>();
                if (go.GetComponentInChildren<Collider>() == null) go.AddComponent<BoxCollider>();
                it.SetPrompt("Weź kartę historii");
                it.SetSingleUse(true);
                it.Interacted += _ => { GameProgress.AddHistoryCard(idx); HistoryCardView.ShowCard(idx); go.SetActive(false); };
            }

            // Ammo cache under the bridge: +8.
            if (Points.Has("miasto_skrytka_most"))
            {
                var go = Spawner.Spawn("OB_Skrytka", "miasto_skrytka_most");
                var it = go.GetComponent<Interactable>();
                if (it == null) it = go.AddComponent<Interactable>();
                if (go.GetComponentInChildren<Collider>() == null) go.AddComponent<BoxCollider>();
                it.SetPrompt("Przeszukaj skrytkę");
                it.SetSingleUse(true);
                it.Interacted += _ =>
                {
                    var w = FindAnyObjectByType<PlayerWeapon>();
                    if (w != null) w.AddReserveAmmo(8);
                    cacheLooted = true;
                    if (Dialog.Exists("pob_skrytka")) Dialog.Play("pob_skrytka", null, false);
                };
            }

            // Railwayman (side talk).
            if (Points.Has("miasto_kolejarz"))
            {
                var go = Spawner.Spawn("CH_Kolejarz", "miasto_kolejarz");
                var it = go.GetComponent<Interactable>();
                if (it == null) it = go.AddComponent<Interactable>();
                it.SetPrompt("Porozmawiaj");
                it.SetSingleUse(true);
                it.Interacted += _ => { railwaymanTalked = true; Dialog.Play("pob_kolejarz"); };
            }
        }

        void SpawnWorkersDoor()
        {
            if (!Points.Has("radio_piwnica_drzwi")) return;
            var go = Spawner.Spawn("OB_Drzwi", "radio_piwnica_drzwi");
            var it = go.GetComponent<Interactable>();
            if (it == null) it = go.AddComponent<Interactable>();
            if (go.GetComponentInChildren<Collider>() == null) go.AddComponent<BoxCollider>();
            it.SetPrompt("Uwolnij pracowników (opcjonalne)");
            it.SetSingleUse(true);
            it.Interacted += _ =>
            {
                workersFreed = true;
                if (NpcDirector.Instance != null) NpcDirector.Instance.FreeWorkers();
                Dialog.Play("a3_gruber");
                Popup("Pracownicy stacji uwolnieni");
            };
        }

        void SpawnHoniok()
        {
            if (Spawner.Find("CH_Honiok") != null) return;
            var go = Spawner.Spawn("CH_Honiok", "radio_cialo_honiok");
            var anim = go.GetComponentInChildren<Animator>();
            if (anim != null) Dialog.SetBool(anim, "Lezy", true);
            else go.transform.rotation = go.transform.rotation * Quaternion.Euler(90f, 0f, 0f); // placeholder lies down
        }

        void AlarmAll()
        {
            var p = FindAnyObjectByType<FirstPersonController>();
            if (NpcDirector.Instance != null) NpcDirector.Instance.AlarmAll(p != null ? p.transform.position : Vector3.zero);
        }

        /// <summary>Radio station guards/workers only exist in the world from act 3 on.</summary>
        static void SetRadioGuards(bool active)
        {
            if (NpcDirector.Instance != null) NpcDirector.Instance.SetGroupActive("radio_", active);
        }

        // ------------------------------------------------------------------ runner

        void Update()
        {
            if (Finished || MissionFail.IsFailed || GameState.IsPaused) return;

            if (skipAction.WasPressedThisFrame() && Current != null && !Dialog.IsPlaying) CompleteCurrent();
            if (saveAction.WasPressedThisFrame()) { Checkpoints.Save(CurrentIndex); Popup("Zapisano (F8)"); }
            if (loadAction.WasPressedThisFrame() && Checkpoints.SavedStep >= 0) { Checkpoints.Reload(); return; }

            if (Current == null) return;
            Current.Tick();
            if (Current.Done) CompleteCurrent();
        }

        void CompleteCurrent()
        {
            var done = Current;
            done.End();
            Current = null;
            StepCompleted?.Invoke(done);
            if (!string.IsNullOrEmpty(done.Title)) SfxBank.Play(SfxBank.Sfx.QuestStep);
            StartStep(CurrentIndex + 1, false);
        }

        void StartStep(int index, bool fromLoad)
        {
            CurrentIndex = index;
            if (index >= steps.Count)
            {
                Finished = true;
                return;
            }

            int actIdx = acts.FindIndex(a => a.Step == index);
            if (actIdx < 0 && fromLoad) actIdx = acts.FindLastIndex(a => a.Step <= index);
            if (actIdx >= 0)
            {
                var act = acts[actIdx];
                CurrentActName = act.Name;
                SetHour(act.Hour);
                bool atSavedSpot = fromLoad && Checkpoints.HasPosition && index == Checkpoints.SavedStep && index != act.Step;
                if (atSavedSpot) FindAnyObjectByType<FirstPersonController>()?.Teleport(Checkpoints.SavedPosition, Checkpoints.SavedYaw);
                else if (fromLoad || index == 0) TeleportTo(act.Spawn, instant: true);
                // Resuming mid-act: replay the act's set-up actions (time of day, spawns, alarm) skipped so far.
                if (fromLoad)
                    for (int i = act.Step; i < index; i++)
                        if (steps[i] is ActionStep setup && !(i == steps.Count - 1)) setup.Begin();
                ActStarted?.Invoke(act.Name);
            }
            // Checkpoint at every step (player feedback), none in historical mode.
            if (!GameMode.Historical && !fromLoad) Checkpoints.Save(index);

            Current = steps[index];
            StepStarted?.Invoke(Current);
            Current.Begin();
            if (Current.Done) CompleteCurrent();
        }

        // ------------------------------------------------------------------ helpers

        public static void SetHour(float hour)
        {
            GameClock.Set(hour);
            AtmosphereController.SetTimeGlobal(hour);
        }

        static void TeleportTo(string point, bool instant = false)
        {
            if (!Points.Has(point)) return;
            var p = FindAnyObjectByType<FirstPersonController>();
            if (p == null) return;
            var t = Points.Get(point);
            if (instant) p.Teleport(t.position + Vector3.up * 0.05f, t.eulerAngles.y);
            else ScreenFade.Transition(() => p.Teleport(t.position + Vector3.up * 0.05f, t.eulerAngles.y));
        }

        static void Popup(string text)
        {
            var fb = FindAnyObjectByType<CombatFeedback>();
            if (fb != null) fb.AddPopup(text, new Color(0.85f, 0.95f, 1f));
        }
    }
}
