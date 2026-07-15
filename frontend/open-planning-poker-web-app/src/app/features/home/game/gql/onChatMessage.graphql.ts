import { gql } from 'apollo-angular';

const ON_CHAT_MESSAGE = gql`
  subscription OnChatMessage($gameId: UUID!) {
    onChatMessage(gameId: $gameId) {
      id
      gameId
      playerName
      content
      timestamp
    }
  }`;

export { ON_CHAT_MESSAGE };
