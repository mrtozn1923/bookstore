// Minimal source file so that `npx tsc` has something to compile (not shown in the post).
export function parseStockLine(line: string) {
  const [isbn, quantity] = line.split(";");
  return { isbn, quantity: Number(quantity) };
}
