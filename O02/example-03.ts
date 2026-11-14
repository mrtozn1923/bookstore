function getLength<T extends { length: number }>(value: T): number {
  return value.length;
}

getLength("Dune"); // 4
getLength([450, 320, 390]); // 3
getLength({ length: 2, label: "cart" }); // 2
// Error: Argument of type 'number' is not assignable to parameter of type '{ length: number; }'.
getLength(450);
