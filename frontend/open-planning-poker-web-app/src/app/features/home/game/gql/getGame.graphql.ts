import { gql } from 'apollo-angular'

const GET_GAME = gql `
  query GetGame($id: UUID!) {
    game(id: $id) {
      ... on Game {
        id
        name
        description
        hostId
        teamId
        settingsDetails {
          ... on Settings {
            id
            gameId
            deckSetup
            autoReveal
            asyncVotingEndsAt
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
