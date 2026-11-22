// Revokes every API token of the demo team (one is visible in the tour video).
import fs from 'node:fs';
import { gql } from './seed.mjs';
const seed = JSON.parse(fs.readFileSync('capture/tmp-seed/seed.json', 'utf8'));
const teamId = seed.teams.phoenix;
const d = await gql(seed.users.host, 'query($t:UUID!){team(id:$t){... on Team{apiTokens{id name}}}}', { t: teamId });
for (const tk of d.team.apiTokens) {
  await gql(seed.users.host, 'mutation($i:RevokeTeamApiTokenInput!){revokeTeamApiToken(input:$i){__typename}}', { i: { teamId, tokenId: tk.id } });
  console.log('revoked', tk.name);
}
