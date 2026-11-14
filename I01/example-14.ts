function describe(value: string): "text";
function describe(value: number): "number";
function describe(value: string | number): "text" | "number" {
  return typeof value === "string" ? "text" : "number";
}

const kind = describe("Dune"); // "text"
console.log(kind); // text
