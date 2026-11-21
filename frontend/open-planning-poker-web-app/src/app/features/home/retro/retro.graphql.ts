import { gql } from 'apollo-angular';

/* Sprint retrospectives in the team room (OPP-162). */

const ERRORS = `errors { ... on ApplicationError { code message } }`;
const ITEM = `id retroId cardId text ownerId done createdOn`;
const CARD = `id column text mine authorId groupId groupTitle myVotes votes`;
export const RETRO_FIELDS = `id teamId gameId template columns phase showAuthors dotsPerPerson dotsLeft canFacilitate countPerColumn
  cards { ${CARD} } actionItems { ${ITEM} } carriedOver { ${ITEM} }`;

export const GET_RETRO = gql`
  query Retro($gameId: UUID!) {
    retro(gameId: $gameId) {
      ... on RetroInfo { ${RETRO_FIELDS} }
      ... on ApplicationError { code message }
    }
  }`;

export const RETRO_FACTS = gql`
  query RetroFacts($retroId: UUID!) {
    retroFacts(retroId: $retroId) {
      ... on SprintFactsInfo {
        hasData estimatedPoints estimatedTickets ticketCount plannedPoints
        velocity { gameId name points current }
        outliers { ticketId name externalKey estimate days cardMedianDays }
        lowConfidence { ticketId name externalKey estimate confidence }
      }
      ... on ApplicationError { code message }
    }
  }`;

export const RETRO_EXPORT = gql`
  query RetroExport($retroId: UUID!, $pdf: Boolean!) {
    retroExport(retroId: $retroId, pdf: $pdf) {
      ... on RetroExportFile { fileName contentType data }
      ... on ApplicationError { code message }
    }
  }`;

export const START_RETRO = gql`
  mutation StartRetro($input: StartRetroInput!) { startRetro(input: $input) { retroInfo { ${RETRO_FIELDS} } ${ERRORS} } }`;
export const UPDATE_RETRO_SETTINGS = gql`
  mutation UpdateRetroSettings($input: UpdateRetroSettingsInput!) { updateRetroSettings(input: $input) { retroInfo { id } ${ERRORS} } }`;
export const SET_RETRO_PHASE = gql`
  mutation SetRetroPhase($input: SetRetroPhaseInput!) { setRetroPhase(input: $input) { retroInfo { id } ${ERRORS} } }`;
export const ADD_RETRO_CARD = gql`
  mutation AddRetroCard($input: AddRetroCardInput!) { addRetroCard(input: $input) { retroCardInfo { id } ${ERRORS} } }`;
export const EDIT_RETRO_CARD = gql`
  mutation EditRetroCard($input: EditRetroCardInput!) { editRetroCard(input: $input) { boolean ${ERRORS} } }`;
export const DELETE_RETRO_CARD = gql`
  mutation DeleteRetroCard($input: DeleteRetroCardInput!) { deleteRetroCard(input: $input) { boolean ${ERRORS} } }`;
export const GROUP_RETRO_CARD = gql`
  mutation GroupRetroCard($input: GroupRetroCardInput!) { groupRetroCard(input: $input) { boolean ${ERRORS} } }`;
export const SET_RETRO_GROUP_TITLE = gql`
  mutation SetRetroGroupTitle($input: SetRetroGroupTitleInput!) { setRetroGroupTitle(input: $input) { boolean ${ERRORS} } }`;
export const VOTE_RETRO_CARD = gql`
  mutation VoteRetroCard($input: VoteRetroCardInput!) { voteRetroCard(input: $input) { int ${ERRORS} } }`;
export const ADD_RETRO_ACTION_ITEM = gql`
  mutation AddRetroActionItem($input: AddRetroActionItemInput!) { addRetroActionItem(input: $input) { retroActionItemInfo { ${ITEM} } ${ERRORS} } }`;
export const UPDATE_RETRO_ACTION_ITEM = gql`
  mutation UpdateRetroActionItem($input: UpdateRetroActionItemInput!) { updateRetroActionItem(input: $input) { retroActionItemInfo { ${ITEM} } ${ERRORS} } }`;
export const SET_RETRO_ACTION_ITEM_DONE = gql`
  mutation SetRetroActionItemDone($input: SetRetroActionItemDoneInput!) { setRetroActionItemDone(input: $input) { retroActionItemInfo { ${ITEM} } ${ERRORS} } }`;
