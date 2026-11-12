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
        sprints { gameId name description createdOn ticketCount estimatedCount points }
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
}
