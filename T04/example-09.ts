function trimSafe(input: unknown) {
  if (typeof input === "string") {
    return input.trim();
  }
  return "";
}

console.log(`[${trimSafe("  9780441172719  ")}]`); // [9780441172719]
console.log(`[${trimSafe(9780441172719)}]`); // []
