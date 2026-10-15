// SVG icon set for UI v2 — weapon silhouettes (side view, muzzle to the RIGHT, like CS2) + 24 px UI glyphs.
// Everything is a single colour via currentColor. Silhouettes use the nonzero fill rule: positive shapes are drawn
// clockwise, holes counter-clockwise. Source of truth for both Figma (create_svg) and the Unity sprites (exported
// from Figma as PNG).
"use strict";

const ICONS = {};
const add = (name, w, h, body) => { ICONS[name] = { w, h, body: body.trim() }; };
const P = (d) => `<path fill="currentColor" d="${d.replace(/\s+/g, " ").trim()}"/>`;
// counter-clockwise circle hole / clockwise disc
const hole = (cx, cy, r) => `M${cx} ${cy - r} A${r} ${r} 0 1 0 ${cx} ${cy + r} A${r} ${r} 0 1 0 ${cx} ${cy - r} Z`;
const disc = (cx, cy, r) => `M${cx} ${cy - r} A${r} ${r} 0 1 1 ${cx} ${cy + r} A${r} ${r} 0 1 1 ${cx} ${cy - r} Z`;
// counter-clockwise rectangle hole
const rhole = (x, y, w, h) => `M${x} ${y} V${y + h} H${x + w} V${y} Z`;

// ---------------------------------------------------------------- pistols
// Vis wz. 35 (Radom): long slide with rear serrations, external hammer, Colt-like grip.
add("w_vis", 200, 112, P(`
  M24 12 H170 L174 7 H183 L185 12 H192 Q198 12 198 18 V34 Q198 38 194 38 H24 Q21 38 21 35 V15 Q21 12 24 12 Z
  M28 37 H152 V46 L146 50 H122 V62 Q122 70 114 70 H76 Q68 70 66 62 L62 50 L58 50 L40 104 H10 L8 98 L26 50 Z
  M22 19 L22 33 L9 31 Q6 24 11 21 Z
  M72 50 V60 Q72 64 76 64 H112 Q116 64 116 60 V50 Z
  M88 50 H93 L91 60 H86 Z
  ${rhole(32, 14, 1.8, 20)} ${rhole(37, 14, 1.8, 20)} ${rhole(42, 14, 1.8, 20)} ${rhole(47, 14, 1.8, 20)}
`));

// Browning wz. 1922: compact, rounded slide, no external hammer.
add("w_browning", 168, 100, P(`
  M18 12 H142 Q150 12 150 19 V31 Q150 34 147 34 H17 Q13 34 13 30 V17 Q13 12 18 12 Z
  M150 20 H158 V28 H150 Z
  M22 33 H128 V42 L122 46 H102 V54 Q102 61 95 61 H62 Q55 61 53 54 L50 46 L46 46 L32 90 H8 L6 85 L20 46 Z
  M60 46 V52 Q60 55 63 55 H92 Q96 55 96 52 V46 Z
  M74 46 H79 L77 54 H72 Z
  ${rhole(26, 15, 1.6, 16)} ${rhole(31, 15, 1.6, 16)} ${rhole(36, 15, 1.6, 16)} ${rhole(41, 15, 1.6, 16)}
`));

// Nagant wz. 1930: gas-seal revolver, bird's-head grip, fluted cylinder.
add("w_nagant", 226, 112, P(`
  M26 12 H128 V22 H26 Z
  M84 8 H128 V50 H84 Z
  ${rhole(94, 12, 2.4, 34)} ${rhole(106, 12, 2.4, 34)} ${rhole(118, 12, 2.4, 34)}
  M128 20 H224 V32 H128 Z
  M206 13 H214 V20 H206 Z
  M130 33 H196 V40 H130 Z
  M28 22 H86 V54 H62 L40 48 L28 36 Z
  M28 12 L28 30 L14 28 Q8 20 14 14 Z
  M30 34 L58 46 L64 56 L50 94 Q48 100 42 100 H18 Q10 100 10 92 Z
  M70 52 V60 Q70 64 74 64 H92 Q96 64 96 60 V52 Z
  M86 50 H91 L89 60 H85 Z
  M60 44 H100 V54 Q100 70 84 70 H70 Q58 70 58 58 Z
`));

// ---------------------------------------------------------------- blades
// Bagnet wz. 1924: ringed crossguard, ribbed grip, fullered blade.
add("w_bagnet", 262, 60, P(`
  M4 22 Q4 18 8 18 H18 V42 H8 Q4 42 4 38 Z
  M18 20 H72 V40 H18 Z
  ${rhole(26, 22, 2, 16)} ${rhole(34, 22, 2, 16)} ${rhole(42, 22, 2, 16)} ${rhole(50, 22, 2, 16)} ${rhole(58, 22, 2, 16)} ${rhole(66, 22, 2, 16)}
  M72 8 H82 V52 H72 Z
  ${disc(90, 12, 9)} ${hole(90, 12, 5)}
  M82 22 H228 L260 32 L234 38 H82 Z
  M96 29 V31.4 H212 V29 Z
`));

// Nóż sprężynowy: short folding knife, opened.
add("w_noz", 222, 54, P(`
  M8 16 H98 V40 H8 Q3 40 3 34 V22 Q3 16 8 16 Z
  ${hole(20, 28, 2.4)} ${hole(50, 28, 2.4)} ${hole(80, 28, 2.4)}
  M98 12 H108 V44 H98 Z
  M108 18 H172 L218 33 Q198 43 150 41 L108 38 Z
`));

