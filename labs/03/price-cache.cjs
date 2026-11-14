// CommonJS build of the same price cache (what a dual package ships)
const prices = new Map();
exports.setPrice = (isbn, price) => prices.set(isbn, price);
exports.getPrice = (isbn) => prices.get(isbn);
