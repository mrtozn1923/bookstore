function risky(n: number): void {
  if (n === 1) throw new Error("real error");
  if (n === 2) throw "just a string";
  if (n === 3) throw { code: 42 };
}

function toError(value: unknown): Error {
  if (value instanceof Error) {
    return value;
  }
  return new Error(String(value));
}

for (const n of [1, 2, 3]) {
  try {
    risky(n);
  } catch (error) {
    const err = toError(error);
    console.log(err.message);
  }
}
