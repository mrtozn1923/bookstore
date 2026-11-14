async function getStock() {
  return 12;
}

async function main() {
  const stock = await getStock(); // number
  console.log(stock + 1); // 13
}

main();

// Error: The return type of an async function or method must be the global Promise<T> type. Did you mean to write 'Promise<number>'?
async function getCartTotal(): number {
  return 770;
}
