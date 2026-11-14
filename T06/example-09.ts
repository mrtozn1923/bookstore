const prices: Record<string, number> = { "9780441172719": 450 };

// Error: Property 'EUR' is missing in type '{ TRY: string; }' but required in type 'Record<"EUR" | "TRY", string>'.
const currencyLabels: Record<"TRY" | "EUR", string> = {
  TRY: "Turkish lira",
};
