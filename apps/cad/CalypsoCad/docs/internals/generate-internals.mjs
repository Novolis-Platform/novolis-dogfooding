/**
 * Calypso internals manufacturable blueprints (HTML + SVG + JSON).
 * Run: node d:\novolis\novolis-dogfooding\apps\cad\CalypsoCad\docs\internals\generate-internals.mjs
 */
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import * as L from "./calypso-lock.mjs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
L.assertLock();

const {
  nice,
  fmt,
  LOA,
  BEAM,
  OAH,
  L_FORE,
  L_MID,
  L_AFT,
  CHAMFER,
  T_SHELL,
  T_BH,
  MATERIAL,
  FORE_STATIONS,
  Z_BRIDGE_AFT,
  Z_VERT_FORE,
  Z_VERT_AFT,
  L_VERT,
  Z_CROSS_FORE,
  Z_CROSS_AFT,
  L_CROSS,
  Z_CREW_FORE,
  Z_CREW_AFT,
  L_CREW,
  CREW_CABIN_COUNT,
  Z_MED_FORE,
  Z_MED_AFT,
  L_MED,
  Z_ENG_FORE,
  Z_ENG_AFT,
  L_ENG,
  CATWALK_FORE,
  CATWALK_AFT,
  CATWALK_D,
  GAP_FORE,
  GAP_CATWALK_C40,
  C40_FORE,
  C40_AFT,
  C40_L,
  C40_W,
  C40_H,
  CELL,
  COLS,
  GRID_W,
  STACK_H,
  HOLD_L,
  RAMP_GAP,
  DOOR_W,
  DOOR_H,
  SILL,
  APRON_D,
  CORR_INNER,
  CORR_W,
  CORR_OUTER,
  STACK,
  ACCESS_W,
  SHAFT_W,
  AIR_INNER,
  AIR_OUTER,
  AIR_W,
  L_AIR_JOG,
  DOOR_PASS,
  DOOR_CORR,
  DOOR_H_PASS,
  DOOR_H_CORR,
  Z_DK_M1,
  Z_DK0,
  Z_DK1,
  Z_TANK,
  ROOM_H,
  hullHeightAt,
} = L;

const REV = "A";
const C = {
  ink: "#1a1a1a",
  mute: "#444",
  dim: "#777",
  accent: "#0b5fff",
  fill: "#e8e8e8",
  room: "#f4f4f4",
  corr: "#ffffff",
  eng: "#c5d4f0",
  hold: "#d8d0e0",
  air: "#dde3ea",
  hatch: "#0b5fff",
  bh: "#333333",
};

const poly = (pts) => pts.map(([x, y], i) => `${i === 0 ? "M" : "L"} ${x} ${y}`).join(" ") + " Z";

function css() {
  return `body{font:14px/1.45 system-ui,Segoe UI,sans-serif;color:${C.ink};margin:24px;max-width:1100px}
h1{font-size:1.35rem;margin:0 0 8px}h2{font-size:1.1rem;margin:28px 0 10px;border-bottom:1px solid #ccc;padding-bottom:4px}
.meta{color:${C.mute};font-size:13px}.pill{display:inline-block;border:1px solid #99a;border-radius:999px;padding:2px 10px;margin:2px 4px 2px 0;font-size:12px}
table{border-collapse:collapse;width:100%;font-size:13px;margin:8px 0 16px}th,td{border:1px solid #ccc;padding:6px 8px;text-align:left}th{background:#f0f0f0}
svg{display:block;width:100%;max-width:100%;height:auto;border:1px solid #ddd;margin:8px 0 16px;background:#fff}
.sheet{page-break-after:always;margin-bottom:40px}@media print{body{margin:12px}.sheet{page-break-after:always}}`;
}

function pills(items) {
  return items.map((t) => `<span class="pill">${t}</span>`).join("\n  ");
}

function dimH(x1, x2, y, label) {
  const mid = (x1 + x2) / 2;
  return `<g>
    <line x1="${x1}" y1="${y}" x2="${x2}" y2="${y}" stroke="${C.accent}" stroke-width="1"/>
    <line x1="${x1}" y1="${y - 3}" x2="${x1}" y2="${y + 3}" stroke="${C.accent}" stroke-width="1"/>
    <line x1="${x2}" y1="${y - 3}" x2="${x2}" y2="${y + 3}" stroke="${C.accent}" stroke-width="1"/>
    <text x="${mid}" y="${y - 5}" text-anchor="middle" font-size="9" fill="${C.accent}">${label}</text>
  </g>`;
}

function dimV(y1, y2, x, label) {
  const mid = (y1 + y2) / 2;
  return `<g>
    <line x1="${x}" y1="${y1}" x2="${x}" y2="${y2}" stroke="${C.accent}" stroke-width="1"/>
    <line x1="${x - 3}" y1="${y1}" x2="${x + 3}" y2="${y1}" stroke="${C.accent}" stroke-width="1"/>
    <line x1="${x - 3}" y1="${y2}" x2="${x + 3}" y2="${y2}" stroke="${C.accent}" stroke-width="1"/>
    <text x="${x - 6}" y="${mid}" text-anchor="end" font-size="9" fill="${C.accent}" transform="rotate(-90 ${x - 6} ${mid})">${label}</text>
  </g>`;
}

// ─── Plan helpers (z down page, y right, CL center) ──────────────────────────
const PLAN = { ox: 220, oy: 40, sx: 7.5, sy: 6.8 };
const plX = (y) => PLAN.ox + y * PLAN.sx;
const plY = (z) => PLAN.oy + z * PLAN.sy;

