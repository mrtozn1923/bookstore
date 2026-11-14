import { fileURLToPath } from "node:url";
import * as sass from "sass";
import sassTrue from "sass-true";
import { describe, expect, it } from "vitest";

const root = fileURLToPath(new URL("..", import.meta.url));

// every *.test.scss file becomes a describe/it tree in Vitest
for (const name of ["units", "colors", "breakpoints", "config"]) {
    sassTrue.runSass({ describe, it, sass }, fileURLToPath(new URL(`./${name}.test.scss`, import.meta.url)));
}

// @error stops compilation, so errors and warnings are tested from JavaScript
function compile(source) {
    const warnings = [];
    const result = sass.compileString(`@use "index" as bs;\n${source}`, {
        loadPaths: [root],
        logger: { warn: (message) => warnings.push(message) },
    });
    return { css: result.css, warnings };
}

describe("errors and warnings", () => {
    it("rejects a space() step outside the scale", () => {
        expect(() => compile(".x { margin: bs.space(6); }")).toThrow("space() expects a step from 0 to 5, got 6");
    });

    it("rejects a string where rem() expects a number", () => {
        expect(() => compile('.x { font-size: bs.rem("18px"); }')).toThrow('rem() expects a number, got "18px" (string)');
    });

    it("warns when the deprecated from() is used", () => {
        const { css, warnings } = compile('.x { @include bs.from("md") { order: 1; } }');
        expect(warnings).toEqual(['from() is deprecated and will be removed in @bookstore/styles 1.0. Use up("md") instead.']);
        expect(css).toContain("@media (width >= 48rem)");
    });
});
