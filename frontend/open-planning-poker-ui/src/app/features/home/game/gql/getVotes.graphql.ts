import { gql } from 'apollo-angular'

const GET_VOTES = gql `
query Votes ($ticketId: UUID!) {
  votes(ticketId: $ticketId) {
    ... on ApiCollectionOfVote {
      items {
        id
        playerId
        value
        __typename
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

export { GET_VOTES };
