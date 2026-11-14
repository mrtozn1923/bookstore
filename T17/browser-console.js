// T17: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 2 ---
console.log(window.document === document); // true
window.console.log("This works too");

var oldWay = "with var";
let newWay = "with let";
console.log(window.oldWay); // "with var"
console.log(window.newWay); // undefined

// --- example 3 ---
location.href;     // "https://books.example.com/books?category=classics&page=2#reviews"
location.protocol; // "https:"
location.hostname; // "books.example.com"
location.pathname; // "/books"
location.search;   // "?category=classics&page=2"
location.hash;     // "#reviews"

// --- example 4 ---
location.href = "https://developer.mozilla.org"; // goes to that address
location.reload();                               // reloads the page
location.replace("https://developer.mozilla.org"); // goes there but leaves no history entry

// --- example 5 ---
history.back();    // one page back
history.forward(); // one page forward
history.go(-2);    // two pages back
history.length;    // how many entries this tab's history has

// --- example 8 ---
alert("Added to cart.");                                  // just shows a message
const confirmed = confirm("Remove Dune from your cart?"); // true or false
const couponCode = prompt("Coupon code?", "SPRING10");    // the typed string, or null on Cancel

console.log(confirmed, couponCode);
