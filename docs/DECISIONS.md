# Decisions

| Date | Decision | Why |
|---|---|---|
| 15.10.2026 | Unity 6.6 URP, Universal 3D template; project folder stays "My project" locally | Created by the user in Hub; folder name doesn't matter for the repo |
| 15.10.2026 | Claude drives the editor through Unity CLI + Pipeline package, not an MCP server | Official, already installed; AI Assistant MCP needs a Unity AI subscription |
| 15.10.2026 | Input actions created in code (no .inputactions asset) | No asset wiring needed; teammates can't break bindings by accident |
| 15.10.2026 | Jump on Space added though not requested | Standard in FPP; one line to remove |
| 15.10.2026 | Waves/spawner removed; linear quest system instead | User: linear adventure across Gliwice with quests, no waves |
| 15.10.2026 | Enemies shoot hitscan with distance-based hit chance; no friendly fire | Readable, tunable placeholder AI |
| 15.10.2026 | HUD in IMGUI | Zero setup for test mechanics; UI team replaces it, binding to the same public members |
| 15.10.2026 | Commits authored as MikolajczykProjects with GitHub noreply email | Keep the private e-mail out of a public repo |
| 15.10.2026 | Play-mode testing is done by the user | User request — faster than scripted tests |
| 15.10.2026 | Team spec arrived (docs/00-PODZIAL-I-ZASADY.txt, ROLA-B-UNITY.txt, FABULA.md) — it is the source of truth for scope | User: "najważniejsze rzeczy, z których potem wszystko będzie robione" |
| 15.10.2026 | Hits on people: dust/cloth puff by default; blood only with the "Krew (16+)" option (GameSettings.MatureContent) | Team rule "bez krwi" + user: blood as a 16+ checkbox with more brutal effects |
| 15.10.2026 | **CAMERA: FIRST PERSON (FPP), NOT third person.** Overrides ROLA-B-UNITY.txt (Starter Assets ThirdPerson / Cinemachine over-the-shoulder). Our own FirstPersonController stays: head bob, slide, stamina. Everything else follows the team docs | User decision: "PIERWSZOOSOBOWA KAMERA, NIE TRZECIOOSOBOWA — z tym się myli. ZAPISZ" |
| 15.10.2026 | Menus (pause, settings, main menu, fail screen, history cards, epilogue) are uGUI built in code with legacy UI.Text | No TMP resources needed, not obsolete in ugui 2.6; nothing to wire by hand |
| 15.10.2026 | Graphics defaults: VSync on, SMAA, shadows High (100 m, 4 cascades); URP asset values restored after Play in the editor | Good look by default; Assets/Settings stays clean in git |
| 15.10.2026 | Time of day driven by mission steps (GameClock → AtmosphereController.SetTime); night from 19:00 | Team doc KROK 7 |
| 15.10.2026 | Mission built in code from contract names (points/prefabs/dialogue ids) with placeholders when missing | Team doc: game must run before art arrives; nothing to wire in the scene |
| 15.10.2026 | Player speeds per team doc: walk 2.5, sneak 2.0, run 5.5 m/s; slide + stamina kept | Team doc KROK 2 + user's FPP movement |
| 15.10.2026 | Vis wz. 35: magazine 8, start 8 + 0, fire only while aiming (RMB), shot noise 30 m | Team doc KROK 3 |
| 15.10.2026 | Player health 1 → any hit = mission fail; enemies down with one hit and stay on the ground | Team doc "jedno trafienie = koniec" |
| 15.10.2026 | Placeholder map (Kosynierzy/Create Placeholder Map) with all contract points; interiors at x ≥ 1000 | Play the whole story before Mapa_Gliwice.prefab arrives |
| 15.10.2026 | UI style "wartime documents": aged paper, typewriter (Courier New from the OS), stamp red, olive; one UiTheme for HUD and menus; no Figma | User: every UI more war-like; speed — styled directly in Unity, colours/fonts in one place |
| 15.10.2026 | Inventory + PlayerHands: 1 Vis, 2 camera (from Zofia, with a 3D hand-over), 3 bayonet; placeholder viewmodels from primitives until OB_Pistolet_Vis | User: items in hands, camera as a real held object later |
| 15.10.2026 | ADS aligns the pistol's sights on the screen centre; sprint pose = muzzle down | User: aiming weak, gun held wrong while running |
| 15.10.2026 | Checkpoint at EVERY mission step (with player position); act set-up actions replayed on resume | User: "not everywhere are checkpoints" |
| 15.10.2026 | Procedural placeholder SFX (gunshots, steps, clicks, shutter, chimes) when SfxBank slots are empty | User: no gunshot sounds; sound team's clips override |
| 15.10.2026 | Stealth à la Hitman: detection meter, hiding spots, silent bayonet takedowns from behind, body discovery and civilian panic; "!"/"?" only over SD/Schupo | User playtest feedback |
| 15.10.2026 | Act 3 entry: fence hole (quiet) OR gate (guarded); optional tasks list (cards, cache, railwayman, workers) | User: game too linear |
| 15.10.2026 | UI v1 (Figma frame 292:2 and the paper-style HUD in Unity) rejected: "looks too AI". Hand-over to Sonnet 5.5 + agents with docs/PLAN_SONNET.md: research shipped war shooters first (BF V, CS2, Sniper Elite 5, Hitman 3, Hell Let Loose), redesign in Figma v2, approval, then implement | User: "wygląda UI jak AI za bardzo… zaplanuj… odpalimy sonnet 5.5 i parę agentów" |
| 15.10.2026 | Armoury: start loadout pistol 1/3 + blade 1/2, rifle 1/3 at the act-3 armoury; hotbar 1 pistol · 2 rifle · 3 blade · 4 camera | User: armoury/loadout like CS2, a pistol of 3 at the start, a rifle later |
| 15.10.2026 | Figma bridge on this PC: own relay ~/.claude/narzedzia/figma-most/socket.js + own plugin ~/.claude/narzedzia/figma-plugin (no telemetry); the official plugin works too (random channel, Inter-only text) | No bridge existed on this machine; the user asked for an own plugin without telemetry |
| 15.10.2026 | UI v2 designed from the research (docs/research/ui_research_2026-10-15.md): bare HUD text with soft shadows, Barlow Condensed + Barlow, one accent per state, CS2-style loadout columns, rendered over real Play captures; spec in docs/design/UI_V2_SPEC.md; Figma push waits for our plugin v2 | User rejected v1 as AI-looking; approval gate before implementing in Unity |
| 15.10.2026 | Figma tooling: plugin v2 (build/batch/fonts/effects/svg) at ~/.claude/narzedzia/figma-plugin + direct client figma_cmd.js; local HTML->PNG preview renderer tools/figma/ui_v2 as fallback | Official plugin is Inter-only; user must import our plugin once |
| 15.10.2026 | Stamina: max 100 -> 160 (+60%), regen 24 -> 31.2/s (+30%); sprint drain 16/s unchanged. Civilian kill: 1st and 2nd = warning, 3rd = fail | User playtest (PLAN_SONNET_3 G7/G8) |
| 15.10.2026 | PERF (user: game unplayable, laggy): URP PC_RPAsset shadowDistance 100 -> 40, shadowCascades 4 -> 2, additional-light shadows on -> off; player camera far clip 1000 -> 400. Measured before: 6102 renderers (6100 shadow casters), 413 point lights (off by day, all on from 19:30), 5694 map colliders, 48 agents on NavMesh (146k tris). Dialog: player freeze now auto-releases after 6 s (long voice lines froze player); loadout screen sets timeScale 0 until confirmed | Not profiled in Play (editor does not tick unfocused) - user to confirm FPS |
| 15.10.2026 | LampCuller (VFX/LampCuller.cs): at night only the 8 nearest map lamps within 60 m stay enabled, map lights never cast shadows. Measured: day 6/428 point lights on (OK); night right after MapaSwiatlo.Ustaw(20) 419/428 on - culler had not yet run (editor stopped ticking at frame 530), so the cull effect is NOT verified in Play | Needs a focused Play check at night (e.g. Admin panel time 20:00) |
| 15.10.2026 | Render opt: SRP Batcher ON, GPU instancing on 12 _Obiekty materials, BatchingStatic+Occluder/Occludee on 6027 map renderers (scene instance). Before (user stats): 66 FPS, 1.0M tris, 3747 draw calls, 51 SetPass. After: not measured | Layer cull distances not done (props share layer 0) |
