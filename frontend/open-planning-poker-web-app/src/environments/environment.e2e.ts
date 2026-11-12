// End-to-end tests (e2e/, docker-compose.e2e.yml): the local backend, with subscriptions straight to the
// Game Engine like in production.
export const environment = {
  production: true,
  gqlGateway: 'http://localhost:10010/graphql',
  gqlGatewayWss: 'ws://localhost:9091/graphql',
};
