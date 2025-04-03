import { gql } from 'apollo-angular'

const CREATE_GAME = gql `
  mutation CreateGame($input: CreateGameInput!) {
    createGame(input: $input) {
      game {
        id
        name
        description
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
