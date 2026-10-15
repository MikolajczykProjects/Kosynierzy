# PLAN_SONNET_3 — playtest feedback + dialogue/audio package (15.10.2026, 13:00)

Deadline 14:00. Two Sonnet sessions, effort MEDIUM, NO sub-agents. 5-h usage window is at ~83 % and resets at 13:40 —
until then work lean; if get_usage shows >= 93 % before 13:40, commit and wait for the reset. FREEZE 13:50: compile clean,
save scene, local commit, short "Testuj teraz" list. No push without the user's yes in chat.
Each step: compile (recompile + recompile_status), then a local commit "s3 step N: ...". Never touch the other session's files.

Already done by Opus (13:00): the team A package is copied into `Assets/_Dialogi/` (Resources/dialogi.json = 56 scenes,
177 lines, every line has `audio`; Resources/Audio, Music (14), Ambience (7), Sfx (7); manifests MANIFEST-*.md explain each
file). `Dialog.cs` now accepts `"Audio/x"` and `"x"`. Before this, `Resources/dialogi.json` did not exist in the project —
dialogues were placeholders, so test them first.

## Session UI (local_039a14ba) — owns Assets/Scripts/UI, Dialogue, new Assets/Scripts/Audio, new Assets/Scripts/Admin

U1. ADMIN PANEL (highest priority — the user cannot test the game without it). F1 (or backquote) toggles; cursor
unlocked, game keeps running (time scale 0 optional). Style = UI v2 (UiTheme). Buttons:
  - Jump to act/step: list every Mission step (act 1..4, step index + name); click = restore that step the same way a
    checkpoint load does (Checkpoints/Mission API — read Mission.cs; add a public `Mission.DebugJumpTo(int stepIndex)` if
    missing, in a small partial or at the bottom of Mission.cs — coordinate: Mission.cs is otherwise owned by session
    GAMEPLAY, so add only that method and tell it).
  - Teleport: list of all named map points (Points) + "current objective"; uses FirstPersonController.Teleport.
  - Toggles: god mode (Health), invisible to detection, infinite ammo, noclip-lite (speed x3), time of day (GameClock
    15:30 / 19:00 / 20:00), alarm all / calm all (NpcDirector), give rifle (all 3) / camera, kill nearest enemy,
    show NPC count + FPS.
  - Plays: each cutscene / transition (from session GAMEPLAY's G6 once it exists), each music track, ending, fail screen.
  Must be available in Play in the editor and in the build (it is a hackathon build, OK).
U2. MUSIC: `GameMusic` (DontDestroyOnLoad, two AudioSources crossfade, MANIFEST-SOUNDTRACK.md has the reference script
  and which track goes where). API: `GameMusic.Play(string id, float fade = 1.5f)`, `GameMusic.Stop(float fade)`.
  Menu scene → mus_menu_main_theme; exploration → mus_eksploracja_spokojna; detection/alarm → mus_skradanie_napiecie /
  mus_akcja_strzelanina (hook to DetectionRunner / NpcDirector alarm state, with a 6-s hold so it does not flicker);
  fail → mus_porazka_game_over; epilogue → mus_epilog_pamiec. Per-step tracks (car follow, cable climax, escape) are
  called by session GAMEPLAY from Mission steps via the API — commit U2 early and tell it.
U3. AMBIENCE: `Ambience.SetZone(string id)` — city loop (amb_miasto_gliwice_1939 before dusk, amb_miasto_wojna_1939
  after) and interiors via DoorTeleport (fade 0.4 s; MANIFEST-SFX-AMBIENCE.md maps doors → rooms). Footsteps
  sfx_kroki_bruk / sfx_kroki_drewno; replace procedural sounds where a real file exists (camera shutter, door, phone,
  reception bell, cable cut) — SfxBank lookup "Sfx/<name>" first, procedural fallback.
U4. DIALOGUE PLAYER: subtitles from `napis`, italics when `mysl`, speaker name; audio drives line length (clip length +
  0.3 s, text-length fallback). Barks: bark_przechodnie, bark_pracownik, bark_sd_* (podejrzenie/alarm/szukanie/zgubil),
  bark_schupo as 3D audio + floating subtitle. History cards karta_1..6 and epilog_1..6, porazka_* on the fail screen.
U5. Stamina bar bigger (HudCombat/HudEquipment, ~1.6x width, clearly visible) — the stamina VALUE is session GAMEPLAY's.

## Session GAMEPLAY (local_2982541d) — owns Player, AI, Mission (except U1's one method), Core, Interaction, Stealth, Weapons

G1. HOLSTER: pressing the key of the already selected slot (1..4) puts it away → empty hands (HandItem.Empty); pressing
  again draws it. Empty hands = no weapon visible, cannot shoot, NPCs are less suspicious (if Detection has a "weapon
  drawn" factor, use it).
G2. OBJECTIVE MARKER: much more visible — a world-space marker (pillar/beam + diamond icon, distance in m, clamped to the
  screen edge when off-screen, pulsing). The user says it is barely visible now.
G3. LONG TRIPS: when the next objective is > 300 m from the player, offer/auto a travel transition: fade to black +
  short title ("Gliwice, Ring — 16:10"), teleport to a NavMesh point <= 100 m from the objective (NavMesh.SamplePosition,
  facing the target), fade in. Use the existing ScreenFade.
G4. SUSPICION: currently does not work and is not visible. Measure first: in Play, read Detection values of an SD near the
  player (log once per second). Fix the cause; make it visible: indicator over the NPC's head (SuspicionIndicator) and a
  HUD edge arrow + fill (HudDetection). Barks bark_sd_podejrzenie / _alarm / _szukanie / _zgubil via Dialog.Bark at the
  state changes.
G5. CROWD: many more people in the city (target ~60 civilians + ~15 SD/Schupo active within 120 m of the player, the rest
  culled/disabled by distance — keep FPS); natural walking: random speed 0.9–1.4 m/s per NPC, NavMeshAgent acceleration
  ~4, angularSpeed ~240, stopping distance 0.5, idle stops 2–8 s (look around, talk in pairs), avoidance quality medium
  with random priority; groups of 2–3 walking together; a few standing at shop windows / benches.
G6. CUTSCENES / TRANSITIONS: act start = fade + title card (act, place, hour); key beats as short camera moves (player
  control locked, 3–6 s, skippable with Space): meeting Zofia, seeing the SD car, entering the radio station, the
  broadcast (a3_komunikat), the escape. Shootout start = camera shake + GameMusic.Play("mus_akcja_strzelanina").
  Hook the unused dialogue scenes: a1_hotel_goscie, pob_fotograf_przed/_gestapo/_po, a2_auto_daleko/_blisko, a2_most,
  a3_wejscie, a3_sd_udaja, a3_brak_sygnalu, a3_mysl_kable, a3_kabel_zly, a3_czas_minal, a4_ucieczka, a4_radio_spiker
  (MANIFEST-DIALOGOW.md gives each one's trigger and map point).
G7. CHECKPOINTS: more often (every step + every interior entry + every 90 s outside if no alarm). Civilian kill no longer
  fails at once: 1st and 2nd = warning text + bark; the 3rd = fail and reload of the last checkpoint (porazka_cywil).
G8. STAMINA: more stamina (max +60 %, regen +30 %; sprint drain unchanged) — write before/after values in DECISIONS.md.

Order for both: U1, U2, U4, U3, U5 / G1, G2, G4, G7, G8, G5, G3, G6. If time runs out, stop at a compiled commit and list
what is left in docs/USER_TODO.md.
