import { formatPrice, toSlug as makeSlug } from "./format.js";
import * as format from "./format.js";

console.log(formatPrice(1160));               // "₺1.160,00"
console.log(makeSlug("Pride and Prejudice")); // "pride-and-prejudice"
console.log(format.formatPrice(40));          // access through the namespace
