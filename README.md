# Bookstore web performance lab

Code for the "Web Performansı" series on mertozen.com. Bookstore is the online bookstore used as the shared
example across the site's series.

- `api/Bookstore.Api` – ASP.NET Core 10 API; also serves the Angular build (`bookstore-web/dist`) and `/covers`.
- `bookstore-web` – Angular 22.2 client (TypeScript 6.0).
- `lab` – measurement scripts (Playwright 1.65 Chromium, Lighthouse 13.6). All numbers they print are lab
  measurements on one machine.

```bash
cd lab && npm ci && npx playwright install chromium && node make-covers.mjs
node third-party.mjs                                  # simulated third-party origins on :5302
cd ../bookstore-web && npm ci && npx ng build
cd ../api/Bookstore.Api && dotnet run --urls http://127.0.0.1:5300
cd lab && node measure.mjs http://127.0.0.1:5300/ --runs 5 --profile mobile
```

Lighthouse needs `CHROME_PATH` pointing to a Chromium binary (for example Playwright's).
