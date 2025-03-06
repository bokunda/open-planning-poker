import { gql } from 'apollo-angular'

const GET_USER = gql `
  query GetUser {
    currentUser {
      id
      userName
    }
  }`;

export { GET_USER };
