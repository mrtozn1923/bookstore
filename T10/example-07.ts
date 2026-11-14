function parse(input: string): number;
function parse(input: string[]): number[];
function parse(input: string | string[]) {
  return Array.isArray(input) ? input.map(Number) : Number(input);
}

const single = parse("450"); // single: number
const many = parse(["450", "320"]); // many: number[]
