// @ts-check

/**
 * @param {number} price
 * @param {number} quantity
 * @returns {number}
 */
function lineTotal(price, quantity) {
  return price * quantity;
}

const input = document.querySelector("input");
const total = lineTotal(input.value, 2);
console.log(total.toFixed(2));
