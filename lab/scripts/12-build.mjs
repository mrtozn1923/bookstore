// A small build script with the modern JS API: pkg: importer, source map written next to the CSS.
import { mkdir, writeFile } from "node:fs/promises";
import { dirname, relative } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import * as sass from "sass";

const input = "12/src/app.scss";
const output = "12/out/app.css";

const result = await sass.compileAsync(input, {
    importers: [new sass.NodePackageImporter()],
    style: "compressed",
    sourceMap: true,
    sourceMapIncludeSources: false,
});

console.log(`first source as returned: ${result.sourceMap.sources[0]}`);
// the API returns absolute file: URLs; make them relative to the CSS file
const map = {
    ...result.sourceMap,
    file: "app.css",
    sources: result.sourceMap.sources.map((url) => relative(dirname(output), fileURLToPath(url))),
};

await mkdir(dirname(output), { recursive: true });
await writeFile(output, `${result.css}\n/*# sourceMappingURL=app.css.map */\n`);
await writeFile(`${output}.map`, JSON.stringify(map));
console.log(`${output}: ${result.css.length} bytes, ${result.loadedUrls.length} files loaded`);
console.log(`sources: ${map.sources.join(", ")}`);
