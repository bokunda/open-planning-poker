import { gql } from 'apollo-angular';
export type Maybe<T> = T | null;
export type InputMaybe<T> = Maybe<T>;
export type Exact<T extends { [key: string]: unknown }> = { [K in keyof T]: T[K] };
export type MakeOptional<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]?: Maybe<T[SubKey]> };
export type MakeMaybe<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]: Maybe<T[SubKey]> };
export type MakeEmpty<T extends { [key: string]: unknown }, K extends keyof T> = { [_ in K]?: never };
export type Incremental<T> = T | { [P in keyof T]?: P extends ' $fragmentName' | '__typename' ? T[P] : never };
/** All built-in and custom scalars, mapped to their actual values */
export type Scalars = {
  ID: { input: string; output: string; }
  String: { input: string; output: string; }
  Boolean: { input: boolean; output: boolean; }
  Int: { input: number; output: number; }
  Float: { input: number; output: number; }
  /** The `Decimal` scalar type represents a decimal floating-point number. */
  Decimal: { input: any; output: any; }
  UUID: { input: any; output: any; }
};

export type ApiCollectionOfGamePlayer = {
  __typename?: 'ApiCollectionOfGamePlayer';
  items: Array<GamePlayer>;
  totalCount: Scalars['Int']['output'];
};

export type ApiCollectionOfTicket = {
  __typename?: 'ApiCollectionOfTicket';
  items: Array<Ticket>;
  totalCount: Scalars['Int']['output'];
};

export type ApiCollectionOfVote = {
  __typename?: 'ApiCollectionOfVote';
  items: Array<Vote>;
  totalCount: Scalars['Int']['output'];
};

export type ApplicationError = Error & {
  __typename?: 'ApplicationError';
  code: Scalars['String']['output'];
  message: Scalars['String']['output'];
};

/** Defines when a policy shall be executed. */
export enum ApplyPolicy {
  /** After the resolver was executed. */
  AfterResolver = 'AFTER_RESOLVER',
  /** Before the resolver was executed. */
  BeforeResolver = 'BEFORE_RESOLVER',
  /** The policy is applied in the validation step before the execution. */
  Validation = 'VALIDATION'
}

export type BaseUserProfile = {
  __typename?: 'BaseUserProfile';
  id: Scalars['UUID']['output'];
  userName: Scalars['String']['output'];
};

export type ChangeUsernameError = ApplicationError;

export type ChangeUsernameInput = {
  username: Scalars['String']['input'];
};

export type ChangeUsernamePayload = {
  __typename?: 'ChangeUsernamePayload';
  boolean?: Maybe<Scalars['Boolean']['output']>;
  errors?: Maybe<Array<ChangeUsernameError>>;
};

export type CreateGameError = ApplicationError;

export type CreateGameInput = {
  description: Scalars['String']['input'];
  name: Scalars['String']['input'];
};

export type CreateGamePayload = {
  __typename?: 'CreateGamePayload';
  errors?: Maybe<Array<CreateGameError>>;
  game?: Maybe<Game>;
};

export type CreateOrUpdateVoteError = ApplicationError;

export type CreateOrUpdateVoteInput = {
  ticketId: Scalars['UUID']['input'];
  value: Scalars['String']['input'];
};

export type CreateOrUpdateVotePayload = {
  __typename?: 'CreateOrUpdateVotePayload';
  errors?: Maybe<Array<CreateOrUpdateVoteError>>;
  vote?: Maybe<Vote>;
};

export type CreateSettingsError = ApplicationError;

export type CreateSettingsInput = {
  deckSetup?: InputMaybe<Scalars['String']['input']>;
  gameId: Scalars['UUID']['input'];
};

export type CreateSettingsPayload = {
  __typename?: 'CreateSettingsPayload';
  errors?: Maybe<Array<CreateSettingsError>>;
  settings?: Maybe<Settings>;
};

export type CreateTicketError = ApplicationError;

export type CreateTicketInput = {
  description: Scalars['String']['input'];
  gameId: Scalars['UUID']['input'];
  name: Scalars['String']['input'];
};

export type CreateTicketPayload = {
  __typename?: 'CreateTicketPayload';
  errors?: Maybe<Array<CreateTicketError>>;
  ticket?: Maybe<Ticket>;
};

