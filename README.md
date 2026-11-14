# Bookstore: Browser APIs and PWA

Code for the "Browser API'leri ve PWA" series on mertozen.com. Bookstore is the online bookstore used as the shared
example across the site's series.

- `api/server.ts`: a small mock of Bookstore.Api (Node.js 24, no dependencies). It also serves `web/`.
- `web/`: framework-free examples (open http://localhost:5281/).
- `bookstore-web/`: the Angular 22.2 client.
- `notifications/Bookstore.Notifications.Api/`: ASP.NET Core 10 API for Web Push subscriptions and sending (07).
- `tools/local-push-service.ts`: a local stand-in for a browser push service, for tests only (07).
- `web/device/`: which device APIs need a user activation, and `watchPosition` (11). The Angular side is
  `bookstore-web/src/app/core/device-capabilities.ts`.
- `tools/baseline-report.ts`: the Baseline status of the features the series uses, from web-features (10): `npm ci && npm run baseline`.

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

```bash
# Web Push (07): generate a VAPID key pair once per environment and keep it in a secret store
cd notifications/Bookstore.Notifications.Api
dotnet run -- generate-vapid-keys      # prints Push__PublicKey=... and Push__PrivateKey=...
# set both as environment variables, then:
dotnet run                             # http://127.0.0.1:5290; the mock API forwards /api/push/* to it
```
