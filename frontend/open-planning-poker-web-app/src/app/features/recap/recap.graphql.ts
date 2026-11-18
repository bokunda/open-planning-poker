import { gql } from 'apollo-angular';

/* Shareable session recaps (OPP-170). */

export type RecapSpread = 'CONSENSUS' | 'CLOSE' | 'WIDE' | 'NO_VOTES' | 'NOT_REVEALED';

export interface RecapTicket {
  name: string;
  externalKey: string | null;
  estimate: string | null;
  spread: RecapSpread;
  voteRounds: number;
  discussionMinutes: number | null;
}

export interface GameRecap {
  gameName: string;
  playedOn: string;
  tickets: RecapTicket[];
  ticketsEstimated: number;
  ticketsOpen: number;
  totalPoints: number;
  consensusRate: number | null;
  durationMinutes: number | null;
  playerCount: number;
  players: string[];
}

export interface RecapSharing {
  shared: boolean;
  token: string | null;
  showNames: boolean;
}

const SHARING = 'shared token showNames';

export const GAME_RECAP = gql`
  query GameRecap($token: String!) {
    gameRecap(token: $token) {
      ... on GameRecap {
        gameName playedOn ticketsEstimated ticketsOpen totalPoints consensusRate durationMinutes playerCount players
        tickets { name externalKey estimate spread voteRounds discussionMinutes }
      }
      ... on ApplicationError { code message }
    }
  }`;

export const RECAP_SHARING = gql`
  query RecapSharing($gameId: UUID!) {
    recapSharing(gameId: $gameId) {
      ... on RecapSharing { ${SHARING} }
      ... on ApplicationError { code message }
    }
  }`;

export const SET_RECAP_SHARING = gql`
  mutation SetRecapSharing($input: SetRecapSharingInput!) {
    setRecapSharing(input: $input) {
      recapSharing { ${SHARING} }
      errors { ... on ApplicationError { code message } }
    }
  }`;

export const RECAP_ORIGIN = 'https://app.openplanningpoker.com';

/** Public link of a shared recap. Uses the current origin in the browser so self-hosted installs link to themselves. */
export function recapUrl(token: string): string {
  const origin = typeof window !== 'undefined' ? window.location.origin : RECAP_ORIGIN;
  return `${origin}/recap/${token}`;
}
