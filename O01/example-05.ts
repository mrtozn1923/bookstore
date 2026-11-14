function echo<T>(value: T): T {
  return value;
}

const a = echo("Dune"); // "Dune"
let b = echo("Dune"); // string
const c = echo<string>("Dune"); // string
const d = echo({ title: "Dune", price: 450 }); // { title: string; price: number; }
