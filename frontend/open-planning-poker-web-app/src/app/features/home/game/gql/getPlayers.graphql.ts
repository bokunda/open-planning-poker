import { gql } from 'apollo-angular'

const GET_GAME_PLAYERS = gql `
  query GetGamePlayers($gameId: UUID!) {
    gamePlayers(gameId: $gameId) {
      ... on ApiCollectionOfGamePlayer {
        items {
          id
          name
        }
        totalCount
      }
      ... on ApplicationError {
        code
        message
        __typename
      }
    }
  }`;

export { GET_GAME_PLAYERS };
