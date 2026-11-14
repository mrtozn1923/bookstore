"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.parseStockLine = parseStockLine;
// Minimal source file so that `npx tsc` has something to compile (not shown in the post).
function parseStockLine(line) {
    const [isbn, quantity] = line.split(";");
    return { isbn, quantity: Number(quantity) };
}
