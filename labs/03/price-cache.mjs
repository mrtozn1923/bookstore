// ESM build of the price cache
const prices = new Map();
export const setPrice = (isbn, price) => prices.set(isbn, price);
export const getPrice = (isbn) => prices.get(isbn);
