import { gql } from 'apollo-angular'

const UPDATE_VOTE = gql `
mutation UpdateVote ($input: UpdateVoteInput!){
  updateVote(input:  $input) {
    vote {
      id
      playerId
      value
      __typename
    }
    errors {
      ... on ApplicationError {
        code
        message
        __typename
      }
    }
  }
}`;

export { UPDATE_VOTE };
