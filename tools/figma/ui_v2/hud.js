// HUD building blocks for UI v2 (1920x1080 reference). Each function returns an array of node specs.
// Anchors: 56 px side margin, 44 px top; the bottom blocks end at y = 1032. Bare text/icons with soft shadows on the
// game image — panels are reserved for dense menus.
"use strict";
const { T, FONT, SH, txt, txtR, txtC, icon, iconFit, rect, frame, hair, keycap } = require("./lib.js");

const R = 1856; // right text edge
const L = 64; // left text edge

// ---------------------------------------------------------------- compass (top centre, bare ticks with fade)
function compass(heading = 224, markers = []) {
  const cx = 960, pxPerDeg = 6, half = 60;
  const kids = [];
  const alpha = (dx) => Math.max(0, Math.cos((Math.PI / 2) * Math.min(1, Math.abs(dx) / (half * pxPerDeg)))) ** 1.15;
  const names = { 0: "N", 45: "NE", 90: "E", 135: "SE", 180: "S", 225: "SW", 270: "W", 315: "NW" };
  const b0 = Math.ceil((heading - half) / 5) * 5;
  for (let b = b0; b <= heading + half; b += 5) {
    const x = cx + (b - heading) * pxPerDeg;
    const a = alpha(x - cx);
    if (a < 0.04) continue;
    const m = ((b % 360) + 360) % 360;
    const card = m % 45 === 0, mid = m % 15 === 0;
    const h = card ? 16 : mid ? 11 : 6;
    kids.push(rect(+(x - (card ? 1 : 0.5)).toFixed(1), 48, card ? 2 : 1, h, T.bone, { opacity: +(a * (card ? 1 : mid ? 0.85 : 0.55)).toFixed(2), name: "tick" + m, effects: [{ type: "shadow", x: 0, y: 0, blur: 3, color: "#000000a0" }] }));
    if (card) kids.push(txtC(names[m], x, 68, 40, { size: 16, lh: 18, style: "SemiBold", opacity: +a.toFixed(2), ls: "6%" }));
    else if (m % 30 === 0) kids.push(txtC(String(m), x, 70, 40, { size: 12, lh: 14, style: "Medium", opacity: +(a * 0.65).toFixed(2) }));
  }
  const hd = String(Math.round(((heading % 360) + 360) % 360)).padStart(3, "0");
  kids.push(txtC(hd, cx, 16, 80, { size: 15, lh: 16, style: "SemiBold", ls: "10%" }));
  kids.push(icon("g_tri_down", cx - 6, 32, 12, T.bone, { name: "heading_marker" }));
  for (const mk of markers) {
    let dx = (((mk.bearing - heading + 540) % 360) - 180) * pxPerDeg;
    const clamp = Math.abs(dx) > half * pxPerDeg;
    dx = Math.max(-half * pxPerDeg, Math.min(half * pxPerDeg, dx));
    const a = clamp ? 0.6 : 1;
    kids.push(icon("g_diamond", cx + dx - 7, 41, 14, mk.color || T.bone, { opacity: a, name: "marker" }));
    if (mk.label) kids.push(txtC(mk.label, cx + dx, 90, 80, { size: 13, lh: 14, style: "Medium", color: T.bone2, opacity: a }));
  }
  return [rect(0, 0, 1920, 170, { type: "linear", angle: 180, stops: [[0, "#0000005c"], [1, "#00000000"]] }, { name: "top shade" }), frame("compass", 0, 0, 1920, 110, {}, kids)];
}

