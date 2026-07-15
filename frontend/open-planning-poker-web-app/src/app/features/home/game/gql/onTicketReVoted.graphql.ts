import { gql } from 'apollo-angular';

const ON_TICKET_RE_VOTED = gql`
subscription($gameId: UUID!) {
  onTicketReVoted(gameId: $gameId) {
    id
    gameId
    name
    description
    __typename
  }
}
`;

export { ON_TICKET_RE_VOTED };
