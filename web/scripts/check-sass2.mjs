// Compiles every non-partial .scss file with the project's own Sass and makes the
// deprecations that become errors in Dart Sass 2.0 fatal. Unlike the Angular build,
// warnings from files found through load paths are not hidden (no quietDeps).
import { readdirSync } from "node:fs";
import { join } from "node:path";
import * as sass from "sass";

const files = readdirSync("src", { recursive: true })
    .filter((f) => f.endsWith(".scss") && !f.split(/[\\/]/).pop().startsWith("_"))
    .map((f) => join("src", f));

const counts = new Map();
let failed = 0;
for (const file of files) {
    try {
        sass.compile(file, {
            loadPaths: ["src/styles", "node_modules"],
            fatalDeprecations: [sass.deprecations.dartSass2],
            logger: {
                warn(message, { deprecation, deprecationType }) {
                    const id = deprecation ? deprecationType.id : "warning";
                    counts.set(id, (counts.get(id) ?? 0) + 1);
                },
            },
        });
    } catch (error) {
        failed++;
        console.log(`FAIL ${file}\n${error.message.split("\n")[0]}\n  ${error.span?.url?.pathname.split("/src/").pop() ?? ""}:${(error.span?.start.line ?? 0) + 1}`);
    }
}
console.log(`${files.length} files, ${failed} failed, warnings: ${JSON.stringify(Object.fromEntries(counts))}`);
process.exitCode = failed ? 1 : 0;
