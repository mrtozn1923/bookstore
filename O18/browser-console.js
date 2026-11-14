// O18: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 11 ---
Temporal.PlainDate.from("2026-01-31").add({ months: 1 }).toString();
// "2026-02-28" (no overflow, last day of the month)

Temporal.ZonedDateTime.from("2026-11-03T09:30[Europe/Istanbul]")
  .withTimeZone("America/New_York")
  .toString();
// "2026-11-03T01:30:00-05:00[America/New_York]"

Temporal.PlainDate.from("2026-11-03")
  .until("2026-12-25", { largestUnit: "months" })
  .toString();
// "P1M22D" (1 month 22 days)
