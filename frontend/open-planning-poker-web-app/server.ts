import { APP_BASE_HREF } from '@angular/common';
import { CommonEngine } from '@angular/ssr/node';
import express from 'express';
import { fileURLToPath } from 'node:url';
import { dirname, join, resolve } from 'node:path';
import bootstrap from './src/main.server';
import { negotiateLanguage, SERVER_LANGUAGE } from './src/app/i18n/language';

// The Express app is exported so that it can be used by serverless Functions.
export function app(): express.Express {
  const server = express();
  const serverDistFolder = dirname(fileURLToPath(import.meta.url));
  const browserDistFolder = resolve(serverDistFolder, '../browser');
  const indexHtml = join(serverDistFolder, 'index.server.html');

  const commonEngine = new CommonEngine({
    allowedHosts: ['app.openplanningpoker.com', 'localhost'],
  });

  server.set('view engine', 'html');
  server.set('views', browserDistFolder);

  // Agent discovery manifest (ARD). express.static ignores dot-folders by default, so without
  // this route the SSR fallback answered /.well-known/ai-catalog.json with the HTML page.
  server.use('/.well-known', express.static(join(browserDistFolder, '.well-known'), {
    dotfiles: 'allow',
    maxAge: '1h',
    setHeaders: (res, filePath) => {
      if (filePath.endsWith('.json')) res.setHeader('Content-Type', 'application/json; charset=utf-8');
    },
  }));

  // Serve static files from /browser
  server.get('*.*', express.static(browserDistFolder, { maxAge: '1y' }));

  // Paths the app has (see app.routes.ts). Anything else renders the not-found page with a real 404,
  // so search engines do not index it as a duplicate of the home page.
  const uuid = '[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}';
  const knownRoute = new RegExp(`^/((game(/${uuid}(/ticket/${uuid})?)?)|(team(/${uuid})?))?/?$`, 'i');

  // All regular routes use the Angular engine
  server.get('*', (req, res, next) => {
    const { protocol, originalUrl, baseUrl, headers } = req;
    if (!knownRoute.test(req.path)) res.status(404);

    commonEngine
      .render({
        bootstrap,
        documentFilePath: indexHtml,
        url: `${protocol}://${headers.host}${originalUrl}`,
        publicPath: browserDistFolder,
        providers: [
          { provide: APP_BASE_HREF, useValue: baseUrl },
          // The page is rendered in the remembered language or the browser's (OPP-24).
          { provide: SERVER_LANGUAGE, useValue: negotiateLanguage(headers.cookie, headers['accept-language']) },
        ],
      })
      .then((html) => {
        // The same URL renders in different languages.
        res.setHeader('Vary', 'Accept-Language, Cookie');
        res.send(html);
      })
      .catch((err) => next(err));
  });

  return server;
}

function run(): void {
  const port = process.env['PORT'] || 4000;

  // Start up the Node server
  const server = app();
  server.listen(port, () => {
    console.log(`Node Express server listening on http://localhost:${port}`);
  });
}

run();
