import { bind, find, getOrElse, map, none, some, type Option } from "./option.ts";

type Customer = { readonly email: string; readonly address?: { readonly city?: string } };

const ada: Customer = { email: "ada@example.com", address: { city: "Izmir" } };
const alan: Customer = { email: "alan@example.com" };

// Most of the time optional chaining and ?? are enough
for (const customer of [ada, alan]) {
  console.log(customer.email, "->", customer.address?.city?.toUpperCase() ?? "no city");
}

// VIP: the rate isn't decided yet
const rates = new Map<string, number | undefined>([
  ["AUTUMN10", 0.1],
  ["VIP", undefined],
]);
console.log("get:", rates.get("VIP"), rates.get("SPRING5"), rates.get("VIP") === rates.get("SPRING5"));

// Dune is in the catalog but its price isn't set yet
const listPrices = new Map<string, Option<number>>([
  ["9780141439587", some(59.9)],
  ["9780441013593", none],
]);
for (const isbn of ["9780141439587", "9780441013593", "9780000000000"]) {
  const nested = find(listPrices, isbn);
  const flat = bind(nested, (price) => price);
  console.log(isbn, JSON.stringify(nested), getOrElse(map(flat, (p) => p * 0.9), 0).toFixed(2));
}