function hullPlanPath() {
  const half = BEAM / 2;
  const stbd = FORE_STATIONS.map((s) => [plX(s.beam / 2), plY(s.z)]);
  const port = [...FORE_STATIONS].reverse().map((s) => [plX(-s.beam / 2), plY(s.z)]);
  const pts = [
    ...stbd,
    [plX(half), plY(L_FORE + L_MID)],
    [plX(half), plY(LOA)],
    [plX(-half), plY(LOA)],
    [plX(-half), plY(L_FORE + L_MID)],
    ...port,
  ];
  return poly(pts);
}

function hatchOpeningFA(y, z, clear, tag) {
  const t = Math.max(T_BH, 0.2);
  return `<g>
    <rect x="${plX(y - clear / 2)}" y="${plY(z) - (t * PLAN.sy) / 2}" width="${clear * PLAN.sx}" height="${t * PLAN.sy}"
      fill="#fff" stroke="${C.hatch}" stroke-width="1.6"/>
    <text x="${plX(y)}" y="${plY(z) - 5}" text-anchor="middle" font-size="6" fill="${C.hatch}">${tag}</text>
  </g>`;
}

function hatchOpeningPS(y, z, clear, tag) {
  const t = Math.max(T_BH, 0.2);
  return `<g>
    <rect x="${plX(y) - (t * PLAN.sx) / 2}" y="${plY(z - clear / 2)}" width="${t * PLAN.sx}" height="${clear * PLAN.sy}"
      fill="#fff" stroke="${C.hatch}" stroke-width="1.6"/>
    <text x="${plX(y) + 10}" y="${plY(z) + 3}" text-anchor="start" font-size="6" fill="${C.hatch}">${tag}</text>
  </g>`;
}

function rectRoom(y0, z0, y1, z1, fill, label) {
  const x = plX(Math.min(y0, y1));
  const y = plY(Math.min(z0, z1));
  const w = Math.abs(y1 - y0) * PLAN.sx;
  const h = Math.abs(z1 - z0) * PLAN.sy;
  const cx = x + w / 2;
  const cy = y + h / 2;
  return `<g>
    <rect x="${x}" y="${y}" width="${w}" height="${h}" fill="${fill}" stroke="${C.ink}" stroke-width="1.1"/>
    ${label ? `<text x="${cx}" y="${cy}" text-anchor="middle" font-size="9" fill="${C.ink}">${label}</text>` : ""}
  </g>`;
}

function bhTransverse(z, y0, y1) {
  return `<rect x="${plX(y0)}" y="${plY(z) - (T_BH * PLAN.sy) / 2}" width="${(y1 - y0) * PLAN.sx}" height="${T_BH * PLAN.sy}" fill="${C.bh}" opacity="0.85"/>`;
}

function bhLong(y, z0, z1) {
  return `<rect x="${plX(y) - (T_BH * PLAN.sx) / 2}" y="${plY(z0)}" width="${T_BH * PLAN.sx}" height="${(z1 - z0) * PLAN.sy}" fill="${C.bh}" opacity="0.85"/>`;
}

