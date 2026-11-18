import { GamePlayer, Ticket } from '../../../graphql/graphql-gateway.service';

/*
 * Fields added to the game engine in 2.2 (roles, co-hosts, final estimates, ticket order,
 * room options). The generated types do not have them yet.
 */

export type PlayerRole = 'PLAYER' | 'OBSERVER';

export type GameChangeKind = 'GAME' | 'PLAYERS' | 'TICKETS' | 'PLAYER_REMOVED' | 'SORTING';

export interface GameChanged {
  gameId: string;
  kind: GameChangeKind;
  ticketId: string | null;
  playerId: string | null;
}

export type RoomPlayer = GamePlayer & {
  role?: PlayerRole;
  isCoHost?: boolean;
  /** Emoji avatar picked in this room (OPP-173); null shows initials. */
  avatar?: string | null;
};

/** Avatars a player can pick; the game engine accepts only these. */
export const AVATARS = ['🦊', '🐼', '🐸', '🦉', '🐙', '🦄', '🐝', '🐢', '🦁', '🐧', '🐳', '🦖', '🚀', '🌵', '🍕', '⚡', '🎸', '🎯'] as const;

/** Room themes (OPP-173): table colour and card back, the same for everyone. */
export const THEMES = ['classic', 'forest', 'ocean', 'sunset', 'midnight', 'paper'] as const;
export type RoomTheme = typeof THEMES[number];

/** Discussion options of a room (OPP-114). */
export interface FacilitationSettings {
  outlierNotes: boolean;
  /** 0 = off, else 2, 3 or 5. */
  discussionMinutes: number;
  confidenceCheck: boolean;
  reactions: boolean;
  /** Confetti when everyone agrees (OPP-172). */
  celebrations: boolean;
  theme: string;
  /** Card backs follow the season. */
  seasonalCards: boolean;
}

export const DEFAULT_FACILITATION: FacilitationSettings = {
  outlierNotes: true, discussionMinutes: 0, confidenceCheck: false, reactions: true,
  celebrations: true, theme: 'classic', seasonalCards: true,
};

/** Discussion data of a ticket (OPP-114). */
export interface TicketFacilitation {
  ticketId: string;
  revealedAt: string | null;
  estimatedAt: string | null;
  voteRounds: number;
  needsInfo: boolean;
  notes: { playerId: string; note: string }[];
  confidenceAverage: number | null;
  confidenceCount: number;
  myConfidence: number | null;
}

/** Below this average confidence a ticket is flagged as risky. */
export const LOW_CONFIDENCE = 3;

export interface Reaction {
  gameId: string;
  playerId: string;
  emoji: string;
}

export const REACTIONS = ['👍', '🤔', '☕', '🎉', '😂', '👀'] as const;

export type RoomTicket = Ticket & {
  finalEstimate?: string | null;
  position?: number;
  /** Jira issue the ticket was imported from (OPP-11). */
  externalKey?: string | null;
  externalUrl?: string | null;
  facilitation?: Pick<TicketFacilitation, 'needsInfo' | 'confidenceAverage' | 'confidenceCount' | 'voteRounds'> | null;
};

export interface RoomSettings {
  id: string;
  gameId: string;
  deckSetup: string;
  autoReveal?: boolean;
  asyncVotingEndsAt?: string | null;
  facilitation?: FacilitationSettings;
}

/** A player action from the players panel. */
export type PlayerAction =
  | { kind: 'role'; playerId: string; role: PlayerRole }
  | { kind: 'coHost'; playerId: string; isCoHost: boolean }
  | { kind: 'transferHost'; playerId: string }
  | { kind: 'remove'; playerId: string }
  | { kind: 'avatar'; playerId: string; avatar: string | null };

/** True while the async voting window is open (players vote on their own before the meeting). */
export function isAsyncVotingOpen(settings: RoomSettings | undefined | null, now = Date.now()): boolean {
  const endsAt = settings?.asyncVotingEndsAt;
  return !!endsAt && new Date(endsAt).getTime() > now;
}

/** Winter holidays card backs: 1 December – 6 January. */
export function isWinterSeason(date: Date): boolean {
  const month = date.getMonth();
  return month === 11 || (month === 0 && date.getDate() <= 6);
}
