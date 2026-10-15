// B · start loadout (pistol + blade, rifle locked until the armoury) — CS2 loadout columns: art-dominant cards, descriptor line, hero detail.
const { screen } = require("../lib.js");
const M = require("../menu.js");

module.exports = {
  id: "B_loadout_start",
  title: "B · wyposażenie na start (siatka jak w CS2)",
  build() {
    const kids = [...M.blurBackdrop("hotel_lobby"), ...M.header("Wyposażenie", "Pistolet i broń biała na start. Karabin odbierzesz w zbrojowni.")];
    const cw = 280, ch = 140, gap = 16;
    kids.push(...M.colHeader("Pistolet", 64, 164, cw, "1 / 3"));
    ["vis", "browning", "nagant"].forEach((w, i) => kids.push(M.weaponCard(w, 64, 196 + i * (ch + gap), cw, ch, w === "vis", i + 1)));
    kids.push(...M.colHeader("Broń biała", 64 + 304, 164, cw, "1 / 2"));
    ["bagnet", "noz"].forEach((w, i) => kids.push(M.weaponCard(w, 64 + 304, 196 + i * (ch + gap), cw, ch, w === "bagnet", i + 1)));
    kids.push(...M.colHeader("Karabin", 64 + 608, 164, cw));
    kids.push(M.lockedCard("Zbrojownia", "Odblokujesz w akcie 3", 64 + 608, 196, cw, ch));
    kids.push(M.hero("vis", 1040, 164, 816, 600));
    kids.push(M.loadoutStrip([{ slot: 1, icon: "w_vis" }, { slot: 2, icon: null }, { slot: 3, icon: "w_bagnet" }, { slot: 4, icon: "w_camera" }], 64, 800));
    kids.push(M.button("Zatwierdź", 1556, 800));
    kids.push(...M.footerHints([["← →", "Wybór"], ["Tab", "Kategoria"], ["Enter", "Zatwierdź"]]));
    return screen("B · wyposażenie (start)", null, kids, { vignette: false });
  },
};
