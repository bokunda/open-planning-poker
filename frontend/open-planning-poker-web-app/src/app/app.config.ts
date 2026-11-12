import { isPlatformBrowser } from '@angular/common';
import { ApplicationConfig, PLATFORM_ID, inject, provideZoneChangeDetection, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { DomSanitizer } from '@angular/platform-browser';
import { MatIconRegistry } from '@angular/material/icon';
import { routes } from './app.routes';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { HttpLink } from 'apollo-angular/http';
import { provideApollo } from 'apollo-angular';
import { ApolloLink, DefaultOptions, InMemoryCache, Observable, split } from '@apollo/client/core';
import { onError } from '@apollo/client/link/error';
import { setContext } from '@apollo/client/link/context';
import { createClient } from 'graphql-ws';
import { GraphQLWsLink } from '@apollo/client/link/subscriptions';
import { getMainDefinition } from '@apollo/client/utilities';
import { provideClientHydration, withEventReplay, withIncrementalHydration } from '@angular/platform-browser';
import { environment } from '../environments/environment';

const GITHUB_ICON_SVG = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
  <path d="M12 0C5.37 0 0 5.37 0 12c0 5.31 3.435 9.795 8.205 11.385.6.105.825-.255.825-.57 0-.285-.015-1.23-.015-2.235-3.015.555-3.795-.735-4.035-1.41-.135-.345-.72-1.41-1.23-1.695-.42-.225-1.02-.78-.015-.795.945-.015 1.62.87 1.845 1.23 1.08 1.815 2.805 1.305 3.495.99.105-.78.42-1.305.765-1.605-2.67-.3-5.46-1.335-5.46-5.925 0-1.305.465-2.385 1.23-3.225-.12-.3-.54-1.53.12-3.18 0 0 1.005-.315 3.3 1.23.96-.27 1.98-.405 3-.405s2.04.135 3 .405c2.295-1.545 3.3-1.23 3.3-1.23.66 1.65.24 2.88.12 3.18.765.84 1.23 1.92 1.23 3.225 0 4.605-2.805 5.625-5.475 5.925.435.375.81 1.095.81 2.22 0 1.605-.015 2.895-.015 3.3 0 .315.225.69.825.57C20.565 21.795 24 17.31 24 12c0-6.63-5.37-12-12-12z"/>
</svg>`;

/**
 * Material icons (Apache 2.0) used by the room and team features, as inline SVG so the
 * self-hosted Material Symbols subset does not need to grow. Use them with svgIcon="opp-…".
 */
const SVG_ICONS: Record<string, string> = {
  'opp-delete': 'M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z',
  'opp-drag': 'M11 18c0 1.1-.9 2-2 2s-2-.9-2-2 .9-2 2-2 2 .9 2 2zm-2-8c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2zm0-6c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2zm6 4c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zm0 2c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2zm0 6c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2z',
  'opp-team': 'M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z',
  'opp-coffee': 'M20 3H4v10c0 2.21 1.79 4 4 4h6c2.21 0 4-1.79 4-4v-3h2c1.11 0 2-.9 2-2V5c0-1.11-.89-2-2-2zm0 5h-2V5h2v3zM4 19h16v2H4z',
  'opp-heart': 'M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z',
  'opp-observe': 'M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5zM12 17c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5zm0-8c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3z',
  'opp-star': 'M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z',
  'opp-chart': 'M5 9.2h3V19H5zM10.6 5h2.8v14h-2.8zm5.6 8H19v6h-2.8z',
  'opp-check': 'M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z',
  'opp-clock': 'M11.99 2C6.47 2 2 6.48 2 12s4.47 10 9.99 10C17.52 22 22 17.52 22 12S17.52 2 11.99 2zM12 20c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8zm.5-13H11v6l5.25 3.15.75-1.23-4.5-2.67z',
  'opp-person-remove': 'M14 8c0-2.21-1.79-4-4-4S6 5.79 6 8s1.79 4 4 4 4-1.79 4-4zm3 2v2h6v-2h-6zM2 18v2h16v-2c0-2.66-5.33-4-8-4s-8 1.34-8 4z',
  'opp-settings': 'M19.14 12.94c.04-.3.06-.61.06-.94 0-.32-.02-.64-.07-.94l2.03-1.58c.18-.14.23-.41.12-.61l-1.92-3.32c-.12-.22-.37-.29-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54c-.04-.24-.24-.41-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96c-.22-.08-.47 0-.59.22L2.74 8.87c-.12.21-.08.47.12.61l2.03 1.58c-.05.3-.09.63-.09.94s.02.64.07.94l-2.03 1.58c-.18.14-.23.41-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32c.12-.22.07-.47-.12-.61l-2.01-1.58zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6 3.6 1.62 3.6 3.6-1.62 3.6-3.6 3.6z',
  'opp-table': 'M3 3v18h18V3H3zm8 16H5v-6h6v6zm0-8H5V5h6v6zm8 8h-6v-6h6v6zm0-8h-6V5h6v6z',
};

/**
 * Icons in the self-hosted Material Symbols subset (public/fonts/material-symbols-outlined.woff2).
 * An icon that is not in the font renders as its name. To add one: download the subset again from
 * https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:opsz,wght,FILL,GRAD@24,400,1,0&icon_names=<sorted list>&display=block
 * (the woff2 URL inside that CSS) and add the name here.
 */
export const ICON_NAMES = [
  'add',
  'chevron_left',
  'chevron_right',
  'content_copy',
  'dark_mode',
  'download',
  'edit',
  'expand_less',
  'expand_more',
  'file_upload',
  'info',
  'language',
  'light_mode',
  'link',
  'login',
  'logout',
  'menu',
  'more_vert',
  'person_add',
  'play_arrow',
  'replay',
  'restart_alt',
  'send',
  'stop',
  'visibility',
] as const;

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withFetch()),
    // Incremental hydration: server-rendered @defer (hydrate ...) blocks load their JS later (demo table).
    provideClientHydration(withEventReplay(), withIncrementalHydration()),
    // The animation engine is loaded in a separate chunk after bootstrap.
    provideAnimationsAsync(),
    // Register custom SVG icons (replaces Font Awesome brand icons)
    {
      provide: APP_INITIALIZER,
      useFactory: () => {
        const registry = inject(MatIconRegistry);
        const sanitizer = inject(DomSanitizer);
        registry.addSvgIconLiteral('github', sanitizer.bypassSecurityTrustHtml(GITHUB_ICON_SVG));
        for (const [name, path] of Object.entries(SVG_ICONS)) {
          registry.addSvgIconLiteral(name, sanitizer.bypassSecurityTrustHtml(
            `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor"><path d="${path}"/></svg>`));
        }
        // <mat-icon>name</mat-icon> uses the Material Symbols subset loaded in index.html.
        registry.setDefaultFontSetClass('material-symbols-outlined');
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

      // WebSocket client only in the browser. Node 22+ has a global WebSocket too, so checking for it
      // opened subscriptions during server rendering that outlived the render and crashed the server.
      const wsLink = isPlatformBrowser(inject(PLATFORM_ID))
        ? new GraphQLWsLink(createClient({
            url: gqlGatewayWss,
            connectionParams: () => {
              if (typeof localStorage === 'undefined') return {};
              return { authToken: localStorage.getItem('token') };
            },
          }))
        : null;

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

      const httpChain = ApolloLink.from([errorLink, basic, auth, http]);

      // Subscriptions go over the WebSocket. On the server they end at once without data: sent over HTTP,
      // the gateway would keep the stream open and the page would never finish rendering.
      const noSubscriptions = new ApolloLink(() => new Observable((observer) => observer.complete()));
      const link = split(
        ({ query }) => {
          const definition = getMainDefinition(query);
          return definition.kind === 'OperationDefinition' && definition.operation === 'subscription';
        },
        wsLink ?? noSubscriptions,
        httpChain
      );

      return {
        link,
        cache: new InMemoryCache(),
        defaultOptions: defaultOptions
      };
    }),
  ],
};
