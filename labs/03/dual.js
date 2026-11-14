import { setPrice, getPrice } from "bookstore-pricing/cache";
import { invoiceLine } from "./invoice.cjs";

setPrice("9780441172719", 349.9);
console.log("ESM side:", getPrice("9780441172719"));
console.log("CJS side:", invoiceLine("9780441172719"));
