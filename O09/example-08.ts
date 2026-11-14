const values = [1, null, 2, undefined, 3];

const numbers = values.filter((v) => v !== null && v !== undefined); // number[]
const shorter = values.filter((v) => v != null); // number[]

const truthy = values.filter(Boolean); // (number | null | undefined)[]
const alsoTruthy = values.filter((v) => !!v); // (number | null | undefined)[]

const explicit = values.filter((v): v is number => typeof v === "number"); // number[]

console.log(numbers.map((n) => n * 10)); // [ 10, 20, 30 ]