// ---------------------------------------------------------------- long guns
const boltRifle = (variant) => P(`
  M8 44 L60 42 L102 38 V56 Q106 60 114 60 L112 68 L100 72 Q92 72 86 66 Q48 80 12 90 Q6 91 6 85 Z
  M100 34 H178 V50 H100 Z
  M150 30 H172 V34 H150 Z
  M120 48 H132 L142 68 Q145 74 139 76 Q131 74 129 68 Z
  M120 50 H166 V58 H120 Z
  M108 58 H164 V68 Q164 76 156 76 H120 Q108 76 108 68 Z
  M118 58 V66 Q118 70 122 70 H152 Q156 70 156 66 V58 Z
  M134 58 H139 L137 68 H132 Z
  M178 38 H${variant === "k98" ? 396 : 338} V52 H178 Z
  M178 40 H434 V46 H178 Z
  M420 32 H426 V40 H420 Z M414 38 H432 V40 H414 Z
  M252 36 H258 V56 H252 Z M${variant === "k98" ? 332 : 322} 36 H${variant === "k98" ? 338 : 328} V56 H${variant === "k98" ? 332 : 322} Z
  ${variant === "k98" ? "M392 38 H410 V54 H392 Z M404 46 H416 V54 H404 Z" : ""}
`);
add("w_wz29", 440, 96, boltRifle("wz29"));
add("w_k98", 440, 96, boltRifle("k98"));

// Erma EMP: tubular receiver, perforated barrel jacket, straight magazine, folding stock.
add("w_erma", 410, 150, P(`
  M72 34 H246 V62 H72 Q66 62 66 56 V40 Q66 34 72 34 Z
  M246 38 H392 V56 H246 Z
  ${[262, 276, 290, 304, 318, 332, 346, 360, 374].map((x) => hole(x, 47, 3.2)).join(" ")}
  M392 40 H404 V54 H392 Z M394 28 H400 V40 H394 Z
  M188 62 H212 V140 H188 Z M186 138 H214 V147 H186 Z
  M128 62 H164 L152 112 Q150 117 144 117 H134 Q127 117 127 111 Z
  M164 62 H188 V80 Q188 85 183 85 H170 Q164 85 164 80 Z
  M170 62 V76 Q170 79 173 79 H179 Q182 79 182 76 V62 Z
  M70 38 L14 52 V58 L70 44 Z
  M70 56 L16 82 V88 L70 62 Z
  M4 46 H16 V94 H4 Z
  M118 26 H132 V34 H118 Z M92 24 H108 V34 H92 Z
`));

// ---------------------------------------------------------------- camera (Leica, front view)
add("w_camera", 150, 100, P(`
  M8 30 H142 Q148 30 148 36 V86 Q148 92 142 92 H8 Q2 92 2 86 V36 Q2 30 8 30 Z
  M14 14 H136 V30 H14 Z
  ${rhole(24, 18, 22, 7)} ${rhole(54, 18, 14, 7)}
  M104 8 H126 V14 H104 Z
  ${hole(75, 62, 28)} ${disc(75, 62, 24)} ${hole(75, 62, 15)} ${disc(75, 62, 9)}
`));

// ---------------------------------------------------------------- 24 px glyphs
const stroke = (d, w = 1.8) => `<path fill="none" stroke="currentColor" stroke-width="${w}" stroke-linecap="round" stroke-linejoin="round" d="${d}"/>`;
add("g_diamond", 24, 24, P("M12 2 L22 12 L12 22 L2 12 Z"));
add("g_diamond_o", 24, 24, stroke("M12 3.5 L20.5 12 L12 20.5 L3.5 12 Z", 1.8));
add("g_check", 24, 24, stroke("M4.5 12.5 L9.5 17.5 L19.5 6.5", 2.4));
add("g_eye", 24, 24, stroke("M1.8 12 C5 6.6 8.4 4.5 12 4.5 C15.6 4.5 19 6.6 22.2 12 C19 17.4 15.6 19.5 12 19.5 C8.4 19.5 5 17.4 1.8 12 Z", 1.8)
  + `<circle cx="12" cy="12" r="3.4" fill="currentColor"/>`);
add("g_chev_r", 24, 24, stroke("M9 4.5 L16.5 12 L9 19.5", 2.2));
add("g_chev_l", 24, 24, stroke("M15 4.5 L7.5 12 L15 19.5", 2.2));
add("g_tri_down", 24, 24, P("M5 8 H19 L12 18 Z"));
add("g_cartridge", 10, 28, P("M1 28 V12 Q1 4 5 0 Q9 4 9 12 V28 Z M1 13.5 V15.2 H9 V13.5 Z"));
add("g_photo", 24, 24, stroke("M3 7.5 H7.2 L9 5 H15 L16.8 7.5 H21 V19 H3 Z", 1.8) + `<circle cx="12" cy="13" r="3.6" fill="none" stroke="currentColor" stroke-width="1.8"/>`);
add("g_close", 24, 24, stroke("M5.5 5.5 L18.5 18.5 M18.5 5.5 L5.5 18.5", 2));

module.exports = { ICONS, hole, disc, rhole, P };
