import { setImmediate as yieldToLoop } from "node:timers/promises";
import type { OrderItem } from "#domain/order";

export interface PriceReport {
    rows: number;
    revenue: number;
    averageUnitPrice: number;
}

const unitPrices = [349.9, 289.5, 310.0, 245.0];

// A deterministic, synthetic order line for row i (stands in for rows read from the database)
function syntheticItem(i: number): OrderItem {
    return { isbn: `978${String(i % 10_000_000).padStart(10, "0")}`, quantity: (i % 5) + 1, unitPrice: unitPrices[i % 4]! };
}

function addRows(acc: { revenue: number; units: number }, from: number, to: number): void {
    for (let i = from; i < to; i++) {
        const item = syntheticItem(i);
        acc.revenue += Math.round(item.quantity * item.unitPrice * 1.1 * 100) / 100;
        acc.units += item.quantity;
    }
}

function toReport(rows: number, acc: { revenue: number; units: number }): PriceReport {
    return { rows, revenue: Math.round(acc.revenue * 100) / 100, averageUnitPrice: Math.round((acc.revenue / acc.units) * 100) / 100 };
}

// Runs in one go: the event loop is blocked until it returns
export function buildPriceReport(rows: number): PriceReport {
    const acc = { revenue: 0, units: 0 };
    addRows(acc, 0, rows);
    return toReport(rows, acc);
}

// Same work in slices; between slices the event loop gets a turn (check phase)
export async function buildPriceReportChunked(rows: number, chunkSize = 250_000): Promise<PriceReport> {
    const acc = { revenue: 0, units: 0 };
    for (let from = 0; from < rows; from += chunkSize) {
        addRows(acc, from, Math.min(from + chunkSize, rows));
        await yieldToLoop();
    }
    return toReport(rows, acc);
}