function deckPlanSvg(deck, title) {
  const W = 520;
  const H = 780;
  const midY = (a, b) => plY((a + b) / 2);
  let body = "";

  body += `<path d="${hullPlanPath()}" fill="${C.fill}" stroke="${C.ink}" stroke-width="1.5"/>`;

  // Bridge / fuel / lounge footprint
  const bowLabel = deck === 0 ? "BRIDGE" : deck === -1 ? "FUEL" : "LOUNGE";
  const bowFill = deck === -1 ? C.air : C.corr;
  body += `<polygon points="${[
    [plX(-3), plY(3)],
    [plX(3), plY(3)],
    [plX(4.5), plY(8)],
    [plX(4.5), plY(Z_BRIDGE_AFT)],
    [plX(-4.5), plY(Z_BRIDGE_AFT)],
    [plX(-4.5), plY(8)],
  ]
    .map(([x, y]) => `${x},${y}`)
    .join(" ")}" fill="${bowFill}" stroke="${C.ink}" stroke-width="1.3"/>`;
  body += `<text x="${plX(0)}" y="${plY(8.5)}" text-anchor="middle" font-size="11" fill="${C.ink}">${bowLabel}</text>`;

  // Stairs | access | elev
  body += rectRoom(-ACCESS_W / 2 - SHAFT_W, Z_VERT_FORE, -ACCESS_W / 2, Z_VERT_AFT, C.air, "STAIRS");
  body += rectRoom(-ACCESS_W / 2, Z_VERT_FORE, ACCESS_W / 2, Z_VERT_AFT, C.corr, "ACCESS");
  body += rectRoom(ACCESS_W / 2, Z_VERT_FORE, ACCESS_W / 2 + SHAFT_W, Z_VERT_AFT, C.air, "ELEV");

  // Crossing
  body += rectRoom(-AIR_INNER, Z_CROSS_FORE, AIR_INNER, Z_CROSS_AFT, C.corr, "CROSSING");
  body += bhTransverse(Z_CROSS_FORE, -AIR_INNER, AIR_INNER);
  body += bhTransverse(Z_CROSS_AFT, -STACK, STACK);

  // L-airlocks DK0 only
  if (deck === 0) {
    for (const side of [-1, 1]) {
      const y0 = Math.min(side * AIR_INNER, side * AIR_OUTER);
      const y1 = Math.max(side * AIR_INNER, side * AIR_OUTER);
      body += rectRoom(y0, Z_CROSS_FORE, y1, Z_CROSS_AFT, C.air, side < 0 ? "A-P" : "A-S");
      body += rectRoom(y0, Z_CROSS_AFT, y1, Z_CROSS_AFT + L_AIR_JOG, C.air, "B");
      const tag = side < 0 ? "P" : "S";
      body += hatchOpeningPS(side * AIR_INNER, (Z_CROSS_FORE + Z_CROSS_AFT) / 2, DOOR_PASS, `D1-${tag}`);
      body += hatchOpeningFA(side * ((AIR_INNER + AIR_OUTER) / 2), Z_CROSS_AFT, DOOR_PASS, `D2-${tag}`);
      body += hatchOpeningPS(side * AIR_OUTER, Z_CROSS_AFT + L_AIR_JOG / 2, DOOR_PASS, `D3-${tag}`);
    }
  }

  // Corridors (clear) + BH faces
  body += rectRoom(-CORR_OUTER, Z_CREW_FORE, -CORR_INNER, Z_ENG_AFT, C.corr, "");
  body += rectRoom(CORR_INNER, Z_CREW_FORE, CORR_OUTER, Z_ENG_AFT, C.corr, "");
  body += bhLong(-STACK, Z_CREW_FORE, Z_ENG_AFT);
  body += bhLong(STACK, Z_CREW_FORE, Z_ENG_AFT);
  body += `<text x="${plX(-(CORR_INNER + CORR_W / 2))}" y="${midY(Z_CREW_FORE, Z_CREW_AFT)}" text-anchor="middle" font-size="7" fill="${C.mute}">CORR P</text>`;
  body += `<text x="${plX(CORR_INNER + CORR_W / 2)}" y="${midY(Z_CREW_FORE, Z_CREW_AFT)}" text-anchor="middle" font-size="7" fill="${C.mute}">CORR S</text>`;

  // Center stack by deck
  if (deck === 0) {
    body += rectRoom(-STACK + T_BH / 2, Z_CREW_FORE + T_BH / 2, STACK - T_BH / 2, Z_CREW_AFT - T_BH / 2, C.room, "CREW CABINS");
    body += rectRoom(-STACK + T_BH / 2, Z_MED_FORE + T_BH / 2, -T_BH / 2, Z_MED_AFT - T_BH / 2, C.air, "INFIRMARY");
    body += rectRoom(T_BH / 2, Z_MED_FORE + T_BH / 2, STACK - T_BH / 2, Z_MED_AFT - T_BH / 2, C.air, "GALLEY");
    body += hatchOpeningFA(-STACK / 2, Z_CREW_FORE, DOOR_PASS, "CAB-P");
    body += hatchOpeningFA(STACK / 2, Z_CREW_FORE, DOOR_PASS, "CAB-S");
    body += hatchOpeningPS(-STACK, (Z_MED_FORE + Z_MED_AFT) / 2, DOOR_PASS, "INF→P");
    body += hatchOpeningPS(STACK, (Z_MED_FORE + Z_MED_AFT) / 2, DOOR_PASS, "GAL→S");
  } else if (deck === -1) {
    body += rectRoom(-STACK + T_BH / 2, Z_CREW_FORE, STACK - T_BH / 2, Z_ENG_FORE - T_BH / 2, C.room, "UTILITY / TANKS");
  } else {
    const zClear0 = Z_CREW_FORE + T_BH / 2;
    const zClear1 = Z_CREW_AFT - T_BH / 2;
    const span = zClear1 - zClear0;
    const slot = span / CREW_CABIN_COUNT;
    for (let i = 0; i < CREW_CABIN_COUNT; i++) {
      const z0 = zClear0 + i * slot + (i === 0 ? 0 : T_BH / 2);
      const z1 = zClear0 + (i + 1) * slot - (i === CREW_CABIN_COUNT - 1 ? 0 : T_BH / 2);
      body += rectRoom(-STACK + T_BH / 2, z0, STACK - T_BH / 2, z1, C.room, `C${i + 1}`);
      if (i > 0) body += bhTransverse(zClear0 + i * slot, -STACK, STACK);
      const zMid = zClear0 + (i + 0.5) * slot;
      body += hatchOpeningPS(-STACK, zMid, DOOR_PASS, `CAB${i + 1}-P`);
      body += hatchOpeningPS(STACK, zMid, DOOR_PASS, `CAB${i + 1}-S`);
    }
    body += rectRoom(-STACK + T_BH / 2, Z_MED_FORE + T_BH / 2, STACK - T_BH / 2, Z_MED_AFT - T_BH / 2, C.air, "STORE");
    body += hatchOpeningPS(-STACK, (Z_MED_FORE + Z_MED_AFT) / 2, DOOR_PASS, "STORE-P");
    body += hatchOpeningPS(STACK, (Z_MED_FORE + Z_MED_AFT) / 2, DOOR_PASS, "STORE-S");
  }

  // Eng atrium
  body += rectRoom(-STACK + T_BH / 2, Z_ENG_FORE + T_BH / 2, STACK - T_BH / 2, Z_ENG_AFT - T_BH / 2, C.eng, "ENGINEERING");
  body += bhTransverse(Z_ENG_FORE, -STACK, STACK);
  body += bhTransverse(Z_ENG_AFT, -CORR_OUTER, CORR_OUTER);

  if (deck === 0 || deck === -1) {
    const tag = deck === 0 ? "0" : "M1";
    body += hatchOpeningPS(-STACK, (Z_ENG_FORE + Z_ENG_AFT) / 2, DOOR_CORR, `ENG-P-DK${tag}`);
    body += hatchOpeningPS(STACK, (Z_ENG_FORE + Z_ENG_AFT) / 2, DOOR_CORR, `ENG-S-DK${tag}`);
  }

  // Hold hatches at corridor aft
  const holdTag = deck === -1 ? "M1" : deck === 0 ? "0" : "P1";
  body += hatchOpeningFA(-(CORR_INNER + CORR_W / 2), Z_ENG_AFT, DOOR_CORR, `HOLD-P-DK${holdTag}`);
  body += hatchOpeningFA(CORR_INNER + CORR_W / 2, Z_ENG_AFT, DOOR_CORR, `HOLD-S-DK${holdTag}`);

  // Hold + C40
  body += rectRoom(-BEAM / 2 + 0.5, CATWALK_FORE, BEAM / 2 - 0.5, LOA, C.hold, "");
  body += rectRoom(-BEAM / 2 + 0.5, CATWALK_FORE, BEAM / 2 - 0.5, CATWALK_AFT, C.corr, `CATWALK ${nice(CATWALK_D)}`);
  for (let col = 0; col < COLS; col++) {
    const y0 = -GRID_W / 2 + col * (C40_W + CELL);
    body += `<rect x="${plX(y0)}" y="${plY(C40_FORE)}" width="${C40_W * PLAN.sx}" height="${C40_L * PLAN.sy}" fill="none" stroke="${C.accent}" stroke-width="1"/>`;
  }
  body += `<text x="${plX(0)}" y="${plY(C40_FORE + C40_L / 2)}" text-anchor="middle" font-size="9" fill="${C.ink}">C40 5×1×3</text>`;
  body += `<line x1="${plX(-DOOR_W / 2)}" y1="${plY(LOA)}" x2="${plX(DOOR_W / 2)}" y2="${plY(LOA)}" stroke="${C.accent}" stroke-width="3"/>`;
  body += `<line x1="${plX(0)}" y1="${plY(0)}" x2="${plX(0)}" y2="${plY(LOA)}" stroke="${C.dim}" stroke-dasharray="4 3"/>`;
  body += dimV(plY(0), plY(LOA), 28, `LOA ${fmt(LOA)}`);
  body += dimH(plX(-BEAM / 2), plX(BEAM / 2), plY(LOA) + 16, `beam ${fmt(BEAM)}`);

  return `<svg viewBox="0 0 ${W} ${H}" xmlns="http://www.w3.org/2000/svg" role="img" aria-label="${title}">
  <text x="16" y="24" font-size="12" fill="${C.ink}">${title}</text>
  <text x="16" y="38" font-size="9" fill="${C.mute}">Clear corridors ${fmt(CORR_W)} · BH face ${fmt(T_BH)} · hatch = opening (white leaf)</text>
  ${body}
</svg>`;
}

