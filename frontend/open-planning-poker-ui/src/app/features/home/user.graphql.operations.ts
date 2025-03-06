import { gql } from 'apollo-angular'

const REGISTER_USER = gql `
mutation {
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

//const GET_USER = gql ``;

export {
  //GET_USER,
  REGISTER_USER
};
