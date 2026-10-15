// Renders every icon of icons.js onto a contact sheet with headless Edge and writes the PNG (local files only).
// usage: node preview_icons.js [outPng] [bg] [fg] [scale]
"use strict";
const fs = require("fs");
const os = require("os");
const path = require("path");
const { spawnSync } = require("child_process");
const { ICONS } = require("./icons.js");

const out = path.resolve(process.argv[2] || path.join(os.tmpdir(), "icons_preview.png"));
const bg = process.argv[3] || "#16181b";
const fg = process.argv[4] || "#ece8dc";
const scale = parseFloat(process.argv[5] || "2");

const names = Object.keys(ICONS);
const cols = 3;
const cells = names.map((n) => {
  const i = ICONS[n];
  const s = n.startsWith("g_") ? scale * 2 : scale;
  return `<div class="c"><svg width="${i.w * s}" height="${i.h * s}" viewBox="0 0 ${i.w} ${i.h}" style="color:${fg}">${i.body}</svg><span>${n}</span></div>`;
}).join("");
const rows = Math.ceil(names.length / cols);
const maxH = 120 * scale;
const html = `<!doctype html><meta charset="utf-8"><style>body{margin:0;background:${bg};font:12px Consolas,monospace;color:#8a8f94}
.g{display:grid;grid-template-columns:repeat(${cols},1fr);gap:8px;padding:12px}.c{display:flex;flex-direction:column;align-items:center;justify-content:center;border:1px solid #2a2d31;padding:10px}
span{margin-top:6px}</style><div class="g">${cells}</div>`;
const htmlPath = path.join(os.tmpdir(), "icons_preview.html");
fs.writeFileSync(htmlPath, html);
const width = 1500, height = Math.min(4000, rows * (maxH + 50) + 40);
const edge = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
const r = spawnSync(edge, ["--headless=new", "--disable-gpu", "--hide-scrollbars", `--screenshot=${out}`, `--window-size=${width},${height}`, "file:///" + htmlPath.replace(/\\/g, "/")], { encoding: "utf8", timeout: 30000 });
console.log(fs.existsSync(out) ? "wrote " + out : "FAILED " + (r.stderr || r.error));
