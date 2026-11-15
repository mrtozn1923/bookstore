import { readFileSync } from "node:fs";
import * as jsYaml from "js-yaml";
import { parse } from "yaml";

const text = readFileSync(process.argv[2], "utf8");

// merge keys are a YAML 1.1 feature; both libraries leave them off by default
console.log("js-yaml        ", JSON.stringify(jsYaml.load(text).shipping));
console.log("js-yaml (1.1)  ", JSON.stringify(jsYaml.load(text, { schema: jsYaml.YAML11_SCHEMA }).shipping));
console.log("yaml           ", JSON.stringify(parse(text).shipping));
console.log("yaml (merge)   ", JSON.stringify(parse(text, { merge: true }).shipping));
