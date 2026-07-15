import { gql } from 'apollo-angular';

const RE_VOTE_TICKET = gql`
mutation($input: ReVoteTicketInput!) {
  reVoteTicket(input: $input) {
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

export { RE_VOTE_TICKET };