// ─── Profile ─────────────────────────────────────────────────────────────────
const PROF = { ox: 40, oy: 40, sx: 8.2, sy: 10 };
const pX = (z) => PROF.ox + z * PROF.sx;
const pY = (up) => PROF.oy + (OAH - up) * PROF.sy;
const MID_CL = OAH / 2;

function clippedRoomPts(z0, z1, floor, ceil) {
  const steps = 24;
  const upper = [];
  const lower = [];
  for (let i = 0; i <= steps; i++) {
    const z = z0 + ((z1 - z0) * i) / steps;
    const half = hullHeightAt(z) / 2;
    const top = Math.min(ceil, MID_CL + half);
    const bot = Math.max(floor, MID_CL - half);
    if (top - bot < 0.15) continue;
    upper.push([pX(z), pY(top)]);
    lower.push([pX(z), pY(bot)]);
  }
  if (upper.length < 2) return [];
  return [...upper, ...lower.reverse()];
}

function profileSvg() {
  const W = 980;
  const H = 300;
  const upper = FORE_STATIONS.map((s) => [pX(s.z), pY(MID_CL + s.h / 2)]);
  const lower = [...FORE_STATIONS].reverse().map((s) => [pX(s.z), pY(MID_CL - s.h / 2)]);
  const hull = [
    ...upper,
    [pX(L_FORE + L_MID), pY(OAH)],
    [pX(LOA), pY(OAH)],
    [pX(LOA), pY(0)],
    [pX(L_FORE + L_MID), pY(0)],
    ...lower,
  ];
  const lounge = clippedRoomPts(2, Z_BRIDGE_AFT, Z_DK1, Z_DK1 + ROOM_H);
  const bridge = clippedRoomPts(2, Z_BRIDGE_AFT, Z_DK0, Z_DK0 + ROOM_H);
  const fuel = clippedRoomPts(2, Z_BRIDGE_AFT, Z_DK_M1, Z_DK_M1 + ROOM_H);

  return `<svg viewBox="0 0 ${W} ${H}" xmlns="http://www.w3.org/2000/svg">
  <text x="16" y="24" font-size="12" fill="${C.ink}">CAL-INT-PRF-001 · PROFILE CL · LOUNGE / BRIDGE / FUEL CLIPPED TO OML</text>
  <path d="${poly(hull)}" fill="${C.fill}" stroke="${C.ink}" stroke-width="1.6"/>
  ${lounge.length ? `<path d="${poly(lounge)}" fill="${C.corr}" stroke="${C.ink}"/>` : ""}
  <text x="${pX(10)}" y="${pY(Z_DK1 + ROOM_H / 2)}" text-anchor="middle" font-size="8">LOUNGE</text>
  ${bridge.length ? `<path d="${poly(bridge)}" fill="${C.room}" stroke="${C.ink}"/>` : ""}
  <text x="${pX(8)}" y="${pY(Z_DK0 + ROOM_H / 2)}" text-anchor="middle" font-size="8">BRIDGE</text>
  ${fuel.length ? `<path d="${poly(fuel)}" fill="${C.air}" stroke="${C.ink}"/>` : ""}
  <text x="${pX(10)}" y="${pY(Z_DK_M1 + ROOM_H / 2)}" text-anchor="middle" font-size="8">FUEL</text>
  <rect x="${pX(Z_VERT_FORE)}" y="${pY(OAH - 0.3)}" width="${L_VERT * PROF.sx}" height="${(OAH - 0.6) * PROF.sy}" fill="${C.air}" stroke="${C.accent}" opacity="0.75"/>
  <text x="${pX(Z_VERT_FORE + L_VERT / 2)}" y="${pY(OAH / 2)}" text-anchor="middle" font-size="8">STAIRS/ELEV</text>
  <rect x="${pX(Z_ENG_FORE)}" y="${pY(OAH - 0.3)}" width="${L_ENG * PROF.sx}" height="${(OAH - 0.6) * PROF.sy}" fill="${C.eng}" stroke="${C.accent}" stroke-width="1.4"/>
  <text x="${pX(Z_ENG_FORE + L_ENG / 2)}" y="${pY(OAH / 2)}" text-anchor="middle" font-size="9">ENGINEERING</text>
  <rect x="${pX(CATWALK_FORE)}" y="${pY(Z_TANK + 9)}" width="${HOLD_L * PROF.sx}" height="${9 * PROF.sy}" fill="${C.hold}" stroke="${C.ink}"/>
  <rect x="${pX(C40_FORE)}" y="${pY(Z_TANK + STACK_H)}" width="${C40_L * PROF.sx}" height="${STACK_H * PROF.sy}" fill="${C.accent}" opacity="0.35" stroke="${C.accent}"/>
  <rect x="${pX(LOA) - 5}" y="${pY(SILL + DOOR_H)}" width="8" height="${DOOR_H * PROF.sy}" fill="${C.accent}" opacity="0.45"/>
  ${[Z_DK_M1, Z_DK0, Z_DK1]
    .map(
      (z) =>
        `<line x1="${pX(0)}" y1="${pY(z)}" x2="${pX(Z_ENG_FORE)}" y2="${pY(z)}" stroke="${C.dim}" stroke-dasharray="3 3"/>
    <line x1="${pX(Z_ENG_AFT)}" y1="${pY(z)}" x2="${pX(CATWALK_FORE)}" y2="${pY(z)}" stroke="${C.dim}" stroke-dasharray="3 3"/>`,
    )
    .join("")}
  ${dimH(pX(0), pX(L_FORE), pY(OAH) - 8, `L_fore ${fmt(L_FORE)}`)}
  ${dimH(pX(L_FORE), pX(L_FORE + L_MID), pY(OAH) - 8, `L_mid ${fmt(L_MID)}`)}
  ${dimH(pX(L_FORE + L_MID), pX(LOA), pY(OAH) - 8, `L_aft ${fmt(L_AFT)}`)}
  ${dimH(pX(0), pX(LOA), pY(0) + 22, `LOA ${fmt(LOA)}`)}
</svg>`;
}

