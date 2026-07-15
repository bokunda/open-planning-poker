import { gql } from 'apollo-angular';

const REVEAL_VOTES = gql`
  mutation RevealVotes($input: RevealVotesInput!) {
    revealVotes(input: $input) {
      votesRevealed {
        ticketId
        revealedBy
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
