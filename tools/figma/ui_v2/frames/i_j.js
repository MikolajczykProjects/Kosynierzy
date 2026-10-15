// I history card (paper = diegetic object), J main menu (radio tower motif).
const { T, FONT, BG, screen, frame, rect, hair, txt, txtC, txtR } = require("../lib.js");
const M = require("../menu.js");

const I = {
  id: "I_history_card", title: "I · karta historii (dokument)",
  build() {
    const kids = [...M.blurBackdrop("street_far", "b0")];
    const w = 800, h = 780, x = 560, y = 120;
    const ink = T.paperInk, dim = "#25221Bb3";
    const mono = (s, px, py, o = {}) => txt(s, px, py, Object.assign({ font: FONT.mono, style: "Regular", size: 19, lh: 31, color: ink, shadow: false, w: w - 128 }, o));
    kids.push(frame("document", x, y, w, h, { fill: T.paper, effects: [{ type: "shadow", x: 0, y: 24, blur: 60, color: "#000000b0" }] }, [
      mono("Karta historii", 64, 56, { size: 15, lh: 18, style: "Bold", ls: "20%", case: "upper", color: dim }),
      txtR("3 / 6", w - 64, 56, 120, { font: FONT.mono, style: "Bold", size: 15, lh: 18, color: dim, shadow: false }),
      rect(64, 92, w - 128, 2, ink, { opacity: 0.7 }),
      mono("Prowokacja gliwicka", 64, 120, { size: 38, lh: 44, style: "Bold" }),
      mono("31 sierpnia 1939, radiostacja Gleiwitz", 64, 178, { size: 16, lh: 22, color: dim }),
      mono("Wieczorem oddział SD pod dowództwem Alfreda Naujocksa, przebrany za powstańców śląskich, zajął rozgłośnię w Gliwicach. W eterze padło dziewięć słów po polsku.", 64, 236),
      mono("Zamordowanego dzień wcześniej Franciszka Honioka podrzucono jako „napastnika”. Całe zajście miało uzasadnić atak na Polskę.", 64, 360),
      rect(64, 650, w - 128, 1, ink, { opacity: 0.4 }),
      mono("Źródło: dzieje.pl", 64, 666, { size: 14, lh: 18, color: dim }),
    ]));
    kids.push(...M.footerHints([["E", "Zamknij"], ["← →", "Inne karty"]]));
    return screen("I · karta historii", null, kids, { vignette: false });
  },
};

// radio tower: tapered wooden lattice (the 111 m Gleiwitz tower), drawn as thin lines
function towerSvg() {
  const W = 420, Hh = 940, cx = 210;
  const xl = (y) => cx - 10 - (150 * y) / Hh, xr = (y) => cx + 10 + (150 * y) / Hh;
  const lv = [0, 90, 200, 330, 480, 650, 840, 940];
  let d = `M${xl(0)} 0 L${xl(Hh)} ${Hh} M${xr(0)} 0 L${xr(Hh)} ${Hh} M${cx - 4} -70 V0`;
  for (let i = 0; i < lv.length; i++) {
    d += ` M${xl(lv[i])} ${lv[i]} H${xr(lv[i])}`;
    if (i < lv.length - 1) {
      const a = lv[i], b = lv[i + 1];
      d += ` M${xl(a)} ${a} L${xr(b)} ${b} M${xr(a)} ${a} L${xl(b)} ${b}`;
    }
  }
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 -80 ${W} ${Hh + 80}"><path d="${d}" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"/></svg>`;
}
const J = {
  id: "J_main_menu", title: "J · menu główne",
  build() {
    const kids = [
      rect(-20, -20, 1960, 1120, BG("street_far"), { name: "backdrop (Play capture)", effects: [{ type: "blur", radius: 5 }] }),
      rect(0, 0, 1920, 1080, "#06080a99", { name: "wash" }),
      rect(0, 0, 1100, 1080, { type: "linear", angle: 90, stops: [[0, "#000000cc"], [1, "#00000000"]] }, { name: "left shade" }),
      rect(0, 0, 1920, 1080, { type: "radial", cx: 0.5, cy: 0.5, rx: 0.8, ry: 0.85, stops: [[0.5, "#00000000"], [1, "#000000aa"]] }, { name: "vignette" }),
      { t: "svg", name: "wieża radiostacji", svg: towerSvg(), x: 1380, y: 110, w: 420, h: 940, color: "#EEEAE0", opacity: 0.3 },
    ];
    kids.push(txt("Sender", 124, 190, { size: 132, lh: 120, style: "Bold", ls: "3%", case: "upper", shadow: false, w: 1000 }));
    kids.push(txt("Gleiwitz", 124, 306, { size: 132, lh: 120, style: "Bold", ls: "3%", case: "upper", shadow: false, w: 1000 }));
    kids.push(rect(128, 458, 96, 3, T.bone, { name: "title rule" }));
    kids.push(txt("31 sierpnia 1939", 128, 480, { size: 24, lh: 28, style: "Medium", ls: "34%", case: "upper", color: T.bone2, shadow: false, w: 700 }));
    const items = ["Nowa gra", "Kontynuuj", "Ustawienia", "Wyjście"];
    items.forEach((s, i) => {
      const y = 640 + i * 56, on = i === 0;
      if (on) kids.push(rect(104, y + 4, 4, 32, T.bone, { name: "active bar" }));
      kids.push(txt(s, 128, y, { size: 34, lh: 40, style: "SemiBold", ls: "10%", case: "upper", color: on ? T.bone : "#EEEAE08c", shadow: false, w: 600 }));
    });
    kids.push(txtR("Wersja hackathonowa", 1856, 1020, 400, { size: 13, lh: 16, style: "Medium", ls: "10%", case: "upper", color: T.bone3, shadow: false }));
    return screen("J · menu główne", null, kids, { vignette: false });
  },
};

module.exports = [I, J];
