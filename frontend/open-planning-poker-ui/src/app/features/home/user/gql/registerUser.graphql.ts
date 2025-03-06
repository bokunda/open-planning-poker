import { gql } from 'apollo-angular'

const REGISTER_USER = gql `
  mutation RegisterUser {
    registerUser(input:  {
      username: ""
    }) {
      registerUserResponse {
        id
        userName
        token
        __typename
      }
      errors {
        __typename
      }
      __typename
    }
  }`;

export { REGISTER_USER };
