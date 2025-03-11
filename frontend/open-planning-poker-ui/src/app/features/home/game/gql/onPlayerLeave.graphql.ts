import { gql } from 'apollo-angular'

const ON_PLAYER_LEAVE  = gql `
  subscription OnPlayerLeave {
    onPlayerLeave {
      id
      userName
    }
  }`;

export { ON_PLAYER_LEAVE };
