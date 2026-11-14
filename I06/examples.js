// I06: run with `node I06/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
function* bookTitles() {
  console.log("started");
  yield "Dune";
  yield "Emma";
}

const it = bookTitles();
console.log("ready");
console.log(it.next());
console.log([...it]);

// --- example 2 ---
const cart = ["Dune", "Emma"];
const iterator = cart[Symbol.iterator]();

console.log(iterator.next()); // { value: "Dune", done: false }
console.log(iterator.next()); // { value: "Emma", done: false }
console.log(iterator.next()); // { value: undefined, done: true }

// --- example 3 ---
const catalogPages = {
  from: 1,
  to: 5,
  [Symbol.iterator]() {
    let current = this.from;
    const last = this.to;
    return {
      next() {
        if (current <= last) {
          return { value: current++, done: false };
        }
        return { value: undefined, done: true };
      }
    };
  }
};

console.log([...catalogPages]); // [1, 2, 3, 4, 5]
for (const page of catalogPages) console.log(page);

// --- example 4 ---
const catalogPages2 = {
  from: 1,
  to: 5,
  *[Symbol.iterator]() {
    for (let n = this.from; n <= this.to; n++) {
      yield n;
    }
  }
};

console.log([...catalogPages2]); // [1, 2, 3, 4, 5]

// --- example 5 ---
function* inner() {
  yield "b";
  yield "c";
}
function* outer() {
  yield "a";
  yield* inner();
  yield "d";
}
console.log([...outer()]); // ["a", "b", "c", "d"]

function* giftNote() {
  const name = yield "Who is the gift for?";
  yield `Gift note for ${name} added`;
}
const note = giftNote();
console.log(note.next().value); // "Who is the gift for?"
console.log(note.next("Ada").value); // "Gift note for Ada added"

// --- example 6 ---
function* fibonacci() {
  let [a, b] = [0, 1];
  while (true) {
    yield a;
    [a, b] = [b, a + b];
  }
}

for (const n of fibonacci()) {
  if (n > 100) break;
  console.log(n);
}
// 0, 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89

// --- example 7 ---
function* withCleanup() {
  try {
    yield 1;
    yield 2;
  } finally {
    console.log("cleaned up");
  }
}

for (const x of withCleanup()) {
  console.log(x);
  break;
}
// 1
// "cleaned up"

// --- example 8 ---
function* naturals() {
  let n = 1;
  while (true) yield n++;
}

const squares = naturals()
  .filter((n) => n % 2 === 1)
  .map((n) => n * n)
  .take(5)
  .toArray();

console.log(squares); // [1, 9, 25, 49, 81]

// --- example 9 ---
const books = [
  { title: "Dune", price: 450 },
  { title: "Emma", price: 320 },
  { title: "The Hobbit", price: 390 },
  { title: "Middlemarch", price: 380 }
];

const result = books.values()
  .map((book) => {
    console.log("map", book.title);
    return { ...book, price: Math.round(book.price * 0.9) };
  })
  .filter((book) => {
    console.log("filter", book.title);
    return book.price < 300;
  })
  .take(1)
  .toArray();

console.log(result);
// map Dune, filter Dune, map Emma, filter Emma, [{ title: "Emma", price: 288 }]
