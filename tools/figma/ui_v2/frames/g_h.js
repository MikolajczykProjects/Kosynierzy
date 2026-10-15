// G1 pause, G2 settings, H fail screen.
const { T, FONT, screen, frame, rect, hair, txt, txtC, txtR, icon } = require("../lib.js");
const M = require("../menu.js");

const R = 1856;

// ---------- G1 pause
const G1 = {
  id: "G1_pause", title: "G1 · pauza",
  build() {
    const kids = [...M.blurBackdrop("street_near", "9a")];
    kids.push(rect(0, 0, 900, 1080, { type: "linear", angle: 90, stops: [[0, "#000000b3"], [1, "#00000000"]] }, { name: "left shade" }));
    kids.push(txt("Pauza", 128, 150, { size: 14, lh: 16, style: "SemiBold", ls: "30%", case: "upper", color: T.bone2, shadow: false, w: 300 }));
    const items = ["Wznów", "Od checkpointu", "Ustawienia", "Menu główne"];
    items.forEach((s, i) => {
      const y = 220 + i * 76, on = i === 0;
      if (on) kids.push(rect(104, y + 6, 4, 44, T.bone, { name: "active bar" }));
      kids.push(txt(s, 128, y, { size: 52, lh: 56, style: "SemiBold", ls: "6%", case: "upper", color: on ? T.bone : "#EEEAE08c", shadow: false, w: 700 }));
    });
    // mission block (right, bottom)
    kids.push(txtR("Akt 1 – Dworzec i hotel", R, 760, 520, { size: 14, lh: 16, style: "SemiBold", ls: "18%", case: "upper", color: T.bone2, shadow: false }));
    kids.push(txtR("Porozmawiaj z panią Zofią", R, 788, 700, { size: 32, lh: 36, style: "Medium", shadow: false }));
    kids.push(hair(R - 360, 842, 360));
    kids.push(txtR("Ostatni checkpoint: 15:41", R, 856, 420, { size: 18, lh: 22, style: "Medium", font: FONT.body, color: T.bone2, shadow: false }));
    kids.push(txtR("Czwartek 31 sierpnia 1939, 15:48", R, 882, 520, { size: 18, lh: 22, style: "Medium", font: FONT.body, color: T.bone2, shadow: false }));
    kids.push(...M.footerHints([["↑ ↓", "Wybór"], ["Enter", "Zatwierdź"], ["Esc", "Wznów"]]));
    return screen("G1 · pauza", null, kids, { vignette: false });
  },
};

