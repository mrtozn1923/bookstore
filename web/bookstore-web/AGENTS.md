# Angular client

These rules apply to everything under `web/bookstore-web`.

- Run `npm test` and `npm run build` from this folder; use the Angular CLI (`npx ng generate`) for new files.
- Standalone components only; no NgModules.
- Use signals for component state and `ChangeDetectionStrategy.OnPush`.
- Use the built-in control flow (`@if`, `@for`) in templates.
- HTTP calls go through services in `src/app/core`; components never inject `HttpClient` directly.
- Every component gets a `.spec.ts` file next to it.
