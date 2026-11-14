# Code review at Bookstore

We review to share ownership of the code, spread knowledge and keep the code base easy to change.
Finding bugs is welcome but it is not the main goal: tests and CI are the first line of defence.

## Who reviews

- Every pull request into main needs one approval from someone other than the author.
- Changes to migrations, authorization and payments also need an owner from CODEOWNERS.
- Juniors review too, including seniors' code. Their approval counts like anyone else's.

## How fast

- First response within one working day: a review, a question, or a time when you will look.
- Authors keep pull requests small: about 400 changed lines at most, generated files excluded.
  Bigger changes are split or agreed with the reviewer before the work starts.

## What we leave to tools

- Formatting, naming style and analyzer rules: .editorconfig, dotnet format, ESLint, Prettier.
- Build, tests and vulnerable packages: CI is green before a review is requested.

## What reviewers look at

- Does the change do what the description says? Edge cases, error paths, tests that check behaviour.
- Design: does the code belong here, will the next change be easy?
- Security and data: authorization, input validation, personal data in logs.

## Approving

- The bar is "better than before", not "as I would have written it".
- Small leftovers can be approved with comments the author fixes before merging.
