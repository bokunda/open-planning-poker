// Records a real Open Planning Poker session on production for the promo videos.
// Output: public/rec/host.webm (desktop host), public/rec/mobile.webm (Ana on a phone), public/rec/marks.json (event times in ms).
import { chromium, devices } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';

const BASE = 'https://app.openplanningpoker.com';
const OUT = path.resolve('public/rec');
const TMP = path.resolve('capture/tmp-video');
fs.mkdirSync(OUT, { recursive: true });
fs.rmSync(TMP, { recursive: true, force: true });

const browser = await chromium.launch({ channel: 'msedge' });
const wait = (ms) => new Promise((r) => setTimeout(r, ms));

// A fake cursor and tap ripples, because headless recordings show no pointer.
const POINTER_SCRIPT = `
(() => {
  const mobile = ${'${MOBILE}'};
  const install = () => {
    if (document.getElementById('__cursor')) return;
    const style = document.createElement('style');
    style.textContent = \`
      #__cursor { position: fixed; left: 0; top: 0; width: 26px; height: 26px; z-index: 2147483647; pointer-events: none;
        transform: translate(-100px,-100px); transition: transform 0s; filter: drop-shadow(0 2px 3px rgba(0,0,0,.35)); }
      .__ripple { position: fixed; z-index: 2147483646; pointer-events: none; width: 44px; height: 44px; margin: -22px 0 0 -22px;
        border-radius: 50%; background: rgba(124, 77, 255, .35); border: 2px solid rgba(124,77,255,.8); animation: __rip .55s ease-out forwards; }
      @keyframes __rip { from { transform: scale(.3); opacity: 1 } to { transform: scale(1.6); opacity: 0 } }\`;
    document.head.appendChild(style);
    if (!mobile) {
      const c = document.createElement('div');
      c.id = '__cursor';
      c.innerHTML = '<svg viewBox="0 0 24 24" width="26" height="26"><path d="M4 2 L4 20 L9 15.5 L12.5 22 L15.5 20.5 L12 14 L19 14 Z" fill="#111" stroke="#fff" stroke-width="1.6" stroke-linejoin="round"/></svg>';
      document.body.appendChild(c);
      const last = window.__lastPointer;
      if (last) c.style.transform = 'translate(' + (last[0] - 4) + 'px,' + (last[1] - 2) + 'px)';
    }
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

/** Gives a fresh anonymous user a name, then returns its storage state (so the recorded context starts already named). */
async function namedState(name, opts) {
  const ctx = await browser.newContext({ ...opts, baseURL: BASE });
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

async function recordedContext(name, opts, videoName, size) {
  const storageState = await namedState(name, opts);
  const ctx = await browser.newContext({
    ...opts, baseURL: BASE, storageState,
    recordVideo: { dir: path.join(TMP, videoName), size },
  });
  await ctx.addInitScript(POINTER_SCRIPT.replace('${MOBILE}', String(!!opts.isMobile)));
  const page = await ctx.newPage();
  const t0 = Date.now();
  return { ctx, page, t0 };
}

async function plainContext(name, opts) {
  const storageState = await namedState(name, opts);
  const ctx = await browser.newContext({ ...opts, baseURL: BASE, storageState });
  return { ctx, page: await ctx.newPage() };
}

const marks = { host: {}, mobile: {} };
let host, mobile;
const mark = (k) => { marks.host[k] = Date.now() - host.t0; console.log('host', k, marks.host[k]); };
const mmark = (k) => { marks.mobile[k] = Date.now() - mobile.t0; console.log('mobile', k, marks.mobile[k]); };

/** Glides the fake cursor to an element and clicks it. */
async function glide(page, locator, { click = true, steps = 28, pause = 220 } = {}) {
  await locator.scrollIntoViewIfNeeded();
  const box = await locator.boundingBox();
  if (!box) throw new Error('no box for ' + locator);
  await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2, { steps });
  await wait(pause);
  if (click) { await page.mouse.down(); await wait(70); await page.mouse.up(); }
}

const desktop = { viewport: { width: 1440, height: 900 }, deviceScaleFactor: 1.5 };
const phone = { ...devices['iPhone 13'] };

// ---------- Host (desktop, recorded) ----------
host = await recordedContext('Bojan', desktop, 'host', { width: 2160, height: 1350 });
const hp = host.page;
await hp.goto('/game');
await hp.getByRole('button', { name: 'Create game' }).first().waitFor();
await hp.mouse.move(1000, 650);
await wait(800);
mark('landing');
await wait(2500);

// Create the game.
await glide(hp, hp.getByRole('button', { name: 'Create game' }).first());
const cd = hp.getByRole('dialog');
await cd.getByLabel('Game name').waitFor();
mark('createDialog');
await wait(500);
await cd.getByLabel('Game name').pressSequentially('Sprint 24 Planning', { delay: 55 });
await wait(500);
await glide(hp, cd.getByRole('button', { name: 'Create game' }));
await hp.waitForURL(/\/game\/[0-9a-f-]{36}/);
await hp.getByRole('region', { name: 'Voting area' }).waitFor();
mark('room');
const gameUrl = new URL(hp.url()).pathname;
await wait(1800);

// Import the backlog.
await glide(hp, hp.getByRole('button', { name: /Import/ }).first());
const idlg = hp.getByRole('dialog');
await idlg.getByLabel('Paste tickets').waitFor();
mark('importDialog');
await wait(400);
await idlg.getByLabel('Paste tickets').pressSequentially('OPP-101 Login with magic link\nOPP-102 Dark mode toggle\nOPP-103 Export sprint report', { delay: 18 });
await wait(700);
await glide(hp, idlg.getByRole('button', { name: /Import 3 tickets/ }));
await idlg.waitFor({ state: 'hidden' });
mark('tickets');
await wait(1500);
// Show the ticket list, then come back up.
await hp.evaluate(() => window.scrollTo({ top: 520, behavior: 'smooth' }));
mark('ticketList');
await wait(2200);
await hp.evaluate(() => window.scrollTo({ top: 0, behavior: 'smooth' }));
await wait(1200);

// Invite.
await glide(hp, hp.getByRole('button', { name: 'Invite' }));
await hp.getByRole('dialog').waitFor();
mark('invite');
await wait(3000);
await glide(hp, hp.getByRole('dialog').getByRole('button', { name: 'Close' }));
await hp.getByRole('dialog').waitFor({ state: 'hidden' });
await hp.mouse.move(1100, 600, { steps: 15 });
await wait(400);

// ---------- Players join ----------
mobile = await recordedContext('Ana', phone, 'mobile', { width: 780, height: 1688 });
const mp = mobile.page;
mark('joinStart');
const ticketUrl = new URL(hp.url()).pathname;
console.log('host url', ticketUrl);
await mp.goto(ticketUrl);
await mp.getByRole('region', { name: 'Voting area' }).waitFor();
await wait(600);
await mp.evaluate(() => window.scrollTo({ top: 230, behavior: 'smooth' }));
mmark('joined');
await wait(900);
const marko = await plainContext('Marko', desktop);
await marko.page.goto(ticketUrl);
await marko.page.getByRole('region', { name: 'Voting area' }).waitFor();
await wait(900);
const jelena = await plainContext('Jelena', desktop);
await jelena.page.goto(ticketUrl);
await jelena.page.getByRole('region', { name: 'Voting area' }).waitFor();
mark('joined');
const heads = async (pg) => (await pg.locator('h1,h2,h3').filter({ hasText: /^OPP-1/ }).first().innerText().catch(() => '?'));
console.log('headings', await heads(hp), '|', await heads(mp), '|', await heads(marko.page), '|', await heads(jelena.page));
await wait(1800);

// Shared timer.
const timer = hp.getByRole('group', { name: 'Voting timer' });
await glide(hp, timer.getByRole('button', { name: 'Start' }));
mark('timer');
mmark('timer');
await wait(2500);

// Votes: Marko, Ana (phone), Jelena, then the host.
mark('votesStart');
await marko.page.getByRole('button', { name: 'Vote 5', exact: true }).click();
await wait(1100);
mmark('voteTap');
await mp.getByRole('button', { name: 'Vote 8', exact: true }).tap();
await wait(1100);
await jelena.page.getByRole('button', { name: 'Vote 3', exact: true }).click();
await wait(1000);
await glide(hp, hp.getByRole('button', { name: 'Vote 5', exact: true }));
mark('hostVoted');
await wait(1500);
await glide(hp, timer.getByRole('button', { name: 'Stop' }));
await wait(900);

// Reveal.
await glide(hp, hp.getByRole('button', { name: 'Reveal cards' }), { click: false });
await wait(500);
mark('reveal');
mmark('reveal');
await hp.mouse.down(); await wait(70); await hp.mouse.up();
await wait(3500);

// Chat from the team.
await jelena.page.getByPlaceholder('Type a message...').fill('We already have a PDF lib, 3 is enough 🙂');
await jelena.page.keyboard.press('Enter');
mark('chat');
mmark('chat');
await wait(2500);

// Results and the final estimate.
await hp.evaluate(() => window.scrollTo({ top: 560, behavior: 'smooth' }));
mark('results');
await wait(2500);
await glide(hp, hp.locator('app-vote-results button.final-card--suggested'));
mark('final');
await wait(2500);
await hp.evaluate(() => window.scrollTo({ top: 0, behavior: 'smooth' }));
await wait(1200);

// Room options.
await glide(hp, hp.getByRole('button', { name: 'Room' }));
mark('roomOptions');
await wait(3000);
await hp.keyboard.press('Escape');
await wait(700);

// Dark mode and languages from the menu.
await glide(hp, hp.locator('header button[mat-icon-button]').first());
mark('menu');
await wait(2200);
await glide(hp, hp.getByRole('menuitem', { name: /Dark mode/ }));
mark('dark');
await wait(2800);

// Team rooms.
await hp.goto('/team');
await hp.getByRole('heading', { name: 'Team rooms' }).first().waitFor().catch(() => {});
mark('team');
await wait(3000);
mark('end');
mmark('end');

// ---------- Save ----------
const hostVideo = hp.video();
const mobileVideo = mp.video();
await host.ctx.close();
await mobile.ctx.close();
await marko.ctx.close();
await jelena.ctx.close();
await hostVideo.saveAs(path.join(OUT, 'host.webm'));
await mobileVideo.saveAs(path.join(OUT, 'mobile.webm'));
fs.writeFileSync(path.join(OUT, 'marks.json'), JSON.stringify(marks, null, 2));
await browser.close();
console.log('saved', gameUrl);
