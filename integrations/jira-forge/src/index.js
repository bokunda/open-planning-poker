import Resolver from '@forge/resolver';
import api, { route } from '@forge/api';

const resolver = new Resolver();

// The panel asks for the issue's key and summary; Forge calls Jira as the current user, so no tokens are stored.
resolver.define('getIssue', async ({ context }) => {
  const key = context.extension.issue.key;
  const response = await api.asUser().requestJira(route`/rest/api/3/issue/${key}?fields=summary`);
  const issue = await response.json();
  return { key, summary: issue.fields?.summary ?? '' };
});

export const handler = resolver.getDefinitions();
