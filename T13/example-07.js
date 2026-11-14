console.log(JSON.stringify([1, undefined, function () {}, NaN]));
// [1,null,null,null]

console.log(JSON.stringify({ tags: new Set(["classic", "bestseller"]) })); // {"tags":{}}

try {
  JSON.stringify({ id: 10n });
} catch (error) {
  console.log(error.name); // "TypeError"
}
