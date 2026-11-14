function parseQuantity(value: string): number {
  return Number(value);
}

// @ts-expect-error legacy call, fixed once the cart module moves
const legacy = parseQuantity(2);

// @ts-ignore
const hidden = parseQuantity(3);

// @ts-expect-error
const fine = parseQuantity("2");
