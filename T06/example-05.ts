const stock: { [isbn: string]: number } = {
  "9780441172719": 12,
  "9780141439587": 8,
};

stock["9780547928227"] = 5;

const count = stock["9780000000000"];
// const count: number
console.log(count.toFixed(0));
