# UI v2 — design spec (15.10.2026, proposal awaiting approval)

Source of truth for the look: the frame specs in `tools/figma/ui_v2/` (`lib.js` tokens, `hud.js`, `menu.js`, `frames/*.js`,
icons in `icons.js`). PNG previews: `docs/design/ui_v2/png/`. Research basis: `docs/research/ui_research_2026-10-15.md`
(rules 1–15, referenced below as R#). Re-render: `node tools/figma/ui_v2/build_ui_v2.js`. Push to Figma (needs our plugin v2
running): `node tools/figma/ui_v2/push_figma.js`.

## Principles (why v1 looked AI-made → what v2 does)
1. HUD = bare text / icons with a two-layer soft shadow on the image; panels only in menus (loadout, settings rows, document). R3
2. Corner zones, empty centre: compass top-centre, objectives top-right, weapon+ammo bottom-right, visibility bottom-left,
   prompt at ~60 % height. Margins 64 px left/right, 48 px top/bottom, 8 px grid. R1, R2, R7
3. Two typefaces: **Barlow Condensed** (labels, numbers, titles, caps tracked) + **Barlow** (body sentences). Two weights in
   practice (Medium / SemiBold; Bold only for the title and the paper document). R4, R5
4. One accent per state: neutral bone `#EEEAE0`; suspicion/searching amber `#E2A93B`; alarm red `#DB4630`; hidden/safe olive
   `#93AD66`. Never two accent hues in one element. R8
5. Real SVG icons (weapon silhouettes facing right, 24 px glyphs) instead of Unicode marks; no stamps/“TAJNE”; copy 1–3 words.
6. Paper + typewriter (Courier Prime) only on diegetic objects: history card, evidence print.
7. Every frame is drawn over a real Play capture (`docs/design/ui_v2/backgrounds/*.jpg`).

## Tokens (1080p reference, scale with `Screen.height / 1080`)
| Token | Value |
|---|---|
| text / text-2 / text-3 | `#EEEAE0` / 65 % / 38 % |
| hairline | `#EEEAE0` 18 % (1 px) |
| safe / warn / danger | `#93AD66` / `#E2A93B` / `#DB4630` |
| ink (dark text on bone, key caps) | `#0C0E10` |
| menu panel | `#14120F` 62 %; card `#1A1E22` 80 %; card selected `#2A2F36` 90 % + 3 px bone strip |
| paper / paper ink / stamp | `#E4DCC4` / `#25221B` / `#B23A2B` |
| shadow (bare text) | `0 1 2 #000 72 %` + `0 0 14 #000 40 %` |
| type scale | 11/12 micro · 13–14 label (caps, tracking 14–18 %) · 16–18 small/body · 22 prompt · 26 objective · 32–36 title/time · 44 hero value · 56 ammo · 64 banner · 96–132 title screen |
| key cap | 24×24, radius 4, fill bone, dark SemiBold 15 px |

## Frames (see PNGs; names = Figma frame names)
| Id | Content | Unity owner (WS3) |
|---|---|---|
| A_hud_calm | compass, objectives, visibility (eye + state + bar), weapon block (silhouette, name, cartridge pips ≤10, 56 px count + reserve), crosshair, prompt | `Hud*.cs`, `CombatHud` |
| A2_hud_alarm | + red edge vignette, ALARM badge, low-ammo amber, `R Przeładuj`, hit marker, target tag, popup | `HudCombat`, `HudDetection` |
| B_loadout_start / B2_armoury_rifle | CS2 loadout columns, art-dominant cards, descriptor line, hero panel with 4 real numbers + relative bar, slot strip, primary button | `LoadoutScreen` |
| C_weapon_list | right-side list, selected = full white + strip + boxed slot digit, melee block variant | `HudEquipment` |
| D1_viewfinder / D2_evidence_print | rounded mask, 0.2–0.8 bright-line brackets, amber focus patch, counter; paper print + red “DOWÓD” stamp | `HudPhoto` |
| E_detection | 4 states: no arc / amber arc 45 % + hollow marker / amber full + filled marker / red + vignette; 26 px plate | `HudDetection` |
| F_objectives | new (amber label) → progress → done (check, strike, dim) → out; timings are proposals | `HudObjectives` |
| G1_pause / G2_settings | left stack of 52 px caps items with 4 px active bar; tab bar + 56 px rows (stepper, slider, toggle) | `PauseMenu`, `SettingsMenu*` |
| H_fail | bare centred banner on 70 % wash, red 3 px rule, 2 buttons | `FailScreen` |
| I_history_card | paper document, typewriter | `HistoryCardView` |
| J_main_menu | 132 px title, date, items, radio-tower line motif | `MainMenu` |

## Open points for the user
* Fonts: Barlow Condensed / Barlow / Courier Prime are SIL OFL; bundling needs a download into `Assets/_Gra/Fonts/`
  (needs the user's yes). Fallback without download: OS font Bahnschrift (SemiCondensed) + Segoe UI + Courier New.
* Weapon silhouettes are hand-drawn paths (`icons.js`); the team's real models can replace them later.
* Animation timings (F, banners) are proposals, not measured from the references.
