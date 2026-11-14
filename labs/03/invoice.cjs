// A CommonJS dependency that reads prices from the cache
const { getPrice } = require("bookstore-pricing/cache");
exports.invoiceLine = (isbn) => `${isbn}: ${getPrice(isbn) ?? "price missing"}`;
