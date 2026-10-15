# UI research for "Sender Gleiwitz 31.08.1939" - what shipped war / tactical shooters actually do

Agent R, 2026-10-15. Research only - nothing in the Unity project or Figma was touched. Written for the designer who will redo
the UI in Figma (PLAN_SONNET.md WS1 -> WS2, frames A..J in section 5.3 of the plan).

## 0. How to read this (method, confidence, sources)

**Frame.** Every position below is in a 1920x1080 frame (x% = x/1920, y% = y/1080), sizes in px at 1080p. Screenshots that were
not 1920x1080 were rescaled by HEIGHT (all these HUDs scale with screen height) before measuring. cs2_hud_awp_4x3 is 1440x1080
(1:1 pixels), the rest are 16:9 or 16:10 rescaled.

**How numbers were obtained.** PowerShell + System.Drawing scripts (in the session scratchpad, not in the project):
bounding box of text pixels above a luma threshold, mean colour of the brightest pixels in a region (= text colour), pixel
sampling for flat fills, run-length scans for lists. Text sizes are given as **cap height in px (measured)** and an **estimated
font-size = cap height / 0.72** (typical for DIN-like grotesques; real fonts vary +-10 %).

**Labels used in tables**
- **[M]** measured from a saved image (file named), +-2 px / +-6 colour levels (JPEG noise). Screenshots are community uploads,
  so the HUD scale and colour settings of the uploader are unknown -> read them as "typical", not as official specs.
- **[E]** estimated by eye from a zoomed view. **[U]** unknown / not visible in any reference found.
- **[P]** design PROPOSAL of mine for our game, not something observed.

**Sources that worked.** (1) Interface In Game (interfaceingame.com) - only Counter-Strike: Global Offensive (CS:GO) of the five games;
no Battlefield V, Sniper Elite 5, Hitman 3, Hell Let Loose or CS2 entries (searched; it also has Call of Duty: WWII 46 shots,
Insurgency: Sandstorm 48 shots - not studied, see section 9). (2) Steam store API
(`store.steampowered.com/api/appdetails?appids=...`, public JSON -> official 1920x1080 store screenshots; most are HUD-less
key art, two useful: CS2 loadout screen, Hell Let Loose barracks menu). (3) Steam Community screenshot pages
(`steamcommunity.com/app/<id>/screenshots`, public) - user screenshots with the HUD visible; this is where all in-game HUD
measurements come from.

**Blocked.** Game UI Database (gameuidatabase.com) answered with a Cloudflare "verifying you are not a bot" interstitial that did
not clear by itself after several seconds - not bypassed, skipped as instructed. Mobbin is not relevant (no game UI).

**Rule note.** Reference images in `docs/research/ui/` are internal study material only (licence rule from the
ui-reference-libraries skill: layout/size/hierarchy study, never copy icons, fonts or art). Team rule "no swastikas / Nazi symbols
anywhere": I did not save any screenshot in which such symbols or a Wehrmacht cross icon are visible (rejected: Sniper Elite 5
scope shot with a swastika flag, Battlefield V frames with banners / the Balkenkreuz ticket icon, a Sniper Elite 5 poster scene);
HUD numbers from those frames were measured only where the symbol was not part of the element.

---

## 1. Counter-Strike 2 (and CS:GO radial buy menu for contrast)

Images: `cs2_hud_awp_4x3_1440x1080.jpg` (1:1 pixels), `cs2_hud_16x9_health_money_radar.jpg`, `cs2_hud_roundwon_weaponlist_killfeed.jpg`,
`cs2_loadout_grid.jpg` (official store shot), `csgo_buy_radial_menu.jpg`, `csgo_buy_radial_weapon_stats.jpg`, `csgo_killfeed_deathcard_minimap.jpg`.
Note: in cs2_hud_16x9 and cs2_hud_roundwon the uploader changed the HUD colour (pink / purple); the default team tint is yellow (T) or blue (CT). Geometry is unaffected.

### 1.1 Layout map (in-game HUD)

