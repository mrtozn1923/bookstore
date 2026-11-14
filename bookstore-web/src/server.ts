import {
  AngularNodeAppEngine,
  createNodeRequestHandler,
  isMainModule,
  writeResponseToNodeResponse,
} from '@angular/ssr/node';
import express from 'express';
import { join } from 'node:path';

const browserDistFolder = join(import.meta.dirname, '../browser');

const app = express();
const angularApp = new AngularNodeAppEngine();

app.use(
  express.static(browserDistFolder, {
    maxAge: '1y',
    index: false,
    redirect: false,
  }),
);

// Every page request: prerendered HTML, server-side render or the CSR shell, per RenderMode in app.routes.server.ts
app.use((req, res, next) => {
  angularApp
    .handle(req)
    .then((response) => (response ? writeResponseToNodeResponse(response, res) : next()))
    .catch(next);
});

if (isMainModule(import.meta.url) || process.env['pm_id']) {
  const port = Number(process.env['PORT'] || 4000);
  // Only Bookstore.Api talks to this server: listen on the loopback interface
  app.listen(port, '127.0.0.1', (error) => {
    if (error) {
      throw error;
    }
    console.log(`Node Express server listening on http://127.0.0.1:${port}`);
  });
}

export const reqHandler = createNodeRequestHandler(app);
