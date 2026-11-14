type Json = string | number | boolean | null | Json[] | { [key: string]: Json };

function countLeaves(value: Json): number {
  if (value === null || typeof value !== "object") {
    return 1;
  }
  const children = Array.isArray(value) ? value : Object.values(value);
  let total = 0;
  for (const child of children) {
    total += countLeaves(child);
  }
  return total;
}

const savedOrder: Json = JSON.parse('{"customer":"Ada","items":["Dune","Emma"],"shipping":{"express":true}}');
console.log(countLeaves(savedOrder)); // 4
