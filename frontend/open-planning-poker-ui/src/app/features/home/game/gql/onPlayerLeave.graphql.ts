import { gql } from 'apollo-angular'

const ON_PLAYER_LEAVE  = gql `
  subscription OnPlayerLeave($gameId: UUID!) {
    onPlayerLeave(gameId: $gameId) {
      id
      userName
    }
  }
`;

export { ON_PLAYER_LEAVE };
