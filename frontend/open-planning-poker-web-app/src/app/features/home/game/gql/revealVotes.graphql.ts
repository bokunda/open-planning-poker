import { gql } from 'apollo-angular';

const REVEAL_VOTES = gql`
  mutation RevealVotes($input: RevealVotesInput!) {
    revealVotes(input: $input) {
      votesRevealed {
        ticketId
        revealedBy
        votes {
          id
          playerId
          value
          playerName
        }
      }
      errors {
        ... on ApplicationError {
          code
          message
        }
      }
    }
  }`;

export { REVEAL_VOTES };
