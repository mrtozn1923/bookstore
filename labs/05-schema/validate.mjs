import { readFileSync } from "node:fs";
import Ajv2020 from "ajv/dist/2020.js";
import addFormats from "ajv-formats";
import JSON5 from "json5";

const [schemaPath, dataPath] = process.argv.slice(2);
const schema = JSON.parse(readFileSync(schemaPath, "utf8"));
// appsettings files are JSONC; JSON5 is a superset, so its parser reads them
const data = JSON5.parse(readFileSync(dataPath, "utf8"));

const ajv = new Ajv2020({ allErrors: true });
addFormats(ajv);
const validate = ajv.compile(schema);

if (validate(data)) {
  console.log(`${dataPath}: valid`);
} else {
  for (const error of validate.errors) {
    console.log(`${dataPath}: ${error.instancePath} ${error.message}`);
  }
  process.exitCode = 1;
}