// ---------------------------------------------------------------- objectives (top right, right aligned, bare)
// s: {time,date,act,obj,detail,optional:[{text,done}],counters, mode:"normal"|"new"|"done"|"out"}
function objectives(s) {
  const kids = [];
  const mode = s.mode || "normal";
  kids.push(rect(1260, 0, 660, 520, { type: "radial", cx: 1, cy: 0, rx: 1, ry: 1, stops: [[0, "#000000a6"], [0.55, "#00000052"], [1, "#00000000"]] }, { name: "readability shade" }));
  kids.push(txtR(s.time || "15:30", R, 38, 220, { size: 36, lh: 36, style: "Medium", ls: "2%" }));
  kids.push(txtR(s.date || "CZWARTEK 31 SIERPNIA 1939", R, 84, 360, { size: 13, lh: 16, style: "Medium", ls: "14%", case: "upper", color: T.bone2 }));
  kids.push(hair(R - 300, 114, 300));
  kids.push(txtR(s.act || "AKT 1 – DWORZEC I HOTEL", R, 128, 380, { size: 13, lh: 16, style: "SemiBold", ls: "14%", case: "upper", color: T.bone2 }));
  const y0 = 156;
  const done = mode === "done" || mode === "out";
  const objColor = done ? T.bone2 : T.bone;
  if (mode === "new") kids.push(txtR("NOWE ZADANIE", R, y0 - 4, 260, { size: 13, lh: 16, style: "SemiBold", ls: "18%", color: T.warn }));
  const oy = mode === "new" ? y0 + 18 : y0;
  kids.push(done ? icon("g_check", R - 22, oy + 4, 22, T.safe) : icon("g_diamond", R - 18, oy + 7, 16, T.bone));
  kids.push(txtR(s.obj || "Porozmawiaj z panią Zofią", R - 32, oy, 460, { size: 26, lh: 30, style: "Medium", color: objColor }));
  if (done) kids.push(rect(R - 32 - (s.objW || 292), oy + 16, s.objW || 292, 2, T.bone, { opacity: 0.85, name: "strike" }));
  kids.push(txtR(s.detail === undefined ? "86 m" : s.detail, R - 32, oy + 36, 300, { size: 16, lh: 20, style: "Medium", color: T.bone2 }));
  let y = oy + 84;
  const opt = s.optional === undefined ? [{ text: "Karty historii 1/6" }, { text: "Skrytka pod mostem" }] : s.optional;
  if (opt.length) {
    kids.push(txtR("OPCJONALNE", R, y, 200, { size: 12, lh: 14, style: "SemiBold", ls: "18%", color: T.bone3 }));
    y += 22;
    for (const o of opt) {
      kids.push(txtR(o.text, R - 22, y, 340, { size: 16, lh: 20, style: "Medium", color: o.done ? T.bone3 : T.bone2 }));
      kids.push(o.done ? icon("g_check", R - 15, y + 3, 15, T.safe) : icon("g_diamond_o", R - 14, y + 4, 13, T.bone2));
      y += 26;
    }
  }
  if (s.counters !== false) {
    y += 8;
    kids.push(txtR(s.counters || "DOWODY 2/6   KARTY 1/6", R, y, 380, { size: 13, lh: 16, style: "SemiBold", ls: "14%", color: T.bone3 }));
  }
  return [frame("objectives", 0, 0, 1920, 520, { opacity: mode === "out" ? 0.45 : undefined }, kids)];
}

// ---------------------------------------------------------------- weapon + ammo (bottom right)
// w: {icon, name, mag, magMax, reserve} or melee {icon, name, melee:true, hint}
function weaponBlock(w) {
  const kids = [];
  kids.push(iconFit(w.icon, R - 220, 852, 220, 72, T.bone, "r", { opacity: 0.94 }));
  kids.push(txtR(w.name, R, 930, 320, { size: 16, lh: 18, style: "SemiBold", ls: "14%", case: "upper", color: T.bone }));
  if (w.melee) {
    kids.push(txtR(w.hint || "", R, 976, 340, { size: 17, lh: 22, style: "Medium", color: T.bone2 }));
    return [frame("weapon", 0, 0, 1920, 1080, {}, kids)];
  }
  const lowColor = w.mag <= Math.max(1, Math.floor(w.magMax * 0.25)) ? (w.mag === 0 ? T.danger : T.warn) : T.bone;
  if (w.magMax <= 10) {
    for (let i = 0; i < w.magMax; i++) {
      const x = R - (w.magMax - i) * 11 + 3;
      kids.push(icon("g_cartridge", x, 954, 7, i < w.mag ? lowColor : T.bone, { opacity: i < w.mag ? 1 : 0.22, shadow: SH }));
    }
  }
  kids.push(txtR(String(w.mag).padStart(2, "0"), R - 52, 972, 140, { size: 56, lh: 56, style: "Medium", color: lowColor, ls: "1%" }));
  kids.push(txtR("/ " + String(w.reserve).padStart(2, "0"), R, 1002, 80, { size: 26, lh: 26, style: "Medium", color: T.bone3 }));
  return [frame("weapon", 0, 0, 1920, 1080, {}, kids)];
}

// CS2-style quick list on weapon switch: silhouettes right aligned, slot number at the edge, selected = full white + strip.
function weaponList(items, sel) {
  const kids = [];
  const rowH = 62, y0 = 568;
  items.forEach((it, i) => {
    const y = y0 + i * rowH, on = it.slot === sel;
    kids.push(iconFit(it.icon, R - 214, y + 6, 172, 46, T.bone, "r", { opacity: on ? 1 : 0.4 }));
    if (on) {
      kids.push(frame("slot" + it.slot, R - 24, y + 17, 24, 24, { fill: T.bone, radius: 3 }, [txt(String(it.slot), 0, 0, { w: 24, h: 24, align: "center", valign: "center", size: 15, lh: 15, style: "SemiBold", color: T.ink, shadow: false })]));
      kids.push(rect(R - 214, y + 54, 172, 3, T.bone, { name: "selection strip", effects: [{ type: "shadow", x: 0, y: 0, blur: 6, color: "#00000080" }] }));
    } else kids.push(txtC(String(it.slot), R - 12, y + 19, 24, { size: 16, lh: 20, style: "Medium", color: T.bone3 }));
  });
  return [frame("weapon list", 0, 0, 1920, 1080, {}, kids)];
}

