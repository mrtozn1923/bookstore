# Bookstore — series/threat-modeling-ve-guvenli-gelistirme-sureci

[Türkçe](#türkçe) · [English](#english)

## Türkçe

Bookstore, mertozen.com'daki serilerin ortak örnek uygulamasıdır: kitap satan bir online kitapçı. Bu branch
"Threat Modeling ve Güvenli Geliştirme Süreci" serisinin kodudur; her yazı bir tag'dir (`ssdlc/NN`).
Bu branch'te veritabanı **SQLite**'tır (SQL Server gerekmez); `main`'deki referans uygulama SQL Server kullanır.

- `threat-model/` — OWASP Threat Dragon 2.6 modeli (`bookstore-checkout.json`) ve threat model kuralları.
- `src/Bookstore.Api` — ASP.NET Core 10 Web API (Catalog, Ordering, Inventory module'leri).
- `tests/Bookstore.Api.Tests` — xUnit v3 integration testleri.

```powershell
dotnet test
dotnet run --project src/Bookstore.Api      # http://localhost:5080, bookstore-dev.db oluşturur
```

## English

Bookstore is the online bookstore used as the shared example across mertozen.com's series. This branch holds the
code of the "Threat Modeling and a Secure Development Process" series; every post is a tag (`ssdlc/NN`).
The database on this branch is **SQLite** (no SQL Server needed); the reference app on `main` uses SQL Server.

- `threat-model/` — the OWASP Threat Dragon 2.6 model (`bookstore-checkout.json`) and the threat model rules.
- `src/Bookstore.Api` — ASP.NET Core 10 Web API (Catalog, Ordering and Inventory modules).
- `tests/Bookstore.Api.Tests` — xUnit v3 integration tests.

```powershell
dotnet test
dotnet run --project src/Bookstore.Api      # http://localhost:5080, creates bookstore-dev.db
```
