// O04: run with `node O04/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
const customer = {
  firstName: "Ada",
  greet() {
    return `Hello, I'm ${this.firstName}`;
  }
};

const greet = customer.greet;
console.log(customer.greet());
console.log(greet());

// --- example 2 ---
const admin = { firstName: "Grace" };
admin.greet = customer.greet;
console.log(admin.greet()); // "Hello, I'm Grace"

// --- example 3 ---
const timer = {
  seconds: 0,
  tick() {
    this.seconds++;
    console.log(this.seconds);
  }
};

setTimeout(timer.tick, 100); // NaN

// --- example 4 ---
setTimeout(() => timer.tick(), 100);   // 1
setTimeout(timer.tick.bind(timer), 200); // 2

// --- example 5 ---
const shelf = {
  category: "Classics",
  titles: ["Emma", "Middlemarch"],
  list() {
    return this.titles.map((t) => `${t} - ${this.category}`);
  },
  listBroken() {
    return this.titles.map(function (t) {
      return `${t} - ${this.category}`;
    });
  }
};

console.log(shelf.list());       // ["Emma - Classics", "Middlemarch - Classics"]
console.log(shelf.listBroken()); // ["Emma - undefined", "Middlemarch - undefined"]

// --- example 6 ---
const cartBadge = {
  count: 3,
  show: () => this.count
};
console.log(cartBadge.show()); // undefined

// --- example 7 ---
function introduce(greeting, punctuation) {
  return `${greeting}, I'm ${this.firstName}${punctuation}`;
}

const ada = { firstName: "Ada" };
const alan = { firstName: "Alan" };

console.log(introduce.call(ada, "Hi", "!"));        // "Hi, I'm Ada!"
console.log(introduce.apply(alan, ["Hello", "."])); // "Hello, I'm Alan."

// --- example 8 ---
const introduceAda = introduce.bind(ada);
console.log(introduceAda("Hey", "?"));           // "Hey, I'm Ada?"
console.log(introduceAda.call(alan, "Hi", "!")); // "Hi, I'm Ada!", still Ada

// --- example 9 ---
function addShipping(fee, price) {
  return price + fee;
}

const withStandardShipping = addShipping.bind(null, 40);
console.log(withStandardShipping(320)); // 360
console.log(withStandardShipping(450)); // 490

const sayHiAsAlan = introduce.bind(alan, "Hi");
console.log(sayHiAsAlan("!!")); // "Hi, I'm Alan!!"
