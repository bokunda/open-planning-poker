import { gql } from 'apollo-angular';

/* Sorting board for long backlogs (OPP-137). */

const ERRORS = `errors { ... on ApplicationError { code message } }`;
const BOARD = `open columns tickets { ticketId column moves contested }`;

export const SORTING_BOARD = gql`
  query SortingBoard($gameId: UUID!) {
    sortingBoard(gameId: $gameId) {
      ... on SortingBoardInfo { ${BOARD} }
      ... on ApplicationError { code message }
    }
  }`;

export const SET_SORTING_OPEN = gql`
  mutation SetSortingOpen($input: SetSortingOpenInput!) {
    setSortingOpen(input: $input) { sortingBoardInfo { ${BOARD} } ${ERRORS} }
  }`;

export const MOVE_SORTED_TICKET = gql`
  mutation MoveSortedTicket($input: MoveSortedTicketInput!) {
    moveSortedTicket(input: $input) { sortedTicket { ticketId column moves contested } ${ERRORS} }
  }`;

export const CLEAR_SORT_CONTESTED = gql`
  mutation ClearSortContested($input: ClearSortContestedInput!) {
    clearSortContested(input: $input) { sortedTicket { ticketId column moves contested } ${ERRORS} }
  }`;

export const APPLY_SORTED_ESTIMATES = gql`
  mutation ApplySortedEstimates($input: ApplySortedEstimatesInput!) {
    applySortedEstimates(input: $input) { sortedEstimatesApplied { applied contested unsorted } ${ERRORS} }
  }`;

export interface SortedTicket {
  ticketId: string;
  /** Card column; null while unsorted. */
  column: string | null;
  moves: number;
  contested: boolean;
}

export interface SortingBoard {
  open: boolean;
  columns: string[];
  tickets: SortedTicket[];
}

export interface SortedEstimatesApplied {
  applied: number;
  contested: number;
  unsorted: number;
}
