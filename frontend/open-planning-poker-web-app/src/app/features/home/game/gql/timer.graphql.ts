import { gql } from 'apollo-angular';

const GET_GAME_TIMER = gql`
  query GetGameTimer($gameId: UUID!) {
    gameTimer(gameId: $gameId) { gameId durationSeconds running endsAt serverTime }
  }`;

const ON_TIMER_CHANGED = gql`
  subscription OnTimerChanged($gameId: UUID!) {
    onTimerChanged(gameId: $gameId) { gameId durationSeconds running endsAt serverTime }
  }`;

const START_TIMER = gql`
  mutation StartTimer($gameId: UUID!, $durationSeconds: Int!) {
    startTimer(input: { gameId: $gameId, durationSeconds: $durationSeconds }) {
      gameTimer { gameId durationSeconds running endsAt serverTime }
      errors { ... on ApplicationError { code message } }
    }
  }`;

const STOP_TIMER = gql`
  mutation StopTimer($gameId: UUID!) {
    stopTimer(input: { gameId: $gameId }) {
      gameTimer { gameId durationSeconds running endsAt serverTime }
      errors { ... on ApplicationError { code message } }
    }
  }`;

/** Shape of the `GameTimer` GraphQL type (not in the generated types yet). */
export interface GameTimerState {
  gameId: string;
  durationSeconds: number;
  running: boolean;
  endsAt: string | null;
  serverTime: string;
}

export { GET_GAME_TIMER, ON_TIMER_CHANGED, START_TIMER, STOP_TIMER };
