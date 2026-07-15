import { gql } from 'apollo-angular';

const GET_CHAT_MESSAGES = gql`
  query GetChatMessages($gameId: UUID!) {
    chatMessages(gameId: $gameId) {
      id
      gameId
      playerName
      content
      timestamp
    }
  }`;

export { GET_CHAT_MESSAGES };
