function isNumber(value: unknown): value is number {
  return typeof value === "string"; // wrong check, the compiler doesn't notice
}

const price: unknown = "450";

if (isNumber(price)) {
  console.log(price.toFixed(2)); // TypeError: price.toFixed is not a function
}
