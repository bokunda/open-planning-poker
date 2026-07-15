import { ApplicationConfig, inject, provideZoneChangeDetection, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import { DomSanitizer } from '@angular/platform-browser';
import { MatIconRegistry } from '@angular/material/icon';
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
import { environment } from '../environments/environment';

const GITHUB_ICON_SVG = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
  <path d="M12 0C5.37 0 0 5.37 0 12c0 5.31 3.435 9.795 8.205 11.385.6.105.825-.255.825-.57 0-.285-.015-1.23-.015-2.235-3.015.555-3.795-.735-4.035-1.41-.135-.345-.72-1.41-1.23-1.695-.42-.225-1.02-.78-.015-.795.945-.015 1.62.87 1.845 1.23 1.08 1.815 2.805 1.305 3.495.99.105-.78.42-1.305.765-1.605-2.67-.3-5.46-1.335-5.46-5.925 0-1.305.465-2.385 1.23-3.225-.12-.3-.54-1.53.12-3.18 0 0 1.005-.315 3.3 1.23.96-.27 1.98-.405 3-.405s2.04.135 3 .405c2.295-1.545 3.3-1.23 3.3-1.23.66 1.65.24 2.88.12 3.18.765.84 1.23 1.92 1.23 3.225 0 4.605-2.805 5.625-5.475 5.925.435.375.81 1.095.81 2.22 0 1.605-.015 2.895-.015 3.3 0 .315.225.69.825.57C20.565 21.795 24 17.31 24 12c0-6.63-5.37-12-12-12z"/>
</svg>`;

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withFetch()),
    provideClientHydration(withEventReplay()),
    provideAnimations(),
    // Register custom SVG icons (replaces Font Awesome brand icons)
    {
      provide: APP_INITIALIZER,
      useFactory: () => {
        const registry = inject(MatIconRegistry);
        const sanitizer = inject(DomSanitizer);
        registry.addSvgIconLiteral('github', sanitizer.bypassSecurityTrustHtml(GITHUB_ICON_SVG));
        return () => {};
      },
      multi: true
    },
    provideApollo(() => {

      const httpLink = inject(HttpLink);

      const gqlGateway = environment.gqlGateway;
      const gqlGatewayWss = environment.gqlGatewayWss;

      // Error handling link — suppress expected auth errors on landing page
      const errorLink = onError(({ graphQLErrors, networkError }) => {
        if (graphQLErrors) {
          graphQLErrors.forEach(({ message }) => {
            // Silently ignore auth errors (expected when no token yet)
            if (!message?.includes('not authorized') && !message?.includes('Unauthorized')) {
              console.error('[GraphQL error]:', message);
            }
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
