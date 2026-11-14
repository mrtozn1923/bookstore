console.log("a".length);   // 1
console.log("😀".length);  // 2

let count = 0;
for (const char of "😀") {
  count++;
}
console.log(count);        // 1
