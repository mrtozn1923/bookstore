const categories = ["science-fiction", "classics", "fantasy"];
console.log(categories.includes("classics")); // true
console.log(categories.indexOf("fantasy"));   // 2
console.log(categories.indexOf("poetry"));    // -1

console.log([NaN].includes(NaN));             // true
console.log([NaN].indexOf(NaN));              // -1
