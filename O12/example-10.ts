// Result, QuantityError and parseQuantity from example-09, which this example uses.
type Result<T, E> =
  | { ok: true; value: T }
  | { ok: false; error: E };

type QuantityError =
  | { kind: "empty" }
  | { kind: "not-a-number"; input: string }
  | { kind: "out-of-range"; value: number };

function parseQuantity(raw: string): Result<number, QuantityError> {
  const text = raw.trim();
  if (text === "") {
    return { ok: false, error: { kind: "empty" } };
  }
  const quantity = Number(text);
  if (!Number.isInteger(quantity)) {
    return { ok: false, error: { kind: "not-a-number", input: text } };
  }
  if (quantity < 1 || quantity > 99) {
    return { ok: false, error: { kind: "out-of-range", value: quantity } };
  }
  return { ok: true, value: quantity };
}

// Error: Property 'value' does not exist on type 'Result<number, QuantityError>'.
console.log(parseQuantity("2").value);
