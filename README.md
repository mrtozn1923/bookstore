# Bookstore.Api (REST API Tasarımı / REST API Design series)

Small Bookstore API used to show the contract decisions of the series with real HTTP exchanges.

Requirements: .NET 10 SDK, Node.js 24 (only for linting the contract).

```
dotnet run --project src/Bookstore.Api      # http://127.0.0.1:52318
npm ci
npm run lint:contract                       # Redocly CLI + Spectral
```

- contract/bookstore.openapi.yaml: the API contract (OpenAPI 3.2), written before the code.
- docs/api-style-guide.md and docs/adr/: design rules and decisions.
- src/Bookstore.Api/Legacy: the old RPC-style endpoint kept as a counterexample.
