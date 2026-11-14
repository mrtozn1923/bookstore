import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { formatAmount, InvalidAmountError, parseMinorUnits } from "#domain/money";

describe("parseMinorUnits", () => {
    // A table of cases: the counterpart of an xUnit [Theory] with [InlineData]
    const cases: [text: string, expected: number][] = [
        ["349,90", 34990],
        ["349.9", 34990],
        ["349", 34900],
        ["19,99", 1999],
        [" 0,05 ", 5],
    ];
    for (const [text, expected] of cases) {
        it(`parses "${text}" as ${expected}`, () => {
            assert.equal(parseMinorUnits(text), expected);
        });
    }

    for (const text of ["", "abc", "1.234,50", "-5", "1,999"]) {
        it(`rejects "${text}"`, () => {
            assert.throws(() => parseMinorUnits(text), InvalidAmountError);
        });
    }
});

describe("formatAmount", () => {
    it("always writes two decimals", () => {
        assert.equal(formatAmount(34990), "349.90");
        assert.equal(formatAmount(5), "0.05");
        assert.equal(formatAmount(-1999), "-19.99");
    });
});
