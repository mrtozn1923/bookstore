// Size of the Material theme as one light-dark() stylesheet vs. one stylesheet per theme.
import { gzipSync } from "node:zlib";
import * as sass from "sass";

const options = { style: "compressed", loadPaths: ["../web/node_modules", "../web/src/styles"] };
for (const name of ["material-one", "material-light", "material-dark"]) {
    const css = Buffer.from(sass.compile(`05/${name}.scss`, options).css);
    const vars = css.toString().match(/--mat-sys-[a-z-]+:/g).length;
    console.log(`${name.padEnd(15)} ${String(css.length).padStart(5)} bytes, gzip ${String(gzipSync(css).length).padStart(4)} bytes, ${vars} --mat-sys-* properties`);
}
