// Demo data on production for the feature tour: anonymous users, two team rooms with past sprints, and an organization.
// Everything goes through the public GraphQL gateway with each user's own anonymous token, like the app does.
import { chromium } from '@playwright/test';

export const BASE = 'https://app.openplanningpoker.com';
const GATEWAY = 'https://gateway.openplanningpoker.com/graphql';
const wait = (ms) => new Promise((r) => setTimeout(r, ms));

/** A fresh anonymous user with a name: returns its storage state and token. */
export async function namedUser(browser, name, opts = {}) {
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
  const token = await p.evaluate(() => localStorage.getItem('token'));
  await ctx.close();
  return { name, state, token };
}

/** Runs one GraphQL operation as a user; throws on errors and on payload errors. */
export async function gql(user, query, variables = {}) {
  const res = await fetch(GATEWAY, {
    method: 'POST',
    headers: { 'content-type': 'application/json', authorization: `Bearer ${user.token}` },
    body: JSON.stringify({ query, variables }),
  });
  const body = await res.json();
  if (body.errors?.length) throw new Error(`${user.name}: ${JSON.stringify(body.errors).slice(0, 400)}`);
  for (const v of Object.values(body.data ?? {})) {
    if (v && Array.isArray(v.errors) && v.errors.length) throw new Error(`${user.name}: ${JSON.stringify(v.errors).slice(0, 400)}`);
  }
  return body.data;
}

export const createTeam = async (u, name, description) =>
  (await gql(u, 'mutation($i:CreateTeamInput!){createTeam(input:$i){team{id} errors{__typename}}}', { i: { name, description } })).createTeam.team.id;
export const joinTeam = (u, teamId) =>
  gql(u, 'mutation($i:JoinTeamInput!){joinTeam(input:$i){__typename}}', { i: { teamId } });
export const createGame = async (u, name, teamId) =>
  (await gql(u, 'mutation($i:CreateGameInput!){createGame(input:$i){game{id} errors{__typename}}}', { i: { name, description: '', teamId, source: 'promo' } })).createGame.game.id;
export const joinGame = (u, gameId) =>
  gql(u, 'mutation($i:JoinGameInput!){joinGame(input:$i){__typename}}', { i: { gameId } });
export const createTicket = async (u, gameId, name, description = '') =>
  (await gql(u, 'mutation($i:CreateTicketInput!){createTicket(input:$i){ticket{id} errors{__typename}}}', { i: { gameId, name, description } })).createTicket.ticket.id;
export const vote = (u, ticketId, value) =>
  gql(u, 'mutation($i:CreateOrUpdateVoteInput!){createOrUpdateVote(input:$i){__typename}}', { i: { ticketId, value } });
export const reveal = (u, ticketId) =>
  gql(u, 'mutation($i:RevealVotesInput!){revealVotes(input:$i){__typename}}', { i: { ticketId } });
export const finalEstimate = (u, ticketId, value) =>
  gql(u, 'mutation($i:SetFinalEstimateInput!){setFinalEstimate(input:$i){__typename}}', { i: { ticketId, value } });

/** A finished sprint: tickets voted by the team, revealed and estimated. votes[i] = one value per member. */
export async function finishedSprint(members, teamId, name, tickets) {
  const [host, ...rest] = members;
  const gameId = await createGame(host, name, teamId);
  for (const m of rest) await joinGame(m, gameId);
  for (const t of tickets) {
    const id = await createTicket(host, gameId, t.name);
    for (let i = 0; i < members.length; i++) await vote(members[i], id, t.votes[i % t.votes.length]);
    await reveal(host, id);
    await finalEstimate(host, id, t.final);
  }
  return gameId;
}

// Ticket sets for the past sprints: [name, votes per member, final].
const T = (name, votes, final) => ({ name, votes, final });
export const PAST_SPRINTS = [
  ['Sprint 20', [T('PAY-201 Card payments', ['5', '5', '8', '5'], '5'), T('PAY-202 Refund flow', ['8', '8', '8', '8'], '8'), T('PAY-203 Receipt e-mail', ['3', '2', '3', '3'], '3'), T('PAY-204 Fraud rules', ['13', '8', '13', '13'], '13')]],
  ['Sprint 21', [T('PAY-211 Saved cards', ['5', '5', '5', '5'], '5'), T('PAY-212 Invoice PDF', ['8', '5', '8', '8'], '8'), T('PAY-213 Currency switch', ['13', '13', '8', '13'], '13'), T('PAY-214 Admin search', ['3', '3', '3', '5'], '3')]],
  ['Sprint 22', [T('PAY-221 Apple Pay', ['8', '8', '13', '8'], '8'), T('PAY-222 Webhook retries', ['5', '5', '5', '3'], '5'), T('PAY-223 Tax rates', ['13', '13', '13', '13'], '13'), T('PAY-224 Audit log', ['5', '8', '5', '5'], '5'), T('PAY-225 Status page', ['2', '2', '3', '2'], '2')]],
  ['Sprint 23', [T('PAY-231 Subscriptions', ['13', '13', '13', '8'], '13'), T('PAY-232 Dunning e-mails', ['5', '5', '8', '5'], '5'), T('PAY-233 Coupon codes', ['8', '8', '8', '8'], '8'), T('PAY-234 Export to CSV', ['3', '3', '2', '3'], '3'), T('PAY-235 Login rate limit', ['5', '3', '5', '5'], '5')]],
];
export const OTHER_TEAM_SPRINTS = [
  ['Atlas Sprint 7', [T('MOB-71 Push notifications', ['8', '13', '5', '8'], '8'), T('MOB-72 Offline mode', ['13', '21', '13', '13'], '13'), T('MOB-73 Settings screen', ['3', '5', '3', '3'], '3')]],
  ['Atlas Sprint 8', [T('MOB-81 Biometric login', ['8', '8', '5', '13'], '8'), T('MOB-82 Deep links', ['5', '3', '8', '5'], '5'), T('MOB-83 Dark theme', ['5', '5', '5', '5'], '5'), T('MOB-84 Crash reports', ['3', '3', '3', '3'], '3')]],
  ['Atlas Sprint 9', [T('MOB-91 Widgets', ['13', '8', '13', '13'], '13'), T('MOB-92 Share sheet', ['5', '5', '3', '5'], '5'), T('MOB-93 App rating prompt', ['2', '3', '2', '2'], '2')]],
];

export async function launch() {
  return chromium.launch({ channel: 'msedge' });
}
