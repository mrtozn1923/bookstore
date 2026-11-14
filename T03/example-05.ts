function setCurrency(value: "TRY" | "EUR") {
  console.log(`Currency: ${value}`);
}

const currency = "TRY";
let selected = "TRY";

setCurrency(currency);
// Error: Argument of type 'string' is not assignable to parameter of type '"EUR" | "TRY"'.
setCurrency(selected);
