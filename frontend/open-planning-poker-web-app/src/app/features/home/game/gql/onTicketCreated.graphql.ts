import { gql } from 'apollo-angular'

const ON_TICKET_CREATED = gql`
subscription($gameId: UUID!) {
  onTicketCreated(gameId: $gameId) {
    id
    gameId
    name
    description
    __typename
  }
}
`;

export { ON_TICKET_CREATED };
