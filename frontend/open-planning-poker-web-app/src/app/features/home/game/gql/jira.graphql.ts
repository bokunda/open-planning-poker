import { gql } from 'apollo-angular';

/** Jira Cloud integration (OPP-10, OPP-11, OPP-12). Query results are unions with ApplicationError. */
const ERRORS = `errors { ... on ApplicationError { code message } }`;
const ERROR = `... on ApplicationError { code message }`;

export const GET_JIRA_CONNECTION = gql`
  query JiraConnection {
    jiraConnection { enabled connected needsReconnect siteName siteUrl }
  }`;

export const GET_JIRA_AUTHORIZE_URL = gql`
  query JiraAuthorizeUrl($returnPath: String!) {
    jiraAuthorizeUrl(returnPath: $returnPath) { ... on JiraAuthorizeLink { url } ${ERROR} }
  }`;

export const GET_JIRA_SITES = gql`
  query JiraSites {
    jiraSites { ... on JiraSiteList { items { cloudId name url } } ${ERROR} }
  }`;

export const GET_JIRA_BOARDS = gql`
  query JiraBoards {
    jiraBoards { ... on JiraBoardList { items { id name type } } ${ERROR} }
  }`;

export const GET_JIRA_SPRINTS = gql`
  query JiraSprints($boardId: Long!) {
    jiraSprints(boardId: $boardId) { ... on JiraSprintList { items { id name state } } ${ERROR} }
  }`;

export const GET_JIRA_ISSUES = gql`
  query JiraIssues($gameId: UUID!, $boardId: Long, $sprintId: Long, $jql: String) {
    jiraIssues(gameId: $gameId, boardId: $boardId, sprintId: $sprintId, jql: $jql) {
      ... on JiraIssueSearch { truncated max issues { key summary description storyPoints url alreadyInGame } }
      ${ERROR}
    }
  }`;

export const COMPLETE_JIRA_CONNECTION = gql`
  mutation CompleteJiraConnection($input: CompleteJiraConnectionInput!) {
    completeJiraConnection(input: $input) {
      jiraConnectionResult { returnPath siteSelected sites { cloudId name url } }
      ${ERRORS}
    }
  }`;

export const SELECT_JIRA_SITE = gql`
  mutation SelectJiraSite($input: SelectJiraSiteInput!) {
    selectJiraSite(input: $input) { jiraConnectionStatus { connected siteName } ${ERRORS} }
  }`;

export const DISCONNECT_JIRA = gql`
  mutation DisconnectJira {
    disconnectJira { boolean ${ERRORS} }
  }`;

export const IMPORT_JIRA_ISSUES = gql`
  mutation ImportJiraIssues($input: ImportJiraIssuesInput!) {
    importJiraIssues(input: $input) { jiraImportSummary { imported skipped } ${ERRORS} }
  }`;

export const SAVE_ESTIMATE_TO_JIRA = gql`
  mutation SaveEstimateToJira($input: SaveEstimateToJiraInput!) {
    saveEstimateToJira(input: $input) { jiraEstimateSaved { issueKey value url } ${ERRORS} }
  }`;

export interface JiraConnectionStatus {
  enabled: boolean;
  connected: boolean;
  needsReconnect: boolean;
  siteName?: string | null;
  siteUrl?: string | null;
}
export interface JiraSite { cloudId: string; name: string; url: string; }
export interface JiraBoard { id: number; name: string; type: string; }
export interface JiraSprint { id: number; name: string; state: string; }
export interface JiraIssue {
  key: string;
  summary: string;
  description: string;
  storyPoints?: number | null;
  url: string;
  alreadyInGame: boolean;
}
export interface AppError { code: string; message: string; }
