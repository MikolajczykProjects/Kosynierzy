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
