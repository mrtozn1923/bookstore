function getRange(values: number[]): [min: number, max: number] {
  return [Math.min(...values), Math.max(...values)];
}

const range = getRange([450, 320, 390]);
console.log(range[0], range[1]); // 320 450