// ---------- G2 settings (tab Rozgrywka)
function tabBar(tabs, active) {
  const kids = [];
    tabs.forEach((t, i) => {
    const on = i === active;
    kids.push(txtC(t, 960 - (tabs.length * 230) / 2 + i * 230 + 115, 44, 230, { size: 26, lh: 30, style: on ? "SemiBold" : "Medium", ls: "14%", case: "upper", color: on ? T.bone : T.bone3, shadow: false }));
  });
  const x0 = 960 - (tabs.length * 230) / 2;
  tabs.forEach((t, i) => { if (i === active) kids.push(rect(x0 + i * 230 + 50, 84, 130, 3, T.bone, { name: "active tab" })); });
  kids.push(hair(64, 96, 1792));
  return kids;
}
function row(y, label, control, note) {
  const kids = [txt(label, 480, y + 14, { size: 22, lh: 26, style: "Medium", shadow: false, w: 560 })];
  if (note) kids.push(txt(note, 480, y + 40, { size: 15, lh: 18, style: "Regular", font: FONT.body, color: T.bone3, shadow: false, w: 560 }));
  kids.push(...control(y));
  kids.push(hair(480, y + (note ? 72 : 56), 960));
  return kids;
}
const stepper = (val) => (y) => [
  icon("g_chev_l", 1190, y + 15, 22, T.bone2, { shadow: false }),
  txtC(val, 1308, y + 15, 160, { size: 22, lh: 26, style: "Medium", shadow: false }),
  icon("g_chev_r", 1404, y + 15, 22, T.bone2, { shadow: false }),
];
const slider = (p, val) => (y) => [
  rect(1090, y + 27, 260, 2, T.bone, { opacity: 0.25 }),
  rect(1090, y + 27, Math.round(260 * p), 2, T.bone),
  rect(1090 + Math.round(260 * p) - 6, y + 21, 12, 14, T.bone, { radius: 2 }),
  txtR(val, 1440, y + 15, 70, { size: 22, lh: 26, style: "Medium", shadow: false }),
];
const toggle = (on) => (y) => [
  frame("toggle", 1388, y + 16, 52, 24, on ? { fill: T.bone, radius: 3 } : { stroke: { color: "#EEEAE073", weight: 1.5 }, radius: 3 }, [
    rect(on ? 30 : 4, 4, 18, 16, on ? T.ink : T.bone2, { radius: 2 }),
  ]),
  txtR(on ? "Wł." : "Wył.", 1370, y + 15, 70, { size: 18, lh: 26, style: "Medium", color: on ? T.bone : T.bone3, shadow: false }),
];
const G2 = {
  id: "G2_settings", title: "G2 · ustawienia",
  build() {
    const kids = [...M.blurBackdrop("street_near", "b0"), ...tabBar(["Grafika", "Sterowanie", "Dźwięk", "Rozgrywka"], 3)];
    let y = 150;
    kids.push(...row(y, "Krew (16+)", toggle(false), "Dodaje krew i brutalniejsze efekty"));
    y += 88;
    kids.push(...row(y, "Drżenie kamery", slider(0.6, "60 %")));
    y += 72;
    kids.push(...row(y, "Pole widzenia", slider(0.45, "90°")));
    y += 72;
    kids.push(...row(y, "Podpowiedzi klawiszy", toggle(true)));
    y += 72;
    kids.push(...row(y, "Język", stepper("Polski")));
    kids.push(...M.footerHints([["Q", "Poprzednia"], ["E", "Następna"], ["← →", "Zmień"], ["Esc", "Wróć"]]));
    return screen("G2 · ustawienia", null, kids, { vignette: false });
  },
};

// ---------- H fail
const H = {
  id: "H_fail", title: "H · misja nieudana",
  build() {
    const kids = [...M.blurBackdrop("street_near", "b8")];
    kids.push(rect(0, 0, 1920, 1080, { type: "radial", cx: 0.5, cy: 0.5, rx: 0.7, ry: 0.8, stops: [[0.45, "#8f1d1200"], [1, "#8f1d1255"]] }, { name: "edge red" }));
    kids.push(txtC("Zadanie nieudane", 960, 150, 800, { size: 14, lh: 16, style: "SemiBold", ls: "34%", case: "upper", color: T.bone2, shadow: false }));
    kids.push(txtC("Zostałeś trafiony", 960, 178, 1400, { size: 64, lh: 68, style: "SemiBold", ls: "8%", case: "upper", shadow: false }));
    kids.push(rect(900, 266, 120, 3, T.danger, { name: "accent rule" }));
    kids.push(txtC("Jedno trafienie kończy misję.", 960, 294, 900, { size: 22, lh: 28, style: "Medium", font: FONT.body, color: T.bone2, shadow: false }));
    kids.push(txtC("Ostatni checkpoint: rozmowa z Zofią, 15:41", 960, 330, 900, { size: 18, lh: 22, style: "Medium", font: FONT.body, color: T.bone3, shadow: false }));
    kids.push(M.button("Od checkpointu", 660, 400, 300, 56));
    kids.push(M.buttonGhost("Menu główne", 976, 400, 300, 56));
    kids.push(...M.footerHints([["Enter", "Od checkpointu"], ["Esc", "Menu"]]));
    return screen("H · misja nieudana", null, kids, { vignette: false });
  },
};

module.exports = [G1, G2, H];
