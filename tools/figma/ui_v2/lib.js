// Design tokens + small builders for UI v2 (Sender Gleiwitz). Everything returns plain node specs that both
// render_html.js (local preview/export) and the Figma plugin's `build` command understand. Absolute positions only.
"use strict";
const path = require("path");
const { ICONS } = require("./icons.js");

// ------------------------------------------------------------------ tokens
const T = {
  // text on the game image (bare, with shadow)
  bone: "#EEEAE0",
  bone2: "#EEEAE0a6", // 65 %
  bone3: "#EEEAE061", // 38 %
  line: "#EEEAE02e", // hairline 18 %
  // state accents: one per state, never decoration
  safe: "#93AD66",
  warn: "#E2A93B",
  danger: "#DB4630",
  // surfaces (menus only)
  ink: "#0C0E10",
  panel: "#0C0E10e6", // 90 %
  panel2: "#15181bf2",
  card: "#1A1E22",
  cardHi: "#232830",
  paper: "#E4DCC4",
  paperInk: "#25221B",
  stampRed: "#B23A2B",
};
const FONT = { display: "Barlow Condensed", body: "Barlow", mono: "Courier Prime" };

// type scale at 1080p (px): [size, line-height]
const TYPE = {
  micro: [11, 14], label: [13, 16], small: [14, 18], body: [18, 24], lead: [22, 26], title: [32, 36], hero: [56, 56], display: [96, 92],
};

// soft shadow stack for bare text / icons on top of the game image
const SH = [
  { type: "shadow", x: 0, y: 1, blur: 2, color: "#000000b8" },
  { type: "shadow", x: 0, y: 0, blur: 14, color: "#00000066" },
];
const SH_SOFT = [{ type: "shadow", x: 0, y: 1, blur: 8, color: "#00000080" }];

const BG = (name) => ({ type: "image", file: path.join(__dirname, "..", "..", "..", "docs", "design", "ui_v2", "backgrounds", name + ".jpg"), mode: "FILL" });

// ------------------------------------------------------------------ builders
function txt(text, x, y, o = {}) {
  const size = o.size || 16;
  const n = {
    t: "text", name: o.name || undefined, text, x, y, font: o.font || FONT.display, style: o.style || "Medium", size,
    lh: o.lh !== undefined ? o.lh : size + 4, color: o.color || T.bone,
  };
  if (o.w !== undefined) n.w = o.w;
  if (o.h !== undefined) { n.h = o.h; n.autoResize = "NONE"; }
  if (o.ls !== undefined) n.ls = o.ls;
  if (o.case) n.case = o.case;
  if (o.align) n.align = o.align;
  if (o.valign) n.valign = o.valign;
  if (o.opacity !== undefined) n.opacity = o.opacity;
  n.effects = o.shadow === false ? undefined : (o.shadow || SH);
  if (o.key) n.key = o.key;
  return n;
}
// right-aligned text ending at xRight
const txtR = (text, xRight, y, w, o = {}) => txt(text, xRight - w, y, Object.assign({}, o, { w, align: "right" }));
const txtC = (text, xCenter, y, w, o = {}) => txt(text, xCenter - w / 2, y, Object.assign({}, o, { w, align: "center" }));

function svgOf(name) {
  const i = ICONS[name];
  if (!i) throw new Error("unknown icon " + name);
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${i.w} ${i.h}">${i.body}</svg>`;
}
// icon at x,y with the given width (height from aspect) — or fitted into a box
function icon(name, x, y, w, color, o = {}) {
  const i = ICONS[name];
  const h = +(w * i.h / i.w).toFixed(2);
  const n = { t: "svg", name: o.name || name, svg: svgOf(name), x, y, w, h, color: color || T.bone };
  if (o.opacity !== undefined) n.opacity = o.opacity;
  n.effects = o.shadow === false ? undefined : (o.shadow || SH);
  return n;
}
// fit icon inside box (bw x bh), anchored: "r" right, "l" left, "c" centre; returns node + final size
function iconFit(name, x, y, bw, bh, color, anchor = "c", o = {}) {
  const i = ICONS[name];
  const s = Math.min(bw / i.w, bh / i.h);
  const w = +(i.w * s).toFixed(2), h = +(i.h * s).toFixed(2);
  const ox = anchor === "r" ? bw - w : anchor === "l" ? 0 : (bw - w) / 2;
  const oy = (bh - h) / 2;
  return icon(name, x + ox, y + oy, w, color, o);
}

const rect = (x, y, w, h, fill, o = {}) => Object.assign({ t: "rect", x, y, w, h, fill }, o);
const frame = (name, x, y, w, h, o = {}, children = []) => Object.assign({ t: "frame", name, x, y, w, h }, o, { children });
const hair = (x, y, w, color, h = 1) => rect(x, y, w, h, color || T.line, { name: "rule" });

// white keycap (Sniper Elite style): dark letter on bone
function keycap(label, x, y, o = {}) {
  const w = o.w || (label.length > 1 ? 14 + label.length * 9 : 24), h = o.h || 24;
  return frame("key_" + label, x, y, w, h, { fill: o.fill || T.bone, radius: 4, effects: [{ type: "shadow", x: 0, y: 1, blur: 3, color: "#00000070" }] }, [
    txt(label, 0, 0, { w, h, align: "center", valign: "center", size: o.size || 15, lh: o.size || 15, style: "SemiBold", color: T.ink, shadow: false, ls: label.length > 1 ? "4%" : 0, case: "upper" }),
  ]);
}

// screen frame 1920x1080 with an image background and a bottom/top legibility gradient
function screen(name, bgName, children, o = {}) {
  const kids = [];
  if (bgName) kids.push(rect(0, 0, 1920, 1080, BG(bgName), { name: "backdrop (Play capture)" }));
  if (o.dim) kids.push(rect(0, 0, 1920, 1080, `#05070a${o.dim}`, { name: "dim" }));
  if (o.vignette !== false) kids.push(rect(0, 0, 1920, 1080, { type: "radial", cx: 0.5, cy: 0.5, rx: 0.8, ry: 0.85, stops: [[0.62, "#00000000"], [1, "#00000070"]] }, { name: "vignette" }));
  kids.push(...children);
  return frame(name, 0, 0, 1920, 1080, { clip: true, fill: "#0b0d0f" }, kids);
}

module.exports = { T, FONT, TYPE, SH, SH_SOFT, BG, txt, txtR, txtC, icon, iconFit, svgOf, rect, frame, hair, keycap, screen, ICONS };
