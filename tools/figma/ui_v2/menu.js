// Menu building blocks (panels allowed here: dense content). One panel style per screen: ink @ ~62 %, hairline.
"use strict";
const { T, FONT, BG, txt, txtR, txtC, icon, iconFit, rect, frame, hair, keycap } = require("./lib.js");
const { WEAPONS } = require("./data.js");

const PANEL = "#14120f9e";

function blurBackdrop(bgName, dim = "b8") {
  return [
    rect(-40, -40, 2000, 1160, BG(bgName), { name: "backdrop (Play capture, blurred)", effects: [{ type: "blur", radius: 18 }] }),
    rect(0, 0, 1920, 1080, `#06080a${dim}`, { name: "wash" }),
    rect(0, 0, 1920, 1080, { type: "radial", cx: 0.5, cy: 0.5, rx: 0.8, ry: 0.85, stops: [[0.5, "#00000000"], [1, "#000000a0"]] }, { name: "vignette" }),
  ];
}

function header(label, sub) {
  return [
    txt(label, 64, 44, { size: 32, lh: 36, style: "SemiBold", ls: "20%", case: "upper", shadow: false }),
    sub ? txt(sub, 64, 92, { size: 16, lh: 20, style: "Medium", font: FONT.body, color: T.bone2, shadow: false, w: 900 }) : null,
    hair(64, 128, 1792),
  ].filter(Boolean);
}

function footerHints(items, y = 1004) {
  const kids = [];
  let x = 64;
  for (const [k, label] of items) {
    const kw = k.length > 1 ? 14 + k.length * 9 : 24;
    kids.push(keycap(k, x, y));
    kids.push(txt(label, x + kw + 10, y - 1, { size: 18, lh: 26, style: "Medium", color: T.bone2, shadow: false, w: 260 }));
    x += kw + 10 + label.length * 9 + 36;
  }
  return kids;
}

function colHeader(label, x, y, w, note) {
  return [
    icon("g_diamond", x, y + 2, 11, T.bone2, { shadow: false }),
    txt(label, x + 20, y, { size: 14, lh: 16, style: "SemiBold", ls: "18%", case: "upper", color: T.bone2, shadow: false, w: w - 20 }),
    note ? txtR(note, x + w, y, 120, { size: 14, lh: 16, style: "Medium", color: T.bone3, shadow: false }) : null,
  ].filter(Boolean);
}

// weapon card: art dominant, name + 2-word descriptor, strip under the selected one
function weaponCard(wp, x, y, w, h, selected, hotkey) {
  const d = WEAPONS[wp];
  const kids = [
    iconFit(d.icon, 20, 14, w - 40, h - 62, T.bone, "c", { opacity: selected ? 1 : 0.6, shadow: false }),
    txt(d.name, 16, h - 42, { size: 17, lh: 20, style: "SemiBold", ls: "6%", case: "upper", shadow: false, w: w - 32 }),
    txt(d.tags.join(", "), 16, h - 22, { size: 13, lh: 16, style: "Medium", ls: "10%", case: "upper", color: selected ? T.bone2 : T.bone3, shadow: false, w: w - 32 }),
  ];
  if (hotkey) kids.push(txt(String(hotkey), w - 28, 10, { size: 13, lh: 16, style: "SemiBold", color: T.bone3, shadow: false, w: 18, align: "right" }));
  if (selected) kids.push(rect(0, h - 3, w, 3, T.bone, { name: "selected strip" }));
  return frame("card_" + wp, x, y, w, h, { fill: selected ? "#2a2f36e6" : "#1a1e22cc" }, kids);
}

function lockedCard(label, note, x, y, w, h) {
  return frame("card_locked", x, y, w, h, { stroke: { color: "#EEEAE033", weight: 1, dash: [4, 4] } }, [
    txtC(label, w / 2, h / 2 - 20, w, { size: 15, lh: 18, style: "SemiBold", ls: "18%", case: "upper", color: T.bone3, shadow: false }),
    txtC(note, w / 2, h / 2 + 4, w, { size: 15, lh: 18, style: "Medium", font: FONT.body, color: T.bone3, shadow: false }),
  ]);
}

