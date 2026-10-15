# Kosynierzy — project notes

> **Hand-over 15.10.2026: start with `docs/PLAN_SONNET.md`** (execution plan for Sonnet 5.5 + agents: UI research → Figma v2 → implementation, gameplay verification, release).


> **CAMERA = FIRST PERSON (FPP). NOT third person.** The team doc ROLA-B-UNITY.txt says TPP / Starter Assets /
> Cinemachine over the shoulder — that part is OVERRIDDEN by the user (15.10.2026). Everything else in
> docs/00-PODZIAL-I-ZASADY.txt, ROLA-B-UNITY.txt and FABULA.md is the source of truth (one hit = fail, no blood unless
> "Krew (16+)", no nazi symbols, prefab/point names).

Hackathon game, Unity 6.6 (6000.6.5f1), URP, new Input System. Team split: **we build mechanics on placeholder
objects**; other team members deliver models, sounds, the Gliwice map, story and dialogues. The GitHub repo
(MikolajczykProjects/Kosynierzy) is the final submission — everything is pushed together at the end.

## Game assumptions (from the user, 15.10.2026)
- First-person, **linear adventure across a large map of Gliwice**, driven by **tasks/quests**. **No waves.**
- Player: a Pole at the Gliwice radio station, stopping the Germans (story details: story teammate).
- Enemies are placed in the level and fight the player (patrol → spot → chase → shoot).

## Historical background (research 15.10.2026)
- **Gliwice incident, 31 Aug 1939:** a 7-man SD/SS squad led by Alfred Naujocks, posing as Silesian insurgents, took
  the German radio station (Gleiwitz, then in Germany), threatened the staff and broadcast only nine words in Polish;
  Franciszek Honiok, arrested the day before, was murdered and left as the "attacker". It was staged as a pretext
  for the invasion of Poland. → The game's premise is fiction / alternative history; keep this in mind for the intro text.
- **Site:** complex built 1934–35 by Lorenz; still has its original layout — wooden **antenna tower 111 m**, Siberian
  larch beams joined with ~16,000 brass bolts (one of the tallest wooden structures in Europe), transmitter building
  (where Honiok's body was placed), technical building (today a radio museum). The station rebroadcast Reichssender Breslau.
- Useful for level design: the tower is the landmark visible from the whole map (good quest-marker anchor).
- Sources: dzieje.pl — [provocation, "only 9 words"](https://dzieje.pl/aktualnosci/prowokacja-gliwicka-niemcy-nadali-tylko-9-slow-po-polsku),
  [tower photo/notes](https://dzieje.pl/node/28156).

## Controls
| Action | Keyboard / mouse | Gamepad |
|---|---|---|
| Move / sprint / jump | WASD / Shift / Space | left stick / L3 / A |
| Look | mouse | right stick |
| Fire / reload | LMB / R | RT / X |
| Interact | E | Y |
| Free cursor | Esc (click to recapture) | — |

Editor gotcha: **Shift+Space maximises the Unity panel under the cursor** — if all panels vanish while testing,
press Shift+Space again (or Window > Layouts > Default).

## Systems (Assets/Scripts)
- `Player/FirstPersonController` — CharacterController movement; `PlayerRespawn` — revive at last checkpoint.
- `Combat/Health` — HP for everything; damage dealers use `GetComponentInParent<Health>()` on the hit collider, so
  art models can be children without code changes.
- `Weapons/HitscanWeapon` — on the camera; magazine 8, reserve 48, damage 25, 300 rpm, semi-auto.
- `Enemies/EnemyController` — NavMeshAgent; Idle/patrol → Chase → Attack (aim telegraph 0.4 s, hit chance 0.85→0.3
  over 15 m) → Dead. `RadioStation` — optional building with Health (not used by quests yet).
- `Interaction/Interactable` + `PlayerInteractor` — "[E] prompt", UnityEvent `onInteract`, singleUse, hideOnUse.
- `Quests/QuestManager` + steps: `ReachLocationObjective` (trigger), `InteractObjective`, `CollectObjective`,
  `KillObjective` (empty list = all enemies alive at step start). Steps run in list order; each has title,
  description, marker, checkpointOnComplete, onStarted/onCompleted UnityEvents. Completing a step sets the checkpoint.
- `UI/CombatHud` — placeholder IMGUI HUD (quest, marker + distance, [E] prompt, ammo, HP, banners, death text).

## How to add a story step (for the story/level team)
1. Create an empty GameObject where the step happens, add one objective component, fill Title/Description.
2. ReachLocation: add a BoxCollider, tick Is Trigger, size it. Interact/Collect: drag the Interactable(s) in.
3. Drag the step into `QuestManager > Steps` in the order of the story.
4. Extra effects (open gate, start dialogue, activate enemies) → `onStarted` / `onCompleted` events.

## Story systems (team doc → code)
- `Core/Points` — `Points.Get/Position/Rotation/Series("miasto_latarnia")`; reads children of any object named "Punkty".
- `Core/Spawner` + `Resources/PrefabRegistry` — `Spawner.Spawn("CH_Zofia", "ksiegarnia_zofia")`; missing prefab = grey
  placeholder. Run *Kosynierzy/Refresh Prefab Registry* after teammates push prefabs.
- `Dialogue/Dialog` — `Dialog.Play("a1_zofia", onEnd)`, `Dialog.Bark(id, pos)`; data `Resources/dialogi.json` (team A),
  audio `Resources/Audio/<audio>`; Space/E next line.
- `Interaction/PhotoCamera` (F) + `Evidence`; `DoorTeleport` (interiors at x ≥ 1000, black fade).
- `Mission/Mission` — acts 1–4 as code steps (Dialog/Reach/Talk/Interact/Photo/CrouchListen/FollowCar/Cable),
  checkpoints per act (`Checkpoints`), clock `GameClock`; dev keys F1 next step, F8 save, F9 load.
- `Core/MissionFail` — one hit / shooting innocents → failure screen.
- `Core/Noise` + `Player/PlayerNoise` — walk 4 m, run 12 m, sneak 1.5 m, shot 30 m.
- AI (`Enemies/`, `AI/`) — SD patrol/suspicion/search/alarm, Schupo, civilians, workers; `NpcDirector` spawns by points.

## Editor menu (Kosynierzy/…)
Create First-Person Player · Add Weapon And HUD · Create Enemy Test Setup · Create Quest Test Setup — all idempotent.

## Working with the editor from Claude
Unity CLI + Pipeline package (`com.unity.pipeline`, local port 7800): `unity command recompile`, `console`,
`menu --path`, `save_scene`, `eval_file --file <cs>` (path inside the project). Pass `--caller plugin --skill unity-cli`.
Play-mode checks are done by the user (faster than scripted tests).
