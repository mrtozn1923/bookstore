import { readFileSync } from "node:fs";

const titles = readFileSync("data/titles.txt", "utf8").split(/\r?\n/).filter(Boolean);

for (const pattern of ["\\p{Script=Cyrillic}", "\\p{sc=Cyrl}", "^\\p{Lu}\\p{Ll}+$", "\\p{IsCyrillic}"]) {
  try {
    const re = new RegExp(pattern, "u");
    console.log(pattern.padEnd(20), titles.filter((t) => re.test(t)).join(" | "));
  } catch (e) {
    console.log(pattern.padEnd(20), `${e.name}: ${e.message}`);
  }
}