// hero detail of the selected weapon
function hero(wp, x, y, w, h) {
  const d = WEAPONS[wp];
  const kids = [
    txt(d.name, 40, 36, { size: 56, lh: 56, style: "SemiBold", ls: "3%", case: "upper", shadow: false, w: w - 80 }),
    txt(d.cal, 40, 98, { size: 18, lh: 22, style: "Medium", ls: "6%", case: "upper", color: T.bone2, shadow: false, w: w - 80 }),
    rect(36, 150, w - 72, 190, { type: "radial", cx: 0.5, cy: 0.5, rx: 0.5, ry: 0.5, stops: [[0, "#EEEAE01c"], [1, "#EEEAE000"]] }, { name: "spot" }),
    iconFit(d.icon, 40, 150, w - 80, 190, T.bone, "c", { shadow: false }),
    hair(40, 366, w - 80),
  ];
  const n = d.nums.length, cw = (w - 80) / n;
  d.nums.forEach(([v, lab], i) => {
    kids.push(txt(v, 40 + i * cw, 384, { size: 44, lh: 46, style: "Medium", shadow: false, w: cw }));
    kids.push(txt(lab, 40 + i * cw, 434, { size: 13, lh: 16, style: "SemiBold", ls: "14%", case: "upper", color: T.bone3, shadow: false, w: cw }));
    kids.push(rect(40 + i * cw, 458, cw - 24, 2, T.bone, { opacity: 0.16 }));
    kids.push(rect(40 + i * cw, 458, Math.round((cw - 24) * d.rel[i]), 2, T.bone, { opacity: 0.85 }));
  });
  kids.push(txt(d.desc, 40, 498, { size: 20, lh: 28, style: "Regular", font: FONT.body, color: T.bone, shadow: false, w: w - 80 }));
  return frame("hero_" + wp, x, y, w, h, { fill: PANEL, stroke: { color: "#EEEAE024", weight: 1 } }, kids);
}

function loadoutStrip(slots, x, y) {
  const kids = [txt("Twoje wyposażenie", 0, 0, { size: 12, lh: 14, style: "SemiBold", ls: "18%", case: "upper", color: T.bone3, shadow: false, w: 300 })];
  slots.forEach((s, i) => {
    kids.push(frame("slot" + s.slot, i * 140, 24, 128, 64, { fill: "#1a1e22cc" }, [
      s.icon ? iconFit(s.icon, 20, 8, 88, 48, T.bone, "c", { shadow: false }) : txtC("—", 64, 20, 128, { size: 22, lh: 24, style: "Medium", color: T.bone3, shadow: false }),
      txt(String(s.slot), 8, 6, { size: 12, lh: 14, style: "SemiBold", color: T.bone3, shadow: false, w: 14 }),
    ]));
  });
  return frame("loadout strip", x, y, 580, 100, {}, kids);
}

function button(label, x, y, w = 300, h = 56) {
  return frame("button primary", x, y, w, h, { fill: T.bone, radius: 3 }, [
    txt(label, 0, 0, { w, h, align: "center", valign: "center", size: 22, lh: 22, style: "SemiBold", ls: "16%", case: "upper", color: T.ink, shadow: false }),
  ]);
}
function buttonGhost(label, x, y, w = 300, h = 56) {
  return frame("button secondary", x, y, w, h, { stroke: { color: "#EEEAE066", weight: 1 }, radius: 3 }, [
    txt(label, 0, 0, { w, h, align: "center", valign: "center", size: 22, lh: 22, style: "SemiBold", ls: "16%", case: "upper", color: T.bone, shadow: false }),
  ]);
}

module.exports = { PANEL, blurBackdrop, header, footerHints, colHeader, weaponCard, lockedCard, hero, loadoutStrip, button, buttonGhost };
