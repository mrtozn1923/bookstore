function firstString(items: string[]): string | undefined {
  return items[0];
}

function firstNumber(items: number[]): number | undefined {
  return items[0];
}

const firstTitle = firstString(["Dune", "Emma"]);
const firstPrice = firstNumber([450, 320]);
