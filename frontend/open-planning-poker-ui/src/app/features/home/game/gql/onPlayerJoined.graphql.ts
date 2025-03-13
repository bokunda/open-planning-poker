import { gql } from 'apollo-angular'

const ON_PLAYER_JOINED = gql`
  subscription OnPlayerJoined($gameId: UUID!) {
    onPlayerJoined(gameId: $gameId) {
      id
      userName
    }
  }
`;

export { ON_PLAYER_JOINED };
