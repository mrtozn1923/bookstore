import { readFileSync } from "node:fs";
import { parse } from "dotenv";

const keys = ["SHIPPING_NOTE", "SHIPPING_TRACKING_URL", "SHIPPING_TEMPLATE", "SHIPPING_DOUBLE", "SHIPPING_SINGLE", "SHIPPING_SPACES", "SHIPPING_EXPORTED"];

// "builtin": values loaded by node --env-file; "dotenv": the dotenv package's parser
const values = process.argv[2] === "dotenv" ? parse(readFileSync("sample.env")) : process.env;
console.log(JSON.stringify(Object.fromEntries(keys.map(k => [k, values[k]]))));
