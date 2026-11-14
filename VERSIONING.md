# Versioning

| Component | Scheme | Source of the version |
|---|---|---|
| Bookstore.Api, bookstore-web | CalVer `YYYY.M.BUILD` + commit | tag `app/vYYYY.M.BUILD`, CI build number |
| Bookstore.Contracts (NuGet) | SemVer 2.0.0 | tag `contracts/vX.Y.Z` (MinVer) |
| @bookstore/ui (npm) | SemVer 2.0.0 | `version` in the library's package.json |

- MAJOR for: removing or renaming public API, changing behavior callers rely on, dropping a target framework, a new major of a dependency whose types are public.
- Pre-release labels: `alpha`, `beta`, `rc` (lower case), numbered with a dot: `rc.1`.
- Commits follow Conventional Commits; `feat` = minor, `fix`/`perf` = patch, `!` or `BREAKING CHANGE:` = major.
- A member is marked `[Obsolete]` in at least one minor release before it is removed.
