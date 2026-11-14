// Temporal types for the order timeline: an exact instant, a calendar date, a duration
const placed = Temporal.Instant.from("2026-11-14T22:30:00Z");
const istanbul = placed.toZonedDateTimeISO("Europe/Istanbul");
console.log(istanbul.toString());

// Months start at 1, not 0 as in Date
const due = Temporal.PlainDate.from({ year: 2026, month: 11, day: 18 });
console.log(due.dayOfWeek, due.toLocaleString("en-GB", { weekday: "long" }));

// Days left until delivery, counted in calendar days
const today = Temporal.PlainDate.from("2026-11-15");
console.log(today.until(due).toString(), today.until(due).days);

// Berlin switches to summer time on 2027-03-28: one day is not always 24 hours
const berlin = Temporal.ZonedDateTime.from("2027-03-27T12:00[Europe/Berlin]");
console.log(berlin.add({ days: 1 }).toString());
console.log(berlin.add({ hours: 24 }).toString());

// Temporal values are immutable and serialize to ISO strings in JSON
console.log(JSON.stringify({ dueDate: due, placedAt: placed }));

// Interop with code that still uses Date
const legacy = new Date("2026-11-14T22:30:00Z");
console.log(legacy.toTemporalInstant().equals(placed));
