function parse(input: string): number;
function parse(input: number): string;
function parse(input: string | number) {
  return typeof input === "string" ? Number(input) : String(input);
}

type Parsed = ReturnType<typeof parse>; // string

console.log(parse("42") + 1); // 43