function sectionEngSvg() {
  const W = 420;
  const H = 380;
  const sx = 11;
  const sy = 12;
  const ox = 210;
  const oy = 190;
  const hw = (BEAM / 2) * sx;
  const hh = (OAH / 2) * sy;
  const cx = CHAMFER * sx;
  const cy = CHAMFER * sy;
  const sec = [
    [ox - hw + cx, oy - hh],
    [ox + hw - cx, oy - hh],
    [ox + hw, oy - hh + cy],
    [ox + hw, oy + hh - cy],
    [ox + hw - cx, oy + hh],
    [ox - hw + cx, oy + hh],
    [ox - hw, oy + hh - cy],
    [ox - hw, oy - hh + cy],
  ];
  const keel = oy + hh;
  const yZ = (z) => keel - z * sy;
  let doors = "";
  for (const z of [Z_DK_M1, Z_DK0]) {
    doors += `<rect x="${ox - STACK * sx - 3}" y="${yZ(z + ROOM_H / 2) - (DOOR_CORR * sy) / 2}" width="6" height="${DOOR_CORR * sy}" fill="#fff" stroke="${C.hatch}" stroke-width="1.5"/>`;
    doors += `<rect x="${ox + STACK * sx - 3}" y="${yZ(z + ROOM_H / 2) - (DOOR_CORR * sy) / 2}" width="6" height="${DOOR_CORR * sy}" fill="#fff" stroke="${C.hatch}" stroke-width="1.5"/>`;
  }
  return `<svg viewBox="0 0 ${W} ${H}" xmlns="http://www.w3.org/2000/svg">
  <text x="16" y="24" font-size="12">CAL-INT-SEC-001a · ENG SECTION LOOKING AFT · P/S OPENINGS DK0 &amp; DK−1</text>
  <path d="${poly(sec)}" fill="${C.fill}" stroke="${C.ink}" stroke-width="1.5"/>
  <rect x="${ox - CORR_OUTER * sx}" y="${yZ(Z_DK0 + ROOM_H)}" width="${CORR_W * sx}" height="${ROOM_H * sy}" fill="${C.corr}" stroke="${C.ink}"/>
  <rect x="${ox + CORR_INNER * sx}" y="${yZ(Z_DK0 + ROOM_H)}" width="${CORR_W * sx}" height="${ROOM_H * sy}" fill="${C.corr}" stroke="${C.ink}"/>
  <rect x="${ox - STACK * sx}" y="${yZ(OAH - 0.4)}" width="${2 * STACK * sx}" height="${(OAH - 0.8) * sy}" fill="${C.eng}" stroke="${C.accent}"/>
  <text x="${ox}" y="${oy}" text-anchor="middle" font-size="11">POWER CORE</text>
  ${doors}
  ${dimH(ox - hw, ox + hw, oy + hh + 18, `beam ${fmt(BEAM)}`)}
</svg>`;
}

