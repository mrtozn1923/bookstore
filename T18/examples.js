// T18: run with `node T18/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
const date = new Date(2026, 1, 30);
console.log(date.getMonth(), date.getDate());

// --- example 2 ---
const now = new Date();
console.log(now);           // the current date and time
console.log(now.getTime()); // e.g. 1793700000000
console.log(Date.now());    // the same number without creating an object

console.log(new Date(0).toISOString()); // "1970-01-01T00:00:00.000Z"

// --- example 3 ---
new Date();                         // right now
new Date(1793700000000);            // from a timestamp
new Date(2026, 0, 15);              // January 15, 2026, 00:00, local time
new Date(2026, 0, 15, 14, 30);      // January 15, 2026, 14:30, local time
new Date("2026-01-15T14:30:00");    // an ISO-format string, local time

// --- example 4 ---
new Date("2026-01-15T00:00").getHours(); // 0: with a time, it's local time
new Date("2026-01-15").getHours();       // 3 in Türkiye: a date alone counts as UTC
new Date("15.01.2026");                  // non-standard format: don't rely on it
new Date("hello");                       // Invalid Date

// --- example 5 ---
const months = ["January", "February", "March", "April", "May", "June",
  "July", "August", "September", "October", "November", "December"];

const d = new Date(2026, 4, 19);
console.log(months[d.getMonth()]); // "May"

// --- example 6 ---
const placedAt = new Date(2026, 0, 15, 14, 30, 45); // the moment order 1042 was placed

placedAt.getFullYear(); // 2026
placedAt.getMonth();    // 0  (January)
placedAt.getDate();     // 15 (day of the month)
placedAt.getDay();      // 4  (Thursday; 0 = Sunday)
placedAt.getHours();    // 14
placedAt.getMinutes();  // 30
placedAt.getSeconds();  // 45

// --- example 7 ---
const returnBy = new Date(2026, 0, 15); // order date
returnBy.setDate(returnBy.getDate() + 30);
console.log(returnBy.toLocaleDateString("tr-TR")); // "14.02.2026"

const copy = new Date(returnBy); // an independent copy of the same moment
copy.setFullYear(2027);
console.log(returnBy.getFullYear(), copy.getFullYear()); // 2026 2027

// --- example 9 ---
const orderDate = new Date(2026, 0, 15, 14, 30);

orderDate.toLocaleDateString("tr-TR"); // "15.01.2026"
orderDate.toLocaleDateString("tr-TR", {
  weekday: "long", year: "numeric", month: "long", day: "numeric"
}); // "15 Ocak 2026 Perşembe"
orderDate.toLocaleTimeString("tr-TR"); // "14:30:00"
orderDate.toLocaleDateString("en-US"); // "1/15/2026"
orderDate.toLocaleDateString("de-DE"); // "15.1.2026"

// --- example 10 ---
const longFormat = new Intl.DateTimeFormat("tr-TR", { dateStyle: "long" });
console.log(longFormat.format(orderDate)); // "15 Ocak 2026"

const fullFormat = new Intl.DateTimeFormat("tr-TR", { dateStyle: "full", timeStyle: "short" });
console.log(fullFormat.format(orderDate)); // "15 Ocak 2026 Perşembe 14:30"

console.log(orderDate.toISOString()); // in Türkiye: "2026-01-15T11:30:00.000Z"

// --- example 11 ---
const publisherCall = new Date(2026, 0, 15, 14, 30); // assume it was created in Türkiye

const timeIn = (zone) =>
  new Intl.DateTimeFormat("tr-TR", { timeStyle: "short", timeZone: zone }).format(publisherCall);

console.log(timeIn("Europe/Istanbul")); // "14:30"
console.log(timeIn("Europe/London"));   // "11:30"
console.log(timeIn("Asia/Tokyo"));      // "20:30"

console.log(Intl.DateTimeFormat().resolvedOptions().timeZone); // e.g. "Europe/Istanbul"

// --- example 12 ---
const a = new Date(2026, 0, 31);
a.setMonth(1);
console.log(a.getMonth(), a.getDate());