export const DELETE_RETRO_ACTION_ITEM = gql`
  mutation DeleteRetroActionItem($input: DeleteRetroActionItemInput!) { deleteRetroActionItem(input: $input) { boolean ${ERRORS} } }`;
export const POST_RETRO_SUMMARY = gql`
  mutation PostRetroSummary($input: PostRetroSummaryInput!) { postRetroSummary(input: $input) { boolean ${ERRORS} } }`;

export type RetroPhase = 'WRITING' | 'GROUPING' | 'VOTING' | 'DISCUSSING';
export const RETRO_PHASES: RetroPhase[] = ['WRITING', 'GROUPING', 'VOTING', 'DISCUSSING'];
export const RETRO_TEMPLATES = ['went-well', 'start-stop-continue', 'mad-sad-glad', '4ls'] as const;
export type RetroTemplate = typeof RETRO_TEMPLATES[number];

export interface RetroCard {
  id: string;
  column: number;
  text: string;
  mine: boolean;
  authorId: string | null;
  groupId: string | null;
  groupTitle: string | null;
  myVotes: number;
  votes: number | null;
}

export interface RetroActionItem {
  id: string;
  retroId: string;
  cardId: string | null;
  text: string;
  ownerId: string | null;
  done: boolean;
  createdOn: string;
}

export interface Retro {
  id: string;
  teamId: string;
  gameId: string;
  template: RetroTemplate;
  columns: number;
  phase: RetroPhase;
  showAuthors: boolean;
  dotsPerPerson: number;
  dotsLeft: number;
  canFacilitate: boolean;
  countPerColumn: number[];
  cards: RetroCard[];
  actionItems: RetroActionItem[];
  carriedOver: RetroActionItem[];
}

export interface SprintFactTicket {
  ticketId: string;
  name: string;
  externalKey: string | null;
  estimate: string | null;
  days?: number | null;
  cardMedianDays?: number | null;
  confidence?: number | null;
}

export interface SprintFacts {
  hasData: boolean;
  estimatedPoints: number;
  estimatedTickets: number;
  ticketCount: number;
  plannedPoints: number | null;
  velocity: { gameId: string; name: string; points: number; current: boolean }[];
  outliers: SprintFactTicket[];
  lowConfidence: SprintFactTicket[];
}

/** A group head with the cards merged into it, or a card on its own. */
export interface RetroStack {
  head: RetroCard;
  members: RetroCard[];
}

/** Cards of one column as stacks: heads in board order (by votes once the results are shown), members after. */
export function stacksOf(cards: RetroCard[], column: number): RetroStack[] {
  const byId = new Map(cards.map(c => [c.id, c]));
  const heads = cards.filter(c => c.column === column && (!c.groupId || !byId.has(c.groupId)));
  return heads.map(head => ({ head, members: cards.filter(c => c.groupId === head.id) }));
}

/** Total dots of a stack once results are shown (dots sit on the head). */
export function stackVotes(stack: RetroStack): number {
  return stack.head.votes ?? 0;
}

export function nextPhase(phase: RetroPhase): RetroPhase | null {
  const i = RETRO_PHASES.indexOf(phase);
  return i >= 0 && i < RETRO_PHASES.length - 1 ? RETRO_PHASES[i + 1] : null;
}

/** The column a sprint fact lands in: the "to improve" kind of column of each template. */
export function factColumn(template: RetroTemplate): number {
  switch (template) {
    case 'went-well': return 1;
    case 'start-stop-continue': return 0;
    case 'mad-sad-glad': return 1;
    case '4ls': return 2;
  }
}

/** Bytes from the API ([Byte!]) as a file download. */
export function downloadBytes(data: number[] | string, fileName: string, contentType: string): void {
  const bytes = typeof data === 'string' ? Uint8Array.from(atob(data), c => c.charCodeAt(0)) : new Uint8Array(data);
  const blob = new Blob([bytes], { type: contentType });
  const a = document.createElement('a');
  a.href = URL.createObjectURL(blob);
  a.download = fileName;
  a.click();
  setTimeout(() => URL.revokeObjectURL(a.href), 1000);
}
