import { gql } from 'apollo-angular'

const GET_USER = gql `
query GetUser {
  currentUser {
      ... on User {
          id
          userName
          __typename
      }
      ... on ApplicationError {
          code
          message
          __typename
      }
  }
}
`;

export { GET_USER };
