const m = "2026-11".match(/(?<year>\d{4})-(\d{2})/);
console.log(`1=${m[1]} 2=${m[2]} year=${m.groups.year}`);
console.log("2026-11".replace(/(?<year>\d{4})-(\d{2})/, "$2/$1"));

// duplicate named groups (ES2025): allowed only in different alternatives
const either = /(?<year>\d{4})-\d{2}|\d{2}\/(?<year>\d{4})/;
console.log("2026-11".match(either).groups.year, "11/2026".match(either).groups.year);
try {
  new RegExp("(?<year>\\d{4})-(?<year>\\d{2})");
} catch (e) {
  console.log(`${e.name}: ${e.message}`);
}