function airlockDetailSvg() {
  const W = 560;
  const H = 300;
  const s = 22;
  const ox = 40;
  const oy = 60;
  const crossHalf = AIR_INNER * s;
  const chW = AIR_W * s;
  const chH = L_CROSS * s;
  const jogH = L_AIR_JOG * s;
  const leaf = Math.max(T_BH, 0.25) * s;
  const hullX = ox + crossHalf + chW;
  return `<svg viewBox="0 0 ${W} ${H}" xmlns="http://www.w3.org/2000/svg">
  <text x="16" y="24" font-size="12">CAL-INT-HTC-001 · L-AIRLOCK · A=${fmt(L_CROSS)} · D3 ON HULL ±${nice(AIR_OUTER)}</text>
  <rect x="${ox}" y="${oy + chH / 2 - CORR_W * s * 0.5}" width="${crossHalf}" height="${CORR_W * s}" fill="${C.corr}" stroke="${C.ink}"/>
  <text x="${ox + crossHalf / 2}" y="${oy + chH / 2 + 4}" text-anchor="middle" font-size="9">CROSSING → ±${nice(AIR_INNER)}</text>
  <rect x="${ox + crossHalf}" y="${oy}" width="${chW}" height="${chH}" fill="${C.air}" stroke="${C.ink}" stroke-width="1.4"/>
  <text x="${ox + crossHalf + chW / 2}" y="${oy + chH / 2}" text-anchor="middle" font-size="10">A</text>
  <rect x="${ox + crossHalf}" y="${oy + chH}" width="${chW}" height="${jogH}" fill="${C.air}" stroke="${C.ink}" stroke-width="1.4"/>
  <text x="${ox + crossHalf + chW / 2}" y="${oy + chH + jogH / 2}" text-anchor="middle" font-size="10">B</text>
  <line x1="${hullX}" y1="${oy - 8}" x2="${hullX}" y2="${oy + chH + jogH + 8}" stroke="${C.accent}" stroke-width="3"/>
  <text x="${hullX + 8}" y="${oy + 12}" font-size="9" fill="${C.accent}">OUTER HULL / SPACE</text>
  <rect x="${ox + crossHalf - leaf / 2}" y="${oy + chH / 2 - (DOOR_PASS * s) / 2}" width="${leaf}" height="${DOOR_PASS * s}" fill="#fff" stroke="${C.hatch}" stroke-width="1.6"/>
  <text x="${ox + crossHalf - 6}" y="${oy + 14}" text-anchor="end" font-size="9" fill="${C.hatch}">D1 opening</text>
  <rect x="${ox + crossHalf + chW / 2 - (DOOR_PASS * s) / 2}" y="${oy + chH - leaf / 2}" width="${DOOR_PASS * s}" height="${leaf}" fill="#fff" stroke="${C.hatch}" stroke-width="1.6"/>
  <text x="${ox + crossHalf + chW + 8}" y="${oy + chH + 4}" font-size="9" fill="${C.hatch}">D2 opening</text>
  <rect x="${hullX - leaf / 2}" y="${oy + chH + jogH / 2 - (DOOR_PASS * s) / 2}" width="${leaf}" height="${DOOR_PASS * s}" fill="#fff" stroke="${C.hatch}" stroke-width="1.6"/>
  <text x="${hullX + 8}" y="${oy + chH + jogH / 2 + 4}" font-size="9" fill="${C.hatch}">D3 → space</text>
  <text x="${ox}" y="${oy + chH + jogH + 36}" font-size="10" fill="${C.mute}">Two doors closed between crew and vacuum. Openings are holes in BH / shell (clear ${fmt(DOOR_PASS)}).</text>
</svg>`;
}

function holdDetailSvg() {
  const W = 560;
  const H = 220;
  const sx = 18;
  const ox = 40;
  const oy = 70;
  const x = (z) => ox + (z - CATWALK_FORE) * sx;
  return `<svg viewBox="0 0 ${W} ${H}" xmlns="http://www.w3.org/2000/svg">
  <text x="16" y="24" font-size="12">CAL-INT-HOLD-001 · HOLD PACK FORE→AFT</text>
  <rect x="${x(CATWALK_FORE)}" y="${oy}" width="${CATWALK_D * sx}" height="60" fill="${C.corr}" stroke="${C.accent}" stroke-width="1.4"/>
  <text x="${x(CATWALK_FORE + CATWALK_D / 2)}" y="${oy + 34}" text-anchor="middle" font-size="10" fill="${C.accent}">CATWALK ${fmt(CATWALK_D)}</text>
  <rect x="${x(GAP_FORE)}" y="${oy}" width="${GAP_CATWALK_C40 * sx}" height="60" fill="${C.air}" stroke="${C.ink}"/>
  <text x="${x(GAP_FORE + GAP_CATWALK_C40 / 2)}" y="${oy + 34}" text-anchor="middle" font-size="8">${fmt(GAP_CATWALK_C40)}</text>
  <rect x="${x(C40_FORE)}" y="${oy}" width="${C40_L * sx}" height="60" fill="${C.accent}" opacity="0.35" stroke="${C.accent}"/>
  <text x="${x(C40_FORE + C40_L / 2)}" y="${oy + 34}" text-anchor="middle" font-size="10">C40 ${C40_L}</text>
  <rect x="${x(C40_AFT)}" y="${oy}" width="${RAMP_GAP * sx}" height="60" fill="${C.air}" stroke="${C.ink}"/>
  <text x="${x(C40_AFT + RAMP_GAP / 2)}" y="${oy + 34}" text-anchor="middle" font-size="8">${fmt(RAMP_GAP)}</text>
  <line x1="${x(LOA)}" y1="${oy - 8}" x2="${x(LOA)}" y2="${oy + 68}" stroke="${C.accent}" stroke-width="3"/>
  <text x="${x(LOA) + 8}" y="${oy + 34}" font-size="9" fill="${C.accent}">HATCH ${nice(DOOR_W)}×${nice(DOOR_H)}</text>
  ${dimH(x(CATWALK_FORE), x(LOA), oy + 88, `hold pack ${fmt(HOLD_L)}`)}
  <text x="${ox}" y="${oy + 120}" font-size="10" fill="${C.mute}">Apron ≥ ${fmt(APRON_D)} beyond sill · sill ${fmt(SILL)} · skin ${MATERIAL} ${T_SHELL * 1000} mm</text>
</svg>`;
}

