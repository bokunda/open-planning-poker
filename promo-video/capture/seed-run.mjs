// Creates the tour's demo data on production and saves the users and ids to capture/tmp-seed/seed.json (gitignored:
// it holds the anonymous tokens). Run once before record-tour.mjs.
import fs from 'node:fs';
import path from 'node:path';
import { devices } from '@playwright/test';
import {
  launch, namedUser, gql, createTeam, joinTeam, finishedSprint, PAST_SPRINTS, OTHER_TEAM_SPRINTS,
} from './seed.mjs';

const OUT = path.resolve('capture/tmp-seed');
fs.mkdirSync(OUT, { recursive: true });
const browser = await launch();
const desktop = { viewport: { width: 1440, height: 900 }, deviceScaleFactor: 1.5 };
const host = await namedUser(browser, 'Bojan', desktop);
const ana = await namedUser(browser, 'Ana', { ...devices['iPhone 13'] });
const marko = await namedUser(browser, 'Marko', desktop);
const jelena = await namedUser(browser, 'Jelena', desktop);
await browser.close();
const team = [host, ana, marko, jelena];

const phoenix = await createTeam(host, 'Payments team', 'Checkout, billing and invoices');
const atlas = await createTeam(host, 'Mobile team', 'iOS and Android apps');
for (const m of [ana, marko, jelena]) { await joinTeam(m, phoenix); await joinTeam(m, atlas); }
console.log('teams', phoenix, atlas);

const sprints = {};
for (const [name, tickets] of PAST_SPRINTS) { sprints[name] = await finishedSprint(team, phoenix, name, tickets); console.log(name); }
for (const [name, tickets] of OTHER_TEAM_SPRINTS) { sprints[name] = await finishedSprint(team, atlas, name, tickets); console.log(name); }

const org = (await gql(host, 'mutation($i:CreateOrganizationInput!){createOrganization(input:$i){organizationInfo{id} errors{__typename}}}', { i: { name: 'Acme Software' } })).createOrganization.organizationInfo.id;
for (const t of [phoenix, atlas]) await gql(host, 'mutation($i:AddTeamToOrganizationInput!){addTeamToOrganization(input:$i){__typename}}', { i: { id: org, teamId: t } });
console.log('org', org);

fs.writeFileSync(path.join(OUT, 'seed.json'), JSON.stringify({ users: { host, ana, marko, jelena }, teams: { phoenix, atlas }, sprints, org }, null, 1));
console.log('saved');
