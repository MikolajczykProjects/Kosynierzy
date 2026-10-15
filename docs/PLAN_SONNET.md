# Execution plan for Sonnet 5.5 + agents — Sender Gleiwitz 31.08.1939 (hackathon)

Written 15.10.2026 by the previous session (Opus) at hand-over. Read this whole file first, then
`docs/NOTES.md`, `docs/DECISIONS.md`, `docs/USER_TODO.md` and the team docs (`docs/00-PODZIAL-I-ZASADY.txt`,
`docs/ROLA-B-UNITY.txt`, `docs/FABULA.md`). The user's global rules are in `~/.claude/CLAUDE.md` (they load
automatically) — the ones that matter most here are restated in §1.

---

## 0. TL;DR — what to do, in order

| # | Workstream | Who | Blocking? |
|---|---|---|---|
| WS0 | Bootstrap: compile, smoke-test, baseline commit, Figma relay up | main session | first 10 min |
| WS1 | **UI research** (Battlefield V, CS2, Sniper Elite 5, Hitman 3, Hell Let Loose) → measured reference sheet | agent R (read-only + browser) | feeds WS2 |
| WS2 | **Figma tooling + UI redesign v2** ("not AI-looking"), user approval gate | main session only (one Figma connection) | gates WS3 |
| WS3 | Implement approved UI v2 in Unity (HUD, loadout/armoury, weapon switch, viewfinder, menus) | agent U (files only) + main verifies | after approval |
| WS4 | Gameplay verification & tuning (aim, sprint pose, VFX, stealth, AI pathing, armoury, checkpoints) | agent G (files only) + main runs Unity | parallel to WS1–WS3 |
| WS5 | Less linear content (optional routes/objectives) | agent G after WS4 | P1 |
| WS6 | Team asset integration (map, CH_/OB_ prefabs, dialogi.json) when they arrive | main session | whenever |
| WS7 | Release: scenes in `Assets/_Gra/Sceny`, build, backup video, push | main session + user | last 30 min |

