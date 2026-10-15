// A · HUD in stealth: hidden, safe. Everything bare on the image, one accent (olive = safe).
const { T, screen } = require("../lib.js");
const H = require("../hud.js");

module.exports = {
  id: "A_hud_calm",
  title: "A · HUD · skradanie (ukryty)",
  build() {
    return screen("A · HUD · skradanie", "street_near", [
      ...H.compass(224, [{ bearing: 238, label: "86 m" }]),
      ...H.objectives({}),
      ...H.visibility({ label: "Ukryty", color: T.safe, level: 0.1, detail: "w cieniu, kucanie", stamina: null }),
      ...H.weaponBlock({ icon: "w_vis", name: "Vis wz. 35", mag: 8, magMax: 8, reserve: 16 }),
      ...H.crosshair(0),
      ...H.prompt("E", "Porozmawiaj"),
    ]);
  },
};
