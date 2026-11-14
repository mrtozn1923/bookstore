# Bookstore: Browser APIs and PWA

Code for the "Browser API'leri ve PWA" series on mertozen.com. Bookstore is the online bookstore used as the shared
example across the site's series.

- `api/server.ts`: a small mock of Bookstore.Api (Node.js 24, no dependencies). It also serves `web/`.
- `web/`: framework-free examples (open http://localhost:5281/).
- `bookstore-web/`: the Angular 22.2 client.

```bash
npm run api                  # http://localhost:5281
cd bookstore-web
npm ci
npm start                    # http://localhost:4200, /api is proxied to the mock API

# production build with the Angular service worker (05)
npm run build
cd ..
npm run serve:app            # http://localhost:5281 serves the build and /api
```
