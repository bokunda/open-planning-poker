import { gql } from 'apollo-angular';

export const GENERATE_GAME_REPORT = gql`
mutation GenerateGameReport($input: GenerateGameReportInput!) {
  generateGameReport(input: $input) {
    gameReport {
      gameId
      fileName
      data
    }
    errors {
      ... on ApplicationError {
        code
        message
      }
    }
  }
}`;
