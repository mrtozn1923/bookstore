function printId(id: string | number) {
  console.log(`ID: ${id}`);
}

printId(1042);
printId("order_1042");
// Error: Argument of type 'boolean' is not assignable to parameter of type 'string | number'.
printId(true);
