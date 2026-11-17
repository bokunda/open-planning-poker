import { gql } from 'apollo-angular';

/* Session summary to Slack or Microsoft Teams (OPP-136). */

export const SESSION_SUMMARY = gql`
  query SessionSummary($gameId: UUID!) {
    sessionSummary(gameId: $gameId) {
      ... on SessionSummaryInfo { text destination }
      ... on ApplicationError { code message }
    }
  }`;

export const POST_SESSION_SUMMARY = gql`
  mutation PostSessionSummary($input: PostSessionSummaryInput!) {
    postSessionSummary(input: $input) { sessionSummaryInfo { destination } errors { ... on ApplicationError { code message } } }
  }`;
