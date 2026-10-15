// Spec -> HTML -> PNG (headless Edge). Renders the SAME node specs that the Figma plugin's `build` command takes
// (frame / rect / ellipse / text / svg, absolute positions only), so every frame can be previewed locally and exported
// even without Figma. Text metrics differ slightly from Figma; geometry is identical.
// usage (library): const { renderPng } = require("./render_html.js"); await renderPng(spec, "out.png", {w:1920,h:1080});
"use strict";
const fs = require("fs");
const os = require("os");
const path = require("path");
const { spawnSync } = require("child_process");

const EDGE = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
const WEIGHT = { thin: 100, extralight: 200, light: 300, regular: 400, medium: 500, semibold: 600, bold: 700, extrabold: 800, black: 900 };
const STYLE_ALIAS = (s) => String(s || "Regular").toLowerCase().replace(/[\s_-]+/g, "");

function color(c) {
  if (c === undefined || c === null) return "transparent";
  if (typeof c === "string") {
    const m = /^#([0-9a-f]{3,8})$/i.exec(c.trim());
    if (m) {
      let h = m[1];
      if (h.length === 3 || h.length === 4) h = h.split("").map((x) => x + x).join("");
      const r = parseInt(h.slice(0, 2), 16), g = parseInt(h.slice(2, 4), 16), b = parseInt(h.slice(4, 6), 16);
      const a = h.length === 8 ? parseInt(h.slice(6, 8), 16) / 255 : 1;
      return `rgba(${r},${g},${b},${+a.toFixed(3)})`;
    }
    return c;
  }
  return `rgba(${Math.round(c.r * 255)},${Math.round(c.g * 255)},${Math.round(c.b * 255)},${c.a === undefined ? 1 : c.a})`;
}

function stops(list) {
  return list.map((s, i) => {
    let pos, col;
    if (Array.isArray(s)) { pos = s[0]; col = s[1]; } else if (s && s.color !== undefined) { pos = s.pos !== undefined ? s.pos : s.position; col = s.color; } else { pos = i / (list.length - 1); col = s; }
    return `${color(col)} ${+(pos * 100).toFixed(2)}%`;
  }).join(",");
}

// Returns a CSS background value for a paint spec (colour | {type:linear|radial|image})
function paintCss(p, w, h) {
  if (p === null || p === undefined || p === false) return null;
  if (Array.isArray(p)) return p.map((q) => paintCss(q, w, h)).filter(Boolean).reverse().join(",");
  if (typeof p === "string" || p.r !== undefined) { const c = color(p); return `linear-gradient(${c},${c})`; }
  const t = String(p.type || "solid").toLowerCase();
  if (t === "solid") { const c = color(p.color); return `linear-gradient(${c},${c})`; }
  if (t === "linear") return `linear-gradient(${p.angle === undefined ? 180 : p.angle}deg,${stops(p.stops)})`;
  if (t === "radial") {
    const cx = p.cx === undefined ? 0.5 : p.cx, cy = p.cy === undefined ? 0.5 : p.cy, rx = p.rx === undefined ? 0.5 : p.rx, ry = p.ry === undefined ? 0.5 : p.ry;
    return `radial-gradient(ellipse ${+(rx * w).toFixed(1)}px ${+(ry * h).toFixed(1)}px at ${cx * 100}% ${cy * 100}%,${stops(p.stops)})`;
  }
  if (t === "image") {
    const src = p.base64 ? `data:image/jpeg;base64,${p.base64}` : "file:///" + path.resolve(p.file).replace(/\\/g, "/");
    return `url('${src.replace(/ /g, "%20")}') center/${p.mode === "FIT" ? "contain" : "cover"} no-repeat`;
  }
  return null;
}

const esc = (s) => String(s).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");

function donutPath(w, h, arc) {
  const cx = w / 2, cy = h / 2, R = Math.min(w, h) / 2, r = R * (arc.inner || 0);
  const a0 = ((arc.from || 0) * Math.PI) / 180, a1 = ((arc.to === undefined ? 360 : arc.to) * Math.PI) / 180;
  const pt = (rad, a) => [cx + rad * Math.cos(a), cy + rad * Math.sin(a)];
  const large = Math.abs(a1 - a0) > Math.PI ? 1 : 0;
  const [x0, y0] = pt(R, a0), [x1, y1] = pt(R, a1), [x2, y2] = pt(r, a1), [x3, y3] = pt(r, a0);
  if (Math.abs(a1 - a0) >= 2 * Math.PI - 1e-6) {
    return `M${cx} ${cy - R} A${R} ${R} 0 1 1 ${cx} ${cy + R} A${R} ${R} 0 1 1 ${cx} ${cy - R} Z` + (r > 0 ? `M${cx} ${cy - r} A${r} ${r} 0 1 0 ${cx} ${cy + r} A${r} ${r} 0 1 0 ${cx} ${cy - r} Z` : "");
  }
  return `M${x0} ${y0} A${R} ${R} 0 ${large} 1 ${x1} ${y1} ` + (r > 0 ? `L${x2} ${y2} A${r} ${r} 0 ${large} 0 ${x3} ${y3} Z` : `L${cx} ${cy} Z`);
}

