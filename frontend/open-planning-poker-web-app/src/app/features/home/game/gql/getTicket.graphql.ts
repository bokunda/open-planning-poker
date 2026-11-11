import { gql } from 'apollo-angular'

const GET_TICKET = gql `
  query GetTicket($id: UUID!) {
    ticket(id: $id) {
      ... on Ticket {
        id
        name
        description
        gameId
        votesRevealed
      }
      ... on ApplicationError {
        code
        message
      }
    }
  }`;

export { GET_TICKET };
