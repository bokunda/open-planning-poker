import { gql } from 'apollo-angular'

const ON_PLAYER_JOINED  = gql `
  subscription OnPlayerJoined {
    onPlayerJoined {
      id
      userName
    }
  }`;

export { ON_PLAYER_JOINED };
