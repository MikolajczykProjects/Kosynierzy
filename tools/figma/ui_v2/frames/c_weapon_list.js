// C · quick weapon list on switch (CS2 right-side list). Only owned slots are listed: 1 pistol, 3 blade, 4 camera (2 = rifle, not yet).
const { T, screen } = require("../lib.js");
const H = require("../hud.js");

module.exports = {
  id: "C_weapon_list",
  title: "C · zmiana broni (lista z prawej)",
  build() {
    return screen("C · zmiana broni", "street_near", [
      ...H.compass(224, [{ bearing: 238, label: "86 m" }]),
      ...H.objectives({}),
      ...H.visibility({ label: "Ukryty", color: T.safe, level: 0.1, detail: "w cieniu", stamina: null }),
      ...H.weaponList([{ slot: 1, icon: "w_vis" }, { slot: 3, icon: "w_bagnet" }, { slot: 4, icon: "w_camera" }], 3),
      ...H.weaponBlock({ icon: "w_bagnet", name: "Bagnet wz. 1924", melee: true, hint: "Od tyłu: cicha likwidacja" }),
      ...H.crosshair(0),
    ]);
  },
};
