import { buildPriceReport } from "#services/priceReport";

// The same 20 million synthetic lines, summed in kuruş integers (post 12) vs. as floats (post 06)
const unitPrices = [349.9, 289.5, 310.0, 245.0];
let floatRevenue = 0;
for (let i = 0; i < 20_000_000; i++) {
    floatRevenue += Math.round(((i % 5) + 1) * unitPrices[i % 4]! * 1.1 * 100) / 100;
}
console.log(`float sum (post 06):   ${(Math.round(floatRevenue * 100) / 100).toFixed(2)}`);
console.log(`integer sum (post 12): ${buildPriceReport(20_000_000).revenue.toFixed(2)}`);
