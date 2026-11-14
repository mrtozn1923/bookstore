async function* newArrivals() {
  yield "Dune";
  yield "Emma";
}

console.log(newArrivals().next() instanceof Promise);

for (const title of newArrivals()) {
  console.log(title);
}
