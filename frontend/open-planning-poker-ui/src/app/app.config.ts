import { ApplicationConfig, inject, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient } from '@angular/common/http';
import { HttpLink } from 'apollo-angular/http';
import { provideApollo } from 'apollo-angular';
import { ApolloLink, InMemoryCache } from '@apollo/client/core';
import { setContext } from '@apollo/client/link/context';
import { gqlUsers } from './shared/constants';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(),
    provideApollo(() => {
      const httpLink = inject(HttpLink);

      const basic = setContext((operation, context) => ({
        headers: {},
      }));

      const auth = setContext((operation, context) => {
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

      return {
        link: ApolloLink.from([basic, auth, httpLink.create({ uri: gqlUsers })]),
        cache: new InMemoryCache(),
      };
    })],
};
