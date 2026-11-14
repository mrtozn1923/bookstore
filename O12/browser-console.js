// O12: examples that need a browser; paste these blocks into the DevTools console one by one
// (if a block uses a function from examples.js, paste that file first)

// --- example 4 ---
const card = document.querySelector(".card");

console.log(card.dataset.id);        // "7"
card.dataset.status = "sold-out";    // adds data-status="sold-out" to the HTML
card.dataset.lastViewed = "today";   // data-last-viewed="today"
delete card.dataset.status;          // removes the attribute

console.log(Number(card.dataset.id) + 1); // 8

// --- example 6 ---
const list = document.querySelector("#list");

list.insertAdjacentHTML("afterbegin", "<li>At the start</li>");
list.insertAdjacentHTML("beforeend", "<li>At the end</li>");
list.insertAdjacentHTML("beforebegin", "<h3>List heading</h3>");
list.insertAdjacentHTML("afterend", "<p>Below the list</p>");

// --- example 8 ---
const box = document.querySelector("#books");
const rect = box.getBoundingClientRect();

console.log(rect.width, rect.height); // size in pixels
console.log(rect.top, rect.left);     // position relative to the top-left of the visible area

const pageTop = rect.top + window.scrollY; // position relative to the top of the page

const isVisible = rect.top < window.innerHeight && rect.bottom > 0;
console.log(isVisible);

// --- example 9 ---
const items = document.querySelectorAll(".book");

// Bad: write, then read, on every iteration. Each read triggers a reflow.
for (const item of items) {
  item.style.height = "40px";
  console.log(item.offsetHeight);
}

// Good: read everything first, then write everything.
const heights = [...items].map((item) => item.offsetHeight);
items.forEach((item, i) => {
  item.style.height = `${heights[i] + 10}px`;
});

// --- example 10 ---
const div = document.createElement("div");
div.dataset.count = 5;
div.dataset.count += 1;
div.dataset.customerName = "Ada";

console.log(div.dataset.count);
console.log(div.outerHTML);
