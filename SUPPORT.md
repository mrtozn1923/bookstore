# Support policy

| Component | Line | Status | What it gets | Until |
|---|---|---|---|---|
| Bookstore.Contracts | 2.x | Active | features, bug fixes, security fixes | six months after 3.0.0 ships |
| Bookstore.Contracts | 1.x | Security fixes only | security fixes | 2027-05-14 |
| @bookstore/ui | 1.x | Active | features, bug fixes, security fixes | six months after 2.0.0 ships |
| Bookstore.Api, bookstore-web | latest release | Active | everything, rolling forward | not applicable |

## Rules

- A new major starts a six-month security-only window for the previous major.
- Security fixes ship to every line that is still supported; the release notes list the affected versions.
- End of support is announced in the release notes and on the package page at least three months ahead.
- Packages target the supported .NET LTS and Angular versions; dropping a target framework or an Angular major is a major release.