**Max 2 agents running at the same time** (user rule). Agents only WRITE files; only the main session drives
the Unity editor (Unity CLI), Figma and git (one connection each, user rule). Every agent prompt must name the
skills to load (agents don't get skills automatically) and the files it owns.

---

## 1. Rules that apply to everything (from the user and the team docs)

- Talk to the user **in Polish**; code, identifiers, commits and agent docs in **English**; player-facing text Polish.
- **Report results, not intentions**: before saying "done", see it (screenshot, console, number). Say plainly what
  was not seen. Visual work (UI, VFX) needs screenshots; code needs a clean compile (0 errors, 0 warnings).
- **The user play-tests personally** — give a short numbered "Testuj teraz" checklist after each delivery
  instead of long scripted play-mode tests (they collided with the user's own session before). Quick probes with
  `eval_file` are fine when the user is not playing; ask first.
- **Camera is FIRST PERSON.** The team doc's third-person / Starter Assets / Cinemachine parts are overridden.
- Team rules: one hit = mission fail; shooting a civilian / Schupo / station worker = fail; **no swastikas or nazi
  symbols anywhere**; no blood unless the "Krew (16+)" option (`GameSettings.MatureContent`) is on.
- Prefab and point names come from the team contract (`docs/00-PODZIAL-I-ZASADY.txt`); load by name, missing =
  grey placeholder + one warning, never a crash.
- Decide alone when reversible in < 1 h and within spec; ask (one concrete Polish question) for money, the look of
  hero assets, platform rules, pushing to GitHub, downloads. Log reasons in `docs/DECISIONS.md` either way.
- Changes outside the request are allowed but must be listed in the report with before → after values.
- Subagents: model `sonnet` (user's choice now), explicit in every Agent call; max 2 at once; check `get_usage`
  before starting one (stop if < 25 % left in the 5-h or weekly window).

---

## 2. Current state (hand-over snapshot)

**Repo**: `C:\Users\Sylwia.Mikolajczyk\My project` → GitHub `MikolajczykProjects/Kosynierzy` (main). Pushed up to
`7a9fd49`. Local, not pushed: `7d8e54d` + the hand-over commit (VFX overhaul + this plan). Do not push without
the user's yes. Unity 6.6 (6000.6.5f1), URP 17.6, Input System only, AI Navigation 2.0.15, Newtonsoft JSON,
`com.unity.pipeline` (CLI bridge). Scene: `Assets/Scenes/SampleScene.unity` (to be moved, WS7).

**Compiles clean** (last check after VFX wiring). Smoke Play: no runtime errors. Most features are **not
play-tested** — treat everything in §2.2 as "built, unverified" unless marked ✔.

### 2.1 Systems (code under `Assets/Scripts/`)
| Area | Files | Notes |
|---|---|---|
| Movement (FPP) | `Player/FirstPersonController.cs` | walk 2.5 / sneak 2.0 / run 5.5 m/s, stamina, slide (C while sprinting), coyote+buffer jump, head bob, landing dip, FOV kick, recoil API, trauma shake |
| Hands & items | `Player/PlayerHands.cs`, `Core/Inventory.cs` | slots 1 pistol · 2 rifle · 3 blade · 4 camera; wheel / d-pad cycle; F = camera; placeholder viewmodels from primitives with real sights (✔ pistol visible in a camera capture); blade stab + takedown hook `PlayStab()`; 3D hand-over animation when an item is received |
| Weapons | `Weapons/WeaponCatalog.cs`, `PlayerWeapon.cs`, `ProjectileSystem.cs`, `Combat/Hitbox.cs`, `Combat/Health.cs` | 3 pistols (Vis, Browning, Nagant), 2 blades (bagnet, nóż), 3 rifles (wz.29, 98k, Erma); simulated fast bullets with drop, hitboxes, per-weapon ammo; aim to fire (team doc) |
| Loadout | `UI/LoadoutScreen.cs` | CS2-like picker at new game (pistol + blade) and act-3 armoury (rifle); functional, old look → redo in WS3 |
| VFX | `VFX/VfxLibrary.cs`, `ScreenFx.cs`, `ShellCasings.cs`, `BulletDecals.cs`, `VfxTextures.cs`, `VfxMeshes.cs`, `DeathEffect.cs` | layered muzzle flash (scale per weapon), shells, decals, tracers, kill/headshot bursts, screen pulses; blood only 16+. **Never seen in Play yet** |
| Feedback | `UI/CombatFeedback.cs`, `Core/TimeControl.cs`, `Core/CameraShake.cs`, `Core/SfxBank.cs`, `Core/ProceduralSfx.cs` | hit-stop on lone kills, slow-mo on last enemy, popups, procedural placeholder sounds (gunshots, steps, clicks, shutter, chimes) until the sound team's clips are dropped into `SfxBank` |
| Stealth & AI | `Stealth/*`, `AI/*`, `Enemies/EnemyController.cs`, `Core/Noise.cs`, `Player/PlayerNoise.cs` | Hitman-like detection meter (`Detection` static API), hiding spots, silent takedowns from behind, bodies discovered, civilian panic → police → SD alarm, SD shoot real bullets with distance/cover accuracy, Schupo never shoots, workers freed in act 3, `NpcDirector` spawns by contract points |
| Mission | `Mission/Mission.cs`, `MissionSteps.cs`, `Checkpoints.cs`, `GameClock.cs`, `StreetLamps.cs` | acts 1–4 built in code from point names; checkpoint at EVERY step (+ player position); F1 skip, F8 save, F9 load; doors-teleports; act 3 entry via fence hole OR gate; armoury crate; optional tasks list |
| Story systems | `Core/Points.cs`, `Spawner.cs`, `PrefabRegistry.cs`, `Dialogue/Dialog.cs`, `Interaction/*` | named points, prefab registry (`Resources/PrefabRegistry`), JSON dialogues (`Resources/dialogi.json` from team A — not delivered yet → placeholders), photo camera + evidence, history cards |
| UI (to be replaced) | `UI/CombatHud.cs`, `Hud*.cs`, `UiTheme.cs`, `DialogUI.cs`, menus (`PauseMenu`, `SettingsMenu*`, `MainMenu`, `FailScreen`, `HistoryCardView`, `Epilogue`) | uGUI built in code, "1939 paper/typewriter" theme — **the user rejected it as unprofessional / AI-looking** |
| Settings & look | `Core/GameSettings.cs`, `GraphicsSettingsRuntime.cs`, `AtmosphereController.cs`, `GameMode.cs` | graphics/controls/sound/"Krew (16+)"; time of day 15:30 → night driven by mission |
| Editor menus | `Editor/*` → menu **Kosynierzy/** | see §6; `Temp/setup_all.ps1` runs the whole scene setup |
| Test map | `Editor/PlaceholderMapMenu.cs` → `Mapa_Testowa` | all contract points, closed interiors at x ≥ 1000, 111 m tower; delete when `Mapa_Gliwice.prefab` arrives |

### 2.2 The user's requests — status
| Request (user, 15.10) | Status |
|---|---|
| FPP movement: slide, sprint, stamina, bob | built, partly tested earlier |
| Shooting, reloading, fast bullets | built; aim/sights: built, **verify per weapon** |
| "Aiming is weak", "gun held wrong while running" | ADS sight alignment + new sprint pose built — **verify** |
| Dopamine effects + complex professional VFX (Shuriken) | built by agent — **not seen, verify & tune** |
| Checkpoints everywhere | every step ✔ in code — verify resume mid-act |
| Missions / less linear | acts 1–4 + 2 entrances + optional list; **more needed (WS5)** |
| Settings + graphics + lighting/atmosphere | built — verify |
| AI pathfinding (to improve per map) | NavMesh on all floors, stuck repath — verify; real map later |
| Stealth à la Hitman, hiding, bayonet takedowns, panic | built — **playtest** |
| Civilians vs enemies readable; no "!" over civilians | placeholder looks per type + indicator only on SD/Schupo — verify |
| Gunshot sounds | procedural placeholders ✔ in code |
| Closed interiors | placeholder rooms closed ✔ in code |
| Camera hand-over animation + camera held in hand | built — verify |
| Armoury: start loadout (pistol 1/3 + blade), rifle later (1/3) | built (functional UI) — redo look in WS3 |
| Weapon selection UI like CS2/CS:GO | functional list + picker; **redo after research** |
| Clear objectives top-right that tick off and disappear | built in paper style; **redo look** |
| **All UI must be professional, not AI-looking; research BF, CS2, a WWII shooter; fix Figma** | **WS1 + WS2 + WS3 (top priority)** |

---

## 3. WS0 — Bootstrap (main session, 10 min)

1. `git status` → commit anything uncommitted ("step N: …", attribution line `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>` as the harness says).
2. Compile + console (commands §6). Expect 0 errors / 0 warnings.
3. Smoke Play 8 s → no errors (§6 recipe).
4. Figma: check `http://localhost:3055/status`. If nothing answers, the relay must run:
   `node "%USERPROFILE%\.claude\narzedzia\figma-most\socket.js"` — the auto-mode classifier blocked Claude from
   starting it before ("Expose Local Services"); the user started it by asking explicitly ("stwórz most"). If blocked,
   ask the user to run that line in a terminal. Then the user runs the Figma plugin; join its channel (§5.1).

---

## 4. WS1 — UI research (agent R, sonnet, ~25 min, read-only + built-in browser)

**Skills to load**: `ui-reference-libraries` (where to find shipped game UI, licence rule: reference only, never
copy assets), `anthropic-skills:built-in-browser`. Write ONLY `docs/research/ui_research_2026-10-15.md` and
screenshots under `docs/research/ui/` (small PNG/JPG, ≤ 30 files, named `<game>_<screen>.jpg`). No downloads of
binaries; images saved from pages for internal reference are OK — note the source URL for each.

**Sources**: Game UI Database (gameuidatabase.com — filter by game), Interface In Game (interfaceingame.com),
official press kits / store screenshots, and frames from official trailers/gameplay (describe; don't redistribute).

**Games and what to capture** (in-game HUD at 1080p wherever possible):
1. **Battlefield V** (WWII) — combat HUD (compass/minimap, ammo, gadgets, objective markers, spotting), kill log,
   objective/sector update banners, loadout/"Company" weapon customisation screen, pause/settings.
2. **Counter-Strike 2** — buy menu grid (5×3, categories, how a card shows a weapon), loadout screen, the right-side
   weapon list on switching (icons facing right, slot numbers, ammo), bottom HUD (health/armour left, ammo right),
   kill feed. Also CS:GO's radial buy menu for contrast.
3. **Sniper Elite 5** (WWII stealth, closest genre match) — detection/awareness indicators, binocular tagging (≈ our
   photo camera), objective list, takedown prompts, mission briefing documents.
4. **Hitman 3 / World of Assassination** — detection arrows & "compromised" states, objective tick-off animations,
   "Silent Assassin" style feedback, item pickup prompts.
5. **Hell Let Loose** (WWII realism) — minimal HUD, compass, how little text is on screen.
   Optional: Call of Duty: WWII / Vanguard objective banners; Wolfenstein: The New Order stealth commander UI.

**Output format** (per game, then a synthesis):
- Layout map: element → anchor + position in % of 1920×1080, size in px at 1080p.
- Type: family style (condensed sans? mono numbers? serif?), sizes in px for label / value / title, letter-spacing,
  case. Colours: hex of text, accents, backgrounds; how much is behind panels vs. bare text with shadow.
- Density & motion: how many elements are visible in calm vs. combat; timings of banners/ticks (s).
- Icons: style (silhouette, outline, stroke width), direction (weapons face right in CS).
- **"Why it does NOT look AI-made"**: concrete traits (custom condensed type, bare text with soft shadows instead of
  boxes, icon-first, asymmetric layouts, tight value/label pairs, restraint in colour, period details only where
  diegetic).
- **Synthesis for our game** (1939, FPP stealth, low-poly): 10–15 rules with numbers (type scale, spacing grid,
  palette tokens, panel usage, icon grid), and a list "do / don't". Include the diagnosis of our v1 (§5.2).

---

## 5. WS2 — Figma tooling + redesign v2 (main session only)

### 5.1 Connection facts
- Relay: `~/.claude/narzedzia/figma-most/socket.js` (no deps, port 3055, `/status` lists channels).
- MCP server `talk-to-figma` = npm `cursor-talk-to-figma-mcp` (40 tools). It works with the **official "Talk To Figma
  MCP Plugin"** (already installed in the user's Figma); that plugin joins a **random channel** shown in its window
  (last one: `ys27uw9p`) → `join_channel` with that name. Limitation: **text is always Inter** (no font family),
  no gradients/effects/images/rotation — one reason v1 looked generic.
- Our own plugin **"Talk to Figma (bez telemetrii)"** is written at `~/.claude/narzedzia/figma-plugin/`
  (`manifest.json`, `code.js`, `ui.html`): local only, fixed channel `claude`, supports **font by layer name**
  (`"Title {font:Barlow Condensed:SemiBold}"`). Not imported yet: the user must do Plugins → Development → Import
  plugin from manifest (Claude's SendKeys automation was blocked by the classifier). After import: Ctrl+/ →
  "bez telemetrii" → Enter.
- **Recommended tooling task (15 min)**: extend our `code.js` with `set_text_style` (fontName, letterSpacing,
  lineHeight, textCase), `set_effects` (DROP_SHADOW, LAYER_BLUR), `set_fills` (GRADIENT_LINEAR/RADIAL, IMAGE from
  base64), `set_rotation`, `create_ellipse`, `create_svg` (icons!), `rename_node`, `set_parent`; and write a tiny
  direct client `~/.claude/narzedzia/figma-most/figma_cmd.js` (Node, raw WebSocket like socket.js) that sends
  `{type:"join"}` then `{type:"message", message:{id, command, params}}` and prints the result, with `--batch
  file.json` and `--export nodeId out.png`. The MCP tools stay for the basics; the client unlocks fonts, icons,
  shadows and screenshots-as-background — exactly what the v1 lacked.

### 5.2 Diagnosis of v1 ("wygląda jak AI") — fix all of these
v1 lives in the user's Figma file "Untitled", frame `292:2` at x = 40000 ("SENDER GLEIWITZ · UI v1 · 15.10"), children
`292:3` (HUD) and `292:85` (loadout). Rename it "ODRZUCONE · UI v1 · za bardzo AI" and keep it; put v2 at x = 44400.
1. Every element sits on the same translucent dark rectangle → "boxes on boxes". Shipped HUDs mostly use **bare
   text/icons with soft shadow**; panels only where content is dense (buy menu, map).
2. One neutral font (Inter) everywhere, everything bold uppercase → generic SaaS. Use a **condensed display sans**
   for labels/titles (e.g. Barlow Condensed / Saira Condensed / Oswald), a **monospaced or tabular** face for numbers
   (IBM Plex Mono / JetBrains Mono), period type (stencil / typewriter) **only** on diegetic documents.
3. Unicode decorations (◆ ✓ ○ ·) and middle dots between every word — a typical generated-UI tell. Use real
   SVG icons (diamond marker, check, eye, ammo) on a 16/24 px grid, consistent stroke.
4. Over-explaining copy ("Wskazówka:", long subtitles, "TAJNE" stamp cliché). Keep labels 1–3 words.
5. Weapon silhouettes built from rectangles look childish → proper right-facing silhouettes (trace SVGs from
   reference photos of the real models, or render our placeholder viewmodels / team D models flat white).
6. Equal-weight symmetric cards and identical stat bars → use hierarchy (big value, tiny label), CS2-style short
   descriptors instead of four bars, asymmetry.
7. Brass accent on everything → one accent per state (neutral white, red = danger, amber = suspicion, olive = safe).
8. Mock background of flat blocks → put the HUD over a **real Play capture** (`capture_game_view --source camera`,
   HUD hidden) so the user judges it in context.

### 5.3 Frames to design (v2), one parent frame `SENDER GLEIWITZ · UI v2 · <date> · propozycja`
A HUD calm (stealth) · A2 HUD combat/alarm · B loadout picker (CS2 grid, start) · B2 armoury rifle picker ·
C weapon switch list (CS2, right side) · D photo viewfinder + "evidence captured" print · E detection states
(unaware / suspicious / searching / alarm) · F objective update sequence (new → done ✓ → disappears) ·
G pause + settings · H fail screen · I history card / briefing document (paper allowed — diegetic) · J main menu.
Annotate dynamic texts with `_dyn` in layer names. Export each frame and **show the user**; one Polish question:
approve / which changes. Do not start WS3 before the yes (user rule: the drawing first, it is cheaper to reject).

---

## 6. Unity CLI cheat sheet (main session)

PowerShell (no `python` on this machine — the Windows Store stub; use PowerShell or Node):
```powershell
$u = "$env:LOCALAPPDATA\Unity\bin\unity.exe"
$c = @("--caller","plugin","--skill","unity-cli","--result-only")
Set-Location "$env:USERPROFILE\My project"
& $u command recompile @c ; & $u command recompile_status @c          # poll until not "compiling"
& $u command console --level warning --tail 20 @c                     # errors + warnings
& $u command console_status @c                                         # counts, compilationFailed
& $u command menu --path "Kosynierzy/Setup Mission" @c
& $u command save_scene @c
& $u command eval_file --file "Temp/qa/probe.cs" @c                   # C# body with `return …;` — avoid inline eval (PowerShell eats quotes)
& $u command set_autotick --enable true --interval_ms 16 @c ; & $u command editor_play @c ; & $u command editor_stop @c ; & $u command set_autotick --enable false @c
& $u command capture_game_view --source camera --save_path "Temp/x.png" --width 1600 --height 900 @c
```
- `capture_game_view` saves under **Assets/** (e.g. `Assets/Temp/x.png`) → delete `Assets/Temp` + `Assets/Temp.meta` after.
- `--source screen` includes uGUI/IMGUI but returns a **stale frame when Unity is not focused**; `--source camera`
  is current but has no overlay UI. For UI screenshots ask the user to click into the Game view (or, with their OK,
  `unity command editor_focus`).
- Without autotick the editor doesn't advance time while unfocused; enable it only for probes, disable after.
- Menus (all idempotent): Create First-Person Player · Add Weapon And HUD · Upgrade Combat Setup · Create Placeholder
  Map · Setup Mission · Create Enemy Test Setup · Add NPC Director · Add Hiding Spots · Bake NavMesh (all floors) ·
  Setup Atmosphere (Low-Poly Dusk) · Create Pause And Settings Menu · Create Main Menu Scene · Refresh Prefab Registry.
  `Temp/setup_all.ps1` runs the core sequence (Temp is wiped when Unity closes — recreate from this list).
- C# gotchas found today: never `??` / `?.` on UnityEngine.Object (fake null in the editor); one MonoBehaviour per
  file named like the class; `FindObjectsByType<T>()` without sort mode (deprecated); old `Input` is disabled;
  reset statics with `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`; IMGUI (SuspicionIndicator, ScreenFade,
  PhotoCamera fallback) draws over uGUI; Shift+Space maximises the Unity panel under the mouse (players hit it with
  sprint+jump in the editor).
- The scratchpad path is too long for native exes — run commands from the project folder.

---

## 7. WS3 — Implement UI v2 (agent U, sonnet, after approval)

**Skills**: `unity:ui` (it routes to `unity:ui-ugui`), `unity:unity-cli` (read only — the agent does not run Unity).
**Owns**: `Assets/Scripts/UI/*`, `Assets/Scripts/Dialogue/DialogUI.cs`, `Assets/Scripts/Interaction/PhotoCamera.cs`
(UI part only), new `Assets/_Gra/Fonts/` (fonts: OFL families only; **downloading needs the user's yes** — until
then use OS fonts via `Font.CreateDynamicFontFromOSFont(new[]{"Bahnschrift","Segoe UI","Arial"}, size)`:
Bahnschrift SemiCondensed is a good condensed fallback present on Windows 10/11).
**Keep APIs**: `CombatHud` class (scene reference), `HudContext`, every data source listed in the previous agents'
contracts (`CombatFeedback`, `PlayerWeapon`, `PlayerHands`, `Inventory`, `WeaponCatalog`, `Mission` incl. `Optional`,
`GameClock`, `GameProgress`, `Detection`, `PhotoCamera`, `Dialog.CurrentLine`, `MissionFail`, `GameState`).
`LoadoutScreen.Open(LoadoutMode, Action)` and `LoadoutScreen.DrawSilhouette` are called by mission/HUD — keep.
**Deliver**: tokens in one theme file from the research synthesis; HUD per frames A/A2/C/D/E/F; loadout B/B2;
menus G/H/I/J. Main session compiles, captures each screen, compares side by side with the Figma export, fixes,
and reports with screenshots.

## 8. WS4 — Gameplay verification & tuning (agent G, sonnet, parallel)

**Skills**: `roblox-vfx` (engine-agnostic VFX rules: three phases, layering, hit-stop only on lone impacts,
trauma shake), `unity:urp-postprocessing`, `unity:initialize-ai-navigation`, `unity:physics-3d-collision`.
**Owns**: `Player/`, `Weapons/`, `VFX/`, `Stealth/`, `AI/`, `Enemies/`, `Mission/`, `Core/` (not UiTheme), Editor menus
except UI ones. The main session runs the probes (agent writes probe `.cs` files into `Temp/qa/`).
Checklist (each item: measure → change one thing → re-measure; numbers in the report):
1. ADS: for every weapon the front/rear sight line is on the screen centre (probe: project the sight points with
   `WorldToViewportPoint` → |x−0.5|,|y−0.5| < 0.005). Sprint pose: muzzle down, nothing clips the camera near plane.
2. Rifle/Erma placeholders look right in hand (camera capture); swap 1–4 + wheel; ammo per weapon persists.
3. VFX visible and layered: muzzle (scale per weapon), shells land and fade, decals on walls only, tracers, kill
   burst, ScreenFx pulses (respect camera-shake = 0). Assign `litShader`/`particleShader` on the scene's VfxLibrary
   so builds include the shaders.
4. Stealth: detection fill times (close/lit/running vs crouched/dark) feel fair; takedown prompt appears only behind
   an unaware SD (≤ 1.6 m, < 70°); bodies get discovered; civilians panic and fetch police; Schupo never shoots.
5. Readability: SD vs Schupo vs civilians distinguishable at 20 m; no "?"/"!" over civilians.
6. Pathing: NPCs reach all patrol points on the placeholder map and inside interiors; stuck repath works; note that
   NPCs can't use teleport doors (by design for now).
7. Mission flow: new game → loadout → act 1…4 → epilogue with F1; checkpoint reload mid-act restores position, clock,
   camera item; armoury gives the rifle; cable puzzle timer and fail; fail screen buttons.
8. Sounds: procedural placeholders audible and not too loud; shot noise radius per weapon works on AI.
Hand the user a "Testuj teraz" list per batch; collect feedback; tune.

## 9. WS5 — Less linear (agent G after WS4, P1)
Ideas within the story: act 1 — reach Naujocks' room via the service stairs with a key from the receptionist
(talk / steal) OR distract the guard (thrown object noise); act 2 — tail on foot OR by bicycle (`OB_Rower`,
`miasto_rower`); act 3 — fence hole (quiet) vs gate (guarded) ✔, plus the optional worker rescue; act 4 — two escape
routes. Use `ReachAnyStep`, `InteractStep`, optional tasks list. Add history card placements; side quest photographer
(P2 in the team doc).

## 10. WS6 — Team assets (whenever they land in git)
`git pull` → Kosynierzy/Refresh Prefab Registry → put `Assets/_Mapa/Mapa_Gliwice.prefab` into the scene, delete
`Mapa_Testowa` → Kosynierzy/Bake NavMesh (all floors) → Add Hiding Spots → check `Resources/dialogi.json` parses
(Dialog logs errors) → characters (`CH_*`) get the shared Animator; weapons (`OB_Pistolet_Vis` etc. with a child
"Lufa") replace placeholder viewmodels automatically (PlayerHands) — re-verify ADS alignment for each real model.

## 11. WS7 — Release (last 30 min)
Move scene to `Assets/_Gra/Sceny/Gra.unity` (Unity `move_asset` keeps GUIDs), create `Menu.unity` (Kosynierzy/Create Main
Menu Scene), both in Build Settings; test build (`unity build`, see `unity:unity-cli` skill); remind the user to record
the backup video (team doc 4:15); ask before `git push`.

---

## 12. Known risks / unverified
- Nothing visual from the last two hours was seen in Play: VFX overhaul, new HUD widgets, loadout screen, armoury,
  rifle viewmodels, ScreenFx.
- Detection tuning was changed by the AI agent (fill 0.18/1.1 per s, crouched footsteps < 2 m silent) — validate.
- `DeathEffect` is off for NPCs whose Animator has "Trafiony" (real models) → no landing VFX there.
- Builds never tried (shader stripping: Particles/Unlit, URP/Lit via Shader.Find).
- The Figma relay is a background process of the old session; if `/status` doesn't answer, restart it (§3.4).
