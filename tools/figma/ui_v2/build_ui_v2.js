// Renders every frame in frames/*.js to PNG (headless Edge). usage: node build_ui_v2.js [--only id,id] [--out dir] [--scale n]
"use strict";
const fs = require("fs");
const path = require("path");
const { renderPng } = require("./render_html.js");

const args = process.argv.slice(2);
const opt = (k, d) => { const i = args.indexOf(k); return i >= 0 ? args[i + 1] : d; };
const only = opt("--only") ? opt("--only").split(",") : null;
const outDir = path.resolve(opt("--out", path.join(__dirname, "..", "..", "..", "docs", "design", "ui_v2", "png")));
const scale = parseFloat(opt("--scale", "1"));
const FONTS = ["Barlow+Condensed:wght@400;500;600;700", "Barlow:wght@400;500;600;700", "Courier+Prime:wght@400;700"];

const dir = path.join(__dirname, "frames");
const files = fs.readdirSync(dir).filter((f) => f.endsWith(".js")).sort();
for (const f of files) {
  const mod = require(path.join(dir, f));
  for (const m of Array.isArray(mod) ? mod : [mod]) {
    if (only && !only.includes(m.id)) continue;
    const spec = m.build();
    const out = path.join(outDir, m.id + ".png");
    renderPng(spec, out, { w: spec.w, h: spec.h, googleFonts: FONTS, scale });
    console.log("rendered", m.id, "->", out);
  }
}
