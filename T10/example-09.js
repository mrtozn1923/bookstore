const classics = ["Emma", "Middlemarch"];
const sciFi = ["Dune", "Frankenstein"];

console.log(classics.concat(sciFi));  // ["Emma", "Middlemarch", "Dune", "Frankenstein"]
console.log([...classics, ...sciFi]); // same result
console.log(["Pride and Prejudice", ...classics, "Frankenstein"]);
// ["Pride and Prejudice", "Emma", "Middlemarch", "Frankenstein"]

const copy = [...classics];
copy.push("Pride and Prejudice");
console.log(classics); // ["Emma", "Middlemarch"]
console.log(copy);     // ["Emma", "Middlemarch", "Pride and Prejudice"]
