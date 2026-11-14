async function run() {
  console.log("A");
  await null;
  console.log("B");
}

console.log("C");
run();
console.log("D");

// C, A, D, B
