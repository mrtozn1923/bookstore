import { readFile, writeFile } from "node:fs/promises";
import path from "node:path";

// esbuild does not process HTML: after the build, write the hashed file names into the template
export function htmlPlugin({ template, entry, liveReload = false }) {
    return {
        name: "html",
        setup(build) {
            build.onEnd(async result => {
                if (result.errors.length > 0) {
                    return;
                }
                const outdir = build.initialOptions.outdir;
                const [file, output] = Object.entries(result.metafile.outputs)
                    .find(([, output]) => output.entryPoint === entry);
                const href = name => "./" + path.relative(outdir, name).replaceAll("\\", "/");
                const css = output.cssBundle ? `    <link rel="stylesheet" href="${href(output.cssBundle)}">\n` : "";
                const scripts = [
                    `<script type="module" src="${href(file)}"></script>`,
                    liveReload ? `<script type="module">new EventSource("/esbuild").addEventListener("change", () => location.reload());</script>` : "",
                ].filter(Boolean).join("\n    ");
                const html = (await readFile(template, "utf8"))
                    .replace("</head>", `${css}</head>`)
                    .replace(/<script type="module" src="[^"]+"><\/script>/, scripts);
                await writeFile(path.join(outdir, "index.html"), html);
            });
        },
    };
}
