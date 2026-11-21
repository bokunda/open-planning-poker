import { gql } from 'apollo-angular';

const ERRORS = `errors { ... on ApplicationError { code message } }`;
const TEAM_FIELDS = `id name description ownerId createdOn`;

export const MY_TEAMS = gql`
  query MyTeams {
    myTeams { ${TEAM_FIELDS} }
  }`;

export const GET_TEAM = gql`
  query GetTeam($id: UUID!) {
    team(id: $id) {
      ... on Team {
        ${TEAM_FIELDS}
        isMember
        averageVelocity
        members { id name }
        summaryWebhook
        retros { gameId phase }
        openActionItems { id retroId text ownerId done }
        sprints { gameId name description createdOn ticketCount estimatedCount points
          stats { revealed firstRoundConsensus firstRoundConsensusShare reVotesPerTicket averageDiscussionSeconds needsInfo } }
      }
      ... on ApplicationError { code message }
    }
  }`;

export const CREATE_TEAM = gql`
  mutation CreateTeam($input: CreateTeamInput!) {
    createTeam(input: $input) { team { ${TEAM_FIELDS} } ${ERRORS} }
  }`;

export const JOIN_TEAM = gql`
  mutation JoinTeam($input: JoinTeamInput!) {
    joinTeam(input: $input) { boolean ${ERRORS} }
  }`;

export const LEAVE_TEAM = gql`
  mutation LeaveTeam($input: LeaveTeamInput!) {
    leaveTeam(input: $input) { boolean ${ERRORS} }
  }`;

/** A sprint planning session is a game that belongs to the team. */
export const CREATE_SPRINT = gql`
  mutation CreateSprint($input: CreateGameInput!) {
    createGame(input: $input) {
      game { id name description hostId teamId }
      ${ERRORS}
    }
  }`;

export interface TeamSummary {
  id: string;
  name: string;
  description: string;
  ownerId: string;
  createdOn: string;
}

export interface Sprint {
  gameId: string;
  name: string;
  description: string;
  createdOn: string;
  ticketCount: number;
  estimatedCount: number;
  points: number;
}

export interface TeamDetails extends TeamSummary {
  isMember: boolean;
  averageVelocity: number | null;
  members: { id: string; name: string | null }[];
  sprints: Sprint[];
  /** Owner only (OPP-136). */
  summaryWebhook?: string | null;
  /** Sprints with a retro and its step (OPP-162). */
  retros?: { gameId: string; phase: string }[];
  /** Open action items of the team's retros (OPP-166). */
  openActionItems?: { id: string; retroId: string; text: string; ownerId: string | null; done: boolean }[];
}

/* Reference stories from past sprints (OPP-115). */
const TEAM_TICKET = `ticketId gameId sprintName sprintDate name externalKey externalUrl estimate spread`;

export const TEAM_ANCHORS = gql`
  query TeamAnchors($id: UUID!) {
    team(id: $id) { ... on Team { id anchors { id value note ticket { ${TEAM_TICKET} } } } }
  }`;

export const TEAM_HISTORY = gql`
  query TeamHistory($id: UUID!, $search: String, $value: String, $skip: Int!, $take: Int!) {
    team(id: $id) { ... on Team { id history(search: $search, value: $value, skip: $skip, take: $take) { totalCount items { ${TEAM_TICKET} } } } }
  }`;

export const PIN_TEAM_ANCHOR = gql`
  mutation PinTeamAnchor($input: PinTeamAnchorInput!) {
    pinTeamAnchor(input: $input) { teamAnchor { id } ${ERRORS} }
  }`;

export const UNPIN_TEAM_ANCHOR = gql`
  mutation UnpinTeamAnchor($input: UnpinTeamAnchorInput!) {
    unpinTeamAnchor(input: $input) { boolean ${ERRORS} }
  }`;

export const TICKET_REFERENCE = gql`
  query TicketReference($ticketId: UUID!) {
    ticketReference(ticketId: $ticketId) {
      ... on TicketReference { teamId anchors { id value note ticket { ${TEAM_TICKET} } } similar { ${TEAM_TICKET} } }
      ... on ApplicationError { code message }
    }
  }`;

export interface TeamTicket {
  ticketId: string;
  gameId: string;
  sprintName: string;
  sprintDate: string;
  name: string;
  externalKey: string | null;
  externalUrl: string | null;
  estimate: string;
  spread: string;
}

export interface TeamAnchor {
  id: string;
  value: string;
  note: string | null;
  ticket: TeamTicket;
}
