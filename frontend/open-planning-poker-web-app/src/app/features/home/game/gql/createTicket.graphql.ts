import { gql } from 'apollo-angular'

const CREATE_TICKET  = gql `
mutation($input: CreateTicketInput!) {
  createTicket(input: $input) {
    ticket {
      id
      gameId
      name
      description
      votesRevealed
      finalEstimate
      position
      __typename
    }
  }
}
`;

export { CREATE_TICKET };
