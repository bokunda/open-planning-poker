import { gql } from 'apollo-angular'

const CREATE_GAME = gql `
  mutation CreateGame($input: CreateGameInput!) {
    createGame(input: $input) {
      game {
        id
        name
        description
        settingsDetails {
          ... on Settings {
            id
          }
        }
      }
      errors {
        ... on ApplicationError {
          code
          message
        }
      }
    }
  }`;

export { CREATE_GAME };
