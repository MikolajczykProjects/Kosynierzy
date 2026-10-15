// D viewfinder + evidence print, E detection states, F objective update sequence.
const { T, FONT, BG, SH, screen, frame, rect, hair, txt, txtC, txtR, icon, keycap } = require("../lib.js");
const H = require("../hud.js");

// ---------- D1 viewfinder
function brackets(x0, y0, x1, y1, len, w, color) {
  const r = (x, y, ww, hh) => rect(x, y, ww, hh, color, { name: "bracket" });
  return [
    r(x0, y0, len, w), r(x0, y0, w, len), r(x1 - len, y0, len, w), r(x1 - w, y0, w, len),
    r(x0, y1 - w, len, w), r(x0, y1 - len, w, len), r(x1 - len, y1 - w, len, w), r(x1 - w, y1 - len, w, len),
  ];
}
function viewfinder() {
  const kids = [];
  const mask = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1920 1080"><path fill="#05060b" fill-opacity="0.9" fill-rule="evenodd" d="M0 0H1920V1080H0Z M176 80H1744A36 36 0 0 1 1780 116V964A36 36 0 0 1 1744 1000H176A36 36 0 0 1 140 964V116A36 36 0 0 1 176 80Z"/></svg>`;
  kids.push({ t: "svg", name: "viewfinder mask", svg: mask, x: 0, y: 0, w: 1920, h: 1080 });
  kids.push(rect(140, 80, 1640, 920, null, { radius: 36, stroke: { color: "#EEEAE026", weight: 1 }, name: "window edge" }));
  kids.push(...brackets(384, 216, 1536, 864, 48, 2, T.bone));
  kids.push(txt("Kadr dowodowy", 384, 190, { size: 12, lh: 14, style: "SemiBold", ls: "18%", case: "upper", color: T.bone3, shadow: false, w: 300 }));
  kids.push(rect(932, 516, 56, 48, "#E2A93B1f", { stroke: { color: T.warn, weight: 1.5 }, radius: 2, name: "focus patch" }));
  kids.push(rect(959, 539, 2, 2, T.bone, { name: "centre" }));
  // bottom info
  kids.push(txt("1/60", 188, 944, { size: 18, lh: 22, style: "SemiBold", ls: "10%", shadow: false, w: 120 }));
  kids.push(txtC("Zdjęcie 4 / 12", 960, 940, 400, { size: 22, lh: 26, style: "SemiBold", ls: "18%", case: "upper", shadow: false }));
  kids.push(txtR("F — zamknij", 1732, 944, 260, { size: 16, lh: 22, style: "Medium", color: T.bone2, shadow: false }));
  return kids;
}
const D1 = { id: "D1_viewfinder", title: "D1 · wizjer aparatu", build: () => screen("D1 · wizjer", "street_mid", viewfinder(), { vignette: false }) };

// ---------- D2 evidence print
function printCard() {
  const w = 288, h = 244;
  return frame("evidence print", 64, 676, w, h, { fill: T.paper, rot: -2.5, effects: [{ type: "shadow", x: 0, y: 8, blur: 24, color: "#000000a0" }] }, [
    rect(14, 14, w - 28, 156, BG("street_mid"), { name: "photo" }),
    txt("Patrol SD, ul. Dworcowa", 14, 182, { font: FONT.mono, style: "Regular", size: 15, lh: 20, color: T.paperInk, shadow: false, w: w - 28 }),
    txt("31.08.1939, 15:42", 14, 204, { font: FONT.mono, style: "Regular", size: 13, lh: 18, color: "#25221Bb0", shadow: false, w: w - 28 }),
    frame("stamp", w - 132, 130, 116, 38, { rot: -8, stroke: { color: T.stampRed, weight: 2 }, radius: 3 }, [
      txt("Dowód", 0, 0, { w: 116, h: 38, align: "center", valign: "center", font: FONT.display, style: "Bold", size: 24, lh: 24, ls: "16%", case: "upper", color: T.stampRed, shadow: false }),
    ]),
  ]);
}
const D2 = {
  id: "D2_evidence_print", title: "D2 · dowód zabezpieczony",
  build: () => screen("D2 · dowód", "street_mid", [
    ...H.compass(224, []), ...H.objectives({ optional: [], counters: "DOWODY 3/6   KARTY 1/6" }),
    ...H.visibility({ label: "Ukryty", color: T.safe, level: 0.1, detail: "w cieniu", stamina: null }),
    ...H.weaponBlock({ icon: "w_camera", name: "Aparat Leica", melee: true, hint: "LPM — zdjęcie" }),
    ...H.crosshair(0), printCard(),
    ...H.popup("Dowód zabezpieczony", "Zdjęcie 3 / 6", 700).map((n) => n),
  ]),
};

