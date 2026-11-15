import { existsSync, readFileSync } from "node:fs";

// Cross-platform "wc -l": how many lines did the export really write?
const file = process.argv[2];
console.log(existsSync(file) ? `${file}: ${readFileSync(file, "utf8").split("\n").length - 1} lines` : `${file}: does not exist`);
