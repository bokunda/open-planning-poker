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

const GET_USER = gql `
  query GetUser {
    currentUser {
      id
      userName
    }
  }`;

export { GET_USER, REGISTER_USER };
