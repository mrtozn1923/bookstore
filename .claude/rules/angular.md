---
paths:
  - "web/bookstore-web/src/**/*.{ts,html,scss}"
---

# Angular rules

- Standalone components only; no NgModules.
- Use signals for component state and `ChangeDetectionStrategy.OnPush`.
- Use the built-in control flow (`@if`, `@for`) in templates.
- HTTP calls go through services in `src/app/core`; components never inject `HttpClient` directly.
- Every component gets a `.spec.ts` file next to it.
