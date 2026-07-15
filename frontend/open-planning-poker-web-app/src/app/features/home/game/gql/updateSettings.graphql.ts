import { gql } from 'apollo-angular';

const UPDATE_SETTINGS = gql`
  mutation UpdateSettings($input: UpdateSettingsInput!) {
    updateSettings(input: $input) {
      settings {
        id
        gameId
        deckSetup
      }
      errors {
        ... on ApplicationError {
          code
          message
        }
      }
    }
  }`;

export { UPDATE_SETTINGS };
