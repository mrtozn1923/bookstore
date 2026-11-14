import webpack from "webpack";

const { Compilation, sources } = webpack;

// Writes asset-manifest.json: which files each entrypoint needs
export class AssetManifestPlugin {
    apply(compiler) {
        compiler.hooks.compile.tap("AssetManifestPlugin", () => console.log("[manifest] compile"));
        compiler.hooks.thisCompilation.tap("AssetManifestPlugin", compilation => {
            compilation.hooks.processAssets.tap(
                { name: "AssetManifestPlugin", stage: Compilation.PROCESS_ASSETS_STAGE_SUMMARIZE },
                () => {
                    const manifest = {};
                    for (const [name, entrypoint] of compilation.entrypoints) {
                        const files = entrypoint.getFiles().filter(file => !file.endsWith(".map"));
                        if (files.length > 0) {
                            manifest[name] = files;
                        }
                    }
                    const json = JSON.stringify(manifest, null, 2);
                    compilation.emitAsset("asset-manifest.json", new sources.RawSource(json));
                    console.log("[manifest] processAssets: asset-manifest.json");
                });
        });
        compiler.hooks.done.tap("AssetManifestPlugin", stats => console.log(`[manifest] done in ${stats.endTime - stats.startTime} ms`));
    }
}