// ---------- E detection states
function arcMarker(cx, cy, centreDeg, fill, color) {
  const R = 66, span = 76;
  const a0 = centreDeg - span / 2, a1 = centreDeg + span / 2;
  const kids = [
    { t: "ellipse", name: "arc track", x: cx - R, y: cy - R, w: R * 2, fill: "#EEEAE033", arc: { from: a0, to: a1, inner: 0.93 } },
  ];
  if (fill > 0) kids.push({ t: "ellipse", name: "arc fill", x: cx - R, y: cy - R, w: R * 2, fill: color, arc: { from: a0, to: a0 + span * fill, inner: 0.93 }, effects: [{ type: "shadow", x: 0, y: 0, blur: 6, color: color + "aa" }] });
  return kids;
}
function plate(label, x, y, bg, fg) {
  const w = 24 + label.length * 11;
  return frame("plate " + label, x - w / 2, y, w, 28, { fill: bg, radius: 2 }, [txt(label, 0, 0, { w, h: 28, align: "center", valign: "center", size: 15, lh: 15, style: "SemiBold", ls: "16%", case: "upper", color: fg, shadow: false })]);
}
function detectionCol(i, s) {
  const cw = 456, ch = 330, x = 24 + i * (cw + 8);
  const cx = cw / 2, cy = 150;
  const kids = [
    rect(-732, -390, 1920, 1080, BG("street_mid"), { name: "backdrop crop" }),
    rect(0, 0, cw, ch, { type: "radial", cx: 0.5, cy: 0.45, rx: 0.7, ry: 0.7, stops: [[0.4, "#00000000"], [1, "#00000070"]] }),
  ];
  if (s.vig) kids.push(rect(0, 0, cw, ch, { type: "radial", cx: 0.5, cy: 0.5, rx: 0.7, ry: 0.8, stops: [[0.45, "#8f1d1200"], [1, "#8f1d1288"]] }));
  kids.push(rect(cx - 1, cy - 15, 2, 9, T.bone), rect(cx - 1, cy + 6, 2, 9, T.bone), rect(cx - 15, cy - 1, 9, 2, T.bone), rect(cx + 6, cy - 1, 9, 2, T.bone), rect(cx - 1, cy - 1, 2, 2, T.bone));
  if (s.fill > 0 || s.track) kids.push(...arcMarker(cx, cy, -55, s.fill, s.color));
  if (s.marker) kids.push(icon(s.marker, cx + 66 * Math.cos((-55 * Math.PI) / 180) - 9, cy + 66 * Math.sin((-55 * Math.PI) / 180) - 9 - 0, 18, s.color, { name: "observer marker" }));
  if (s.plate) kids.push(plate(s.plate, cx, 232, s.color, s.fg || T.ink));
  return { col: frame("E" + (i + 1), x, 24, cw, ch, { clip: true, fill: "#0b0d0f" }, kids), x };
}
const E = {
  id: "E_detection", title: "E · stany wykrycia",
  build() {
    const states = [
      { name: "1  Nie widzą cię", note: "Brak łuku, brak plakietki", fill: 0, color: T.bone },
      { name: "2  Podejrzenie", note: "Łuk rośnie wraz z miernikiem obserwatora", fill: 0.45, color: T.warn, marker: "g_diamond_o", plate: "Podejrzenie" },
      { name: "3  Poszukiwania", note: "Łuk pełny, znacznik wypełniony", fill: 1, color: T.warn, marker: "g_diamond", plate: "Poszukiwania" },
      { name: "4  Alarm", note: "Czerwień tylko tutaj, brzeg ekranu +8 %", fill: 1, color: T.danger, marker: "g_diamond", plate: "Alarm", fg: "#fff", vig: true },
    ];
    const kids = [];
    states.forEach((s, i) => {
      const { col, x } = detectionCol(i, s);
      kids.push(col);
      kids.push(txt(s.name, x, 372, { size: 22, lh: 26, style: "SemiBold", ls: "8%", case: "upper", shadow: false, w: 440 }));
      kids.push(txt(s.note, x, 404, { size: 16, lh: 22, style: "Medium", font: FONT.body, color: T.bone2, shadow: false, w: 440 }));
    });
    kids.push(hair(24, 460, 1872));
    kids.push(txt("Łuk (2–4 px) wokół celownika, po jednym na obserwatora, zwrócony w jego stronę. Biały → bursztyn → czerwień zależnie od miernika. Plakietka stanu: 26 px, wersaliki, ciemny tekst na kolorze stanu.", 24, 476, { size: 16, lh: 24, style: "Regular", font: FONT.body, color: T.bone2, shadow: false, w: 1200 }));
    return frame("E · stany wykrycia", 0, 0, 1920, 560, { clip: true, fill: "#0b0d0f" }, kids);
  },
};

