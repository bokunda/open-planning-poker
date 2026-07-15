import { gql } from 'apollo-angular';

const ON_VOTES_REVEALED = gql`
  subscription OnVotesRevealed($ticketId: UUID!) {
    onVotesRevealed(ticketId: $ticketId) {
      ticketId
      revealedBy
    }
  }`;

export { ON_VOTES_REVEALED };
