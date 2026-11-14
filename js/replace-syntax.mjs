const price = "Emma: 45";

console.log(price.replace(/(\d+)/, "$10 TRY"));
console.log(price.replace(/(\d+)/, "$010 TRY"));
console.log(price.replace(/(\d+)/, "$$$1"));
console.log(price.replace(/(\d+)/, "[$&]"));
console.log(price.replace(/(\d+)/, "$2"));
console.log(price.replace(/(?<amount>\d+)/, "$<amount>0 TRY"));
