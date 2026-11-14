function readQuantity(raw: string): number {
  try {
    const quantity = Number(raw);
    if (!Number.isInteger(quantity)) {
      throw new Error(`Invalid quantity: ${raw}`);
    }
    return quantity;
  } catch (error) {
    console.log(error.message);
    return 1;
  }
}

console.log(readQuantity("2"));
