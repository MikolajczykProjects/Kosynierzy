// B2 · armoury at the radio station (act 3): choose one rifle; same card language as B.
const { screen } = require("../lib.js");
const M = require("../menu.js");

module.exports = {
  id: "B2_armoury_rifle",
  title: "B2 · zbrojownia, wybór karabinu",
  build() {
    const kids = [...M.blurBackdrop("hotel_lobby", "c4"), ...M.header("Zbrojownia", "Jeden karabin na resztę misji.")];
    const cw = 480, ch = 150, gap = 16;
    kids.push(...M.colHeader("Karabin", 64, 164, cw, "1 / 3"));
    ["wz29", "k98", "erma"].forEach((w, i) => kids.push(M.weaponCard(w, 64, 196 + i * (ch + gap), cw, ch, w === "wz29", i + 1)));
    kids.push(M.hero("wz29", 1040, 164, 816, 600));
    kids.push(M.loadoutStrip([{ slot: 1, icon: "w_vis" }, { slot: 2, icon: "w_wz29" }, { slot: 3, icon: "w_bagnet" }, { slot: 4, icon: "w_camera" }], 64, 800));
    kids.push(M.button("Weź karabin", 1556, 800));
    kids.push(...M.footerHints([["↑ ↓", "Wybór"], ["Enter", "Weź"], ["Esc", "Wróć"]]));
    return screen("B2 · zbrojownia", null, kids, { vignette: false });
  },
};
