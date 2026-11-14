function parse(input: string): number;
function parse(input: string[]): number[];
function parse(input: string | string[]) {
  return Array.isArray(input) ? input.map(Number) : Number(input);
}

function parseAny(input: string | string[]) {
  // Error: No overload matches this call.
  return parse(input);
}
