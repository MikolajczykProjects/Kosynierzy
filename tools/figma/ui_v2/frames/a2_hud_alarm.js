// A2 · HUD in alarm/combat: red is used only for danger; crosshair opens, hit marker, kill popup, low ammo amber.
const { T, screen } = require("../lib.js");
const H = require("../hud.js");

module.exports = {
  id: "A2_hud_alarm",
  title: "A2 · HUD · alarm i walka",
  build() {
    return screen("A2 · HUD · alarm", "street_far", [
      H.edgeVignette("#8f1d12", "66"),
      ...H.compass(198, [{ bearing: 238, label: "86 m" }]),
      ...H.objectives({ detail: "86 m", optional: [], counters: false }),
      ...H.alarmBadge("Alarm", T.danger, "SD wie, gdzie jesteś"),
      ...H.visibility({ label: "Wykryty", color: T.danger, level: 1, detail: "biegniesz, w świetle", stamina: 0.6 }),
      ...H.weaponBlock({ icon: "w_vis", name: "Vis wz. 35", mag: 2, magMax: 8, reserve: 8 }),
      ...H.prompt("R", "Przeładuj", 1608, 800),
      ...H.crosshair(9),
      ...H.hitMarker(),
      ...H.targetTag("SD", "wróg", T.danger, 960, 596),
      ...H.popup("Trafienie w głowę", "SD wyeliminowany", 714),
    ], { vignette: false });
  },
};
