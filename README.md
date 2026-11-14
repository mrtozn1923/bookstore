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

### Kapak testleri (Azurite, OAuth)

Kapak endpoint testleri, Azure'daki gibi Entra ID token'ı ve user delegation SAS ile çalışır. Bunun için Azurite'i
HTTPS ve `--oauth basic` ile başlat; değişken tanımlı değilse bu testler atlanır:

```powershell
openssl req -x509 -newkey rsa:2048 -nodes -keyout .azurite/key.pem -out .azurite/cert.pem -days 30 `
  -subj "/CN=127.0.0.1" -addext "subjectAltName=IP:127.0.0.1,DNS:localhost"
docker run -d --name bookstore-azurite -p 10000:10000 -v "${PWD}/.azurite:/certs:ro" `
  mcr.microsoft.com/azure-storage/azurite azurite-blob --blobHost 0.0.0.0 --skipApiVersionCheck `
  --oauth basic --cert /certs/cert.pem --key /certs/key.pem
$env:AZURITE_OAUTH_BLOB_URI = "https://127.0.0.1:10000/devstoreaccount1"
dotnet test
```


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
| PUT | `/api/books/{isbn}/cover` | Kapak yükle (multipart, `file` alanı; JPEG/PNG/WebP, en fazla 2 MB) |
| GET | `/api/books/{isbn}/cover` | Kapağın kısa ömürlü, salt okunur SAS adresine 302 |

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

### Cover tests (Azurite, OAuth)

The cover endpoint tests use a Microsoft Entra ID token and a user delegation SAS, as on Azure. Start Azurite with
HTTPS and `--oauth basic`; when the variable isn't set, these tests are skipped:

```powershell
openssl req -x509 -newkey rsa:2048 -nodes -keyout .azurite/key.pem -out .azurite/cert.pem -days 30 `
  -subj "/CN=127.0.0.1" -addext "subjectAltName=IP:127.0.0.1,DNS:localhost"
docker run -d --name bookstore-azurite -p 10000:10000 -v "${PWD}/.azurite:/certs:ro" `
  mcr.microsoft.com/azure-storage/azurite azurite-blob --blobHost 0.0.0.0 --skipApiVersionCheck `
  --oauth basic --cert /certs/cert.pem --key /certs/key.pem
$env:AZURITE_OAUTH_BLOB_URI = "https://127.0.0.1:10000/devstoreaccount1"
dotnet test
```


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
| PUT | `/api/books/{isbn}/cover` | Upload a cover (multipart, `file` field; JPEG/PNG/WebP, up to 2 MB) |
| GET | `/api/books/{isbn}/cover` | 302 to a short-lived, read-only SAS URL of the cover |