// ---------------------------------------------------------------- visibility + stamina (bottom left)
// v: {label, color, level 0..1, detail, stamina 0..1 or null}
function visibility(v) {
  const kids = [];
  kids.push(rect(0, 880, 640, 200, { type: "radial", cx: 0, cy: 1, rx: 1, ry: 1, stops: [[0, "#00000080"], [1, "#00000000"]] }, { name: "readability shade" }));
  kids.push(icon("g_eye", L, 970, 26, v.color));
  kids.push(txt(v.label, L + 38, 971, { size: 22, lh: 26, style: "SemiBold", ls: "14%", case: "upper", color: v.color, w: 300 }));
  kids.push(rect(L, 1008, 180, 3, T.bone, { opacity: 0.2, name: "visibility track" }));
  kids.push(rect(L, 1008, Math.max(2, Math.round(180 * v.level)), 3, v.color, { name: "visibility fill" }));
  kids.push(txt(v.detail, L, 1016, { size: 15, lh: 18, style: "Medium", font: FONT.body, color: T.bone2, w: 320 }));
  if (v.stamina !== null && v.stamina !== undefined) {
    for (let i = 0; i < 5; i++) {
      const fill = Math.max(0, Math.min(1, v.stamina * 5 - i));
      kids.push(rect(L + i * 38, 944, 34, 3, T.bone, { opacity: 0.2 }));
      if (fill > 0) kids.push(rect(L + i * 38, 944, Math.round(34 * fill), 3, T.bone, { opacity: 0.92 }));
    }
  }
  return [frame("visibility", 0, 0, 1920, 1080, {}, kids)];
}

// ---------------------------------------------------------------- crosshair, hit marker, prompt, target tag
function crosshair(spread = 0, o = {}) {
  const c = 540, x = 960, gap = 6 + spread, len = 9, col = o.color || T.bone;
  const fx = [{ type: "shadow", x: 0, y: 0, blur: 2, color: "#000000c8" }];
  const mk = (a, b, w, h) => rect(a, b, w, h, col, { effects: fx, name: "crosshair" });
  return [mk(x - 1, c - gap - len, 2, len), mk(x - 1, c + gap, 2, len), mk(x - gap - len, c - 1, len, 2), mk(x + gap, c - 1, len, 2), mk(x - 1, c - 1, 2, 2)];
}
function hitMarker(color = T.bone) {
  const x = 960, y = 540, r = 12, w = 2;
  const fx = [{ type: "shadow", x: 0, y: 0, blur: 3, color: "#000000d0" }];
  const diag = (rot) => rect(x - w / 2, y - r, w, r * 2, color, { rot, effects: fx, name: "hit" });
  return [diag(45), diag(-45)];
}
function prompt(keyLabel, text, x = 880, y = 636, o = {}) {
  const kw = keyLabel.length > 1 ? 14 + keyLabel.length * 9 : 26;
  return [keycap(keyLabel, x, y), txt(text, x + kw + 12, y, { size: 22, lh: 26, style: "Medium", w: 320, color: o.color || T.bone })];
}
function targetTag(label, sub, color = T.danger, x = 960, y = 590) {
  return [txtC(label, x, y, 240, { size: 15, lh: 18, style: "SemiBold", ls: "18%", case: "upper", color }), sub ? txtC(sub, x, y + 19, 240, { size: 13, lh: 16, style: "Medium", font: FONT.body, color: T.bone2 }) : null].filter(Boolean);
}

// ---------------------------------------------------------------- alert states
function alarmBadge(label, color, sub) {
  return [
    txtC(label, 960, 118, 700, { size: 36, lh: 36, style: "SemiBold", ls: "30%", color, case: "upper" }),
    rect(900, 164, 120, 2, color, { name: "badge rule" }),
    sub ? txtC(sub, 960, 176, 600, { size: 15, lh: 18, style: "Medium", color: T.bone2 }) : null,
  ].filter(Boolean);
}
function edgeVignette(color, a) {
  return rect(0, 0, 1920, 1080, { type: "radial", cx: 0.5, cy: 0.5, rx: 0.72, ry: 0.78, stops: [[0.55, color + "00"], [1, color + a]] }, { name: "edge vignette" });
}
function popup(line1, line2, y = 706) {
  return [txtC(line1, 960, y, 640, { size: 24, lh: 28, style: "SemiBold", ls: "18%", case: "upper" }), line2 ? txtC(line2, 960, y + 32, 640, { size: 16, lh: 20, style: "Medium", color: T.bone2 }) : null].filter(Boolean);
}

module.exports = { R, L, compass, objectives, weaponBlock, weaponList, visibility, crosshair, hitMarker, prompt, targetTag, alarmBadge, edgeVignette, popup };
