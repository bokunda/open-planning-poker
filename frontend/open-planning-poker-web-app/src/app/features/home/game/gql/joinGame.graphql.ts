import { gql } from 'apollo-angular'

const JOIN_GAME = gql `
  mutation JoinGame($input: JoinGameInput!) {
    joinGame(input: $input) {
      boolean
      errors {
        ... on ApplicationError {
          code
          message
        }
      }
    }
  }`;

export { JOIN_GAME };
