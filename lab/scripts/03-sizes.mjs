// Compressed CSS size of the mixin and extend versions, raw and gzipped.
import { gzipSync } from "node:zlib";
import * as sass from "sass";

for (const name of ["surface-mixin", "surface-extend"]) {
    const css = Buffer.from(sass.compile(`03/${name}.scss`, { style: "compressed" }).css);
    const gz = gzipSync(css, { level: 9 }).length;
    console.log(`${name.padEnd(16)} ${String(css.length).padStart(5)} bytes, gzip ${String(gz).padStart(4)} bytes`);
}
