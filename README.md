# Bookstore

[Türkçe](#türkçe) · [English](#english)

---

## Türkçe

Bookstore, [mertozen.com](https://www.mertozen.com)'daki serilerin ortak örnek uygulamasıdır: kitap satan bir online
kitapçı. Bu `main` branch'i küçük ama çalışan **referans uygulamadır**; seriler kendi branch'lerinde
(`series/<seri-slug>`) bu çekirdeği genişletir, her yazı bir tag'dir (`<seri-slug>/NN`).
Anlatım: "Bookstore: Sitedeki Örnek Uygulama" serisi.

### İçerik

| Parça | Teknoloji | Klasör |
|---|---|---|
| Web API (Catalog, Ordering, Inventory module'leri) | ASP.NET Core 10, Minimal API | `src/Bookstore.Api` |
| Veritabanı, migration ve seed (20 klasik kitap) | EF Core 10, SQL Server | `src/Bookstore.Api/Data` |
| Integration testleri | xUnit v3, WebApplicationFactory | `tests/Bookstore.Api.Tests` |
| İstemci (katalog, kitap detayı, sepet) | Angular 22, standalone, signals | `web/bookstore-web` |

### Gerekenler

- .NET 10 SDK
- Node.js 24 LTS (Angular 22: `^22.22.3 || ^24.15.0 || >=26`)
- Docker Desktop (SQL Server container'ı için)

### Çalıştırma (Windows, PowerShell)

1. SQL Server 2025'i container olarak başlat (`<YourStrong!Passw0rd>` yerine kendi güçlü şifreni yaz):

   ```powershell
   docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<YourStrong!Passw0rd>" `
     -p 1433:1433 --name bookstore-sql -d mcr.microsoft.com/mssql/server:2025-latest
   ```

2. Connection string'i repoya değil, user-secrets'a yaz:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:BookstoreDb" `
     "Server=localhost,1433;Database=BookstoreDb;User Id=sa;Password=<YourStrong!Passw0rd>;TrustServerCertificate=True" `
     --project src/Bookstore.Api
   ```

   İstersen aynı değeri `ConnectionStrings__BookstoreDb` environment variable'ı olarak da verebilirsin.

3. API'yi çalıştır. Development ortamında migration'lar (şema + seed) açılışta uygulanır:

   ```powershell
   dotnet run --project src/Bookstore.Api
   # http://localhost:5080/api/books
   # http://localhost:5080/openapi/v1.json
   ```

4. Angular istemcisini ikinci bir terminalde çalıştır (`/api` istekleri API'ye proxy'lenir):

   ```powershell
   cd web/bookstore-web
   npm ci
   npm start
   # http://localhost:4200
   ```

5. Testler (aynı SQL Server'da `BookstoreDb_Tests` veritabanını sıfırdan kurar):

   ```powershell
   dotnet test
   cd web/bookstore-web; npm test
   ```

Migration'ları elle yönetmek istersen: `dotnet tool restore` sonra
`dotnet ef database update --project src/Bookstore.Api`.

### Endpoint'ler

| Method | Yol | Açıklama |
|---|---|---|
| GET | `/api/books?search=&category=&page=&pageSize=` | Katalog listesi ve arama |
| GET | `/api/books/{isbn}` | Kitap detayı ve stok |
| GET | `/api/categories` | Kategoriler |
| POST | `/api/carts` | Yeni sepet |
| GET | `/api/carts/{cartId}` | Sepet |
| PUT | `/api/carts/{cartId}/items/{isbn}` | Kitap ekle / adet değiştir (`{ "quantity": 2 }`) |
| DELETE | `/api/carts/{cartId}/items/{isbn}` | Kitabı sepetten çıkar |
| POST | `/api/orders` | Sipariş ver (`{ "cartId", "customerName", "customerEmail" }`) |
| GET | `/api/orders/{orderId}` | Sipariş |

### Observability serisi

Bu branch, mertozen.com'daki Observability serisinin kodudur. Bu tag'e kadar eklenenler:

- `tools/Bookstore.FakePaymentProvider`: dış ödeme sağlayıcısının yerine geçen uygulama (http://localhost:5090). Denemelerin yüzde 25'i 3 saniye sürer (`Provider:SlowRatio`, `Provider:SlowDelayMs`).
- `tools/Bookstore.Traffic`: checkout trafiği üretir ve süre özetini yazar.

  ```powershell
  dotnet run --project tools/Bookstore.FakePaymentProvider
  dotnet run --project src/Bookstore.Api
  dotnet run --project tools/Bookstore.Traffic -- 300 4
  ```
- OpenTelemetry (trace, metric, log) ve OTLP exporter'ı (`src/Bookstore.Api/Observability`). Lokal görüntüleme için Aspire Dashboard:

  ```powershell
  docker run -d --name bookstore-dashboard -p 18888:18888 -p 4317:18889 -p 4318:18890 mcr.microsoft.com/aspire/dashboard:13.6.5
  $env:OTEL_EXPORTER_OTLP_ENDPOINT = "http://localhost:4317"
  dotnet run --project src/Bookstore.Api
  ```
- İş metric'leri: `Bookstore.Ordering` (sipariş sayısı, checkout süresi, işlenen checkout'lar) ve `Bookstore.Inventory` (stoğu azalan kitaplar); `MetricCollector` testleri.
- Kendi span'larımız (`Bookstore.Ordering`, `Bookstore.Notifications`), sipariş onayı için in-process queue ve trace context propagation, `traceparent`/`tracestate` için CORS (`Cors:Origins`).
- `deploy/observability`: tek container'lık LGTM stack'i (Collector contrib, Prometheus, Tempo, Loki, Grafana) ve Collector config'i (filter, PII transform, tail sampling):

  ```powershell
  docker compose -f deploy/observability/compose.yaml up -d
  # Grafana: http://localhost:3000, API: OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
  ```

---

## English

Bookstore is the shared example application of the series on [mertozen.com](https://www.mertozen.com): an online
bookstore. This `main` branch is a small but working **reference application**; each series extends this core on its
own branch (`series/<series-slug>`) and every post is a tag (`<series-slug>/NN`).
It is explained in the series "Bookstore: The Site's Example Application".

### What is inside

| Part | Technology | Folder |
|---|---|---|
| Web API (Catalog, Ordering, Inventory modules) | ASP.NET Core 10, Minimal APIs | `src/Bookstore.Api` |
| Database, migration and seed (20 classic books) | EF Core 10, SQL Server | `src/Bookstore.Api/Data` |
| Integration tests | xUnit v3, WebApplicationFactory | `tests/Bookstore.Api.Tests` |
| Client (catalog, book details, cart) | Angular 22, standalone, signals | `web/bookstore-web` |

### Requirements

- .NET 10 SDK
- Node.js 24 LTS (Angular 22: `^22.22.3 || ^24.15.0 || >=26`)
- Docker Desktop (for the SQL Server container)

### Running it (Windows, PowerShell)

1. Start SQL Server 2025 in a container (replace `<YourStrong!Passw0rd>` with your own strong password):

   ```powershell
   docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<YourStrong!Passw0rd>" `
     -p 1433:1433 --name bookstore-sql -d mcr.microsoft.com/mssql/server:2025-latest
   ```

2. Store the connection string in user-secrets, not in the repository:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:BookstoreDb" `
     "Server=localhost,1433;Database=BookstoreDb;User Id=sa;Password=<YourStrong!Passw0rd>;TrustServerCertificate=True" `
     --project src/Bookstore.Api
   ```

   You can also pass the same value as the `ConnectionStrings__BookstoreDb` environment variable.

3. Run the API. In the Development environment pending migrations (schema + seed) are applied on startup:

   ```powershell
   dotnet run --project src/Bookstore.Api
   # http://localhost:5080/api/books
   # http://localhost:5080/openapi/v1.json
   ```

4. Run the Angular client in a second terminal (`/api` requests are proxied to the API):

   ```powershell
   cd web/bookstore-web
   npm ci
   npm start
   # http://localhost:4200
   ```

5. Tests (they create a fresh `BookstoreDb_Tests` database on the same SQL Server):

   ```powershell
   dotnet test
   cd web/bookstore-web; npm test
   ```

To manage migrations by hand: `dotnet tool restore`, then
`dotnet ef database update --project src/Bookstore.Api`.

### Endpoints

| Method | Path | Description |
|---|---|---|
| GET | `/api/books?search=&category=&page=&pageSize=` | Catalog list and search |
| GET | `/api/books/{isbn}` | Book details and stock |
| GET | `/api/categories` | Categories |
| POST | `/api/carts` | New cart |
| GET | `/api/carts/{cartId}` | Cart |
| PUT | `/api/carts/{cartId}/items/{isbn}` | Add a book / change its quantity (`{ "quantity": 2 }`) |
| DELETE | `/api/carts/{cartId}/items/{isbn}` | Remove a book from the cart |
| POST | `/api/orders` | Place an order (`{ "cartId", "customerName", "customerEmail" }`) |
| GET | `/api/orders/{orderId}` | Order |

### Observability series

This branch holds the code of the Observability series on mertozen.com. Added up to this tag:

- `tools/Bookstore.FakePaymentProvider`: stands in for the external payment provider (http://localhost:5090). 25 percent of attempts take 3 seconds (`Provider:SlowRatio`, `Provider:SlowDelayMs`).
- `tools/Bookstore.Traffic`: generates checkout traffic and prints a duration summary.

  ```powershell
  dotnet run --project tools/Bookstore.FakePaymentProvider
  dotnet run --project src/Bookstore.Api
  dotnet run --project tools/Bookstore.Traffic -- 300 4
  ```
- OpenTelemetry (traces, metrics, logs) with the OTLP exporter (`src/Bookstore.Api/Observability`). The Aspire Dashboard for viewing locally:

  ```powershell
  docker run -d --name bookstore-dashboard -p 18888:18888 -p 4317:18889 -p 4318:18890 mcr.microsoft.com/aspire/dashboard:13.6.5
  $env:OTEL_EXPORTER_OTLP_ENDPOINT = "http://localhost:4317"
  dotnet run --project src/Bookstore.Api
  ```
- Business metrics: `Bookstore.Ordering` (orders placed, checkout duration, checkouts in progress) and `Bookstore.Inventory` (books running low); `MetricCollector` tests.
- Our own spans (`Bookstore.Ordering`, `Bookstore.Notifications`), an in-process queue for order confirmations with trace context propagation, CORS for `traceparent`/`tracestate` (`Cors:Origins`).
- `deploy/observability`: a one-container LGTM stack (Collector contrib, Prometheus, Tempo, Loki, Grafana) and the Collector config (filter, PII transform, tail sampling):

  ```powershell
  docker compose -f deploy/observability/compose.yaml up -d
  # Grafana: http://localhost:3000, API: OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
  ```
