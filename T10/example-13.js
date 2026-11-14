const original = [3, 1, 2];
const sorted = original.sort((a, b) => a - b);
console.log(sorted);   // [1, 2, 3]
console.log(original); // [1, 2, 3]
console.log(sorted === original); // true
