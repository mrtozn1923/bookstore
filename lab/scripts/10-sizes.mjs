// Rule count and size (compressed, then gzip) of the generated utility layers.
import { gzipSync } from "node:zlib";
import * as sass from "sass";

for (const name of ["utilities-trimmed", "utilities-everything", "utilities-large-scale"]) {
    const { css } = sass.compile(`10/${name}.scss`, { style: "compressed" });
    const rules = css.match(/\{[^{}]*\}/g).length;
    console.log(`${name.padEnd(21)} ${String(rules).padStart(4)} rules ${String(css.length).padStart(6)} bytes, gzip ${gzipSync(css).length} bytes`);
}
