// CommonJS: synchronous require, module.exports and module-scoped variables
const path = require("node:path");

let created = 0;

function createCart(customer) {
    created++;
    return { customer, items: [] };
}

module.exports = { createCart, createdCount: () => created };

console.log("cart.cjs evaluated in", path.basename(__dirname), "as", path.basename(__filename));