function node(s, isRoot) {
  const kind = String(s.t || s.type || "frame").toLowerCase();
  const w = s.w !== undefined ? s.w : (kind === "text" ? undefined : 100);
  const h = s.h !== undefined ? s.h : (kind === "text" ? undefined : (kind === "ellipse" ? w : 100));
  const css = [];
  css.push("position:absolute", `left:${s.x || 0}px`, `top:${s.y || 0}px`);
  if (isRoot) { css[0] = "position:relative"; css.splice(1, 2); }
  if (w !== undefined) css.push(`width:${w}px`);
  if (h !== undefined) css.push(`height:${h}px`);
  css.push("box-sizing:border-box");
  if (s.opacity !== undefined) css.push(`opacity:${s.opacity}`);
  if (s.rot) css.push(`transform:rotate(${s.rot}deg)`);
  if (s.blend) css.push(`mix-blend-mode:${String(s.blend).toLowerCase().replace(/_/g, "-")}`);
  if (s.visible === false) css.push("display:none");

  const filters = [], shadows = [], textShadows = [];
  for (const e of s.effects || []) {
    const t = String(e.type || "shadow").toLowerCase();
    if (t === "shadow" || t === "drop_shadow") {
      shadows.push(`${e.x || 0}px ${e.y || 0}px ${e.blur === undefined ? 4 : e.blur}px ${e.spread || 0}px ${color(e.color || "#00000066")}`);
      textShadows.push(`${e.x || 0}px ${e.y || 0}px ${e.blur === undefined ? 4 : e.blur}px ${color(e.color || "#00000066")}`);
    } else if (t === "inner_shadow" || t === "inner") shadows.push(`inset ${e.x || 0}px ${e.y || 0}px ${e.blur === undefined ? 4 : e.blur}px ${e.spread || 0}px ${color(e.color || "#00000066")}`);
    else if (t === "blur" || t === "layer_blur") filters.push(`blur(${e.radius === undefined ? 4 : e.radius}px)`);
    else if (t === "bg_blur" || t === "background_blur") css.push(`backdrop-filter:blur(${e.radius === undefined ? 8 : e.radius}px)`);
  }

  if (s.radius !== undefined) css.push(`border-radius:${Array.isArray(s.radius) ? s.radius.map((r) => r + "px").join(" ") : s.radius + "px"}`);
  if (kind === "ellipse" && !s.arc) css.push("border-radius:50%");
  if (s.stroke) {
    const st = s.stroke, c = color(st.color !== undefined ? st.color : "#000"), wt = st.weight === undefined ? 1 : st.weight;
    if (st.top !== undefined || st.right !== undefined || st.bottom !== undefined || st.left !== undefined) {
      for (const side of ["top", "right", "bottom", "left"]) if (st[side]) css.push(`border-${side}:${st[side]}px ${st.dash ? "dashed" : "solid"} ${c}`);
    } else if (st.dash) css.push(`border:${wt}px dashed ${c}`);
    else if (st.align === "OUTSIDE") shadows.unshift(`0 0 0 ${wt}px ${c}`);
    else shadows.unshift(`inset 0 0 0 ${wt}px ${c}`);
  }
  if (kind === "ellipse" && s.arc) { const fx = (s.effects||[]).filter(e=>/shadow/i.test(e.type||"")).map(e=>`drop-shadow(${e.x||0}px ${e.y||0}px ${(e.blur===undefined?4:e.blur)/2}px ${color(e.color||"#00000066")})`); if (fx.length) filters.push(...fx); }
  else if (kind !== "text" && kind !== "svg" && shadows.length) css.push(`box-shadow:${shadows.join(",")}`);
  if (kind === "svg" && (s.effects || []).length) filters.push(...(s.effects || []).filter((e) => /shadow/i.test(e.type || "")).map((e) => `drop-shadow(${e.x || 0}px ${e.y || 0}px ${e.blur === undefined ? 4 : e.blur / 2}px ${color(e.color || "#00000066")})`));
  if (filters.length) css.push(`filter:${filters.join(" ")}`);

  let inner = "";
  if (kind === "text") {
    css.push(`font-family:'${s.font || "Inter"}',sans-serif`);
    const stl = STYLE_ALIAS(s.style || (s.weight ? Object.keys(WEIGHT).find((k) => WEIGHT[k] === s.weight) : "regular"));
    css.push(`font-weight:${WEIGHT[stl] || 400}`);
    if (/italic/.test(stl)) css.push("font-style:italic");
    css.push(`font-size:${s.size || 14}px`);
    const col = s.color !== undefined ? s.color : (s.fill !== undefined ? s.fill : "#000");
    css.push(`color:${color(typeof col === "object" && col.type ? "#fff" : col)}`);
    if (s.ls !== undefined) css.push(`letter-spacing:${typeof s.ls === "string" && s.ls.endsWith("%") ? parseFloat(s.ls) / 100 + "em" : s.ls + "px"}`);
    if (s.lh !== undefined) css.push(`line-height:${typeof s.lh === "string" ? (s.lh === "auto" ? "normal" : s.lh.endsWith("%") ? parseFloat(s.lh) / 100 : s.lh) : s.lh + "px"}`); else css.push("line-height:normal");
    if (s.case) css.push(`text-transform:${{ upper: "uppercase", lower: "lowercase", title: "capitalize" }[String(s.case).toLowerCase()] || "none"}`);
    if (s.align) css.push(`text-align:${String(s.align).toLowerCase()}`);
    if (s.decoration) css.push(`text-decoration:${String(s.decoration).toLowerCase().replace("_", "-")}`);
    css.push(s.w !== undefined ? "white-space:pre-wrap" : "white-space:pre");
    if (textShadows.length) css.push(`text-shadow:${textShadows.join(",")}`);
    if (s.valign && h !== undefined) css.push("display:flex", `align-items:${{ top: "flex-start", center: "center", bottom: "flex-end" }[String(s.valign).toLowerCase()]}`, `justify-content:${{ left: "flex-start", center: "center", right: "flex-end" }[String(s.align || "left").toLowerCase()]}`);
    css.push("font-feature-settings:'tnum' 1,'lnum' 1");
    inner = esc(s.text === undefined ? "" : s.text);
  } else if (kind === "svg") {
    let svg = s.svg;
    if (s.color) svg = svg.replace(/currentColor/g, color(s.color));
    svg = svg.replace(/<svg\b([^>]*)>/i, (m, attrs) => {
      attrs = attrs.replace(/\s(width|height)\s*=\s*["'][^"']*["']/gi, "");
      if (!/viewBox/i.test(m) && s._vb) attrs += ` viewBox="${s._vb}"`;
      return `<svg${attrs} width="${w}" height="${h}" style="display:block;overflow:visible">`;
    });
    inner = svg;
  } else {
    if (kind === "ellipse" && s.arc) {
      const c = color(s.fill !== undefined && s.fill !== null ? (typeof s.fill === "string" ? s.fill : "#fff") : "transparent");
      inner = `<svg width="${w}" height="${h}" style="position:absolute;left:0;top:0;overflow:visible"><path d="${donutPath(w, h, s.arc)}" fill="${c}" fill-rule="evenodd"/></svg>`;
    } else {
      const bg = paintCss(s.fill, w, h);
      if (bg) css.push(`background:${bg}`);
    }
    if (kind === "frame" && s.clip) css.push("overflow:hidden");
    if (kind === "polygon" || kind === "star") {
      const pts = [];
      const n = kind === "polygon" ? (s.sides || 3) : (s.points || 5) * 2;
      for (let i = 0; i < n; i++) {
        const a = (i / n) * 2 * Math.PI - Math.PI / 2;
        const rr = kind === "star" && i % 2 === 1 ? (s.inner === undefined ? 0.5 : s.inner) * 50 : 50;
        pts.push(`${(50 + rr * Math.cos(a)).toFixed(2)}% ${(50 + rr * Math.sin(a)).toFixed(2)}%`);
      }
      css.push(`clip-path:polygon(${pts.join(",")})`);
    }
  }
  const kids = (s.children || []).filter(Boolean).map((c) => node(c, false)).join("");
  const title = s.name ? ` data-name="${esc(s.name)}"` : "";
  return `<div${title} style="${css.join(";")}">${inner}${kids}</div>`;
}