| Element | Anchor | Position (1080p) | Size | Source |
|---|---|---|---|---|
| Radar (round) | top-left | x 20..270 (1.0 %..14.1 %), y 25..275 (2.3 %..25.5 %) | 250 px circle, thin 1-2 px ring in tint | [M] cs2_08 |
| Map-area label under radar | under radar | x ~109, y 274 | cap 13 px (~18 px font), tint colour | [M] cs2_15 |
| Round timer + score strip | top-centre | timer x 925..990, y 11..31 | cap 20 px (~28 px); team avatars 5+5 tiles ~48 px in a row left/right of the scores | [M] cs2_08 / [E] cs2_15 |
| Money | bottom-left | x 21..190 (1.1 %..9.9 %), y 1020..1048 | cap 27-28 px (~38 px), green cart icon right of it | [M] cs2_08/15 |
| Health (big) | bottom-centre, left of emblem | x 452..508 in the 4:3 frame (= 268 px left of screen centre), y 1022..1054 | cap 32 px (~44 px), 3 px underline bar = HP | [M] cs2_15 |
| Armour (small) | left of health | x 416..438, y 1019..1045 | 22x26 px shield icon with number inside | [M] cs2_15 |
| Team emblem | bottom-centre, ON the centre line | centre x 50 %, y 1006..1060 | ~80x54 px (hex badge with wings), plus a 1-2 px hairline through it (measured >= 334 px long inside a clipped region, so the real strip is longer): the line ties health, emblem and ammo into one strip | [M] cs2_15 |
| Ammo (mag / reserve) | bottom-centre, right of emblem | mag x 932..975 (4:3) = 212 px right of centre, y 1023..1052; reserve x 1007..1027 | mag cap 29 (~40 px), reserve cap 19 (~26 px), underlined | [M] cs2_15 |
| Weapon list | right edge, lower third | slot digits x 1424..1430 of 1440 (right margin ~10 px in 4:3, ~17 px in 16:9), digits at y 796 / 866 / 936 (73.7 % / 80.2 % / 86.7 %) | row pitch **70 px**; selected row: icon 132x26 (AWP) at y 806..832 + name line cap 14 px at y 843..857; unselected: icon only 41x26 (pistol) .. 93x37 (knife), tinted ~65 % | [M] cs2_15 |
| Kill feed | top-right | right edge ~55 px inside the frame (4:3), rows at y 74 / 105 / 136 | row text 19 px tall, **pitch 31 px**, each row a dark translucent band (#241F19 blended); the player's own kill gets a ~2 px red outline (#A40F11 in CS:GO) | [M] cs2_15 |
| Warm-up / state banner | centre, lower-middle | x 585..855 (4:3), y 783..815 | text cap 13 px (~18 px) white, thin 2 px brackets in tint at both ends, dark translucent fill | [M] cs2_15 |
| Round-won banner | top-centre third | [E] y ~17-25 % | "ROUND WON" wide-tracked condensed caps ~32 px, « » chevrons both sides, 11-12 px sub-line "cookie threw 3 grenades that round."; MVP strip is a full-width blue gradient band ~75 px tall | [E] cs2_18 |
| Pick-up toast | lower-centre | [E] y ~82 % | "You picked up a AWP | Acheron" one-liner, 1 px side brackets, no fill | [E] cs2_18 |

### 1.2 Loadout screen (this is the "5x3 grid" of the brief)
`cs2_loadout_grid.jpg`, official 1920x1080 store shot. The 3-column x 5-row grid is the **Loadout** tab (per side: Starting pistol /
Mid-tier / Rifles). The in-game buy menu I could document is CS:GO's **radial** wheel (below); I could not confirm any 5x3 grid for
the in-game CS2 buy menu itself - treat the Loadout screen as the grid reference.

- Card (weapon tile): **160 px wide**, pitch 176 px horizontally (gap 16), **row pitch 102 px** [M] (underline rows at y 199 / 301 / 403 / 505); card art sits on a barely lighter fill; **a 1 px rarity underline** closes each card (colours measured: #4B6AFF, #8947FF, #D42BE6, #FF4B54-ish red, light-blue/white for common).
- Column header: filter-funnel icon + condensed caps, cap 12 px (~16 px font), warm gold #F8D35E [M].
- Inventory grid below: **8 columns**, card 166 px wide, **pitch 192 x 190 px** (gap 26), weapon art 166x121 on a flat grey gradient (#5D5C5A), **4 px rarity bar** under the art, then a two-line caption: weapon name bold cap 9 px (~12-13 px), skin name regular, #E2DFDE [M].
- Top tab bar (INVENTORY / LOADOUT / PLAY / NEWS): condensed caps, cap 19 px (~26 px), tabs at x 736 / 874 / ..., active tab tinted cyan #8FF5FE; bar fill #292421 [M]; home / settings / power icons far left.
- Sides: T side gets a gold wash, CT a red wash behind the character - **side identity = background tint, not a different layout**.
- Weapon art is always **right-facing, large, no frame around it**; the frame is the card.

### 1.3 CS:GO radial buy menu (contrast)
`csgo_buy_radial_menu.jpg`, `csgo_buy_radial_weapon_stats.jpg` (1920x1080 from the 3840x2160 originals).
- Wheel ~611 px diameter [E] centred at (654, 532), 6 wedges, ring #333333, fill #151515, selected wedge lit by a white-to-transparent wedge gradient (#CCCCCC at the lit edge) [M]; hub with team logo; slot digits 1-6 at 12 px.
- "Buy Time Remaining 00:56": x 469..849, y 103..129 (**white, wide-tracked, no panel**), money top-left "$10000" cap 28 [M].
- Right pane: weapon name on a **horizontal gradient bar** (#696156 -> #312C28), 28 px tall, text cap 14 px; weapon photo; then a stat table: tiny uppercase tracked labels (cap 9 px ~12 px font, #DBD8D8), right-aligned values, **flat teal bars** (fill ~#234444..#2E7F7C) of one 15-18 px height - not four heavy bars, a calm table.
- Prices in mint green **#79CB79**, cap 17 px. Footer hint bar "[ESCAPE] BACK ... [F4] RE-BUY PREVIOUS [F3] AUTO BUY": cap 18 px, white, x 82 (4.3 %), bottom 3.7 %.

### 1.4 Type / colour / density / motion / icons
- **Type:** Stratum 2 + condensed cut (see fonts); digits big and light-weight, labels tiny caps; **all HUD numbers bare (no panel)**, underlined by a coloured bar instead.
- **Colour:** a SINGLE tint drives the whole HUD (yellow T / blue CT; money mint, cart green); neutral white for names; red only for own-kill outline and low HP (#A40F11 outline [M]). Panels only: kill-feed rows, team strip, warm-up bar (all ~#241F19-#282520 blended, about 60 % opaque [E]).
- **Density calm:** radar, score strip, money, health/armour, ammo, weapon list = **6 zones**; combat adds kill feed + hit markers = 7-8 [E].
- **Motion / timings:** [U] not measurable from stills; weapon list is shown only after a switch (behaviour known from play, timing not measured).
- **Icons:** weapon silhouettes solid, flat, **facing right**, 26 px tall for the selected row; rarity shown as a colour bar, not a badge; no outline icons in the HUD.
- **Why it does NOT look AI-made:** one hue for the whole HUD; tiny caps labels vs big digits (value:label cap ratio ~2.5:1); a centre-anchored strip with a hairline instead of boxes; weapon silhouette art instead of drawn rectangles; kill feed rows only as wide as their content; no decoration characters, no "stat dashboards".

---

## 2. Battlefield V

Images: `bfv_hud_infantry_weaponlist.jpg`, `bfv_score_popup_bare_text.jpg` (Polish/Spanish-localised community shots). Ticket bar numbers from a third frame (not saved - Wehrmacht cross icon in it).

### 2.1 Layout map

| Element | Anchor | Position (1080p) | Size | Source |
|---|---|---|---|---|
| Match timer | top-centre-left | x 761..818 (39.6 %..42.6 %), y 57..73 (5.3 %..6.8 %) | cap 16 px (~22 px) | [M] bfv_13 |
| Squad / spawn counter | top-centre | icon + "3" x ~920..980, label "RECUAR" x 929..990 y 86..98 | label cap 12 px (~17 px) light caps | [M] bfv_13 |
| Tickets | top-centre-right | target icon + "300" x 1126..1167, y 55..74 | cap 19 px (~26 px), dim #C5CBD2 | [M] bfv_13 |
| Ticket bars (conquest) | top-centre | x ~553..1366, y ~58; below them the sector row A B C D E F G as 20 px circles | timer cap 16 (31:57), ticket digits cap 16 | [M] 3rd frame |
| Minimap | bottom-left | x 65..288 (3.4 %..15.0 %), y 788..1028 (73.0 %..95.2 %) | **223x240 px**, translucent dark with 1 px frame | [M] bfv_13 |
| Squad list | under minimap | x ~300..520, rows pitch ~33 px at y ~778 / 811 / 844 | name text ~15 px, 24 px role icon in a circle left | [E] bfv_13 |
| Health / gadget | bottom-centre | x 903..1044, y 1005..1030 | health "100" cap 18 dim #A19E9A, shown dim when full | [M] bfv_13 |
| **Weapon list** | bottom-right | x 1632..1855 (85.0 %..96.6 %), y 698..985 (64.6 %..91.2 %) | **223 px wide, rows 57.5 px**, 5 rows (gadget T + slots 1..4); 22x22 px slot badge at the right end (x 1821..1843); ammo count left of icon, ~20 px | [M] bfv_13 |
| Ammo | bottom-right under list | bullets icon x ~1640, mag "31" x 1696..1736 y 1000..1032, reserve "155" next, hairline divider x ~1779, grenade icon + "1" x ~1810..1845 | mag cap **32 px (~44 px font)**, reserve cap ~20, colours #FFFEF9 / dim #928576..#C4B9AB | [M] bfv_13 |
| Score popup | centre, 40 % down | title x 827..1090 (centre 958 = 50 %), y ~432..458; value "50" x 939..987 y 697..725; caption x 850..1039 y 732..749 | title cap ~24 [E] wide-tracked light caps white #FFFFFC; value cap 28 (~39 px) white; caption cap ~12 warm grey #C1B7A7 | [M] bfv_06 |
| Chat | top-left | the only filled panel: grey translucent rows ~24 px pitch | text cap ~13 | [E] 3rd frame |

Margins: minimap left 65 px = weapon list right 65 px (symmetrical 3.4 %); minimap bottom margin 52 px (4.8 %).

### 2.2 Weapon list details (frame C reference)
`bfv_hud_infantry_weaponlist.jpg`: a **vertical stack**, active row = light fill (#AEA9A3) with dark glyph and an inverted slot badge (dark square, light digit), inactive rows = dark translucent fill with light glyph (#7B7167 sampled blended over sand, true alpha [U]). Row heights equal, no gaps (rows touch, separated by a 1-2 px darker line). Silhouettes face right, flat, one colour.

### 2.3 Type / colour / density / motion / icons
- **Type:** light-weight grotesque with DIN-like proportions; caps for labels, tabular digits; value:label cap ratio 28:12 in the popup.
- **Colour:** nearly everything neutral white/warm grey; blue (#558095 blended, probably ~#3B8DBF [E]) only for friendly squad / "your team", red for enemy; green only for the "heal" icon. No accent colour on the HUD chrome.
- **Panels vs bare:** bare = timer, tickets, ammo, health, popup, squad names; panel = weapon list rows, minimap, chat. Roughly 3 panel elements vs 7 bare elements [E].
- **Density calm:** ~8 zones (timer, tickets, minimap, squad list, health, weapon list, ammo, objective marker) [E]; this is the busiest of the five.
- **Motion:** [U] (popups fade in from below and stack; timing not measurable).
- **Icons:** flat solid silhouettes, 22 px badges for key numbers, circular role icons (squad) 24 px.
- **Why it does NOT look AI-made:** the score popup is bare wide-tracked light caps + big number + tiny caption with no box; the weapon list uses *inversion* (light row = active) instead of an extra glow/border; strict 65 px symmetric margins; icon silhouettes of the real weapons.

---

## 3. Sniper Elite 5 (closest genre match: WWII stealth, takedowns, tagging, field report)

Images: `se5_hud_melee_kill_popup.jpg`, `se5_hud_keycap_hint_pl.jpg` (Polish UI!), `se5_hud_climb_prompt.jpg` (Italian UI), `se5_scope_view_hud.jpg`, `se5_field_report_document.jpg`.

### 3.1 Layout map

| Element | Anchor | Position (1080p) | Size | Source |
|---|---|---|---|---|
| Circular minimap + awareness mark | bottom-left | left edge x 114 (5.9 %), spans y ~774..1002, **~224 px circle** | dark disc, thin ring, amber double-chevron "rank / awareness" marker at its centre | [M] se5_27 |
| Health ring | bottom-right cluster | x ~1344..1428, y ~936..1020 | ~84 px ring [E]; number inside cap 16 px [M] (digits x 1373..1396) | [E]/[M] se5_27 |
| Vertical hairline divider | between health and ammo | x ~1445 | 1 px, ~60 px tall | [E] |
| Ammo (mag / reserve) | bottom-right | mag x 1512..1542 (78.8 %), y 945..986; reserve "19" x 1556..1576 y 971..986 | **mag cap 41 px (~57 px font)**, reserve cap 15 (~21 px) | [M] se5_27 |
| Weapon silhouette + gadget icon | right of ammo | pistol + gadget group x 1714..1821 (89.3 %..94.8 %), y 948..979 [M se5_27]; with a rifle the silhouette spans ~x 1608..1764 [E se5_03] | solid white, barrel to the right: pistol ~44x24, rifle ~156x26 [E]; gadget icon ~36x30 at x ~1784..1820 with a count (cap ~10) | [M]/[E] |
| Segmented bar (stamina / noise) | under weapon | x 1589..1840, y 998..1016 | **251x18 px, 4 segments**, #D7D6D4 | [M] se5_27 |
| Hint line | bottom-centre | key-cap x 781..804, y 1005..1030 (93 %); label x 826..1016 | key-cap **23x24 px**, cream #F6F4E8 with dark glyph; label cap ~18 (~20-22 px), #F6F3EC | [M] se5_27 |
| Context prompt | just below screen centre | key-cap x 930..990 (58x24 wide key "SPAZIO"), label x 1009..1110, y 770..795 (71 %) | label cap 21 incl. descender | [M] se5_11 |
| **Kill / XP popup** | right, lower | amber plate x 1434..1829, y 807..837 (74.7 %..77.5 %); sub-lines right-aligned to x ~1842: line 1 y 832..856, line 2 y ~862..880 | plate **30 px tall**, fill ~#FFB556 / #FACF72 (two shots), dark text; XP value in light amber #FCC267 cap 25; sub-lines white #FCFCF8 cap ~16, second sub-line dimmer (#A3A4A4) | [M] se5_03 / se5_20 |
| Awareness label ("Enemy Hunting" / "Searching") | left edge above minimap | x 71..176, y 734..760 | **105x26 px** amber plate (#DB9C03..#F0A500), dark text cap ~13 | [M] se5_28 (frame not saved - poster scene) |
| World markers | at enemies | amber diamond with double chevrons (awareness), amber "?" at screen edge, red outline around tracked enemy | ~24-30 px | [E] se5_03/27 |
| Scope overlay | centre | thin black reticle lines, range ticks across the top (~240 px wide), zoom readout "x4" right of the lens x 1569..1630 y 517..560 (cap ~30 px) with +/- ticks above/below | [M]/[E] se5_20 |

Margins: ammo cluster right margin 80 px (4.2 %), minimap left 114 px (5.9 %), bottom ~78 px (7.2 %) - SE5 uses the biggest margins of the five.

### 3.2 Field report / debrief document (frame I reference)
`se5_field_report_document.jpg` (16:10 original; numbers on a 1728x1080 normalised copy): the **chrome is clean UI**, the **content is a paper
book**. Chrome: title "OCCUPIED RESIDENCE" x 590..1139 y 58..92 -> cap **34 px (~47 px font), light weight, caps, tracking ~+0.15 em [E]**, white #F8F8F0; sub-tab "FIELD REPORT" = inverted light-grey plate (#CACBC6) 170x32 px with dark text; dark blurred scene behind (#101115); a thin full-width white hairline under the header; footer key hints at y 1010 (key-cap + bold caps label, cap 19). Paper: fill ~#BBB291 (in shade), dark text #363226, bold serif-ish headings ("Assault", "Lethal") ~28 px cap, rows = label left / number right on a dotted rule, one stamp-like black strip with the logo. A rank-up card top-right with gold-to-dark gradient (#B8A188 -> transparent) and white text.
-> The lesson for our history card / briefing: **paper only inside the document; everything around it (title, buttons, hints) stays modern, clean and neutral.**

### 3.3 Type / colour / density / motion / icons
- **Type:** [U] font not identified (looks like a light, slightly humanist grotesque for the HUD, heavier condensed for the logo); caps + wide tracking for titles, sentence case for prompts (Polish "Wł./Wył. celowanie" = 190 px at ~20 px font), tabular figures in the ammo cluster.
- **Colour:** white by default; **amber (#FFB556) = awareness and rewards only**; red outline = tracked/hit enemy; dark translucent discs for minimap/health. No coloured panel behind the ammo.
- **Panels vs bare:** bare = ammo, weapon icon, hint text, sub-lines; plates only for the *state label* (amber, 26-30 px) and key-caps (cream). Panel share ~15 % of elements [E].
- **Density calm:** 3 clusters (minimap, bottom-right ammo cluster, hint line); popup appears on events -> 4.
- **Motion:** popups appear lower-right and stack upward; **timings [U]**.
- **Icons:** weapon = solid silhouette, right-facing; ammo/health glyphs are small solid shapes inside the ring; awareness = chevron diamond.
- **Why it does NOT look AI-made:** the HUD is one tight cluster in the corner (health | divider | big ammo | silhouette | segmented bar) instead of floating boxes; reward plate is amber with dark text on a 30 px strip, not a card; prompts are a cream key-cap + sentence-case label; the debrief is a believable paper object inside a clean frame; margins are generous and equal.

---

## 4. Hitman 3 / World of Assassination

Images: `hitman3_hud_objective_completed.jpg`, `hitman3_hud_objectives_hide_prompt.jpg`, `hitman3_objective_tick_crop.jpg` (3x crop), `hitman3_item_unlock_card.jpg`.
I could not obtain a frame with detection arrows / "compromised" (none of the community shots on the first page had one) -> section 4.3 marks that as [U].

### 4.1 Layout map

| Element | Anchor | Position (1080p) | Size | Source |
|---|---|---|---|---|
| Mission timer (when shown) | top-centre | x 868..1047 (45.2 %..54.5 %), y 7..45 (0.6 %..4.2 %) | digit height ~38 px (~52 px font), bold, white, **bare** | [M] hitman3_19 |
| **Objective list** | top-left | icon column x 44..68 (2.3 %), first icon y 43..67 (4.0 %), text x 79 | icon **24x24 px** in a rounded square badge; text cap ~16 (~22 px), medium weight; **line pitch 33.4 px** (rows at y 47, 82, 115, 148, 181, 214); **bare text + soft shadow, NO panel** | [M] hitman3_07/04 |
| Done objective | in the list | same row | grey #909397 text + grey check box icon; open objective = white + red target icon (#F40210) | [M] hitman3_06 crop |
| Money / currency | top-right | x 1787..1836, y 49..65 | cap 16, white, "M" currency glyph in a white badge | [M] hitman3_07 |
| Challenge row | top-right | text x 1682..1841, y 85..100; trophy + check below | cap 15 | [M] hitman3_19 |
| **Objective banner** | upper-centre | line 1 x 823..1097, y 215..228 ("OBJECTIVE COMPLETED  +10 XP"), line 2 x 891..1030, y 248..261 ("UNNOTICED KILL") (centre 50 %, 20-24 % down) | caps cap 13 px (~18 px), white, **bare**, line pitch 33 px | [M] hitman3_19 |
| Target-eliminated kill-cam | top-left | picture x 46..~478, y ~40..309; red strip x 46..478 y 286..309 = **23 px tall, #FA000E**, white caps cap 13 | the only saturated red element | [M] hitman3_19 |
| Minimap | bottom-left | x 47..255 (2.4 %..13.3 %), y 832..1038 (77.0 %..96.1 %) | **208x206 px square**, light-grey map on dark frame (#C6CACD / #2F3336), "N" marker top-left | [M] hitman3_07 |
| Interaction prompt | world-anchored | "G EXIT MISSION" skewed text beside a 40 px key badge; "hide item" prompt in a thin skewed outline frame (no fill) at lower-centre | cap ~12 | [E] hitman3_19/04 |
| Interaction marker | on objects | teal rhombus with glyph, **#1BFDD3** | ~24 px | [M] hitman3_07 |
| Subtitles | bottom-centre | x 644..1276, y 959..979 (88.8 %..90.6 %) | cap ~14-16 (~20 px), white #F1EFED, **bare with shadow** | [M] hitman3_19 |
| Weapon / ammo | bottom-right | thin white vertical weapon silhouette (portrait, rotated) + "5 /10", small red "!" | cap ~14 | [E] hitman3_04 |

### 4.2 Item / unlock card (armoury reference)
`hitman3_item_unlock_card.jpg`: card at x 514..1662, y 188..678 (1148x490). Left square = item render on a flat #404146 panel, right panel flat **#2D3039**, page background #08090B. Right panel: 20 px light label "DISTRACTION" (cap 15, caps) over a 34 px **bold, tight-tracked** item name "GOLD COIN" (cap 25), a 1 px **dotted rule** across the card, then a 14-15 px description paragraph. Bottom strip: icon in a white rounded square (~64 px) + 20 px caps label + a **48 px bold tight-tracked title** (cap ~36 at x 173..1547) separated from the card by a 1 px dotted hairline running x 80..1839 (4.2 % margins). Footer key hints: ENTER / P key-caps (light grey rounded, 27 px) + sentence-case label. Everything is flat grey-blue, **no gradients, no glow, no borders except the hairlines**.

### 4.3 Type / colour / density / motion / icons
- **Type:** Neue Haas Grotesk (+ Noto Sans for non-Latin / numbers) per Fonts In Use (secondary source, not confirmed by the studio); tight tracking on bold titles (-0.02 em [E]), caps labels light and tracked.
- **Colour:** white text on dark; **red #FA000E only for the target / "eliminated"**; teal #1BFDD3 only for interactable objects; amber/yellow for suspicion is known from play but **not measured here [U]**; completed rows dim to #909397.
- **Panels vs bare:** objective list, banner, timer, subtitles, money = bare text with shadow; panels only for minimap (flat light square), kill-cam strip, item card.
- **Density calm:** objectives (often hidden after a few seconds), minimap, bottom-right item, prompt = 3-4 zones.
- **Motion / timings:** [U] (objective rows appear at top-left, completed ones grey + check, then the list fades; real durations not measurable from stills).
- **Icons:** 24 px rounded-square badges (filled colour for type: red target, blue, yellow, purple) with a white glyph; check = a heavier tick in a square; world markers = rhombus.
- **Why it does NOT look AI-made:** nothing is boxed; red is reserved for the target; hairline dotted rules instead of card borders; two-size contrast (tiny light caps label above a big bold title); icons are consistent badges; banner text is small and calm (18 px) rather than shouting.

---

## 5. Hell Let Loose (minimal HUD)

Images: `hll_hud_capture_banner.jpg`, `hll_hud_compass_weapon_squad.jpg`, `hll_hud_minimal_markers.jpg` (1536x864 source), `hll_barracks_loadout_menu.jpg` (official store shot).

### 5.1 Layout map

| Element | Anchor | Position (1080p) | Size | Source |
|---|---|---|---|---|
| Timer | top-centre | x 938..981, y 14..29 | cap 15 (~21 px), white | [M] hll_15 |
| **Sector ribbons** | top-centre | x ~624..1296 (32.5 %..67.5 %), y ~43..67 (4.0 %..6.2 %) | 5 chevron-ended ribbons ~132 px wide x ~20 px tall: allies blue (#4A6583 blended), axis red, **active one lit white-grey**; lock/bolt glyph inside | [M]/[E] hll_15/18 |
| Chat / orders | top-left | x ~29..394, y 66..118 | the **only filled panel**: translucent khaki; text ~14 px | [E] |
| Squad list | bottom-left | x 25..205, y ~860..1064; names cap 11 (~15 px), **pitch 23 px** | 14 px role glyph left of each name, squad header cap 16 | [M] hll_15/18 |
| Compass strip | bottom-centre | x 795..1081, y 1034..1070 (bottom margin ~10 px!) | **286x36 px**, tick labels ~10 px dim (#93918B), centre heading box "83" + cardinal letter | [M] hll_15 |
| Weapon + ammo | bottom-right | name x 1747..1832, y 1019..1033; silhouette x ~1642..1840 y ~954..986; mag digit "2" x 1869..1881 y 1014..1039; hairline between name and digit | name cap 14 (~19 px) caps, sub-name 12 px #ADA192; digit cap 25 (~34 px) | [M] hll_15/18 |
| **Capture banner** | upper-centre | tiny sector label x 886..1030 y 96..106 (cap 10, **tracking ~+0.3 em [E]**); title "ЗАХВАЧЕН" x 846..1070 y 128..170 (cap 42, ~58 px, condensed bold #DBDDE0); second line x 673..1246 y 216..262 (cap ~40); countdown x 928..991 y 281..300 (cap 19) | hairline rules between lines; **no panel** | [M] hll_15 |
| World markers | on units | white 12-16 px glyph in a small blue circle + name (cap ~10) above | [E] hll_21 |

### 5.2 Type / colour / density / motion / icons
- **Type:** [U] font unknown (looks like a DIN-condensed / Bebas-class display face with Cyrillic support); big banner in bold condensed caps, tiny tracked caps for the label above, sentence-case for the countdown.
- **Colour:** neutral white + muted team colours only on the ribbons; **no health bar, no crosshair, no minimap on screen in these frames** - information lives in the world (bandages, hit marker) and on the map screen.
- **Panels vs bare:** everything bare except chat.
- **Density calm: 5 zones** (ribbons+timer, chat, squad, compass, weapon). Elements on screen in total ~25 small items but only 5 groups, all at the edges; the **centre 50 % of the screen is empty**.
- **Motion:** [U].
- **Icons:** weapon silhouette (check: HLL bazooka/rifle icons point left-to-right? **[U] direction not verified**), role glyphs 14 px solid.
- **Why it does NOT look AI-made:** extreme restraint, no health UI at all; one huge condensed banner line with a tiny tracked label above and hairlines between lines; compass hugs the very bottom edge as a calm strip.

---

## 6. Cross-game comparison (all measured, 1080p)

| Metric | CS2 | BFV | SE5 | Hitman 3 | HLL |
|---|---|---|---|---|---|
| Left margin of bottom-left block | 20 (1.0 %) | 65 (3.4 %) | 114 (5.9 %) | 47 (2.4 %) | 25-56 (1.3-2.9 %) |
| Right margin of bottom-right block | 10-17 (0.5-0.9 %) | 65 (3.4 %) | 80 (4.2 %) | 58-79 (3.0-4.1 %, top-right) | 39 (2.0 %) |
| Bottom margin | 32 (3.0 %) | 52 (4.8 %) | 78 (7.2 %) | 42 (3.9 %) | 10 (0.9 %) compass |
| Minimap | 250 round | 223x240 | 224 round | 208x206 | none |
| Primary value cap height (ammo/health) | 29-32 | 32 | **41** | timer 38 | 25 |
| Primary value font-size (est.) | 40-44 | 44 | **57** | 52 | 34 |
| Secondary value cap | 19 | ~20 | 15 | - | - |
| Label / caption cap | 13 | 12 | ~13-18 | 13 | 10-14 |
| List row pitch | weapons 70, kill feed 31 | weapons 57.5, squad 33 | - | objectives 33.4 | squad 23 |
| Icon badge | 22x26 shield | 22 slot badge | 23x24 key-cap | **24x24 badge** | ~14 |
| Banner / popup text | wide-tracked caps ~32 | caps ~33 + value 39 | amber plate 30 tall | caps 18, pitch 33 | 58 title + 14 tracked label |
| Calm HUD zones | 6 | 8 | 3 | 3-4 | 5 |
| Panels used | kill feed, strip, banner | list, minimap, chat | plate, key-caps | minimap, strip, card | chat only |

Reading it: primary digits sit at 44-57 px, labels at 16-20 px; margins 3-6 % except CS2; only SE5 / BFV / Hitman keep the minimap square or round at ~210-240 px.

---

## 7. SYNTHESIS for our game (1939, first person, stealth, low-poly)

Numbers are 1080p. "Evidence" cites the games above; **[P]** = my proposal where the references do not decide.

### 7.1 Fifteen rules

1. **Corner HUD, empty centre.** Calm HUD = max **4 zones** (objectives top-left, ammo cluster bottom-right, prompt line bottom-centre, optional photo/minimap bottom-left); a 4th/5th element only on events. Centre 40 % stays empty except the crosshair dot. (HLL 5 zones, SE5 3, Hitman 3-4.)
2. **Margins on the 8 px grid:** left/right **64 px (3.3 %)**, top/bottom **48 px (4.4 %)**; the bottom-left and bottom-right blocks share the same margin (BFV 65/65, Hitman 47/42). Do not hug the edge like CS2 (20 px) - it reads cramped for a calm stealth game; SE5's 114 px (bottom-left) wastes screen space.
3. **Bare text first.** Numbers, objective lines, popups, subtitles, prompts = bare text with a two-layer shadow `0 1 2 rgba(0,0,0,.85)` + `0 0 10 rgba(0,0,0,.45)` [P from Hitman/BFV/HLL look]. A panel is allowed only where a list has >= 3 rows or content is dense (loadout grid, armoury card, document, kill/event list). **One panel style per screen**, fill `#14120F` at 62 % [P; CS2 #241F19-#282520 blended, Hitman #2D3039 solid].
4. **Type scale (px, 1080p):** 12 / 14 / 18 / 22 / 32 / 44 / 56 / 64 -> 12 slot digits & footnotes; 14 tracked caps labels; 18 labels & objective lines; 22 prompts & body; 32 secondary values; **44 primary value (ammo/health/timer; measured 44-57)**; 56-64 banner title (HLL 58). Two weights only: Regular/Medium and SemiBold/Bold.
5. **Case & tracking:** titles and labels = CAPS; light weight titles tracked **+0.15..+0.25 em** (BFV popup, SE5 header, HLL sector label); labels +0.06..+0.10 em; digits 0; prompts and subtitles = sentence case, tracking 0. (Polish text is ~20-30 % longer than English - keep 25 % slack in every box.)
6. **Value over label:** every value has a tiny label; cap-height ratio **2.3:1** (BFV 28:12, CS2 32:13). Pair them tight (label 4-8 px above/under, left-aligned to the value's first digit).
7. **8 px spacing grid:** gaps 8 / 16 / 24 / 32 / 48 / 64; row pitches 32 (objective lines; Hitman 33.4), 64 (weapon rows; CS2 70, BFV 57.5), 96 (loadout card row; CS2 102), 192 (item grid column pitch, CS2 192 = 24 x 8).
8. **Palette tokens.** Neutrals: `ink #0E0F12`, `panel #14120F@62%`, `text #F4F1EA` (measured family #F6F3EC / #FFFFFA / #F7F6EF), `text-dim #9A9B9C` (Hitman done #909397; BFV caption #C1B7A7), `hairline #FFFFFF@28%`. **One accent per state:** neutral (calm) = white only; **suspicion = amber `#FFB556`** (SE5 measured, dark text #1A1206 on it); **danger/alarm = red `#E0261B`** (Hitman strip measured #FA000E - pull slightly down for 1939 grit) [P]; **safe/hidden/interact = olive-teal** (Hitman interact teal #1BFDD3 measured; olive `#9DB36A` [P] for "hidden / safe"). Never two accent hues in one element; never accent on idle chrome. CS2 proves the single-tint principle (whole HUD one hue).
9. **Key-cap prompt:** cream `#F6F4E8` cap, dark glyph, **23x24 px**, radius 3, label 20-22 px sentence case with 8 px gap (SE5). Placement: contextual prompts at ~**71 % of height** just right of centre (SE5 y 770), persistent hints on the bottom line at **93 %** (y 1005..1030).
10. **Icon grid:** HUD icons on a **24 px grid, 2 px stroke or solid fill, never mixed in one row**; 16 px for inline/legend; objective badges = **24x24 rounded square (radius 4)** filled in the state colour with a white glyph (Hitman); weapon silhouettes **solid, right-facing, 26 px tall** (CS2 measured 26), width by aspect (41-132 px), selected = 100 % opacity, others 60-65 %.
11. **Weapon list (frame C):** bottom-right, right-aligned, vertical stack, **row pitch 64**, slot digit 12 px at the far right (x = 1856), silhouette left of it, **selected row has the weapon name (18 px caps) under the icon and full brightness**, others dim and **without a panel**; list occupies y 74 %..88 % [CS2 73.7..86.7 %]. Appears on switch / scroll only.
12. **Loadout / armoury grid (frames B, B2):** card **160 x 96** (CS2 160 x ~98) on a 176 x 104 pitch (gap 16 / 8), 1 px rarity-style underline in the weapon-class colour only (do not invent rainbow rarities - our game has 3 pistols / 2 blades / 3 rifles: use 3 neutral tints + amber for "selected"), weapon art large and right-facing, caption = name bold 14 / variant regular 14 dim. Header per column: 16 px condensed caps in dim text with a small funnel/diamond icon; top tab bar 26 px caps with active tab in the accent. No stat bars - a 2-3 word descriptor line under the name (e.g. "CICHY - 8 NABOI").
13. **Objective list (frame F):** top-left at (64, 48); icon 24x24 at x 64, text at x 104 (icon 24 + 16 gap), **pitch 32**, text 18 px; open = white + accent icon; done = `#909397` + check icon. New objective = 2-line centred banner at y ~20-24 % (caps 18 px, pitch 33 as Hitman, bare). **Timings [P, not measured]:** banner in 0.25 s, hold 2.5 s, out 0.4 s; tick: icon swaps to check with 0.15 s scale pop, text dims over 0.3 s, row collapses after 1.2 s over 0.4 s.
14. **State label for detection (frame E):** 26 px high plate, caps 13-14 px dark text, fill amber (suspicious / searching) or red (alarm), placed bottom-left above the optional minimap or under the crosshair; plus a world marker 24-30 px (diamond with chevrons = SE5). Four states [P]: unaware = nothing; suspicious = hollow amber "?" marker; searching = filled amber marker + plate; alarm = red marker + red edge vignette 8 % + the plate. Detection fill = a thin 2 px arc around the marker, not a bar.
15. **Paper only where it is an object (frame I) and menus stay clean (G, J):** history card / briefing = paper `#BBB291` with dark text `#363226`, typewriter/stencil faces only inside it; title + footer hints + buttons around it in the UI face (SE5 field report: title cap 34, tracked light caps; inverted tab 170x32; footer key hints at y 1010). Menus = top tab bar (CS2: 26 px caps, bar `#292421`, 1 accent for active tab) not a column of heavy buttons; dividers = 1 px dotted/hairline (Hitman), no card borders.

### 7.2 Spacing, grid, sizes at a glance
- Grid 8 px; safe margins 64 / 48; HUD block widths multiples of 8: ammo cluster ~ 320 x 64, weapon rows 224 x 64 (BFV 223), minimap (if used) 208-224 square or round, objective list 360 wide.
- Photo viewfinder (frame D) [P from Hitman camera icon + SE5 scope]: corner brackets 2 px, 48 px long, inset 120 px; centre cross 24 px; bottom bar with 18 px caps "ZDJĘCIE 1/5" at y 93 %; confirm print = bare text popup (BFV-style: 24 px tracked caps + 28 px value + 12 px caption) at 40 % height.
- Main menu (J) [P]: left-aligned stack at x 128, y 40-70 %, items 32 px caps, 48 px pitch, tab bar optional; Hitman card style for the "Nowa gra" tile (flat `#2D3039` panel, dotted rule). Pause + settings (G): CS2 tab bar + rows with label left / value right on 1 px rule, 56 px row height [P].
- Fail screen (H): bare banner (HLL pattern: tiny tracked label 14 px, title 58 px condensed, hairline, countdown 22 px) centred at y 12-28 %, on a 70 % black wash - no card.

### 7.3 DO / DON'T
**DO:** bare text with shadow for all HUD numbers; one tint/accent per state; big digit + tiny label pairs; right-facing solid weapon silhouettes; condensed display face + a neutral grotesque for prompts; 1 px hairlines and dotted rules as structure; cream key-caps; invert (not glow) for selected rows; keep the centre empty; real artefacts (paper, stamps) only inside diegetic documents; test with Polish diacritics (ąćęłńóśźż) and 25 % longer strings; show it over a real Play capture.
**DON'T:** put every element on the same translucent rectangle; use one font for everything; use Unicode decoration (◆ ✓ ○ ·) as icons; add explanatory sub-titles ("Wskazówka:"); draw weapons from rectangles; use equal-weight symmetric cards with four identical stat bars; use the brass/gold accent everywhere; use stencil/typewriter faces in the HUD; hide the scene behind menus when a blur of the scene works (SE5 debrief, CS2 tabs); stack more than 4 persistent zones.

### 7.4 Table: our v1 problem -> fix (from PLAN_SONNET section 5.2)

| # | v1 problem | Evidence in references | Fix (numbers) |
|---|---|---|---|
| 1 | Every element on the same translucent box | Hitman objectives, BFV popup, HLL banner, CS2 money/health/ammo are bare; panels only for dense content | bare text + 2-layer shadow; panel `#14120F@62%` only for loadout grid, event list, document (rule 3) |
| 2 | One neutral font, all bold caps | CS2 Stratum 2 + condensed; Hitman grotesque + bold/light contrast | condensed display (Barlow Condensed / Saira Condensed) for labels/titles/numbers, neutral grotesque (Inter Tight / Archivo) for prompts and body; 2 weights; caps only for labels/titles |
| 3 | Unicode decorations and middle dots | all five use real glyph badges | 24 px grid SVG icons, 2 px stroke or solid, badge radius 4 (rule 10) |
| 4 | Over-explaining copy, "TAJNE" stamp | references use 1-3 word labels; the only extra text is a tiny caption | labels 1-3 words; max one 12-14 px caption under a value; stamps only inside paper documents |
| 5 | Weapon silhouettes from rectangles | CS2/BFV/SE5/HLL all show proper right-facing silhouettes | trace real models or render our viewmodels flat white; 26 px tall; right-facing (rule 10) |
| 6 | Equal-weight cards, four identical stat bars | CS2 loadout: art-dominant card + 2-line caption; Hitman card: big name, tiny label | 160x96 cards, hierarchy name 14 bold / variant 14 dim, 2-3 word descriptor, no stat bars (rule 12) |
| 7 | Brass accent on everything | CS2 single tint, SE5 amber only for awareness/XP, Hitman red only for target | one accent per state: white / amber / red / olive-teal (rule 8) |
| 8 | Mock background of flat blocks | all references are judged over real scenes | put each frame over a real Play capture, HUD hidden |
| 9 | (additional) Symmetric, centred layout | HUDs are corner-anchored and asymmetric (SE5 cluster, BFV list, CS2 centre strip with hairline) | corner clusters + empty centre (rule 1), one centred element max (banner) |
| 10 | (additional) Unequal margins / arbitrary sizes | measured margins 3-6 %, row pitches 32-70 | 8 px grid, 64/48 margins, pitches 32 / 64 / 96 (rule 2, 7) |

---

## 8. FONTS

### 8.1 What the games really use (secondary sources - none is an official studio statement)
| Game | Typeface | Confidence |
|---|---|---|
| CS:GO / CS2 | **Stratum 2** (Process Type Foundry, commercial) incl. a condensed cut for headlines; Noto Sans for non-Latin/long text | medium - Fonts In Use + Steam community thread; a design-blog source declines to name it |
| Hitman (WoA) | **Neue Haas Grotesk** for most UI, **Noto Sans** for numbers/non-Latin | medium - Fonts In Use; IO Interactive has not documented it |
| Battlefield V | not confirmed; the franchise UI face since BF3 is reported to be **Purista** (and BF2042 still) | low for BFV |
| Sniper Elite 5 | [U] not found; looks like a light grotesque (HUD) + heavy condensed (logo) | unknown |
| Hell Let Loose | [U] not found; looks DIN-condensed / Bebas-class, Cyrillic supported | unknown |

### 8.2 Closest OPEN-LICENCE alternatives (Google Fonts; Polish diacritics verified via the Google Fonts catalogue metadata: the `latin-ext` subset is present; licence is OFL unless noted - from my knowledge, re-check at download time; **nothing downloaded**)
| Role | Pick (first = recommended) | Why / notes |
|---|---|---|
| **Condensed display sans** (labels, titles, banners, numbers) | **Barlow Condensed** (100-900 + italics, static files), **Saira Condensed / Semi Condensed** (100-900; squarer, military-tech - nearest to Stratum 2 feel), Oswald (200-700, heavier, variable), Rajdhani (300-700, squared), Bebas Neue (one weight, caps only - banners), Sofia Sans Extra Condensed (variable) | all list `latin-ext` -> ąćęłńóśźż OK. Unity/TMP is easiest with **static** TTFs (Barlow Condensed and Saira have them per weight; Oswald and Sofia are variable). Suggested use: Barlow Condensed 500 labels, 600 values/titles, 300 for tracked light caps; Saira Condensed 600 as the alternative personality. |
| **Neutral grotesque for prompts / body** (Hitman-like) | Inter Tight, Archivo (variable width), Inter | all `latin-ext`; Archivo Narrow (400-700) as a compact body |
| **Tabular / mono for numbers** | IBM Plex Mono (100-700), JetBrains Mono (100-800), Roboto Mono, Space Mono, DM Mono | `latin-ext` present. Share Tech Mono and B612 Mono have **NO latin-ext** (no Polish) - digits only. For HUD digits first test the condensed family's own figures; **tabular figures (`tnum`) were not verified per font - test in Figma / TMP before committing**. |
| **Stencil** (diegetic only: crates, document headers, stamps) | Black Ops One (`latin-ext`), Stardos Stencil and Allerta Stencil (**no latin-ext**: codes/numbers only, no Polish words) | never in the HUD |
| **Typewriter** (diegetic documents, briefing, telegram) | **Courier Prime** (400/700 + italics, `latin-ext`), Special Elite (one weight, `latin-ext`, Apache-2.0), Cutive Mono (`latin-ext`) | paper only (frames I; history cards) |

**Test string to render before approval:** `ĄĆĘŁŃÓŚŹŻ ąćęłńóśźż  Gliwice 31.08.1939  0123456789  /  :  %`.

---

## 9. Not measured / limits / follow-ups
- **Motion and timings** (banner in/out, tick-off, list fade, weapon-list hold time, detection fill speed): **[U] for all five games** - stills only. Section 7 gives clearly flagged [P] values; verify against gameplay footage (frame counting) if the user wants real numbers.
- **Detection / "compromised" arrows (Hitman), SE5 objective list, BFV objective/sector-update banner, BFV pause/settings, SE5 binocular tagging UI, CS2 in-game buy menu:** not found in the sampled community shots (first page of each game's screenshot list, 30 shots each) or not accessible; nothing invented.
- **Panel opacity:** panels are blended with the scene in every shot - true alpha values are [U]; the 60-65 % in section 7 is an estimate.
- **HUD scale/colour settings** of uploaders are unknown (CS2 shots recoloured); measurements are "typical", +-2 px.
- **Game UI Database** (blocked, Cloudflare) would have given more states (settings, pause, fail screens) with OCR search; a human opening it in a normal browser and saving 5-8 screens for "Pause / Settings / Game over" of Hitman 3 / SE5 would close the remaining gap.
- **Interface In Game** also has Call of Duty: WWII (46 screenshots), Insurgency: Sandstorm (48) and Call of Duty: Modern Warfare (30) - optional follow-up for objective banners; not studied for lack of time budget. Wolfenstein: The New Order not studied.
- HLL weapon-icon direction not verified.

---

## 10. Image index (internal reference only; all in `docs/research/ui/`, <= 300 KB each)

| File | What it shows | Source URL |
|---|---|---|
| cs2_loadout_grid.jpg | CS2 Loadout tab: 3x5 weapon-card grid, 8-col inventory, tab bar | https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/730/ss_fe70d46859593aef623a0614f4686e2814405035.1920x1080.jpg (Steam store, official) |
| cs2_hud_roundwon_weaponlist_killfeed.jpg | CS2 round-won banner, MVP strip, weapon list with names, kill feed, pick-up toast (HUD recoloured) | https://images.steamusercontent.com/ugc/16122785070650605580/B412D21CF3AD68452BAADA3B62007CE8EB333BC3/ (Steam Community, app 730) |
| cs2_hud_awp_4x3_1440x1080.jpg | CS2 bottom strip (health/armour/emblem/ammo), money, radar, weapon list, kill feed, warm-up bar - 1:1 pixels | https://images.steamusercontent.com/ugc/15677214663458882776/EBF9073FFBFA99BEC7645AE56A204A1BFAFBB413/ |
| cs2_hud_16x9_health_money_radar.jpg | CS2 16:9 HUD: radar, timer, money, health, weapon list (pink tint) | https://images.steamusercontent.com/ugc/18048564987501005824/40B300A01E720432FD721E56E4921814F5DB128A/ |
| csgo_buy_radial_menu.jpg | CS:GO radial buy menu, category wedges | https://interfaceingame.com/wp-content/uploads/counter-strike-global-offensive/counter-strike-global-offensive-buy-menu.png |
| csgo_buy_radial_weapon_stats.jpg | CS:GO radial menu with UMP-45 stats table, prices | https://interfaceingame.com/wp-content/uploads/counter-strike-global-offensive/counter-strike-global-offensive-buy-weapon.jpg |
| csgo_killfeed_deathcard_minimap.jpg | CS:GO kill feed (own-kill red row), death card, money, radar | https://interfaceingame.com/wp-content/uploads/counter-strike-global-offensive/counter-strike-global-offensive-killed-by.jpg |
| bfv_hud_infantry_weaponlist.jpg | Battlefield V infantry HUD: timer/tickets, minimap, squad, weapon list, ammo | https://images.steamusercontent.com/ugc/18147312207002956066/BFF94B5084384DDBD109F055CCD2A5B02433455D/ (Steam Community, app 1238810) |
| bfv_score_popup_bare_text.jpg | Battlefield V score popup (bare wide-tracked caps + value + caption) | https://images.steamusercontent.com/ugc/18134709398388513993/F9D5D9CE3FC2041993B4CFFDA36F41DA42631E44/ |
| se5_hud_melee_kill_popup.jpg | Sniper Elite 5 HUD: minimap, health ring, ammo cluster, amber kill plate + XP | https://images.steamusercontent.com/ugc/16386654471096738964/0D2F22E4FD7FE960D25342523A3BD363E4BBA096/ (Steam Community, app 1029690) |
| se5_hud_keycap_hint_pl.jpg | Sniper Elite 5 key-cap hints in Polish, ammo cluster | https://images.steamusercontent.com/ugc/12051754892794743321/DDD984C80F701B16A8D383F209C3502ED47D3C9D/ |
| se5_hud_climb_prompt.jpg | Sniper Elite 5 contextual key-cap prompt, minimap with markers | https://images.steamusercontent.com/ugc/17870402148969159054/D7F6B74258D5C1117D65A45F59F1F144AC5517BB/ |
| se5_field_report_document.jpg | Sniper Elite 5 debrief "field report" book on a clean frame | https://images.steamusercontent.com/ugc/12138357549799662674/F8F39C7F0B315BB26AEEFDAC5B9B5B2D4E71A559/ |
| se5_scope_view_hud.jpg | Sniper Elite 5 scope overlay, zoom readout, kill plate | https://images.steamusercontent.com/ugc/11457587289391901729/90ADE07591C8B85440E6E77C2711EB2822B9A18F/ |
| hitman3_hud_objective_completed.jpg | Hitman 3: timer, objective banner, target-eliminated strip, minimap, prompt, subtitles | https://images.steamusercontent.com/ugc/13290681662165794051/5C6C5570CCC06A947B0F8B727D97677EBDC79202/ (Steam Community, app 1659040) |
| hitman3_hud_objectives_hide_prompt.jpg | Hitman 3: objective list top-left, money, minimap, hide-item prompt, ammo | https://images.steamusercontent.com/ugc/17584666849770071288/AD0C1C361B87F050A57DA11F3AF8725459275AC8/ |
| hitman3_objective_tick_crop.jpg | 3x crop: done (grey + check) vs open (white + red target) objective rows | https://images.steamusercontent.com/ugc/15872857476846106311/E139376E57EEFF0187942FC1E4CA410E986C423C/ (crop) |
| hitman3_item_unlock_card.jpg | Hitman 3 item/unlock card with dotted rules and key-caps | https://images.steamusercontent.com/ugc/9475841078293342715/F015060F018EA30AA2CC10898971AF09469659AA/ |
| hll_hud_capture_banner.jpg | Hell Let Loose capture banner, sector ribbons, squad list, weapon/ammo | https://images.steamusercontent.com/ugc/12840963237447156826/225B782AEEF0998B1EF8133C221D2444F4555091/ (Steam Community, app 686810) |
| hll_hud_compass_weapon_squad.jpg | Hell Let Loose: ribbons, compass strip, weapon name/silhouette, chat | https://images.steamusercontent.com/ugc/15868987574212203772/D3520DC4DCC82EF6AE110BB68C1C5E000961E797/ |
| hll_hud_minimal_markers.jpg | Hell Let Loose: almost empty HUD, compass, world name markers | https://images.steamusercontent.com/ugc/18089163093487723488/004CFC0E67013A40D804FF7A04C9234D2AD868AE/ |
| hll_barracks_loadout_menu.jpg | Hell Let Loose barracks (class / appearance) menu | https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/686810/ss_4a0298406d284993944c79f44dd51ae6c3a22f84.1920x1080.jpg (Steam store, official) |

22 images total. Copyright of every image belongs to the game owners / the uploading players; the files are layout/size/hierarchy study material and must not be copied into the game (icons, fonts, art).
