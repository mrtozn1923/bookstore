---
name: angular-component
description: Scaffold a standalone Angular component in web/bookstore-web that follows the project conventions (signals, OnPush, spec file). Use when creating a new component, page or dialog in the Angular client.
paths:
  - "web/bookstore-web/src/**"
---

# Angular component for bookstore-web

Create the component the user asked for. Before writing files, read the folder layout and naming rules in [reference.md](reference.md).

1. Put the files in the feature folder `web/bookstore-web/src/app/<feature>/`: `<name>.ts`, `<name>.html` and `<name>.spec.ts`.
2. The component is standalone, uses `ChangeDetectionStrategy.OnPush`, keeps its state in signals and gets data from a service in `src/app/core`.
3. Use the `input()` and `output()` functions, not decorators.
4. Write at least one test that renders the component with test data.
5. Run `npm test` in `web/bookstore-web` and show the test summary.