export type CurrentUserResult = ApplicationError | User;

export type DeleteTicketError = ApplicationError;

export type DeleteTicketInput = {
  id: Scalars['UUID']['input'];
};

export type DeleteTicketPayload = {
  __typename?: 'DeleteTicketPayload';
  errors?: Maybe<Array<DeleteTicketError>>;
  ticket?: Maybe<Ticket>;
};

export type Error = {
  message: Scalars['String']['output'];
};

export type Game = {
  __typename?: 'Game';
  description: Scalars['String']['output'];
  id: Scalars['UUID']['output'];
  name: Scalars['String']['output'];
  settingsDetails: SettingsDetailsResult;
};

export type GamePlayer = {
  __typename?: 'GamePlayer';
  id: Scalars['UUID']['output'];
  name?: Maybe<Scalars['String']['output']>;
};

export type GamePlayersResult = ApiCollectionOfGamePlayer | ApplicationError;

export type GameResult = ApplicationError | Game;

export type Info = {
  __typename?: 'Info';
  author: Scalars['String']['output'];
  contact: Scalars['String']['output'];
  version: Scalars['String']['output'];
};

export type InfoResult = ApplicationError | Info;

export type JoinGameError = ApplicationError;

export type JoinGameInput = {
  gameId: Scalars['UUID']['input'];
};

export type JoinGamePayload = {
  __typename?: 'JoinGamePayload';
  boolean?: Maybe<Scalars['Boolean']['output']>;
  errors?: Maybe<Array<JoinGameError>>;
};

export type LeaveGameError = ApplicationError;

export type LeaveGameInput = {
  gameId: Scalars['UUID']['input'];
};

export type LeaveGamePayload = {
  __typename?: 'LeaveGamePayload';
  boolean?: Maybe<Scalars['Boolean']['output']>;
  errors?: Maybe<Array<LeaveGameError>>;
};

export type Mutation = {
  __typename?: 'Mutation';
  changeUsername: ChangeUsernamePayload;
  /** Creates a game, returns game details. */
  createGame: CreateGamePayload;
  /** Creates or updates a vote. */
  createOrUpdateVote: CreateOrUpdateVotePayload;
  /** Creates Game Settings. */
  createSettings: CreateSettingsPayload;
  /** Creates a ticket for given game. */
  createTicket: CreateTicketPayload;
  /** Deletes a ticket for given game. */
  deleteTicket: DeleteTicketPayload;
  /** Join Game */
  joinGame: JoinGamePayload;
  /** Leave a Game */
  leaveGame: LeaveGamePayload;
  /** Dummy health-check mutation. */
  ping: PingPayload;
  registerUser: RegisterUserPayload;
  /** Updates Game Settings. */
  updateSettings: UpdateSettingsPayload;
  /** Updates a ticket for given game. */
  updateTicket: UpdateTicketPayload;
};


export type MutationChangeUsernameArgs = {
  input: ChangeUsernameInput;
};


export type MutationCreateGameArgs = {
  input: CreateGameInput;
};


export type MutationCreateOrUpdateVoteArgs = {
  input: CreateOrUpdateVoteInput;
};


export type MutationCreateSettingsArgs = {
  input: CreateSettingsInput;
};


export type MutationCreateTicketArgs = {
  input: CreateTicketInput;
};


export type MutationDeleteTicketArgs = {
  input: DeleteTicketInput;
};


export type MutationJoinGameArgs = {
  input: JoinGameInput;
};


export type MutationLeaveGameArgs = {
  input: LeaveGameInput;
};


export type MutationRegisterUserArgs = {
  input: RegisterUserInput;
};


export type MutationUpdateSettingsArgs = {
  input: UpdateSettingsInput;
};


export type MutationUpdateTicketArgs = {
  input: UpdateTicketInput;
};

export type PingPayload = {
  __typename?: 'PingPayload';
  string?: Maybe<Scalars['String']['output']>;
};

