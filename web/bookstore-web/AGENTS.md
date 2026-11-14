# AGENTS.md — bookstore-web

- Angular 22, standalone components, signals for state. No NgModules.
- TypeScript 6.0 (Angular 22.2 needs >=6.0 <6.1); do not upgrade it.
- Use `inject()`, not constructor parameters.
- Pages read data with `httpResource`; writes go through a store such as `src/app/cart/cart-store.ts`.
- API types live in `src/app/core/api-models.ts` and mirror the C# records.
- Tests: `npm test` (Vitest). Format: `npx prettier --write .`
- `/api` is proxied to the API (`proxy.conf.json`); never hard-code http://localhost:5080.
