function getMinMax(values: number[]): [number, number] {
  return [Math.min(...values), Math.max(...values)];
}

const [low, high] = getMinMax([450, 320, 390, 380, 280, 300]);
console.log(low, high); // 280 450
