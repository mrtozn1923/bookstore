# Bookstore

**TR** · Bookstore, [mertozen.com](https://www.mertozen.com) sitesindeki bütün yazı serilerinin ortak örnek uygulamasıdır:
kitap satan bir online kitapçı (bireysel satış + okul/kütüphane/şirketler için kurumsal sipariş). Seriler aynı ürünün
farklı yüzlerini anlatır — veri modeli, Web API, Angular arayüzü, güvenlik, test, deploy, ekip süreçleri.

**EN** · Bookstore is the shared example application of every article series on [mertozen.com](https://www.mertozen.com):
an online bookstore (retail sales + bulk orders for schools, libraries and companies). Each series covers a different
side of the same product — data model, Web API, Angular front end, security, testing, deployment, team process.

## Repo düzeni / Layout

| Ref | İçerik / Contents |
|---|---|
| `main` | Referans uygulama (tanıtım serisi) / the reference application (introduction series) |
| `series/<seri-slug>` | Bir serinin kodu; her yazı bir commit / one series, one commit per article |
| tag `<seri-slug>/NN` | NN numaralı yazı bittiğindeki kodun tam hâli / the code exactly as it is at the end of article NN |

Bir yazının kodunu görmek için / To see an article's code:

```bash
git clone https://github.com/<owner>/bookstore.git
cd bookstore
git switch --detach <seri-slug>/NN      # ör. data-modeling-ve-normalizasyon/06
```

İki yazı arasındaki farkı görmek için / To diff two articles: `git diff <seri-slug>/05 <seri-slug>/06`.

Teknoloji / Stack: .NET 10 (C# 14), ASP.NET Core, EF Core, SQL Server, Angular 22 — konusu farklı olan seriler kendi
araçlarını kullanır (Node.js, Go, PostgreSQL, Redis…). / Series on other topics use their own tools.

Lisans / License: MIT (bkz. / see `LICENSE`).