export type Query = {
  __typename?: 'Query';
  currentUser: CurrentUserResult;
  /** Returns Game details. */
  game: GameResult;
  /** Returns game with participants */
  gamePlayers: GamePlayersResult;
  /** Returns an info about the Game Engine service. */
  info: InfoResult;
  /** Dummy health-check query. */
  ping: Scalars['String']['output'];
  /** Gets Game Settings. */
  settings: SettingsResult;
  /** Gets a ticket by id. */
  ticket: TicketResult;
  /** Gets a list of tickets by gameId. */
  tickets: TicketsResult;
  /** Gets votes by ticketId. */
  votes: VotesResult;
};


export type QueryGameArgs = {
  id: Scalars['UUID']['input'];
};


export type QueryGamePlayersArgs = {
  gameId: Scalars['UUID']['input'];
};


export type QuerySettingsArgs = {
  gameId: Scalars['UUID']['input'];
};


export type QueryTicketArgs = {
  id: Scalars['UUID']['input'];
};


export type QueryTicketsArgs = {
  gameId: Scalars['UUID']['input'];
};


export type QueryVotesArgs = {
  ticketId: Scalars['UUID']['input'];
};

export type RegisterUserError = ApplicationError;

export type RegisterUserInput = {
  username?: InputMaybe<Scalars['String']['input']>;
};

export type RegisterUserPayload = {
  __typename?: 'RegisterUserPayload';
  errors?: Maybe<Array<RegisterUserError>>;
  registerUserResponse?: Maybe<RegisterUserResponse>;
};

export type RegisterUserResponse = {
  __typename?: 'RegisterUserResponse';
  id: Scalars['UUID']['output'];
  token: Scalars['String']['output'];
  userName: Scalars['String']['output'];
};

export type Settings = {
  __typename?: 'Settings';
  deckSetup: Scalars['String']['output'];
  gameId: Scalars['UUID']['output'];
  id: Scalars['UUID']['output'];
};

export type SettingsDetailsResult = ApplicationError | Settings;

export type SettingsResult = ApplicationError | Settings;

export type Subscription = {
  __typename?: 'Subscription';
  onPlayerJoined: BaseUserProfile;
  onPlayerLeave: BaseUserProfile;
  onTicketCreated: Ticket;
  onVoteCreatedOrUpdated: Vote;
};


export type SubscriptionOnPlayerJoinedArgs = {
  gameId: Scalars['UUID']['input'];
};


export type SubscriptionOnPlayerLeaveArgs = {
  gameId: Scalars['UUID']['input'];
};


export type SubscriptionOnTicketCreatedArgs = {
  gameId: Scalars['UUID']['input'];
};


export type SubscriptionOnVoteCreatedOrUpdatedArgs = {
  ticketId: Scalars['UUID']['input'];
};

export type Ticket = {
  __typename?: 'Ticket';
  averageVotingValue: Scalars['Decimal']['output'];
  description: Scalars['String']['output'];
  gameId: Scalars['UUID']['output'];
  id: Scalars['UUID']['output'];
  name: Scalars['String']['output'];
  votes: ApiCollectionOfVote;
};

export type TicketResult = ApplicationError | Ticket;

export type TicketsResult = ApiCollectionOfTicket | ApplicationError;

export type UpdateSettingsError = ApplicationError;

export type UpdateSettingsInput = {
  deckSetup?: InputMaybe<Scalars['String']['input']>;
  gameId: Scalars['UUID']['input'];
  id: Scalars['UUID']['input'];
};

export type UpdateSettingsPayload = {
  __typename?: 'UpdateSettingsPayload';
  errors?: Maybe<Array<UpdateSettingsError>>;
  settings?: Maybe<Settings>;
};

export type UpdateTicketError = ApplicationError;

export type UpdateTicketInput = {
  description: Scalars['String']['input'];
  id: Scalars['UUID']['input'];
  name: Scalars['String']['input'];
};

export type UpdateTicketPayload = {
  __typename?: 'UpdateTicketPayload';
  errors?: Maybe<Array<UpdateTicketError>>;
  ticket?: Maybe<Ticket>;
};

export type User = {
  __typename?: 'User';
  id: Scalars['UUID']['output'];
  userName: Scalars['String']['output'];
};

export type Vote = {
  __typename?: 'Vote';
  id: Scalars['UUID']['output'];
  playerId: Scalars['UUID']['output'];
  playerName: Scalars['String']['output'];
  value: Scalars['String']['output'];
};

export type VotesResult = ApiCollectionOfVote | ApplicationError;
