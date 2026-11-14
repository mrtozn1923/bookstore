type Formatter = (value: number) => string;

const toCurrency: Formatter = (value) => `${value.toFixed(2)} TRY`;
const toPercent: Formatter = (value) => `${Math.round(value * 100)}%`;

function formatAll(values: number[], format: Formatter) {
  return values.map(format);
}

console.log(formatAll([0.1, 0.25], toPercent)); // [ '10%', '25%' ]
console.log(toCurrency(450)); // 450.00 TRY
