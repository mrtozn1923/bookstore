import { readFileSync } from "node:fs";

console.log("Total: 1234567 TRY, 950 items".replace(/(?<=\d)(?=(?:\d{3})+\b)/g, "."));
console.log("price: 450 TRY".match(/(?<=price:\s*)\d+/)[0]);

const log = readFileSync("data/app.log", "utf8");
console.log(log.match(/^(?!.* DBG ).*Bookstore\.Payments.*$/gm).join("\n"));

