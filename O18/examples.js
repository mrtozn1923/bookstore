// O18: run with `node O18/examples.js` (or paste the blocks into the browser console in order)
// The daylight saving example (block 9) needs a New York time zone: TZ=America/New_York node O18/examples.js

// --- example 1 ---
const a = new Date("2026-11-03");
const b = new Date("2026-11-03T00:00");

console.log(a.getTime() === b.getTime());
console.log(a.toISOString());
console.log(b.toISOString());

// --- example 2 ---
const orderPlacedAt = new Date(Date.UTC(2026, 10, 3, 19, 0)); // November 3, 19:00 UTC

orderPlacedAt.getTime();           // the same moment, a single number
orderPlacedAt.getUTCHours();       // 19
orderPlacedAt.getHours();          // 22 in Istanbul
orderPlacedAt.getTimezoneOffset(); // -180 in Istanbul

// --- example 3 ---
// When run in Istanbul
a.getTime() === b.getTime(); // false
a.toISOString();             // "2026-11-03T00:00:00.000Z"
b.toISOString();             // "2026-11-02T21:00:00.000Z"

// --- example 4 ---
const date = new Date(Date.UTC(2026, 10, 3, 9, 30));

new Intl.DateTimeFormat("en-US", { dateStyle: "full", timeStyle: "short" }).format(date);
// "Tuesday, November 3, 2026 at 12:30 PM" (in Istanbul)

new Intl.DateTimeFormat("en-US", {
  weekday: "long", day: "numeric", month: "long", year: "numeric"
}).format(date);
// "Tuesday, November 3, 2026"

// Throws on purpose (see the post); uncomment to see the TypeError:
// new Intl.DateTimeFormat("en-US", { dateStyle: "long", hour: "2-digit" });
// TypeError: dateStyle can't be combined with individual part options

// --- example 5 ---
const fmt = new Intl.DateTimeFormat("en-US", { day: "numeric", month: "long", year: "numeric" });

fmt.formatToParts(date);
// [{ type: "month", value: "November" }, { type: "literal", value: " " }, { type: "day", value: "3" }, ...]

fmt.formatRange(new Date(2026, 10, 3), new Date(2026, 10, 7));
// "November 3 – 7, 2026"

Intl.DateTimeFormat().resolvedOptions().timeZone;
// "Europe/Istanbul"

// --- example 6 ---
const publisherCall = new Date("2026-11-03T09:30:00Z");
const zones = ["Europe/Istanbul", "America/New_York", "Asia/Tokyo"];

for (const timeZone of zones) {
  const text = new Intl.DateTimeFormat("en-US", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone
  }).format(publisherCall);
  console.log(timeZone, text);
}
// Europe/Istanbul Nov 3, 2026, 12:30 PM
// America/New_York Nov 3, 2026, 4:30 AM
// Asia/Tokyo Nov 3, 2026, 6:30 PM

// --- example 7 ---
const rtf = new Intl.RelativeTimeFormat("en", { numeric: "auto" });

rtf.format(-1, "day");     // "yesterday"
rtf.format(1, "day");      // "tomorrow"
rtf.format(-3, "day");     // "3 days ago"
rtf.format(2, "hour");     // "in 2 hours"
rtf.format(-1, "week");    // "last week"

// --- example 8 ---
const rtfAuto = new Intl.RelativeTimeFormat("en", { numeric: "auto" });
const units = [
  ["year", 60 * 60 * 24 * 365],
  ["month", 60 * 60 * 24 * 30],
  ["week", 60 * 60 * 24 * 7],
  ["day", 60 * 60 * 24],
  ["hour", 60 * 60],
  ["minute", 60],
  ["second", 1]
];

function timeAgo(date, now = new Date()) {
  const seconds = Math.round((date - now) / 1000);
  for (const [unit, size] of units) {
    if (Math.abs(seconds) >= size || unit === "second") {
      return rtfAuto.format(Math.round(seconds / size), unit);
    }
  }
}

console.log(timeAgo(new Date(Date.now() - 5 * 60 * 1000)));        // "5 minutes ago"
console.log(timeAgo(new Date(Date.now() - 3 * 24 * 60 * 60 * 1000))); // "3 days ago"

// --- example 9 ---
// With the browser in America/New_York (DST starts on March 8, 2026)
const start = new Date(2026, 2, 7);
const end = new Date(2026, 2, 9);
console.log((end - start) / 86400000); // 1.9583333333333333

// Adding a day by adding 24 hours
const day = new Date(2026, 2, 8);
console.log(new Date(day.getTime() + 86400000).toString()); // March 9, 01:00, not midnight

// Adding a calendar day
day.setDate(day.getDate() + 1);
console.log(day.toString()); // March 9, 00:00

// --- example 10 ---
function daysBetween(a, b) {
  const utcA = Date.UTC(a.getFullYear(), a.getMonth(), a.getDate());
  const utcB = Date.UTC(b.getFullYear(), b.getMonth(), b.getDate());
  return Math.round((utcB - utcA) / 86400000);
}

daysBetween(new Date(2026, 2, 7), new Date(2026, 2, 9)); // 2, in every time zone
