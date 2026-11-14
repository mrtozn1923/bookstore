import { readFileSync } from "node:fs";

for (const line of readFileSync("data/descriptions.txt", "utf8").split(/\r?\n/).filter(Boolean)) {
  for (const m of line.matchAll(/\b(?<word>\w+) \k<word>\b/gi)) {
    console.log(`${line.slice(0, 13)} "${m[0]}" at ${m.index}`);
  }
}

console.log("BKS-2026-000123".match(/BKS|BKS-2026/)[0]);
console.log(/^paperback|ebook$/.test("paperback edition"), /^(?:paperback|ebook)$/.test("paperback edition"));
