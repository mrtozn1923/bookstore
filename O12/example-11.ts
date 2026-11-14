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

function describeError(error: QuantityError): string {
  switch (error.kind) {
    case "empty":
      return "Quantity is required";
    case "not-a-number":
      return `"${error.input}" is not a number`;
    case "out-of-range":
      return `${error.value} is outside 1-99`;
    default: {
      const unreachable: never = error;
      return unreachable;
    }
  }
}

for (const raw of ["2", "", "abc", "150"]) {
  const result = parseQuantity(raw);
  if (result.ok) {
    console.log("quantity:", result.value);
  } else {
    console.log("error:", describeError(result.error));
  }
}
