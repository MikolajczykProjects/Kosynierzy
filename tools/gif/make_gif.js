// Turns a DemoRecorder frame folder (f000.rgb…, info.txt "w h frames fps") into an animated GIF, no dependencies.
// Usage: node tools/gif/make_gif.js <frameFolder> <out.gif> [--png first.png]
// Palette: fixed 6x7x6 colour cube (252 colours) with light 4x4 ordered dithering; LZW as in omggif.
"use strict";
const fs = require("fs");
const path = require("path");
const zlib = require("zlib");

const [dir, outGif, ...rest] = process.argv.slice(2);
const pngIdx = rest.indexOf("--png");
const pngOut = pngIdx >= 0 ? rest[pngIdx + 1] : null;
const [W, H, N, FPS] = fs.readFileSync(path.join(dir, "info.txt"), "utf8").trim().split(/\s+/).map(Number);
const delay = Math.round(100 / FPS);

const RL = 6, GL = 7, BL = 6;
const palette = Buffer.alloc(768);
for (let r = 0; r < RL; r++) for (let g = 0; g < GL; g++) for (let b = 0; b < BL; b++) {
  const i = (r * GL + g) * BL + b;
  palette[i * 3] = Math.round(r * 255 / (RL - 1));
  palette[i * 3 + 1] = Math.round(g * 255 / (GL - 1));
  palette[i * 3 + 2] = Math.round(b * 255 / (BL - 1));
}
const bayer = [0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5];

function quantize(rgb) {
  const out = new Uint8Array(W * H);
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const p = (y * W + x) * 3;
    const d = (bayer[(y & 3) * 4 + (x & 3)] - 7.5) / 16 * 0.6; // light dither, keeps LZW efficient
    const q = (v, levels) => Math.max(0, Math.min(levels - 1, Math.round(v / 255 * (levels - 1) + d)));
    out[y * W + x] = (q(rgb[p], RL) * GL + q(rgb[p + 1], GL)) * BL + q(rgb[p + 2], BL);
  }
  return out;
}

function lzw(indices, minCode) {
  const clear = 1 << minCode, eoi = clear + 1;
  let codeSize = minCode + 1, next = eoi + 1;
  const table = new Map();
  const bytes = [];
  let cur = 0, bits = 0;
  const emit = (code) => {
    cur |= code << bits; bits += codeSize;
    while (bits >= 8) { bytes.push(cur & 255); cur >>>= 8; bits -= 8; }
  };
  emit(clear);
  let ib = indices[0];
  for (let i = 1; i < indices.length; i++) {
    const k = indices[i];
    const key = (ib << 8) | k;
    const found = table.get(key);
    if (found === undefined) {
      emit(ib);
      if (next === 4096) {
        emit(clear);
        next = eoi + 1; codeSize = minCode + 1; table.clear();
      } else {
        if (next >= (1 << codeSize)) codeSize++;
        table.set(key, next++);
      }
      ib = k;
    } else ib = found;
  }
  emit(ib);
  emit(eoi);
  if (bits > 0) bytes.push(cur & 255);
  return Buffer.from(bytes);
}

function subBlocks(data) {
  const parts = [];
  for (let i = 0; i < data.length; i += 255) {
    const chunk = data.subarray(i, i + 255);
    parts.push(Buffer.from([chunk.length]), chunk);
  }
  parts.push(Buffer.from([0]));
  return Buffer.concat(parts);
}

const u16 = (v) => Buffer.from([v & 255, (v >> 8) & 255]);
const parts = [
  Buffer.from("GIF89a"), u16(W), u16(H), Buffer.from([0xF7, 0, 0]), palette,
  Buffer.from([0x21, 0xFF, 0x0B]), Buffer.from("NETSCAPE2.0"), Buffer.from([3, 1, 0, 0, 0]),
];
let first = null;
for (let f = 0; f < N; f++) {
  const file = path.join(dir, `f${String(f).padStart(3, "0")}.rgb`);
  if (!fs.existsSync(file)) break;
  const rgb = fs.readFileSync(file);
  if (!first) first = rgb;
  parts.push(Buffer.from([0x21, 0xF9, 4, 0]), u16(delay), Buffer.from([0, 0]));
  parts.push(Buffer.from([0x2C]), u16(0), u16(0), u16(W), u16(H), Buffer.from([0]));
  parts.push(Buffer.from([8]), subBlocks(lzw(quantize(rgb), 8)));
}
parts.push(Buffer.from([0x3B]));
fs.writeFileSync(outGif, Buffer.concat(parts));
console.log(`${outGif}: ${W}x${H}, ${(fs.statSync(outGif).size / 1024).toFixed(0)} KB`);

if (pngOut && first) {
  const crcTable = Array.from({ length: 256 }, (_, n) => {
    let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xEDB88320 ^ (c >>> 1) : c >>> 1; return c >>> 0;
  });
  const crc = (buf) => { let c = 0xFFFFFFFF; for (const b of buf) c = crcTable[(c ^ b) & 255] ^ (c >>> 8); return (c ^ 0xFFFFFFFF) >>> 0; };
  const chunk = (type, data) => {
    const len = Buffer.alloc(4); len.writeUInt32BE(data.length);
    const td = Buffer.concat([Buffer.from(type), data]);
    const c = Buffer.alloc(4); c.writeUInt32BE(crc(td));
    return Buffer.concat([len, td, c]);
  };
  const raw = Buffer.alloc((W * 3 + 1) * H);
  for (let y = 0; y < H; y++) first.copy(raw, y * (W * 3 + 1) + 1, y * W * 3, (y + 1) * W * 3);
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(W, 0); ihdr.writeUInt32BE(H, 4); ihdr[8] = 8; ihdr[9] = 2;
  fs.writeFileSync(pngOut, Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk("IHDR", ihdr),
    chunk("IDAT", zlib.deflateSync(raw)), chunk("IEND", Buffer.alloc(0))]));
}
