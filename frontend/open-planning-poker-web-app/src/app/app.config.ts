import { ApplicationConfig, inject, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { HttpLink } from 'apollo-angular/http';
import { provideApollo } from 'apollo-angular';
import { ApolloLink, DefaultOptions, InMemoryCache, split } from '@apollo/client/core';
import { onError } from '@apollo/client/link/error';
import { setContext } from '@apollo/client/link/context';
import { createClient } from 'graphql-ws';
import { GraphQLWsLink } from '@apollo/client/link/subscriptions';
import { getMainDefinition } from '@apollo/client/utilities';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withFetch()),
    provideClientHydration(withEventReplay()),
    provideApollo(() => {

      const httpLink = inject(HttpLink);

      const config = (typeof window !== 'undefined' ? (window as any).appConfig : null);

      const gqlGateway = config?.gqlGateway ?? 'http://localhost:10010/graphql';
      const gqlGatewayWss = config?.gqlGatewayWss ?? 'ws://localhost:10010/graphql';

      // Error handling link
      const errorLink = onError(({ graphQLErrors, networkError }) => {
        if (graphQLErrors) {
          graphQLErrors.forEach(({ message }) => {
            console.error('[GraphQL error]:', message);
          });
        }
        if (networkError) {
          console.error('[Network error]:', networkError.message);
        }
      });

      const basic = setContext((operation, context) => ({
        headers: {},
      }));

      const auth = setContext((operation, context) => {
        if (typeof localStorage === 'undefined') return {};

        const token = localStorage.getItem('token');

        if (token === null) {
          return {};
        } else {
          return {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          };
        }
      });

      const wsLink = new GraphQLWsLink(createClient({
        url: gqlGatewayWss,
        connectionParams: () => {
          if (typeof localStorage === 'undefined') return {};
          return { authToken: localStorage.getItem('token') };
        },
      }));

      const http = httpLink.create({ uri: gqlGateway });

      const defaultOptions: DefaultOptions = {
        watchQuery: {
          fetchPolicy: 'no-cache',
          errorPolicy: 'ignore',
        },
        query: {
          fetchPolicy: 'no-cache',
          errorPolicy: 'all',
        },
      }

      const link = split(
        ({ query }) => {
          const definition = getMainDefinition(query);
          return (
            definition.kind === 'OperationDefinition' &&
            definition.operation === 'subscription'
          );
        },
        wsLink,
        ApolloLink.from([errorLink, basic, auth, http])
      );

      return {
        link,
        cache: new InMemoryCache(),
        defaultOptions: defaultOptions
      };
    }),
  ],
};
