import { gql } from 'apollo-angular';

/* GitHub, Linear and Azure DevOps (OPP-116). Jira has its own queries in jira.graphql.ts. */

const ERRORS = `errors { ... on ApplicationError { code message } }`;

export const TRACKERS = gql`
  query Trackers { trackers { provider name enabled connected needsReconnect accountName queryHint } }`;

export const TRACKER_AUTHORIZE_URL = gql`
  query TrackerAuthorizeUrl($provider: String!, $returnPath: String!) {
    trackerAuthorizeUrl(provider: $provider, returnPath: $returnPath) {
      ... on TrackerAuthorizeLink { url }
      ... on ApplicationError { code message }
    }
  }`;

export const TRACKER_SOURCES = gql`
  query TrackerSources($provider: String!) {
    trackerSources(provider: $provider) {
      ... on TrackerSourceList { items { id name } }
      ... on ApplicationError { code message }
    }
  }`;

export const TRACKER_ISSUES = gql`
  query TrackerIssues($provider: String!, $gameId: UUID!, $sourceId: String!, $query: String) {
    trackerIssues(provider: $provider, gameId: $gameId, sourceId: $sourceId, query: $query) {
      ... on TrackerIssueList { max issues { key title url estimate alreadyInGame } }
      ... on ApplicationError { code message }
    }
  }`;

export const COMPLETE_TRACKER_CONNECTION = gql`
  mutation CompleteTrackerConnection($input: CompleteTrackerConnectionInput!) {
    completeTrackerConnection(input: $input) { trackerConnected { returnPath } ${ERRORS} }
  }`;

export const DISCONNECT_TRACKER = gql`
  mutation DisconnectTracker($input: DisconnectTrackerInput!) {
    disconnectTracker(input: $input) { boolean ${ERRORS} }
  }`;

export const IMPORT_TRACKER_ISSUES = gql`
  mutation ImportTrackerIssues($input: ImportTrackerIssuesInput!) {
    importTrackerIssues(input: $input) { trackerImportSummary { imported skipped } ${ERRORS} }
  }`;

export const SAVE_ESTIMATE_TO_TRACKER = gql`
  mutation SaveEstimateToTracker($input: SaveEstimateToTrackerInput!) {
    saveEstimateToTracker(input: $input) { trackerEstimateSaved { issueKey value url } ${ERRORS} }
  }`;

export interface TrackerInfo {
  provider: string;
  name: string;
  enabled: boolean;
  connected: boolean;
  needsReconnect: boolean;
  accountName: string | null;
  queryHint: string;
}

export interface TrackerIssue {
  key: string;
  title: string;
  url: string;
  estimate: number | null;
  alreadyInGame: boolean;
}

/** Which tracker a ticket's link points to; Jira for atlassian.net. */
export function trackerOfUrl(url: string | null | undefined): string | null {
  if (!url) return null;
  try {
    const host = new URL(url).hostname;
    if (host.endsWith('.atlassian.net')) return 'jira';
    if (host === 'github.com') return 'github';
    if (host === 'linear.app') return 'linear';
    if (host === 'dev.azure.com') return 'azure-devops';
  } catch { /* not a link */ }
  return null;
}
