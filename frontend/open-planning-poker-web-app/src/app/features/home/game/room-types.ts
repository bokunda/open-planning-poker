import { GamePlayer, Ticket } from '../../../graphql/graphql-gateway.service';

/*
 * Fields added to the game engine in 2.2 (roles, co-hosts, final estimates, ticket order,
 * room options). The generated types do not have them yet.
 */

export type PlayerRole = 'PLAYER' | 'OBSERVER';

export type GameChangeKind = 'GAME' | 'PLAYERS' | 'TICKETS' | 'PLAYER_REMOVED';

export interface GameChanged {
  gameId: string;
  kind: GameChangeKind;
  ticketId: string | null;
  playerId: string | null;
}

export type RoomPlayer = GamePlayer & {
  role?: PlayerRole;
  isCoHost?: boolean;
};

export type RoomTicket = Ticket & {
  finalEstimate?: string | null;
  position?: number;
};

export interface RoomSettings {
  id: string;
  gameId: string;
  deckSetup: string;
  autoReveal?: boolean;
  asyncVotingEndsAt?: string | null;
}

/** A player action from the players panel. */
export type PlayerAction =
  | { kind: 'role'; playerId: string; role: PlayerRole }
  | { kind: 'coHost'; playerId: string; isCoHost: boolean }
  | { kind: 'transferHost'; playerId: string }
  | { kind: 'remove'; playerId: string };

/** True while the async voting window is open (players vote on their own before the meeting). */
export function isAsyncVotingOpen(settings: RoomSettings | undefined | null, now = Date.now()): boolean {
  const endsAt = settings?.asyncVotingEndsAt;
  return !!endsAt && new Date(endsAt).getTime() > now;
}
