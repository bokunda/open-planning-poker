import Resolver from '@forge/resolver';
import api, { route } from '@forge/api';

const resolver = new Resolver();

/** Names Jira uses for the story points field (company-managed and team-managed projects). */
const STORY_POINT_FIELDS = ['Story Points', 'Story point estimate'];

/**
 * The panel asks for the issue: key, summary and current story points. Forge calls Jira as the current user, so
 * Open Planning Poker stores no tokens and the app needs no access outside Atlassian.
 */
resolver.define('getIssue', async ({ context }) => {
  const key = context.extension.issue.key;
  const fieldsResponse = await api.asUser().requestJira(route`/rest/api/3/field`);
  const fields = fieldsResponse.ok ? await fieldsResponse.json() : [];
  const pointFields = fields.filter(field => STORY_POINT_FIELDS.includes(field.name)).map(field => field.id);

  const wanted = ['summary', ...pointFields].join(',');
  const response = await api.asUser().requestJira(route`/rest/api/3/issue/${key}?fields=${wanted}`);
  const issue = await response.json();
  const points = pointFields.map(id => issue.fields?.[id]).find(value => value !== null && value !== undefined);
  return { key, summary: issue.fields?.summary ?? '', storyPoints: points ?? null };
});

export const handler = resolver.getDefinitions();
