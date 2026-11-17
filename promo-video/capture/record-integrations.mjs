// Records the Microsoft Teams tab on production for the integrations videos.
// Output in public/rec/: teams-config.webm (the tab setup page), teams-panel.webm (the host in the meeting side panel),
// teams-stage.webm (Ana on the meeting stage) and teams-marks.json (event times in ms, per recording).
// Teams itself is not recorded: the videos draw a simple meeting frame around these clips. The page runs as if
// it were in a Teams frame: window.parent is replaced and the Teams JS SDK is a stub that remembers the save handler.
import { chromium } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';

const BASE = 'https://app.openplanningpoker.com';
const OUT = path.resolve('public/rec');
const TMP = path.resolve('capture/tmp-video-int');
const SHOTS = path.resolve('capture/shots');
fs.mkdirSync(OUT, { recursive: true });
fs.mkdirSync(SHOTS, { recursive: true });
fs.rmSync(TMP, { recursive: true, force: true });

const browser = await chromium.launch({ channel: 'msedge' });
const wait = (ms) => new Promise((r) => setTimeout(r, ms));

const POINTER_SCRIPT = `
(() => {
  const install = () => {
    if (document.getElementById('__cursor')) return;
    const style = document.createElement('style');
    style.textContent = \`
      #__cursor { position: fixed; left: 0; top: 0; width: 22px; height: 22px; z-index: 2147483647; pointer-events: none;
        transform: translate(-100px,-100px); filter: drop-shadow(0 2px 3px rgba(0,0,0,.35)); }
      .__ripple { position: fixed; z-index: 2147483646; pointer-events: none; width: 40px; height: 40px; margin: -20px 0 0 -20px;
        border-radius: 50%; background: rgba(124, 77, 255, .35); border: 2px solid rgba(124,77,255,.8); animation: __rip .55s ease-out forwards; }
      @keyframes __rip { from { transform: scale(.3); opacity: 1 } to { transform: scale(1.6); opacity: 0 } }\`;
    document.head.appendChild(style);
    const c = document.createElement('div');
    c.id = '__cursor';
    c.innerHTML = '<svg viewBox="0 0 24 24" width="22" height="22"><path d="M4 2 L4 20 L9 15.5 L12.5 22 L15.5 20.5 L12 14 L19 14 Z" fill="#111" stroke="#fff" stroke-width="1.6" stroke-linejoin="round"/></svg>';
    document.body.appendChild(c);
    const last = window.__lastPointer;
    if (last) c.style.transform = 'translate(' + (last[0] - 4) + 'px,' + (last[1] - 2) + 'px)';
  };
  document.addEventListener('mousemove', (e) => {
    window.__lastPointer = [e.clientX, e.clientY];
    const c = document.getElementById('__cursor');
    if (c) c.style.transform = 'translate(' + (e.clientX - 4) + 'px,' + (e.clientY - 2) + 'px)';
  }, true);
  document.addEventListener('pointerdown', (e) => {
    const r = document.createElement('div');
    r.className = '__ripple'; r.style.left = e.clientX + 'px'; r.style.top = e.clientY + 'px';
    document.body.appendChild(r); setTimeout(() => r.remove(), 700);
  }, true);
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', install); else install();
  new MutationObserver(install).observe(document.documentElement, { childList: true });
})();`;

// The page checks window.parent !== window and calls these SDK functions; Teams' own Save button calls the handler.
const FAKE_TEAMS = `
try { window.parent = { postMessage() {} }; } catch {}
window.__teams = { valid: false };`;
const FAKE_SDK = `
window.microsoftTeams = {
  app: { initialize: () => Promise.resolve() },
  pages: { config: {
    setValidityState: (v) => { window.__teams.valid = v; },
    registerOnSaveHandler: (h) => { window.__teams.save = h; },
    setConfig: (c) => { window.__teams.config = c; return Promise.resolve(); },
  } },
};`;

