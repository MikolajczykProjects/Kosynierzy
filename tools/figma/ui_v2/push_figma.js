// Pushes every UI v2 frame into the user's Figma file through OUR plugin (channel "claude", plugin v2 must be running).
// usage: node push_figma.js [--only id,id] [--x 44400] [--y 0] [--rename-v1 292:2]
"use strict";
const fs = require("fs");
const os = require("os");
const path = require("path");
const { open } = require(path.join(os.homedir(), ".claude", "narzedzia", "figma-most", "figma_cmd.js"));

const args = process.argv.slice(2);
const opt = (k, d) => { const i = args.indexOf(k); return i >= 0 ? args[i + 1] : d; };
const only = opt("--only") ? opt("--only").split(",") : null;
const X0 = parseFloat(opt("--x", "44400")), Y0 = parseFloat(opt("--y", "0"));

// attach base64 to image paints that only carry a file path
function inline(node) {
  const fix = (p) => {
    if (p && typeof p === "object" && !Array.isArray(p) && p.type === "image" && p.file && !p.base64) p.base64 = fs.readFileSync(p.file).toString("base64");
    if (Array.isArray(p)) p.forEach(fix);
  };
  fix(node.fill);
  (node.children || []).forEach(inline);
  return node;
}

(async () => {
  const f = await open({ timeout: 180000 });
  const v = await f.cmd("version", {}, 5000).catch(() => null);
  if (!v) { console.error("Our plugin is not answering on channel 'claude'. Import + run it in Figma first."); process.exit(3); }
  if (opt("--rename-v1")) await f.cmd("rename_node", { nodeId: opt("--rename-v1"), name: "ODRZUCONE · UI v1 · za bardzo AI" });
  const dir = path.join(__dirname, "frames");
  const frames = [];
  for (const file of fs.readdirSync(dir).filter((n) => n.endsWith(".js")).sort()) {
    const mod = require(path.join(dir, file));
    for (const m of Array.isArray(mod) ? mod : [mod]) if (!only || only.includes(m.id)) frames.push(m);
  }
  const GAP = 120, COLS = 3;
  let col = 0, x = 0, y = 0, rowH = 0, maxX = 0;
  const placed = [];
  for (const m of frames) {
    const spec = inline(m.build());
    if (col === COLS) { col = 0; x = 0; y += rowH + GAP; rowH = 0; }
    spec.x = x; spec.y = y; spec.name = m.title;
    placed.push(spec);
    x += spec.w + GAP; rowH = Math.max(rowH, spec.h); maxX = Math.max(maxX, x - GAP); col++;
  }
  const parent = await f.cmd("build", { spec: { t: "frame", name: "SENDER GLEIWITZ · UI v2 · " + new Date().toISOString().slice(0, 10) + " · propozycja", x: X0, y: Y0, w: maxX + 160, h: y + rowH + 160, fill: "#0b0d0f" } });
  for (const s of placed) {
    s.x += 80; s.y += 80;
    const r = await f.build(s, parent.id);
    console.log("pushed", s.name, r.id, "nodes", r.count, r.warnings && r.warnings.length ? r.warnings : "");
  }
  console.log("parent", parent.id);
  f.close();
})().catch((e) => { console.error("ERROR:", e.message); process.exit(1); });
