# Bookstore.Analytics — "Analytics ve Ölçüm" series code

Every dataset under `data/` is synthetic, made for the examples; none of it is real site data.

## Requirements
- .NET 10 SDK
- Node.js 24 (Angular 22.2, from post 04)

## Build and test
    dotnet build
    dotnet test

## Commands
    # 01: classify an access log (combined format) by traffic source
    dotnet run --project src/Bookstore.Analytics.Cli -- logs data/access-sample.log www.bookstore.example

    # 02: low-CTR queries compared with the site's own CTR per position bucket
    dotnet run --project src/Bookstore.Analytics.Cli -- opportunities data/gsc-pages-queries.csv

    # 02: try a Search Console regex with Go's RE2-based regexp package (Go 1.27)
    cd tools/re2check && go run . '(^|\s)(nasıl|nasil|nedir)(\s|$)' ../../data/queries-sample.txt

    # 03: fetch Search Analytics data day by day into SQL Server, then the weekly report
    # BOOKSTORE_ANALYTICS_SQL = connection string of your own SQL Server database
    # BOOKSTORE_GSC_KEY_FILE  = path of the service account key file, outside the repository
    dotnet run --project src/Bookstore.Analytics.Cli -- fetch sc-domain:bookstore.example
    dotnet run --project src/Bookstore.Analytics.Cli -- report 2026-11-11

    # 03: local run without Google: a stub that serves synthetic data on 127.0.0.1
    dotnet run --project tools/Bookstore.Analytics.GscStub --urls http://127.0.0.1:52703
    dotnet run --project src/Bookstore.Analytics.Cli -- fetch sc-domain:bookstore.example --from 2026-10-29 --to 2026-11-11 --stub http://127.0.0.1:52703/ --page-size 25

    # 03: BigQuery bulk export query (run in your own Google Cloud project)
    sql/bigquery-weekly-pages.sql

## 04: GA4 in the Razor pages and in the Angular app
    # Razor: the Google tag with a CSP nonce; G-TEST123 is a placeholder ID
    Analytics__MeasurementId=G-TEST123 dotnet run --project src/Bookstore.Analytics.Web --urls http://127.0.0.1:52704

    # Angular 22.2 (TypeScript 6.0)
    cd web/bookstore-web && npm ci && npx ng build

    # page_view check in headless Chromium; requests to Google are blocked
    cd web/lab && npm ci && npx playwright install chromium
    node serve.mjs ../bookstore-web/dist/bookstore-web/browser 52714 &
    node spa-pageviews.mjs http://127.0.0.1:52714

## 05: consent
    # Razor pages: no Google tag and no Measurement Protocol call before .AspNet.Consent=yes
    Analytics__MeasurementId=G-TEST123 dotnet run --project src/Bookstore.Analytics.Web --urls http://127.0.0.1:52704
    curl -si -X POST http://127.0.0.1:52704/consent/analytics/grant

    # Angular: reject / accept / returning visitor, checked in headless Chromium
    cd web/lab && node consent-check.mjs http://127.0.0.1:52714

    # GA4 sessions against server-side Google landings (synthetic data)
    dotnet run --project src/Bookstore.Analytics.Cli -- coverage data/coverage-daily.csv

## 06: Core Web Vitals field data
    # RUM endpoint + report (p75 per route pattern and form factor, Search Console thresholds)
    dotnet run --project src/Bookstore.Analytics.Web --urls http://127.0.0.1:52706
    node web/lab/rum-sim.mjs http://127.0.0.1:52706     # synthetic beacons
    curl -s http://127.0.0.1:52706/rum/report

    # the beacon the Angular app sends (headless Chromium, lab values)
    cd web/lab && node rum-check.mjs http://127.0.0.1:52714
