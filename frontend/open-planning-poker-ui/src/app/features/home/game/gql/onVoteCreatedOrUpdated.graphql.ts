import { gql } from 'apollo-angular'

const ON_VOTE_CREATED_OR_UPDATED = gql`
subscription OnVoteCreatedOrUpdated($ticketId: UUID!) {
  onVoteCreatedOrUpdated(ticketId: $ticketId) {
    id
    playerId
    value
    __typename
  }
}
`;

export { ON_VOTE_CREATED_OR_UPDATED };
