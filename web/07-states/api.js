// Stand-in for the Bookstore.Api cart and search endpoints.
// Scenarios come from the query string: ?cart=empty, ?api=fail (cart does not load), ?qty=fail (quantity update fails).
const scenario = new URLSearchParams(location.search);
const catalog = [
  { id: 'dune', title: 'Dune', author: 'Frank Herbert', price: 420 },
  { id: 'emma', title: 'Emma', author: 'Jane Austen', price: 199.9 },
  { id: 'middlemarch', title: 'Middlemarch', author: 'George Eliot', price: 310 },
  { id: 'hobbit', title: 'The Hobbit', author: 'J. R. R. Tolkien', price: 415 },
  { id: 'persuasion', title: 'Persuasion', author: 'Jane Austen', price: 185 },
  { id: 'jane-eyre', title: 'Jane Eyre', author: 'Charlotte Brontë', price: 240 },
];
let cart = scenario.get('cart') === 'empty' ? [] : [
  { ...catalog[0], quantity: 1 },
  { ...catalog[1], quantity: 2 },
];
const wait = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

window.api = {
  catalog,
  async getCart() {
    await wait(1200);
    if (scenario.get('api') === 'fail') throw new Error('503 Service Unavailable');
    return structuredClone(cart);
  },
  async setQuantity(id, quantity) {
    await wait(800);
    if (scenario.get('qty') === 'fail') throw new Error('409 Conflict: not enough stock');
    cart = cart.map((item) => (item.id === id ? { ...item, quantity } : item));
    return structuredClone(cart);
  },
  async removeItem(id) {
    await wait(800);
    cart = cart.filter((item) => item.id !== id);
    return structuredClone(cart);
  },
  async restoreItem(item, index) {
    await wait(800);
    cart.splice(index, 0, item);
    return structuredClone(cart);
  },
  async search(query) {
    await wait(600);
    const q = query.trim().toLowerCase();
    return catalog.filter((book) => `${book.title} ${book.author}`.toLowerCase().includes(q));
  },
};
