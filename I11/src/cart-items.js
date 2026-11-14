// @ts-check
/** @import { Book } from "./types.ts" */

/**
 * @typedef {object} CartItem
 * @property {Book} book
 * @property {number} quantity
 */

/** @type {CartItem[]} */
const items = [];

/**
 * @param {Book} book
 * @param {number} [quantity]
 */
export function addToCart(book, quantity = 1) {
  items.push({ book, quantity });
}

/**
 * @template T
 * @param {T[]} list
 * @returns {T | undefined}
 */
export function last(list) {
  return list[list.length - 1];
}

addToCart({ isbn: "9780441172719", title: "Dune", price: 450 });
const lastItem = last(items);
console.log(lastItem?.book.title);
addToCart({ isbn: "9780141439587", title: "Emma" }); // Error
