// require(esm): synchronous ESM can be required, ESM with top-level await cannot
const price = require("./price.js");
console.log("price.js via require:", price.formatPrice(289.5));
console.log("require_module:", process.features.require_module);

try {
    require("./exchange-rates.js");
} catch (err) {
    console.log(err.code);
    console.log(err.message.split("\n")[0]);
}
