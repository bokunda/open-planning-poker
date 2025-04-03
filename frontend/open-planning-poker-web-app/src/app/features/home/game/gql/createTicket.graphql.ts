import { gql } from 'apollo-angular'

const CREATE_TICKET  = gql `
mutation($input: CreateTicketInput!) {
  createTicket(input: $input) {
    ticket {
      id
      gameId
      name
      description
      __typename
    }
  }
}
`;

export { CREATE_TICKET };
