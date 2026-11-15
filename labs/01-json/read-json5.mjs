import { readFileSync } from "node:fs";
import JSON5 from "json5";

const shipping = JSON5.parse(readFileSync("shipping.json5", "utf8"));
console.log(JSON.stringify(shipping, null, 2));
