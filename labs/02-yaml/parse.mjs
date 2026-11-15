import { readFileSync } from "node:fs";
import * as jsYaml from "js-yaml";
import { parse } from "yaml";

const text = readFileSync(process.argv[2], "utf8");

console.log("js-yaml 5.5.0  ", JSON.stringify(jsYaml.load(text)));
console.log("yaml 2.9.1     ", JSON.stringify(parse(text)));
// the same library in YAML 1.1 mode
console.log("yaml (1.1)     ", JSON.stringify(parse(text, { version: "1.1" })));
