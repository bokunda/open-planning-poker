import { gql } from 'apollo-angular';

/* Running the discussion after the reveal (OPP-114). Shapes are in room-types.ts. */

const ERRORS = `errors { ... on ApplicationError { code message } }`;
const INFO = `ticketId revealedAt estimatedAt voteRounds needsInfo notes { playerId note } confidenceAverage confidenceCount myConfidence`;
export const FACILITATION_SETTINGS = `outlierNotes discussionMinutes confidenceCheck reactions`;

export const TICKET_FACILITATION = gql`
  query TicketFacilitation($ticketId: UUID!) {
    ticketFacilitation(ticketId: $ticketId) {
      ... on TicketFacilitationInfo { ${INFO} }
      ... on ApplicationError { code message }
    }
  }`;

export const SET_VOTE_NOTE = gql`
  mutation SetVoteNote($input: SetVoteNoteInput!) {
    setVoteNote(input: $input) { ticketFacilitationInfo { ${INFO} } ${ERRORS} }
  }`;

export const SET_CONFIDENCE = gql`
  mutation SetConfidence($input: SetConfidenceInput!) {
    setConfidence(input: $input) { ticketFacilitationInfo { ${INFO} } ${ERRORS} }
  }`;

export const SET_TICKET_NEEDS_INFO = gql`
  mutation SetTicketNeedsInfo($input: SetTicketNeedsInfoInput!) {
    setTicketNeedsInfo(input: $input) { ticketFacilitationInfo { ${INFO} } ${ERRORS} }
  }`;

export const UPDATE_FACILITATION_OPTIONS = gql`
  mutation UpdateFacilitationOptions($input: UpdateFacilitationOptionsInput!) {
    updateFacilitationOptions(input: $input) { facilitationSettings { ${FACILITATION_SETTINGS} } ${ERRORS} }
  }`;

export const SEND_REACTION = gql`
  mutation SendReaction($input: SendReactionInput!) {
    sendReaction(input: $input) { boolean ${ERRORS} }
  }`;

export const ON_REACTION = gql`
  subscription OnReaction($gameId: UUID!) {
    onReaction(gameId: $gameId) { gameId playerId emoji }
  }`;
