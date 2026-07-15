import { gql } from 'apollo-angular';

const SEND_CHAT_MESSAGE = gql`
  mutation SendChatMessage($input: SendChatMessageInput!) {
    sendChatMessage(input: $input) {
      chatMessage {
        id
        gameId
        playerName
        content
        timestamp
      }
      errors {
        ... on ApplicationError {
          code
          message
        }
      }
    }
  }`;

export { SEND_CHAT_MESSAGE };