function hatchTableRows() {
  return L.hatches()
    .map(
      (h) =>
        `<tr><td>${h.id}</td><td>${h.deck}</td><td>${h.from} → ${h.to}</td><td>${nice(h.clearW)}×${nice(h.clearH)}</td><td>${h.faces}</td><td>y=${nice(h.y)} z=${nice(h.z)}</td></tr>`,
    )
    .join("\n");
}

function wrapHtml(drawing, title, body) {
  return `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8"/>
<title>${drawing} Rev ${REV} — ${title}</title>
<style>${css()}</style>
</head>
<body>
${body}
<p class="meta">Generated by <code>generate-internals.mjs</code> · lock <code>calypso-lock.mjs</code> · companion manufacturer <code>CAL-HULL-*</code> (LOA ${nice(LOA)}).
Do not hand-edit SVG; regenerate from math.</p>
</body>
</html>
`;
}

// ─── Emit sheets ─────────────────────────────────────────────────────────────
const sheets = [];

sheets.push({
  file: "CAL-INT-GA-001.html",
  html: wrapHtml(
    "CAL-INT-GA-001",
    "Internals index",
    `<div class="sheet">
<h1>CAL-INT-GA-001 Rev ${REV} — Calypso internals GA index</h1>
<p class="meta">Manufacturable general arrangement for CalypsoCad. Midbody stretch for hold + hab packing.
Outer skin: <code>../manufacturer/CAL-HULL-GA-001.html</code> (same LOA).</p>
${pills([`LOA ${fmt(LOA)}`, `beam ${fmt(BEAM)}`, `OAH ${fmt(OAH)}`, `L_mid ${fmt(L_MID)}`, MATERIAL])}
<h2>Drawing index</h2>
<table>
<tr><th>Drawing</th><th>Content</th></tr>
<tr><td>CAL-INT-DK0-001</td><td>Deck 0 plan — rooms, corridors, hatch openings, L-airlocks</td></tr>
<tr><td>CAL-INT-DKM1-001</td><td>Deck −1 — fuel, utility, eng P/S openings, hold access</td></tr>
<tr><td>CAL-INT-DKP1-001</td><td>Deck +1 — lounge, 5 crew cabins, store, hold access (no eng side doors)</td></tr>
<tr><td>CAL-INT-PRF-001</td><td>Profile CL — bow clip, shafts, eng atrium, hold</td></tr>
<tr><td>CAL-INT-SEC-001</td><td>Sections — eng / hold / airlock</td></tr>
<tr><td>CAL-INT-HTC-001</td><td>Hatch schedule + L-airlock detail</td></tr>
<tr><td>CAL-INT-HOLD-001</td><td>Hold packing + aft door</td></tr>
<tr><td>CAL-INT-GA-001.json</td><td>Machine compartments, hatches, corridor CLs</td></tr>
</table>
<h2>Fabrication rules</h2>
<ul>
<li>Dimensions are <strong>clear</strong> inside BH faces (BH face ${fmt(T_BH)}).</li>
<li>Hatches are <strong>openings</strong> (holes) with clear ${fmt(DOOR_PASS)} personnel / ${fmt(DOOR_CORR)} corridor-WT.</li>
<li>Port/stbd corridors ${fmt(CORR_W)} clear, continuous crossing → hold BH.</li>
<li>Engineering atrium open −1/0/+1; side access openings on decks <strong>0 and −1 only</strong>.</li>
<li>L-airlock: D1/D2/D3 — two doors closed between crew and space; D3 on outer hull ±${nice(AIR_OUTER)} m.</li>
</ul>
</div>`,
  ),
});

sheets.push({
  file: "CAL-INT-DK0-001.html",
  html: wrapHtml(
    "CAL-INT-DK0-001",
    "Deck 0",
    `<div class="sheet"><h1>CAL-INT-DK0-001 Rev ${REV} — Deck 0 mid-deck plan</h1>
${pills([`LOA ${fmt(LOA)}`, `corr ${fmt(CORR_W)}`, `pass hatch ${fmt(DOOR_PASS)}`])}
${deckPlanSvg(0, "CAL-INT-DK0-001 · DECK 0 · CLEAR ROOMS + HATCH OPENINGS")}
</div>`,
  ),
});

