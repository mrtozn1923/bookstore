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
   # http://localhost:5080/scalar
   ```

   Development ortamında iki demo kullanıcı vardır (`appsettings.Development.json`): `ada@example.com` (Customer) ve
   `grace@example.com` (StoreStaff). Token'ı `POST /api/auth/login` verir; JWT imza anahtarı Development'ta her
   açılışta rastgele üretilir, diğer ortamlarda `Auth:SigningKey` (Base64, en az 32 byte) zorunludur.

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

Uçtan uca smoke test (bash, curl 8.3+ ve jq; Windows'ta Git Bash ya da WSL):
`BOOKSTORE_PASSWORD=Bookstore-Demo-1 ./scripts/smoke.sh`

`.http` dosyaları: `src/Bookstore.Api/Bookstore.Api.http` Visual Studio ve VS Code REST Client için (şifre
`src/Bookstore.Api/.env`'den, örnek `.env.example`); `tests/http/bookstore-flow.http` Rider ve `ijhttp` için
(şifre `tests/http/http-client.private.env.json`'dan):
`docker run --rm -v "$PWD/tests/http:/workdir" jetbrains/intellij-http-client -D -e dev -v http-client.env.json -p http-client.private.env.json bookstore-flow.http --report`

Postman collection'ı (`postman/`): Postman'de import et ya da komut satırından koş:
`npx newman run postman/Bookstore.postman_collection.json -e postman/dev.postman_environment.json --env-var password=Bookstore-Demo-1`
(Postman CLI ile: `postman collection run ...` aynı parametrelerle).

Bruno collection'ı (`bruno/`, OpenCollection YAML): Bruno'da aç ya da CLI ile koş
(şifre `bruno/.env`'den, örnek `.env.example`):
`cd bruno; npx @usebruno/cli@4.3.0 run --env dev`. GitHub Actions'taki `API tests` workflow'u aynı collection'ı koşar;
repository secret olarak `CI_SQL_PASSWORD` (SQL Server sa şifresi) ve `BOOKSTORE_PASSWORD` (demo şifresi) gerekir.

Migration'ları elle yönetmek istersen: `dotnet tool restore` sonra
`dotnet ef database update --project src/Bookstore.Api`.

### Endpoint'ler

| Method | Yol | Açıklama |
|---|---|---|
| POST | `/api/auth/login` | Token al (`{ "email", "password" }`) |
| GET | `/api/auth/me` | Token'ın sahibi (token gerekir) |
| GET | `/api/books?search=&category=&page=&pageSize=` | Katalog listesi ve arama |
| GET | `/api/books/{isbn}` | Kitap detayı ve stok |
| GET | `/api/categories` | Kategoriler |
| PUT | `/api/books/{isbn}/cover` | Kapak yükle (multipart, `file` alanı; JPEG/PNG, en çok 2 MB; StoreStaff) |
| GET | `/api/books/{isbn}/cover` | Kapak görseli |
| POST | `/api/carts` | Yeni sepet (`/api/carts` ve `/api/orders` altındaki her istek token ister) |
| GET | `/api/carts/{cartId}` | Sepet |
| PUT | `/api/carts/{cartId}/items/{isbn}` | Kitap ekle / adet değiştir (`{ "quantity": 2 }`) |
| DELETE | `/api/carts/{cartId}/items/{isbn}` | Kitabı sepetten çıkar |
| POST | `/api/orders` | Sipariş ver (`{ "cartId", "customerName", "customerEmail" }`) |
| GET | `/api/orders/{orderId}` | Sipariş (müşteri yalnız kendi siparişini, StoreStaff hepsini görür) |

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
   # http://localhost:5080/scalar
   ```

   The Development environment has two demo users (`appsettings.Development.json`): `ada@example.com` (Customer) and
   `grace@example.com` (StoreStaff). `POST /api/auth/login` issues the token; in Development the JWT signing key is
   generated randomly on every start, other environments must set `Auth:SigningKey` (Base64, at least 32 bytes).

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

End-to-end smoke test (bash, curl 8.3+ and jq; on Windows use Git Bash or WSL):
`BOOKSTORE_PASSWORD=Bookstore-Demo-1 ./scripts/smoke.sh`

`.http` files: `src/Bookstore.Api/Bookstore.Api.http` for Visual Studio and the VS Code REST Client (password from
`src/Bookstore.Api/.env`, see `.env.example`); `tests/http/bookstore-flow.http` for Rider and `ijhttp`
(password from `tests/http/http-client.private.env.json`):
`docker run --rm -v "$PWD/tests/http:/workdir" jetbrains/intellij-http-client -D -e dev -v http-client.env.json -p http-client.private.env.json bookstore-flow.http --report`

Postman collection (`postman/`): import it into Postman or run it from the command line:
`npx newman run postman/Bookstore.postman_collection.json -e postman/dev.postman_environment.json --env-var password=Bookstore-Demo-1`
(with the Postman CLI: `postman collection run ...` with the same parameters).

Bruno collection (`bruno/`, OpenCollection YAML): open it in Bruno or run it with the CLI
(password from `bruno/.env`, see `.env.example`):
`cd bruno; npx @usebruno/cli@4.3.0 run --env dev`. The `API tests` GitHub Actions workflow runs the same collection;
it needs the repository secrets `CI_SQL_PASSWORD` (SQL Server sa password) and `BOOKSTORE_PASSWORD` (demo password).

To manage migrations by hand: `dotnet tool restore`, then
`dotnet ef database update --project src/Bookstore.Api`.

### Endpoints

| Method | Path | Description |
|---|---|---|
| POST | `/api/auth/login` | Get a token (`{ "email", "password" }`) |
| GET | `/api/auth/me` | The token's owner (token required) |
| GET | `/api/books?search=&category=&page=&pageSize=` | Catalog list and search |
| GET | `/api/books/{isbn}` | Book details and stock |
| GET | `/api/categories` | Categories |
| PUT | `/api/books/{isbn}/cover` | Upload a cover (multipart, `file` field; JPEG/PNG, at most 2 MB; StoreStaff) |
| GET | `/api/books/{isbn}/cover` | Cover image |
| POST | `/api/carts` | New cart (every request under `/api/carts` and `/api/orders` needs a token) |
| GET | `/api/carts/{cartId}` | Cart |
| PUT | `/api/carts/{cartId}/items/{isbn}` | Add a book / change its quantity (`{ "quantity": 2 }`) |
| DELETE | `/api/carts/{cartId}/items/{isbn}` | Remove a book from the cart |
| POST | `/api/orders` | Place an order (`{ "cartId", "customerName", "customerEmail" }`) |
| GET | `/api/orders/{orderId}` | Order (customers see only their own orders, StoreStaff sees all) |
