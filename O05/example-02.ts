const price = 450;
let stock = 12;

console.log(typeof price); // number

type Price = typeof price; // 450
type Stock = typeof stock; // number
