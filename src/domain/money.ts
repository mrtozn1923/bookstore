// Money as an integer number of minor units (kuruş for TRY, cent for EUR): no binary fractions
export type Currency = "TRY" | "EUR";

export class InvalidAmountError extends Error {
    constructor(text: string) {
        super(`invalid amount: ${text}`);
        this.name = "InvalidAmountError";
    }
}

// "349,90", "349.9" or "349" -> 34990; parsed as text, never through a binary float
export function parseMinorUnits(text: string): number {
    const match = /^(\d{1,9})(?:[.,](\d{1,2}))?$/.exec(text.trim());
    if (!match) {
        throw new InvalidAmountError(text);
    }
    return Number(match[1]) * 100 + Number((match[2] ?? "0").padEnd(2, "0"));
}

// 34990 -> 349.9 for JSON; dividing an integer by 100 gives the nearest double, as decimal(10,2) would
export function toAmount(minorUnits: number): number {
    return minorUnits / 100;
}

// 34990 -> "349.90" for CSV and logs
export function formatAmount(minorUnits: number): string {
    const sign = minorUnits < 0 ? "-" : "";
    const abs = Math.abs(minorUnits);
    return `${sign}${Math.trunc(abs / 100)}.${String(abs % 100).padStart(2, "0")}`;
}
