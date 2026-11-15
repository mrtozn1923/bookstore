# bookstore-web

Angular 22 client of Bookstore: standalone components, signals, no NgModules.

## Commands (run in this folder)

- Install: `npm ci`
- Dev server: `npm start` (proxies `/api` to the API, see `proxy.conf.json`)
- Unit tests: `npm test` (Vitest through `ng test`)

## Rules

- State lives in signals (`signal`, `computed`); components read stores, they do not call `HttpClient` directly.
- API shapes are in `src/app/core/api-models.ts`; keep them in sync with the C# records.
- Use `data-testid` attributes for elements that tests look up.
