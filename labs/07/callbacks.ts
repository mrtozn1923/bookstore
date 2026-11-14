import { readFile } from "node:fs";
import { promisify } from "node:util";

// Error-first callback: the first argument is the error, the rest is the result
readFile("missing-catalog.csv", "utf8", (err, text) => {
    if (err) {
        console.log(`callback: ${err.code} (${err.syscall})`);
        return;
    }
    console.log(text.length);
});

// A legacy shipping client that only knows callbacks
function quoteShipping(city: string, weightGrams: number, callback: (err: Error | null, priceTry?: number) => void): void {
    setTimeout(() => {
        if (weightGrams <= 0) {
            callback(new Error(`invalid weight: ${weightGrams}`));
            return;
        }
        callback(null, city === "Istanbul" ? 49.9 : 79.9);
    }, 10);
}

const quoteShippingAsync = promisify(quoteShipping);
console.log(`promisified: ${await quoteShippingAsync("Ankara", 450)} TRY`);
try {
    await quoteShippingAsync("Izmir", 0);
} catch (err) {
    console.log(`promisified rejection: ${(err as Error).message}`);
}
