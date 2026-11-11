import { gql } from 'apollo-angular';

const ON_VOTES_REVEALED = gql`
  subscription OnVotesRevealed($ticketId: UUID!) {
    onVotesRevealed(ticketId: $ticketId) {
      ticketId
      revealedBy
      votes {
        id
        playerId
        value
        playerName
      }
    }
  }`;

export { ON_VOTES_REVEALED };
