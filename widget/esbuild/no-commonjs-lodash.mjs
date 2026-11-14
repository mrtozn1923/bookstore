// Stops the build if someone imports the CommonJS "lodash" instead of "lodash-es"
export function noCommonJsLodash() {
    return {
        name: "no-commonjs-lodash",
        setup(build) {
            build.onResolve({ filter: /^lodash(\/.*)?$/ }, args => ({
                errors: [{ text: `"${args.path}" is CommonJS and cannot be tree-shaken; import from "lodash-es" instead` }],
            }));
        },
    };
}
