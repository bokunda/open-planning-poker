import { gql } from 'apollo-angular';

/*
 * Room management (players, tickets, options) added in 2.2. The generated types in
 * graphql-gateway.service.ts predate these fields, so their shapes are in room-types.ts.
 */

const ERRORS = `errors { ... on ApplicationError { code message } }`;

export const ON_GAME_CHANGED = gql`
  subscription OnGameChanged($gameId: UUID!) {
    onGameChanged(gameId: $gameId) { gameId kind ticketId playerId }
  }`;

export const SET_PLAYER_ROLE = gql`
  mutation SetPlayerRole($input: SetPlayerRoleInput!) {
    setPlayerRole(input: $input) { boolean ${ERRORS} }
  }`;

export const SET_CO_HOST = gql`
  mutation SetCoHost($input: SetCoHostInput!) {
    setCoHost(input: $input) { boolean ${ERRORS} }
  }`;

export const REMOVE_PLAYER = gql`
  mutation RemovePlayer($input: RemovePlayerInput!) {
    removePlayer(input: $input) { boolean ${ERRORS} }
  }`;

export const TRANSFER_HOST = gql`
  mutation TransferHost($input: TransferHostInput!) {
    transferHost(input: $input) { boolean ${ERRORS} }
  }`;

export const UPDATE_TICKET = gql`
  mutation UpdateTicket($input: UpdateTicketInput!) {
    updateTicket(input: $input) {
      ticket { id gameId name description votesRevealed finalEstimate position }
      ${ERRORS}
    }
  }`;

export const DELETE_TICKET = gql`
  mutation DeleteTicket($input: DeleteTicketInput!) {
    deleteTicket(input: $input) { boolean ${ERRORS} }
  }`;

export const REORDER_TICKETS = gql`
  mutation ReorderTickets($input: ReorderTicketsInput!) {
    reorderTickets(input: $input) { boolean ${ERRORS} }
  }`;

export const SET_FINAL_ESTIMATE = gql`
  mutation SetFinalEstimate($input: SetFinalEstimateInput!) {
    setFinalEstimate(input: $input) {
      ticket { id gameId name description votesRevealed finalEstimate position }
      ${ERRORS}
    }
  }`;

export const UPDATE_GAME_OPTIONS = gql`
  mutation UpdateGameOptions($input: UpdateGameOptionsInput!) {
    updateGameOptions(input: $input) {
      settings { id gameId deckSetup autoReveal asyncVotingEndsAt }
      ${ERRORS}
    }
  }`;
