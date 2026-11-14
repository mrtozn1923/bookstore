type Result<T, E = Error> =
  | { ok: true; value: T }
  | { ok: false; error: E };

function parseQuantity(input: string): Result<number, string> {
  const quantity = Number(input);
  if (Number.isNaN(quantity)) {
    return { ok: false, error: `Not a number: ${input}` };
  }
  return { ok: true, value: quantity };
}

const result = parseQuantity("2");

if (result.ok) {
  console.log(result.value + 1); // 3
} else {
  console.log(result.error.toUpperCase());
}
