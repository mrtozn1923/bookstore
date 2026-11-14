import { setImmediate as yieldToLoop } from "node:timers/promises";
import type { OrderItem } from "#domain/order";
import { toAmount } from "#domain/money";

export interface PriceReport {
    rows: number;
    revenue: number;
    averageUnitPrice: number;
}

// Integer minor units (kuruş), like the catalog since post 12
const unitPricesMinor = [34990, 28950, 31000, 24500];

// A deterministic, synthetic order line for row i (stands in for rows read from the database)
function syntheticItem(i: number): OrderItem {
    return { isbn: `978${String(i % 10_000_000).padStart(10, "0")}`, quantity: (i % 5) + 1, unitPriceMinor: unitPricesMinor[i % 4]! };
}

function addRows(acc: { revenue: number; units: number }, from: number, to: number): void {
    for (let i = from; i < to; i++) {
        const item = syntheticItem(i);
        // 10% VAT per line, rounded to whole kuruş; integer sums stay exact
        acc.revenue += Math.round((item.quantity * item.unitPriceMinor * 110) / 100);
        acc.units += item.quantity;
    }
}

function toReport(rows: number, acc: { revenue: number; units: number }): PriceReport {
    return { rows, revenue: toAmount(acc.revenue), averageUnitPrice: toAmount(Math.round(acc.revenue / acc.units)) };
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
