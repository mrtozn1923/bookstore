function formatStock(stock: number | undefined) {
  if (!stock) {
    return "no data";
  }
  return `${stock} in stock`;
}

console.log(formatStock(0)); // no data
