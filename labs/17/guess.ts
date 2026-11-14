// Ada's order page promises delivery three days after the order date, in Istanbul time
const placedAt = "2026-11-14T22:30:00Z";

// Date only knows UTC and the machine's own time zone
const viaDate = new Date(placedAt);
viaDate.setUTCDate(viaDate.getUTCDate() + 3);
console.log("Date:    ", viaDate.toISOString().slice(0, 10));

// Temporal: the time zone is part of the calculation
const viaTemporal = Temporal.Instant.from(placedAt).toZonedDateTimeISO("Europe/Istanbul").toPlainDate().add({ days: 3 });
console.log("Temporal:", viaTemporal.toString());
