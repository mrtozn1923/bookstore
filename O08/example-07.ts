type OrderStatus = "pending" | "shipped" | "cancelled";
type Visible = Exclude<OrderStatus, "canceled">; // compiles, nothing is filtered out

function parse(input: string): number;
function parse(input: number): string;
function parse(input: string | number): string | number {
  return typeof input === "string" ? Number(input) : String(input);
}

type Parsed = ReturnType<typeof parse>; // string
type ParseArgs = Parameters<typeof parse>; // [input: number]

function identity<T>(value: T): T {
  return value;
}

type Id = ReturnType<typeof identity>; // unknown