function page(spec, opts) {
  const fonts = (opts.googleFonts || []).map((f) => `family=${f}`).join("&");
  const link = fonts ? `<link rel="stylesheet" href="https://fonts.googleapis.com/css2?${fonts}&display=block">` : "";
  return `<!doctype html><meta charset="utf-8">${link}<style>html,body{margin:0;background:${opts.bg || "#000"}}*{-webkit-font-smoothing:antialiased}</style>${node(spec, true)}`;
}

function renderPng(spec, outPng, opts = {}) {
  const w = opts.w || spec.w, h = opts.h || spec.h;
  const html = page(spec, opts);
  const htmlPath = path.join(os.tmpdir(), "ui_v2_" + path.basename(outPng).replace(/\W/g, "_") + ".html");
  fs.writeFileSync(htmlPath, html);
  fs.mkdirSync(path.dirname(path.resolve(outPng)), { recursive: true });
  const args = ["--headless=new", "--disable-gpu", "--hide-scrollbars", "--force-device-scale-factor=" + (opts.scale || 1), "--virtual-time-budget=" + (opts.wait || 6000), `--screenshot=${path.resolve(outPng)}`, `--window-size=${w},${h}`, "file:///" + htmlPath.replace(/\\/g, "/")];
  const r = spawnSync(EDGE, args, { encoding: "utf8", timeout: 60000 });
  if (!fs.existsSync(outPng)) throw new Error("render failed: " + (r.stderr || r.error));
  return outPng;
}

module.exports = { renderPng, node, page };
