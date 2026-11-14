// I16: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 2 ---
const saleStart = Temporal.PlainDate.from("2026-01-31");
const saleEnd = saleStart.add({ months: 1 });

console.log(saleEnd.toString());   // "2026-02-28"
console.log(saleStart.toString()); // "2026-01-31", unchanged
console.log(saleStart.month);      // 1

const today = Temporal.Now.plainDateISO();
console.log(today.add({ days: 1 }).toString()); // tomorrow's date

const first = Temporal.PlainDate.from("2026-01-01");
const last = Temporal.PlainDate.from("2026-03-15");
console.log(first.until(last).toString());                           // "P73D"
console.log(first.until(last, { largestUnit: "month" }).toString()); // "P2M14D"
console.log(Temporal.PlainDate.compare(first, last));                // -1
console.log(saleStart.toLocaleString("tr-TR", { dateStyle: "long" })); // "31 Ocak 2026"

// --- example 3 ---
const berlin = Temporal.ZonedDateTime.from("2026-03-28T12:00[Europe/Berlin]");

console.log(berlin.add({ days: 1 }).toString());
// "2026-03-29T12:00:00+02:00[Europe/Berlin]"
console.log(berlin.add({ hours: 24 }).toString());
// "2026-03-29T13:00:00+02:00[Europe/Berlin]"

console.log(berlin.withTimeZone("Europe/Istanbul").toString());
// "2026-03-28T14:00:00+03:00[Europe/Istanbul]"

// --- example 4 ---
const duration = Temporal.Duration.from({ hours: 1, minutes: 30 });
console.log(duration.toString());                 // "PT1H30M"
console.log(duration.total({ unit: "minutes" })); // 90

const meeting = Temporal.PlainTime.from("09:45");
console.log(meeting.add(duration).toString());    // "11:15:00"

const longDuration = Temporal.Duration.from({ minutes: 150 });
console.log(longDuration.round({ largestUnit: "hours" }).toString()); // "PT2H30M"

// --- example 8 ---
function setup() {
  using stack = new DisposableStack();

  const timer = setInterval(() => console.log("tick"), 1000);
  stack.defer(() => clearInterval(timer));

  const observer = new ResizeObserver(() => console.log("size changed"));
  observer.observe(document.body);
  stack.defer(() => observer.disconnect());

  return stack.move(); // if all went well, cleanup responsibility passes to the caller
}

const cleanup = setup();
// Later, when we're done with this section:
cleanup.dispose();

// --- example 12 ---
const titles = ["Dune", "Emma", "The Hobbit"];
const prices = [450, 320, 390];

for (const [title, price] of Iterator.zip([titles, prices])) {
  console.log(`${title}: ${price} TRY`);
}
