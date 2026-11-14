// Compares the operations of the design contract with the document ASP.NET Core generates
// from the code. An operation in only one of them is drift: a shadow endpoint or a missing one.
import { readFile } from "node:fs/promises";

const methods = ["get", "put", "post", "delete", "options", "head", "patch", "trace", "query"];
const operations = (doc) =>
  new Set(Object.entries(doc.paths ?? {}).flatMap(([path, item]) =>
    methods.filter((m) => item[m]).map((m) => `${m.toUpperCase()} ${path}`)));

// Known differences, each with its reason.
const allowed = new Map([
  ["QUERY /books", "ASP.NET Core 10 generates OpenAPI 3.1.1, which has no query operation"],
]);

const contract = operations(JSON.parse(await readFile("contract/.bundled.json", "utf8")));
const url = process.env.API_URL ?? "http://127.0.0.1:52318";
const generated = operations(await (await fetch(`${url}/openapi/v1.json`)).json());

let drift = 0;
for (const op of [...generated].filter((o) => !contract.has(o)).sort()) {
  console.log(`only in code:     ${op}`);
  drift++;
}
for (const op of [...contract].filter((o) => !generated.has(o)).sort()) {
  if (allowed.has(op)) {
    console.log(`allowed:          ${op} (${allowed.get(op)})`);
    continue;
  }
  console.log(`only in contract: ${op}`);
  drift++;
}
console.log(`${contract.size} operations in the contract, ${generated.size} in the code, ${drift} drifting`);
process.exitCode = drift > 0 ? 1 : 0;
