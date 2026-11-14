const letters = ["a", "b", "c", "d", "e"];

const removed = letters.splice(1, 2);
console.log(removed); // ["b", "c"]
console.log(letters); // ["a", "d", "e"]

letters.splice(1, 0, "x", "y");
console.log(letters); // ["a", "x", "y", "d", "e"]