sheets.push({
  file: "CAL-INT-DKM1-001.html",
  html: wrapHtml(
    "CAL-INT-DKM1-001",
    "Deck −1",
    `<div class="sheet"><h1>CAL-INT-DKM1-001 Rev ${REV} — Deck −1</h1>
${pills(["fuel under bridge", "eng P/S openings", "hold access"])}
${deckPlanSvg(-1, "CAL-INT-DKM1-001 · DECK −1 · FUEL / UTILITY / ENG ACCESS")}
</div>`,
  ),
});

sheets.push({
  file: "CAL-INT-DKP1-001.html",
  html: wrapHtml(
    "CAL-INT-DKP1-001",
    "Deck +1",
    `<div class="sheet"><h1>CAL-INT-DKP1-001 Rev ${REV} — Deck +1</h1>
${pills(["lounge over bridge", "no eng side doors", "hold access"])}
${deckPlanSvg(1, "CAL-INT-DKP1-001 · DECK +1 · LOUNGE / CABINS / STORE")}
</div>`,
  ),
});

sheets.push({
  file: "CAL-INT-PRF-001.html",
  html: wrapHtml(
    "CAL-INT-PRF-001",
    "Profile",
    `<div class="sheet"><h1>CAL-INT-PRF-001 Rev ${REV} — Profile</h1>
${profileSvg()}
</div>`,
  ),
});

sheets.push({
  file: "CAL-INT-SEC-001.html",
  html: wrapHtml(
    "CAL-INT-SEC-001",
    "Sections",
    `<div class="sheet"><h1>CAL-INT-SEC-001 Rev ${REV} — Sections</h1>
${sectionEngSvg()}
${airlockDetailSvg()}
${holdDetailSvg()}
</div>`,
  ),
});

sheets.push({
  file: "CAL-INT-HTC-001.html",
  html: wrapHtml(
    "CAL-INT-HTC-001",
    "Hatches",
    `<div class="sheet"><h1>CAL-INT-HTC-001 Rev ${REV} — Hatch schedule</h1>
${airlockDetailSvg()}
<table>
<tr><th>Tag</th><th>Deck</th><th>From → to</th><th>Clear W×H</th><th>Faces</th><th>Center</th></tr>
${hatchTableRows()}
</table>
<p class="meta">Rule: whenever D3 is open to vacuum, at least two of {D1,D2,D3} remain closed on that lock (typically D1+D2 or D2 closed with D3 cycling).</p>
</div>`,
  ),
});

sheets.push({
  file: "CAL-INT-HOLD-001.html",
  html: wrapHtml(
    "CAL-INT-HOLD-001",
    "Hold",
    `<div class="sheet"><h1>CAL-INT-HOLD-001 Rev ${REV} — Hold packing</h1>
${holdDetailSvg()}
<table>
<tr><th>Item</th><th>Value</th></tr>
<tr><td>Catwalk F–A</td><td>${fmt(CATWALK_D)}</td></tr>
<tr><td>Gap catwalk→C40</td><td>${fmt(GAP_CATWALK_C40)}</td></tr>
<tr><td>C40 external</td><td>${C40_L} × ${C40_W} × ${C40_H}</td></tr>
<tr><td>Gap C40→hatch</td><td>${fmt(RAMP_GAP)}</td></tr>
<tr><td>Hold pack L</td><td>${fmt(HOLD_L)}</td></tr>
<tr><td>Door clear</td><td>${nice(DOOR_W)} × ${nice(DOOR_H)}</td></tr>
<tr><td>Apron</td><td>≥ ${fmt(APRON_D)}</td></tr>
</table>
</div>`,
  ),
});

const json = {
  drawing: "CAL-INT-GA-001",
  rev: REV,
  units: "m",
  envelope: L.envelope,
  decks: { m1: Z_DK_M1, d0: Z_DK0, d1: Z_DK1, roomH: ROOM_H },
  structure: { T_BH, T_SHELL, MATERIAL },
  circulation: {
    CORR_W,
    CORR_INNER,
    CORR_OUTER,
    ACCESS_W,
    SHAFT_W,
    AIR_INNER,
    AIR_OUTER,
    AIR_W,
    L_CROSS,
    L_AIR_JOG,
  },
  hold: {
    CATWALK_D,
    GAP_CATWALK_C40,
    C40_L,
    C40_W,
    C40_H,
    CELL,
    COLS,
    RAMP_GAP,
    HOLD_L,
    DOOR_W,
    DOOR_H,
    SILL,
    APRON_D,
    CATWALK_FORE,
    C40_FORE,
    C40_AFT,
  },
  compartments: L.compartments(),
  airlocks: L.airlockVolumes(),
  hatches: L.hatches(),
  corridorCenterlines: L.corridorCenterlines(),
  stations: {
    Z_BRIDGE_AFT,
    Z_VERT_FORE,
    Z_VERT_AFT,
    Z_CROSS_FORE,
    Z_CROSS_AFT,
    Z_CREW_FORE,
    Z_CREW_AFT,
    CREW_CABIN_COUNT,
    Z_MED_FORE,
    Z_MED_AFT,
    Z_ENG_FORE,
    Z_ENG_AFT,
  },
};

for (const s of sheets) {
  fs.writeFileSync(path.join(__dirname, s.file), s.html, "utf8");
  console.log("wrote", s.file);
}
fs.writeFileSync(path.join(__dirname, "CAL-INT-GA-001.json"), JSON.stringify(json, null, 2), "utf8");
console.log("wrote CAL-INT-GA-001.json");
console.log("OK — internals package Rev", REV, "LOA", LOA);
