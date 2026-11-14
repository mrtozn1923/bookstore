import { readFileSync } from "node:fs";

const csv = readFileSync("data/authors.csv", "utf8");
const author = /"(?<last>[^",]+), (?<first>[^"]+)"$/gm;

console.log(csv.replace(author, '"$<first> $<last>"'));
console.log(csv.replace(author, (...args) => {
  const { last, first } = args.at(-1);
  return `"${last.toUpperCase()}, ${first}"`;
}));
