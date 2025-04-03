import { gql } from 'apollo-angular'

const GET_TICKETS = gql `
query Tickets($gameId: UUID!) {
  tickets(gameId: $gameId ) {
    ... on ApiCollectionOfTicket {
      items {
        id
        gameId
        name
        description
        votes {
            items {
                id
                playerId
                value
                playerName
                __typename
            }
            totalCount
        }
        averageVotingValue
         __typename
      }
      totalCount
      __typename
    }
    ... on ApplicationError {
      code
      message
      __typename
    }
    __typename
  }
}
`;

export { GET_TICKETS };
