# bookstore-web layout

- `src/app/core/`: API services (`CatalogApi`, `OrdersApi`), interceptors and models shared by features
- `src/app/catalog/`, `src/app/cart/`, `src/app/checkout/`, `src/app/orders/`, `src/app/admin/`: one folder per feature
- Each feature exports its routes from `<feature>.routes.ts`; `app.routes.ts` lazy-loads them
- File names are kebab-case without a `.component` suffix: `low-stock-list.ts` holds the class `LowStockList`
- Shared test data lives in `src/testing/` (books: Dune, Emma, The Hobbit)
