const picked = new Date(1990, 4, 14); // date picker: local midnight
console.log(JSON.stringify({ birthDate: picked }));
const fromApi = new Date("1990-05-14");
console.log(fromApi.toLocaleDateString("en-CA"));

function toDateOnly(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}
console.log(toDateOnly(picked));
