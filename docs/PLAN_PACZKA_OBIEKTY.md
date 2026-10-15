# Plan: integrate team D's objects package (`paczka.rar`) — ~40 min, main session + 2 agents

Written 15.10.2026 (Opus) for a Sonnet 5.5 session. Read with `docs/PLAN_SONNET.md` §1 (rules) and §6 (Unity CLI
cheat sheet). Another Sonnet session may be restyling the UI at the same time — see §6 "Coexistence".

## 0. What the package is (checked)
- Source: `%USERPROFILE%\Downloads\paczka.rar` (10.8 MB), already extracted to
  `%USERPROFILE%\Downloads\paczka_rozpakowana\paczka\` (75 files, nothing failed).
- `_Obiekty/` = team D's Unity folder (owner: Borys): `Modele/` 50 × `OB_*.fbx`, `Materialy/` atlas textures
  (`T_Obiekty_Atlas.png` + `_MS` metal/smoothness + `_N` normal, plus T_Mapa/T_Lista/T_Kartka/T_Plakat…),
  `Editor/BudujObiekty.cs` (+ `_P2`, `_P3`): menu **Obiekty > Zbuduj wszystko** builds 44 prefabs into
  `Assets/_Obiekty/Prefaby`, Animator controllers + clips into `Animacje/`, FX prefabs (`FX_BlyskLufy`, `FX_Iskra`,
  `FX_Dym`, `FX_FlashAparatu`, `FX_Luska`) into `Efekty/`, URP/Lit materials into `Materialy/`. An AssetPostprocessor
  (`ObiektyImport`) sets import settings only under `Assets/_Obiekty/…`. Global-namespace classes `BudujObiekty`,
  `ObiektyImport` — no clash with our `Kosynierzy` namespace. Reviewed: writes only inside `Assets/_Obiekty`.
- **Obiekty > Scena testowa** creates and OPENS a new scene (`NewSceneMode.Single`) — save our scene first and reopen
  `Assets/Scenes/SampleScene.unity` afterwards, or skip it.
- `_Obiekty/CZYTAJ.txt` lists the Animator parameters and child points for us (copied below §3).
- `blender/` = team D's sources (`obiekty.blend`, `*.py`). **Do not put them under `Assets/`** (Unity would try to
  import `.blend` through Blender). Copy to `SourceArt/Obiekty_blender/` at the repo root without `*.blend1`.
- Docs `ROLA-A-MAPA-DIALOGI.txt`, `ROLA-C-POSTACIE.txt`, `ROLA-D-OBIEKTY.txt` are new → copy to `docs/`.
  `00-PODZIAL…`, `ROLA-B…`, `FABULA.md` are identical to ours. Their "camera is third person" lines are overridden
  (we are FIRST PERSON — models are made for viewing distance; fine for the world, check the viewmodels).

## 1. Timeline (T = start)
| When | Who | What |
|---|---|---|
| T+0–8 | main | copy files (§2), import, run Obiekty > Zbuduj wszystko, Kosynierzy > Refresh Prefab Registry, check console + prefab count (expect 44), commit `obiekty D: import + prefaby` |
| T+5–25 | agent W (world, sonnet) | §3 world/mission hooks (files: Mission/*, Interaction/Evidence.cs, Interaction/DoorTeleport.cs, Core/Spawner.cs, new Editor/TeamPropsMenu.cs) |
| T+5–25 | agent H (hands, sonnet) | §4 viewmodels (files: Player/PlayerHands.cs, Weapons/PlayerWeapon.cs, Weapons/WeaponCatalog.cs) |
| T+25–35 | main | compile after each agent, fix errors, run Kosynierzy > Place Team Props, Bake NavMesh (all floors), Add Hiding Spots, save scene, camera captures (§5) |
| T+35–40 | main | "Testuj teraz" list for the user, local commit (only our paths), report with captures |

## 2. Main session — import (T+0–8)
```powershell
$src = "$env:USERPROFILE\Downloads\paczka_rozpakowana\paczka"
$dst = "$env:USERPROFILE\My project"
Copy-Item "$src\_Obiekty\*" "$dst\Assets\_Obiekty\" -Recurse -Force        # keeps the .keep file
New-Item -ItemType Directory "$dst\SourceArt\Obiekty_blender" -Force | Out-Null
Get-ChildItem "$src\blender" -File | Where-Object Extension -ne ".blend1" | Copy-Item -Destination "$dst\SourceArt\Obiekty_blender\"
Copy-Item "$src\ROLA-A-MAPA-DIALOGI.txt","$src\ROLA-C-POSTACIE.txt","$src\ROLA-D-OBIEKTY.txt","$src\_Obiekty\CZYTAJ.txt" "$dst\docs\" -Force
```
Then `recompile` + wait, `menu --path "Obiekty/Zbuduj wszystko" --timeout 300`, read console (warnings like "brak modelu"
or "root FBX ma rotację" must be listed in the report), count `Assets/_Obiekty/Prefaby/*.prefab`, run
`menu --path "Kosynierzy/Refresh Prefab Registry"` (it scans `_Obiekty` → the registry log should say ~44 prefabs).
From now on every `Spawner.Spawn("OB_…")` uses the real prefab instead of the grey cube — doors, cards, cache, lamps,
car, switchboard appear automatically. Pink materials? → note them; the script sets URP/Lit itself.

## 3. Agent W — world & mission hooks (files listed in §1 only)
Skills to load: `unity:initialize-ai-navigation`, `unity:physics-3d-collision`. Facts from CZYTAJ.txt:
`OB_Drzwi` AC_Drzwi: bool **Otwarte**, trigger **Otworz** (opens, closes after 1 s); wing has a BoxCollider; child
`PunktInterakcji`. `OB_Karta_Historii` AC_Karta idle loop, Light "Poswiata", trigger BoxCollider 0.5 m at 1 m.
`OB_Lacznica` AC_Lacznica bool **Zasilanie**; children **Kabel_1 (czerwony), Kabel_2 (czarny), Kabel_3 (biały)** each
with its own Animator AC_Kabel trigger **Przetnij**; `FX_Iskra_1..3` → `Play()`; `PunktInterakcji`. `OB_Skrytka`
AC_Skrytka trigger **Otworz**, child `Wnetrze` with OB_Amunicja. `OB_Mikrofon_Burzowy` bool **Nadaje**, point
`CelZdjecia`. `OB_Latarnia` bool **Wlaczona** (default true, flicker), Light "Swiatlo" (spot down 8 m).
`OB_Auto_SD` float **Speed** (wheels), trigger OtworzDrzwi, bool **Swiatla**; `OB_Ciezarowka` same + `Paka`.
`OB_Rower` float Speed, `Siodelko`. `OB_Mapa_Stol`, `OB_Lista`, `OB_Kartka`: point **CelZdjecia**.
`OB_Lokomotywa` Komin/FX_Dym loops. Cover: `OB_Beczka`, `OB_Skrzynia`, `OB_Worki` ≈ 1 m with BoxCollider.
Tasks (set Animator params only if they exist — `Dialog.SetBool` helper or `Character`-style param check; explicit
null checks, never `??` on Unity objects):
1. **Doors**: `DoorTeleport` already fires trigger Otworz ✔. `InteractStep.HideOnUse` (hotel room door): when the
   Animator has bool `Otwarte` → set true and keep it (do not hide the object, disable only the wing collider).
2. **Switchboard (act 3 `CableStep`)**: if `OB_Lacznica` is a real prefab, use its `Kabel_1..3` children as the three
   interactables (add Interactable + make sure each has a collider), colours **czerwony / czarny / biały** (update the
   Gruber note text + prompts), cut = SetTrigger("Przetnij") + `FX_Iskra_i.Play()` + our impact VFX; success sets
   `Zasilanie` false. Keep the current placeholder cylinders as the fallback when the prefab is missing.
3. **Microphone**: spawn `OB_Mikrofon_Burzowy` at `radio_sterownia_mikrofon` in the act-3 setup; `Nadaje` true from
   `a3_komunikat` until the right cable is cut. Optional evidence via its `CelZdjecia`.
4. **Evidence**: `Evidence.FocusPoint` → use a child named `CelZdjecia` when present. `PhotoStep`: spawn the real props
   instead of grey plates — `hotel_pokoj_mapa` → `OB_Mapa_Stol`, `hotel_pokoj_lista` → `OB_Biurko` + `OB_Lista` on it,
   `radio_kartka_honiok` → `OB_Kartka` (on the ground); `radio_kartka_grubera` (CableStep note) → `OB_Kartka` with the
   read interaction; `miasto_zagajnik_auto` → `OB_Auto_SD` parked (doors open via OtworzDrzwi) as the photo target.
   Keep the placeholder path when a prefab is missing.
5. **Cache**: `OB_Skrytka` → on use SetTrigger("Otworz"), +8 ammo after 0.6 s, hide `Wnetrze`'s OB_Amunicja.
6. **Lamps** (`StreetLamps`): when the real prefab is used, do NOT add our extra point light; drive `Wlaczona` only
   (false by day — the prefab defaults to true!). Placeholders keep our light.
7. **SD car tail** (`FollowCarStep`): set Animator `Speed` = current speed, `Swiatla` = `GameClock.IsNight`.
8. **Runtime props and NavMesh**: in `Spawner`, add `NavMeshObstacle` (carve) to spawned `OB_*` props that have a
   collider and are not doors/cards/pickups, so NPCs path around them.
9. **New editor menu `Kosynierzy/Place Team Props`** (idempotent, Undo, MarkSceneDirty; parent "Rekwizyty_OB"): place
   static set dressing at contract points so it can be baked into the NavMesh: cover `OB_Beczka/Skrzynia/Worki` cycling
   at `miasto_oslona_1..12`; `OB_Lokomotywa` + `OB_Wagon` beside `spawn_dworzec` (offset 6 m sideways, rotated along
   the platform); hotel: `OB_Lada_Recepcja` at `hotel_recepcja`, `OB_Telefon` at `hotel_telefon` (+0.9 m on a
   `OB_Stol`), `OB_Kanapa` + `OB_Lampa_Biurkowa` in the lobby, `OB_Lozko` + `OB_Szafa` in Naujocks' room; bookshop:
   `OB_Regal_Ksiazki` ×3 along the walls near `ksiegarnia_zofia`, `OB_Radio_Odbiornik`; photographer: `OB_Aparat_Studyjny`
   at `fotograf_wlasciciel`, `OB_Lampa_Ciemnia` at `fotograf_ciemnia`; radio station: `OB_Pulpit` at
   `radio_sterownia_mikrofon` (offset so the microphone stands in front), `OB_Szafa_Aparatura` ×2 near
   `radio_technika_kable`; street: `OB_Lawka`, `OB_Kiosk`, `OB_Slup_Ogloszeniowy`, `OB_Hydrant`, `OB_Kosz`,
   `OB_Wozek_Reczny` scattered along the street points (`miasto_tlum_*` offsets 3–4 m, never on a route point);
   `OB_Rower` at `miasto_rower`; `OB_Ciezarowka` at `miasto_ciezarowka`. Do NOT place mission-spawned objects (doors,
   history cards, cache, switchboard, notes, map table, list, microphone, SD car) — the mission spawns those.
   Skip silently when a point or prefab is missing; one summary log.

## 4. Agent H — hands & viewmodels (files listed in §1 only)
Facts: `OB_Pistolet_Vis` ≈ 0.21 m, **pivot in the grip**, points **Lufa** (0, 0.065, 0.172) +Z, **Uchwyt** (0, −0.012,
−0.012), **Wyrzut** (0.016, 0.066, 0.025); AC_Vis trigger **Strzal** (hammer + slide 0.16 s), trigger **Przeladuj**
(1.8 s); children `Zamek`, `Kurek`, `Spust`, `Magazynek`; FX at `Lufa/FX_BlyskLufy`, `Wyrzut/FX_Luska`.
`OB_Aparat` ≈ 0.13 m with `Obiektyw/FX_FlashAparatu`.
1. `PlayerHands.GetFirearm` real-prefab branch: hip pose so the grip (Uchwyt) sits where the placeholder grip was
   (≈ (0.15, −0.16, 0.30) camera space); **aim pose from the real sights**: find the top-most renderer bounds above
   Lufa (front sight ≈ y 0.075–0.08) — measure, then set Aim so that sight line = camera axis (verify by projecting the
   front/rear sight points with `WorldToViewportPoint`, |x−0.5|,|y−0.5| < 0.005); add our placeholder hands/sleeve
   around `Uchwyt` (real model has no hands); all viewmodel renderers `ShadowCastingMode.Off`; remove colliders ✔.
2. `PlayerWeapon`: when the viewmodel has an Animator with `Strzal`/`Przeladuj`, fire them on shot / reload start;
   Vis `ReloadTime` 1.8 s / empty 2.0 s in `WeaponCatalog` to match the clip; shell eject from **Wyrzut** when present
   (else muzzle). Keep our VfxLibrary muzzle flash; don't also play D's `FX_BlyskLufy`/`FX_Luska` (avoid doubles) —
   or play D's and skip ours, but only one.
3. Camera item: use `OB_Aparat` (if in the registry) as the held camera model with our hands; on
   `PhotoCamera.PhotoTaken` play `Obiektyw/FX_FlashAparatu` (subscribe from PlayerHands — do NOT edit PhotoCamera.cs,
   the UI session may own it). Hand-over animation uses the same model.
4. Other weapons (Browning, Nagant, rifles, knife) have no models yet → placeholders stay.

## 5. Verification (main) — numbers and captures, then the user
Camera captures (`capture_game_view --source camera`, delete `Assets/Temp` after): Vis hip + ADS (sights centred,
report the measured offset), camera item, a door before/after E, switchboard close-up, bookshop + hotel interiors with
furniture, street with cover/lamps at 15:30 and 19:30, SD car moving (wheels). Console: 0 errors, list warnings.
NavMesh: NPCs still reach patrol points (one probe). Then "Testuj teraz" (Polish, numbered): doors, cache, cables,
evidence photos (map, list, note, car), lamps on at dusk, cover crouch-hiding, Vis shooting/reloading animation.

## 6. Coexistence with the UI session
- Same Unity editor: only one session at a time may run `editor_play`/menus — check `unity command editor_status`
  (or ask the UI session via SendMessage) before Play; never `editor_stop` someone else's test.
- Files: this plan does not touch `Assets/Scripts/UI/*`, `DialogUI.cs`, `PhotoCamera.cs`. If a compile error comes from
  the UI session's files, tell that session instead of editing its files.
- Git: `git add Assets/_Obiekty SourceArt docs Assets/Scripts/Mission Assets/Scripts/Player Assets/Scripts/Weapons
  Assets/Scripts/Interaction/Evidence.cs Assets/Scripts/Interaction/DoorTeleport.cs Assets/Scripts/Core/Spawner.cs
  Assets/Scripts/Editor/TeamPropsMenu.cs Assets/Resources Assets/Scenes` — never `git add -A` while the other session
  has uncommitted work. No push without the user's yes.