async function namedState(name, viewport) {
  const ctx = await browser.newContext({ viewport, baseURL: BASE });
  const p = await ctx.newPage();
  await p.goto('/game');
  await p.waitForFunction(() => localStorage.getItem('token'), null, { timeout: 30000 });
  await p.locator('button.header-welcome').click();
  const d = p.getByRole('dialog');
  await d.getByLabel('Your name').fill(name);
  await d.getByRole('button', { name: 'Save name' }).click();
  await d.waitFor({ state: 'hidden' });
  await wait(500);
  const state = await ctx.storageState();
  await ctx.close();
  return state;
}

async function recorded(key, storageState, viewport, initScripts = []) {
  const ctx = await browser.newContext({ viewport, baseURL: BASE, storageState, recordVideo: { dir: path.join(TMP, key), size: viewport } });
  for (const s of [...initScripts, POINTER_SCRIPT]) await ctx.addInitScript(s);
  const page = await ctx.newPage();
  return { ctx, page, t0: Date.now() };
}

const marks = {};
const mark = (rec, key, k) => { (marks[key] ??= {})[k] = Date.now() - rec.t0; console.log(key, k, marks[key][k]); };

async function glide(page, locator, { click = true, steps = 24, pause = 220 } = {}) {
  await locator.scrollIntoViewIfNeeded();
  const box = await locator.boundingBox();
  if (!box) throw new Error('no box for ' + locator);
  await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2, { steps });
  await wait(pause);
  if (click) { await page.mouse.down(); await wait(70); await page.mouse.up(); }
}

const desktop = { width: 1440, height: 900 };
const hostState = await namedState('Bojan', desktop);

// ---------- 1. The tab setup page, as Teams shows it in its "add a tab" dialog ----------
const cfg = await recorded('cfg', hostState, { width: 640, height: 520 }, [FAKE_TEAMS]);
await cfg.ctx.route('https://res.cdn.office.net/teams-js/**', (route) => route.fulfill({ contentType: 'text/javascript', body: FAKE_SDK }));
const cp = cfg.page;
await cp.goto('/teams/config');
await cp.getByRole('heading', { name: 'Open Planning Poker in Teams' }).waitFor();
await cp.waitForFunction(() => window.__teams.save, null, { timeout: 15000 });
await cp.mouse.move(420, 380);
await wait(600);
mark(cfg, 'cfg', 'open');
await wait(1200);
const nameInput = cp.getByLabel('Game name');
await glide(cp, nameInput);
await cp.keyboard.press('Control+A');
await nameInput.pressSequentially('Sprint 25 Planning', { delay: 60 });
mark(cfg, 'cfg', 'named');
await wait(700);
await glide(cp, cp.getByRole('combobox'));
await cp.getByRole('option', { name: 'Fibonacci', exact: true }).waitFor();
mark(cfg, 'cfg', 'deckOpen');
await wait(1400);
await glide(cp, cp.getByRole('option', { name: 'Fibonacci', exact: true }));
await wait(900);
await cp.mouse.move(600, 500, { steps: 18 });
await wait(400);
mark(cfg, 'cfg', 'save');
await cp.evaluate(() => new Promise((resolve, reject) => window.__teams.save({ notifySuccess: resolve, notifyFailure: reject })));
const config = await cp.evaluate(() => window.__teams.config);
mark(cfg, 'cfg', 'saved');
await cp.screenshot({ path: path.join(SHOTS, 'int-cfg.png') });
await wait(600);
mark(cfg, 'cfg', 'end');
console.log('tab config', config);
const gameId = config.entityId.replace(/^opp-/, '');
const ownerState = await cfg.ctx.storageState();
const cfgVideo = cp.video();
await cfg.ctx.close();
await cfgVideo.saveAs(path.join(OUT, 'teams-config.webm'));

// ---------- 2. Backlog (not recorded) ----------
const prep = await browser.newContext({ viewport: desktop, baseURL: BASE, storageState: ownerState });
const pp = await prep.newPage();
await pp.goto(`/game/${gameId}`);
await pp.getByRole('button', { name: /Import/ }).first().click();
const idlg = pp.getByRole('dialog');
await idlg.getByLabel('Paste tickets').fill('OPP-201 Slack sign-in\nOPP-202 Teams meeting tab\nOPP-203 Jira story points');
await idlg.getByRole('button', { name: /Import 3 tickets/ }).click();
await idlg.waitFor({ state: 'hidden' });
await pp.waitForURL(/\/ticket|\/game\/[0-9a-f-]{36}\/./, { timeout: 15000 }).catch(() => {});
await wait(1500);
const ticketPath = new URL(pp.url()).pathname;
console.log('ticket', ticketPath);
await prep.close();