// ---------- F objective sequence
function objCrop(i, objState, caption, timing) {
  const cw = 456, ch = 330, x = 24 + i * (cw + 8);
  const objs = H.objectives(objState).map((n) => Object.assign({}, n, { x: -1416, y: -10 }));
  const kids = [rect(-1416, 0, 1920, 1080, BG("street_near"), { name: "backdrop crop" }), ...objs];
  return [
    frame("F" + (i + 1), x, 24, cw, ch, { clip: true, fill: "#0b0d0f" }, kids),
    txt(caption, x, 372, { size: 22, lh: 26, style: "SemiBold", ls: "8%", case: "upper", shadow: false, w: 440 }),
    txt(timing, x, 404, { size: 16, lh: 22, style: "Medium", font: FONT.body, color: T.bone2, shadow: false, w: 440 }),
  ];
}
const F = {
  id: "F_objectives", title: "F · aktualizacja zadania",
  build() {
    const base = { optional: [], counters: false };
    const kids = [
      ...objCrop(0, Object.assign({ mode: "new", obj: "Zdobądź klucz od recepcjonisty", detail: "0 / 2", objW: 288 }, base), "1  Nowe zadanie", "Wjazd z prawej 0,25 s, etykieta bursztynowa, trzyma 2,5 s"),
      ...objCrop(1, Object.assign({ obj: "Zdobądź klucz od recepcjonisty", detail: "1 / 2" }, base), "2  Postęp", "Licznik zmienia się bez ruchu, liczba mignie bielą 0,15 s"),
      ...objCrop(2, Object.assign({ mode: "done", obj: "Zdobądź klucz od recepcjonisty", detail: "Zrobione", objW: 288 }, base), "3  Zrobione", "Ptaszek skacze 0,15 s, tekst szarzeje 0,3 s, przekreślenie"),
      ...objCrop(3, Object.assign({ mode: "out", obj: "Zdobądź klucz od recepcjonisty", detail: "", objW: 288 }, base), "4  Znika", "Po 1,2 s zwija się 0,4 s; następne zadanie wjeżdża po 0,5 s"),
    ];
    kids.push(hair(24, 460, 1872));
    kids.push(txt("Czasy to propozycje do sprawdzenia w grze. Dodatkowe zadania i liczniki dowodów zostają pod głównym zadaniem, ciszej.", 24, 476, { size: 16, lh: 24, style: "Regular", font: FONT.body, color: T.bone2, shadow: false, w: 1200 }));
    return frame("F · aktualizacja zadania", 0, 0, 1920, 560, { clip: true, fill: "#0b0d0f" }, kids);
  },
};

module.exports = [D1, D2, E, F];
