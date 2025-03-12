import { gql } from 'apollo-angular'

const GET_GAME = gql `
  query GetGame($id: UUID!) {
    game(id: $id) {
      ... on Game {
        id
        name
        description
        settingsDetails {
          ... on Settings {
            id
            gameId
            deckSetup
          }
          ... on ApplicationError {
            code
            message
          }
        }
      }
      ... on ApplicationError {
        code
        message
      }
    }
  }`;

export { GET_GAME };
