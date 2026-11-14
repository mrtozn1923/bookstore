// T14: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 2 ---
document.documentElement; // the <html> element
document.head;            // the <head> element
document.body;            // the <body> element
document.title;           // the title in the tab

// --- example 4 ---
const h1 = document.querySelector("#title");

h1.textContent = "<em>Weekly</em> Bestsellers";
// Shows up literally: <em>Weekly</em> Bestsellers

h1.innerHTML = "<em>Weekly</em> Bestsellers";
// "Weekly" is italic, the tag was parsed as HTML

// --- example 5 ---
const box = document.querySelector("#title");
const review = '<img src="x" onerror="alert(1)">';

box.innerHTML = review;   // The image fails to load, onerror runs: an alert opens
box.textContent = review; // Shows up as plain text, nothing runs

// --- example 9 ---
const p = document.createElement("p");
p.textContent = "<b>Bold?</b>";
console.log(p.innerHTML);
console.log(document.body.contains(p));
