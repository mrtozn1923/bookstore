export default class Cart {
  #items = [];
  add(item) { this.#items.push(item); }
  get size() { return this.#items.length; }
}
