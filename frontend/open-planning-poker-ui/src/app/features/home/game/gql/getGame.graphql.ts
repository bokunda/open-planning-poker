import { gql } from 'apollo-angular'

const GET_GAME = gql `
  query GetGame($id: UUID!) {
    game(id: $id) {
      ... on Game {
        id
        name
        description
      }
      ... on ApplicationError {
        message
      }
    }
  }`;

export { GET_GAME };
