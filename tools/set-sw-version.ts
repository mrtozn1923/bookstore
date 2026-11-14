// "Deploys" a new version of the service worker lab: node tools/set-sw-version.ts v2
import { readFileSync, writeFileSync } from "node:fs";

const version = process.argv[2];
if (!/^v\d+$/.test(version ?? "")) throw new Error("usage: node tools/set-sw-version.ts v2");
for (const file of ["sw.js", "app.js", "chunk.js"]) {
  const path = new URL(`../web/sw/${file}`, import.meta.url);
  writeFileSync(path, readFileSync(path, "utf8").replace(/const VERSION = "v\d+";/, `const VERSION = "${version}";`));
}
console.log(`web/sw is now ${version}`);
