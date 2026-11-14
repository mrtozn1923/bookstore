// Prints the layer statement and the first characters of every layer block in the built global CSS.
import { readdirSync, readFileSync } from "node:fs";

const dir = "../web/dist/bookstore-web/browser";
const css = readFileSync(`${dir}/${readdirSync(dir).find((f) => /^styles-.*\.css$/.test(f))}`, "utf8");
console.log(css.match(/@layer [a-z,]+;/)[0]);
for (const m of css.matchAll(/@layer ([a-z]+)\{/g)) {
    console.log(`${m[1]} -> ${css.slice(m.index + m[0].length, m.index + m[0].length + 90)}`);
}
