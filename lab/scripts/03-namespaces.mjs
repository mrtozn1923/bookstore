// Prints the CSS Tailwind generates for each class in 03/namespaces.html.
import { execFileSync } from "node:child_process";
import { readFileSync } from "node:fs";

const css = execFileSync("node_modules/.bin/tailwindcss", ["-i", "03/namespaces.css"], { stdio: ["ignore", "pipe", "ignore"] }).toString();
const classes = readFileSync("03/namespaces.html", "utf8").match(/class="([^"]+)"/)[1].split(" ");
const escape = (cls) => "." + cls.replace(/^(\d)/, (d) => `\\3${d} `).replace(/([:.\/])/g, "\\$1");
for (const cls of classes) {
    const sel = escape(cls);
    const start = css.indexOf(sel + " {");
    const body = css.slice(start + sel.length + 2, css.indexOf("}", start)).trim().replace(/\s+/g, " ");
    // a variant sits inside an at-rule: show it as well
    const at = css.lastIndexOf("@media", start);
    const media = at > css.lastIndexOf("}", start - 1) - 200 && at > 0 && css.slice(at, start).split("{").length === 2 ? css.slice(at, css.indexOf("{", at)).trim() + " " : "";
    console.log(cls.padEnd(19), "→", start < 0 ? "(not generated)" : media + body);
}
