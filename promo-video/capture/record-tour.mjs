// Records the feature tour on production, one video per segment: public/rec/tour-<segment>.webm and
// public/rec/tour-marks.json ({ segment: { mark: ms } }). Run capture/seed-run.mjs first.
// Usage: node capture/record-tour.mjs [team game retro org api]   (no args: all segments)
// Only the host (desktop) is recorded; Ana, Marko and Jelena act through the GraphQL API as themselves.
import fs from 'node:fs';
import path from 'node:path';
import { launch, BASE, gql, joinGame, vote, reveal, finalEstimate } from './seed.mjs';

const OUT = path.resolve('public/rec');
const TMP = path.resolve('capture/tmp-video-tour');
const seed = JSON.parse(fs.readFileSync('capture/tmp-seed/seed.json', 'utf8'));
const { host, ana, marko, jelena } = seed.users;
const others = [ana, marko, jelena];
const marksFile = path.join(OUT, 'tour-marks.json');
const allMarks = fs.existsSync(marksFile) ? JSON.parse(fs.readFileSync(marksFile, 'utf8')) : {};
const wait = (ms) => new Promise((r) => setTimeout(r, ms));
const browser = await launch();

const POINTER_SCRIPT = `
(() => {
  const install = () => {
    if (document.getElementById('__cursor')) return;
    const style = document.createElement('style');
    style.textContent = \`
      #__cursor { position: fixed; left: 0; top: 0; width: 26px; height: 26px; z-index: 2147483647; pointer-events: none;
        transform: translate(-100px,-100px); filter: drop-shadow(0 2px 3px rgba(0,0,0,.35)); }
      .__ripple { position: fixed; z-index: 2147483646; pointer-events: none; width: 44px; height: 44px; margin: -22px 0 0 -22px;
        border-radius: 50%; background: rgba(124, 77, 255, .35); border: 2px solid rgba(124,77,255,.8); animation: __rip .55s ease-out forwards; }
      @keyframes __rip { from { transform: scale(.3); opacity: 1 } to { transform: scale(1.6); opacity: 0 } }\`;
    document.head.appendChild(style);
    const c = document.createElement('div');
    c.id = '__cursor';
    c.innerHTML = '<svg viewBox="0 0 24 24" width="26" height="26"><path d="M4 2 L4 20 L9 15.5 L12.5 22 L15.5 20.5 L12 14 L19 14 Z" fill="#111" stroke="#fff" stroke-width="1.6" stroke-linejoin="round"/></svg>';
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

/** One recorded segment: a fresh host context whose video becomes public/rec/tour-<name>.webm. */
async function segment(name, run) {
  console.log('== segment', name);
  const dir = path.join(TMP, name);
  fs.rmSync(dir, { recursive: true, force: true });
  const ctx = await browser.newContext({
    viewport: { width: 1440, height: 900 }, deviceScaleFactor: 1.5, baseURL: BASE, storageState: host.state,
    recordVideo: { dir, size: { width: 2160, height: 1350 } },
  });
  await ctx.addInitScript(POINTER_SCRIPT);
  const page = await ctx.newPage();
  const t0 = Date.now();
  const marks = {};
  const mark = (k) => { marks[k] = Date.now() - t0; console.log(' ', k, marks[k]); };
  try {
    await run(page, mark);
  } catch (e) {
    await page.screenshot({ path: `capture/shots/fail-${name}.png` }).catch(() => {});
    throw e;
  } finally {
    mark('end');
    const video = page.video();
    await ctx.close();
    await video.saveAs(path.join(OUT, `tour-${name}.webm`));
    allMarks[name] = marks;
    fs.writeFileSync(marksFile, JSON.stringify(allMarks, null, 2));
  }
}

async function glide(page, locator, { click = true, steps = 26, pause = 220 } = {}) {
  await locator.scrollIntoViewIfNeeded();
  const box = await locator.boundingBox();
  if (!box) throw new Error('no box for ' + locator);
  await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2, { steps });
  await wait(pause);
  if (click) { await page.mouse.down(); await wait(70); await page.mouse.up(); }
}

/** Smoothly scrolls so the element's top sits `offset` px below the top of the viewport. */
async function scrollTo(page, locator, offset = 80, ms = 1100) {
  await locator.first().waitFor();
  await locator.first().evaluate((el, off) => {
    const top = el.getBoundingClientRect().top + window.scrollY - off;
    window.scrollTo({ top, behavior: 'smooth' });
  }, offset);
  await wait(ms);
}
const scrollTop = async (page) => { await page.evaluate(() => window.scrollTo({ top: 0, behavior: 'smooth' })); await wait(900); };

const ticketsOf = async (gameId) =>
  (await gql(host, 'query($g:UUID!){tickets(gameId:$g){... on ApiCollectionOfTicket{items{id name}}}}', { g: gameId })).tickets.items;
const retroOf = async (u, gameId) =>
  (await gql(u, 'query($g:UUID!){retro(gameId:$g){... on RetroInfo{id cards{id text column}}}}', { g: gameId })).retro;
const move = (u, ticketId, column) =>
  gql(u, 'mutation($i:MoveSortedTicketInput!){moveSortedTicket(input:$i){__typename}}', { i: { ticketId, column } });
const later = (ms, fn) => wait(ms).then(fn).catch((e) => console.log('  background:', e.message));

const SEGMENTS = {
  // Team room: velocity, history, sprint plan, insights, API token.
  async team(page, mark) {
    await page.goto(`/team/${seed.teams.phoenix}`);
    await page.getByRole('heading', { name: 'Payments team' }).waitFor();
    await page.mouse.move(900, 500);
    await wait(1200);
    mark('teamRoom');
    await wait(2000);
    await scrollTo(page, page.locator('section[aria-labelledby="velocity-title"]'), 90);
    mark('velocity');
    await page.mouse.move(700, 420, { steps: 20 });
    await wait(2600);
    await scrollTo(page, page.locator('section[aria-labelledby="history-title"]'), 90);
    mark('history');
    await wait(2400);
    const plan = page.getByRole('heading', { name: 'Plan the sprint' });
    await scrollTo(page, plan, 70);
    mark('plan');
    await wait(800);
    const anaDays = page.getByLabel('Available days for Ana');
    await glide(page, anaDays);
    await anaDays.press('Control+A');
    await anaDays.pressSequentially('6', { delay: 90 });
    await anaDays.press('Tab');
    await wait(900);
    await scrollTo(page, page.getByText('Tickets in the sprint'), 200, 800);
    for (const key of ['PAY-231', 'PAY-233', 'PAY-235']) {
      await glide(page, page.getByRole('checkbox', { name: new RegExp(key) }), { steps: 16 });
      await wait(600);
    }
    mark('planned');
    await wait(2200);
    await scrollTo(page, page.getByRole('heading', { name: 'How the sessions went' }), 80);
    mark('insights');
    await wait(2800);
    await scrollTo(page, page.getByRole('heading', { name: 'API and webhooks' }), 80);
    mark('api');
    await wait(700);
    const tokenName = page.getByPlaceholder('e.g. Reporting script');
    await glide(page, tokenName);
    await tokenName.pressSequentially('Sprint report script', { delay: 45 });
    await wait(400);
    await glide(page, page.getByRole('button', { name: 'Create token' }));
    await wait(800);
    mark('apiToken');
    await wait(3200);
  },

  // A new sprint planning from the team room: sorting board, discussion tools, consensus and the recap.
  async game(page, mark) {
    await page.goto(`/team/${seed.teams.phoenix}`);
    const sprint = page.locator('#sprint-name');
    await sprint.waitFor();
    await wait(1000);
    await glide(page, sprint);
    await sprint.press('Control+A');
    await sprint.pressSequentially('Sprint 24', { delay: 60 });
    await glide(page, page.locator('section.team-start').getByRole('button', { name: /Start/ }));
    await page.waitForURL(/\/game\/[0-9a-f-]{36}/);
    await page.getByRole('region', { name: 'Voting area' }).waitFor();
    mark('room');
    const gameId = page.url().match(/\/game\/([0-9a-f-]{36})/)[1];
    for (const u of others) await joinGame(u, gameId);
    await gql(host, 'mutation($i:UpdateFacilitationOptionsInput!){updateFacilitationOptions(input:$i){__typename}}',
      { i: { gameId, outlierNotes: true, discussionMinutes: 0, confidenceCheck: true, reactions: true, celebrations: true } });
    await wait(1200);

    // Import the backlog.
    await glide(page, page.getByRole('button', { name: /Import/ }).first());
    const dlg = page.getByRole('dialog');
    await dlg.getByLabel('Paste tickets').waitFor();
    await glide(page, dlg.getByLabel('Paste tickets'));
    await dlg.getByLabel('Paste tickets').fill(
      'PAY-241 Partial refunds\nPAY-242 Invoice reminders\nPAY-243 VAT number check\nPAY-244 Payment links\nPAY-245 Team billing page\nPAY-246 Card expiry e-mails');
    await wait(400);
    await glide(page, dlg.getByRole('button', { name: /Import 6 tickets/ }));
    await dlg.waitFor({ state: 'hidden' });
    await page.getByText('PAY-246 Card expiry e-mails').first().waitFor();
    await wait(2500);
    const tickets = await ticketsOf(gameId);
    if (tickets.length !== 6) throw new Error(`imported ${tickets.length} tickets`);
    const byKey = (k) => tickets.find((t) => t.name.startsWith(k)).id;

    // Silent sorting.
    await glide(page, page.getByRole('button', { name: 'Room' }));
    await wait(700);
    await glide(page, page.getByRole('menuitem', { name: 'Sort the backlog' }));
    await page.getByRole('heading', { name: 'Sort the backlog' }).first().waitFor().catch(() => {});
    mark('sorting');
    const moves = [
      [ana, 'PAY-241', '5'], [marko, 'PAY-242', '3'], [jelena, 'PAY-243', '8'], [ana, 'PAY-244', '5'],
      [marko, 'PAY-245', '13'], [jelena, 'PAY-246', '2'], [ana, 'PAY-243', '3'], [marko, 'PAY-243', '8'], [jelena, 'PAY-243', '5'],
    ];
    const sorting = (async () => { for (const [u, k, c] of moves) { await wait(900); await move(u, byKey(k), c); } })();
    // The board scrolls sideways; glide along so every column shows.
    const scrollBoard = (x) => page.evaluate((left) => {
      const el = [...document.querySelectorAll('*')].find((e) => e.scrollWidth > e.clientWidth + 40 && /auto|scroll/.test(getComputedStyle(e).overflowX));
      el?.scrollTo({ left, behavior: 'smooth' });
    }, x);
    await wait(2500);
    await page.mouse.move(800, 520, { steps: 25 });
    await scrollBoard(450);
    await wait(3500);
    await scrollBoard(1200);
    await sorting;
    await wait(1500);
    await scrollBoard(600);
    await wait(2500);
    mark('contested');
    await wait(2000);
    await glide(page, page.getByRole('button', { name: 'Room' }));
    await wait(500);
    await glide(page, page.getByRole('menuitem', { name: 'Back to voting' }));
    await wait(1500);

    // Vote on the contested ticket: discussion notes from the lowest and highest voter, reactions, confidence.
    const contested = byKey('PAY-243');
    await page.goto(`/game/${gameId}/ticket/${contested}`);
    await page.getByRole('region', { name: 'Voting area' }).waitFor();
    await wait(1200);
    mark('vote');
    await vote(ana, contested, '3'); await wait(700);
    await vote(marko, contested, '13'); await wait(700);
    await vote(jelena, contested, '5'); await wait(500);
    await glide(page, page.getByRole('button', { name: 'Vote 5', exact: true }));
    await wait(1000);
    await glide(page, page.getByRole('button', { name: 'Reveal cards' }));
    mark('reveal');
    await wait(1500);
    await gql(marko, 'mutation($i:SetVoteNoteInput!){setVoteNote(input:$i){__typename}}', { i: { ticketId: contested, note: 'The EU VIES service is slow, we need a cache and a retry queue' } });
    await wait(900);
    await gql(ana, 'mutation($i:SetVoteNoteInput!){setVoteNote(input:$i){__typename}}', { i: { ticketId: contested, note: 'We already validate the format, it is one API call' } });
    for (const [u, e] of [[jelena, '🤔'], [ana, '👍'], [marko, '👍']]) {
      await gql(u, 'mutation($i:SendReactionInput!){sendReaction(input:$i){__typename}}', { i: { gameId, emoji: e } }).catch(() => {});
      await wait(300);
    }
    await scrollTo(page, page.locator('app-vote-results'), 90);
    mark('notes');
    await wait(3800);
    await glide(page, page.locator('app-vote-results button').filter({ hasText: /^\s*8\s*$/ }).first());
    await wait(1200);
    mark('final');
    for (const [u, v] of [[ana, 3], [marko, 4], [jelena, 4]]) {
      await gql(u, 'mutation($i:SetConfidenceInput!){setConfidence(input:$i){__typename}}', { i: { ticketId: contested, value: v } });
      await wait(400);
    }
    await wait(2500);
    mark('confidence');
    await wait(1500);

    // A ticket everyone agrees on: confetti.
    const easy = byKey('PAY-246');
    await scrollTop(page);
    await page.goto(`/game/${gameId}/ticket/${easy}`);
    await page.getByRole('region', { name: 'Voting area' }).waitFor();
    await wait(1000);
    for (const u of others) { await vote(u, easy, '2'); await wait(400); }
    await glide(page, page.getByRole('button', { name: 'Vote 2', exact: true }));
    await wait(800);
    await glide(page, page.getByRole('button', { name: 'Reveal cards' }));
    mark('consensus');
    await wait(3500);
    await finalEstimate(host, easy, '2');

    // Estimate the rest off camera, so the recap has numbers.
    for (const [k, vals, fin] of [['PAY-241', ['5', '5', '5', '8'], '5'], ['PAY-242', ['3', '3', '2', '3'], '3'], ['PAY-244', ['5', '8', '5', '5'], '5'], ['PAY-245', ['13', '13', '13', '13'], '13']]) {
      const id = byKey(k);
      for (let i = 0; i < 4; i++) await vote([host, ...others][i], id, vals[i]);
      await reveal(host, id);
      await finalEstimate(host, id, fin);
    }
    await wait(1000);

    // Share the recap.
    await glide(page, page.getByRole('button', { name: 'Room' }));
    await wait(600);
    await glide(page, page.getByRole('menuitem', { name: 'Share the recap' }));
    const share = page.getByRole('dialog');
    await share.waitFor();
    mark('recapDialog');
    await wait(1200);
    const shareButton = share.getByRole('button', { name: 'Share', exact: true });
    if (await shareButton.count()) { await glide(page, shareButton); await wait(1500); }
    const link = share.getByRole('link', { name: 'Open the recap' });
    await link.waitFor();
    await glide(page, link, { click: false });
    const href = await link.getAttribute('href');
    await wait(600);
    await page.goto(href);
    await page.waitForLoadState('networkidle').catch(() => {});
    await wait(1500);
    mark('recap');
    await wait(2500);
    await page.evaluate(() => window.scrollTo({ top: 600, behavior: 'smooth' }));
    await wait(3000);
    fs.writeFileSync('capture/tmp-seed/game24.json', JSON.stringify({ gameId, href }));
  },

  // Retro of Sprint 23: silent writing, reveal, grouping, dot voting, action items, the sprint in numbers.
  async retro(page, mark) {
    const gameId = seed.sprints[process.env.RETRO_SPRINT ?? 'Sprint 23'];
    await page.goto(`/retro/${gameId}`);
    await page.getByRole('button', { name: 'Start the retro' }).waitFor();
    await page.mouse.move(800, 500);
    await wait(1500);
    mark('retroStartPage');
    await glide(page, page.getByRole('button', { name: 'Start the retro' }));
    await page.getByPlaceholder('Write a card…').first().waitFor();
    mark('retroStart');
    const retro = await retroOf(host, gameId);
    const add = (u, column, text) => gql(u, 'mutation($i:AddRetroCardInput!){addRetroCard(input:$i){__typename}}', { i: { retroId: retro.id, column, text } });
    const cards = [
      [ana, 0, 'Pairing on the subscriptions work'], [marko, 1, 'Code reviews waited two days'], [jelena, 0, 'Release went out on Thursday, no rollback'],
      [ana, 1, 'Reviews took too long'], [marko, 2, 'A review rotation in Slack'], [jelena, 1, 'Flaky checkout tests'], [ana, 2, 'Demo to support every sprint'],
    ];
    const writing = (async () => { for (const [u, c, t] of cards) { await wait(1100); await add(u, c, t); } })();
    await wait(1500);
    const input = page.getByPlaceholder('Write a card…').first();
    await glide(page, input);
    await input.pressSequentially('Payment links shipped early', { delay: 45 });
    await input.press('Enter');
    mark('retroWrite');
    await writing;
    await wait(2500);

    await glide(page, page.getByRole('button', { name: 'Reveal all cards' }));
    mark('retroReveal');
    await wait(2500);
    const now = await retroOf(host, gameId);
    const card = (t) => now.cards.find((c) => c.text === t).id;
    await gql(host, 'mutation($i:GroupRetroCardInput!){groupRetroCard(input:$i){__typename}}', { i: { cardId: card('Reviews took too long'), targetCardId: card('Code reviews waited two days') } });
    await wait(600);
    await gql(host, 'mutation($i:SetRetroGroupTitleInput!){setRetroGroupTitle(input:$i){__typename}}', { i: { cardId: card('Code reviews waited two days'), title: 'Slow code reviews' } }).catch((e) => console.log('  title', e.message));
    mark('retroGroup');
    await wait(3000);

    await glide(page, page.getByRole('button', { name: 'Start dot voting' }));
    mark('retroVoting');
    const dot = (u, t) => gql(u, 'mutation($i:VoteRetroCardInput!){voteRetroCard(input:$i){__typename}}', { i: { cardId: card(t), add: true } });
    const dots = (async () => {
      for (const [u, t] of [[ana, 'Code reviews waited two days'], [marko, 'Code reviews waited two days'], [jelena, 'Flaky checkout tests'], [ana, 'Flaky checkout tests'], [marko, 'A review rotation in Slack'], [jelena, 'Code reviews waited two days']]) {
        await wait(500); await dot(u, t);
      }
    })();
    await wait(1200);
    for (let i = 0; i < 2; i++) {
      await glide(page, page.getByRole('button', { name: 'Add a dot' }).nth(i === 0 ? 1 : 3), { steps: 18 });
      await wait(700);
    }
    await dots;
    await wait(1500);
    await glide(page, page.getByRole('button', { name: 'End voting and show results' }));
    mark('retroResults');
    await wait(3000);
    const item = page.getByPlaceholder('What will we do?').first();
    await scrollTo(page, item, 300);
    await glide(page, item);
    await item.pressSequentially('Start a review rotation, two reviewers a day', { delay: 35 });
    await item.press('Enter');
    mark('retroAction');
    await wait(2500);
    await scrollTop(page);
    const facts = page.getByRole('button', { name: /Sprint in numbers/ }).first();
    await glide(page, facts);
    mark('retroFacts');
    await wait(1500);
    await page.mouse.move(900, 600, { steps: 20 });
    await wait(3000);
  },

  // Organization: teams side by side.
  async org(page, mark) {
    await page.goto(`/org/${seed.org}`);
    await page.getByRole('heading', { name: 'Acme Software' }).first().waitFor();
    await page.mouse.move(900, 400);
    await wait(1500);
    mark('org');
    await scrollTo(page, page.getByRole('heading', { name: 'Teams side by side' }), 120);
    mark('compare');
    await page.mouse.move(1100, 520, { steps: 25 });
    await wait(1500);
    await glide(page, page.getByRole('button', { name: 'Download as CSV' }), { click: false });
    await wait(2500);
  },

  // The public API docs on the website.
  async api(page, mark) {
    await page.goto('https://openplanningpoker.com/public-api/');
    await page.waitForLoadState('networkidle').catch(() => {});
    await page.mouse.move(900, 400);
    await wait(1500);
    mark('docs');
    for (let i = 0; i < 4; i++) { await page.mouse.wheel(0, 420); await wait(1400); }
    mark('docsEnd');
    await wait(800);
  },
};

const want = process.argv.slice(2);
for (const [name, run] of Object.entries(SEGMENTS)) {
  if (want.length && !want.includes(name)) continue;
  await segment(name, run);
}
await browser.close();
console.log('done');
