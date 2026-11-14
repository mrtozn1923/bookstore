function risky(n: number): void {
  if (n === 2) throw "just a string";
}

try {
  risky(2);
} catch (error: unknown) {
  if (typeof error === "string") {
    console.log(error.toUpperCase());
  }
}

try {
  risky(2);
  // Error: Catch clause variable type annotation must be 'any' or 'unknown' if specified.
} catch (error: Error) {
  console.log(error.message);
}