// ---------- 3. The meeting: host in the side panel, Ana on the stage, two more players ----------
const embedUrl = `${ticketPath}?embed=teams`;
const anaState = await namedState('Ana', desktop);
const panel = await recorded('panel', ownerState, { width: 360, height: 760 });
const stage = await recorded('stage', anaState, { width: 1280, height: 720 });
marks.stageOffset = stage.t0 - panel.t0; // stage clock = panel clock - stageOffset
const hp = panel.page;
const sp = stage.page;
await Promise.all([hp.goto(embedUrl), sp.goto(embedUrl)]);
await hp.getByRole('region', { name: 'Voting area' }).waitFor();
await sp.getByRole('region', { name: 'Voting area' }).waitFor();
await hp.mouse.move(250, 500);
await sp.mouse.move(900, 500);
mark(panel, 'panel', 'open');
await wait(1500);

const players = [];
for (const name of ['Marko', 'Jelena']) {
  const st = await namedState(name, desktop);
  const ctx = await browser.newContext({ viewport: desktop, baseURL: BASE, storageState: st });
  const p = await ctx.newPage();
  await p.goto(ticketPath);
  await p.getByRole('region', { name: 'Voting area' }).waitFor();
  players.push({ ctx, p });
  await wait(700);
}
mark(panel, 'panel', 'joined');
await hp.screenshot({ path: path.join(SHOTS, 'int-panel-joined.png') });
await sp.screenshot({ path: path.join(SHOTS, 'int-stage-joined.png') });
await wait(1500);

const timer = hp.getByRole('group', { name: 'Voting timer' });
const hasTimer = await timer.getByRole('button', { name: 'Start' }).isVisible().catch(() => false);
if (hasTimer) { await glide(hp, timer.getByRole('button', { name: 'Start' })); mark(panel, 'panel', 'timer'); await wait(1500); }

mark(panel, 'panel', 'votesStart');
await players[0].p.getByRole('button', { name: 'Vote 5', exact: true }).click();
await wait(900);
await glide(sp, sp.getByRole('button', { name: 'Vote 8', exact: true }));
mark(panel, 'panel', 'anaVoted');
await wait(900);
await players[1].p.getByRole('button', { name: 'Vote 3', exact: true }).click();
await wait(900);
await glide(hp, hp.getByRole('button', { name: 'Vote 5', exact: true }));
mark(panel, 'panel', 'hostVoted');
await wait(1500);
if (hasTimer) { await glide(hp, timer.getByRole('button', { name: 'Stop' })); await wait(700); }

await glide(hp, hp.getByRole('button', { name: 'Reveal cards' }), { click: false });
await wait(400);
mark(panel, 'panel', 'reveal');
await hp.mouse.down(); await wait(70); await hp.mouse.up();
await wait(3500);
await hp.screenshot({ path: path.join(SHOTS, 'int-panel-revealed.png') });
await sp.screenshot({ path: path.join(SHOTS, 'int-stage-revealed.png') });
await hp.evaluate(() => window.scrollTo({ top: document.body.scrollHeight * 0.45, behavior: 'smooth' }));
await sp.evaluate(() => window.scrollTo({ top: 420, behavior: 'smooth' }));
mark(panel, 'panel', 'results');
await wait(3500);
mark(panel, 'panel', 'end');

const pv = hp.video();
const sv = sp.video();
await panel.ctx.close();
await stage.ctx.close();
for (const pl of players) await pl.ctx.close();
await pv.saveAs(path.join(OUT, 'teams-panel.webm'));
await sv.saveAs(path.join(OUT, 'teams-stage.webm'));
fs.writeFileSync(path.join(OUT, 'teams-marks.json'), JSON.stringify(marks, null, 2));
await browser.close();
console.log('saved game', gameId);
