import { gql } from 'apollo-angular'

const CREATE_OR_UPDATE_VOTE = gql `
mutation CreateOrUpdateVote ($input: CreateOrUpdateVoteInput!){
  createOrUpdateVote(input:  $input) {
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

export { CREATE_OR_UPDATE_VOTE };
