import { gql } from 'apollo-angular'

const CHANGE_USERNAME = gql `
  mutation ChangeUsername($username: String!) {
    changeUsername(input:  {
      username: $username
    }) {
      boolean
      errors {
        ... on ApplicationError {
          code
          message
          __typename
        }
      }
    }
  }`;

export { CHANGE_USERNAME };
