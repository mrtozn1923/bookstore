const prices = [450, 90, 1200, 320];

prices.sort((a, b) => a - b);
console.log(prices); // [90, 320, 450, 1200]

prices.sort((a, b) => b - a);
console.log(prices); // [1200, 450, 320, 90]
