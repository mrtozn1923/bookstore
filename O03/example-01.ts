class Stack<T> {
  #items: T[] = [];

  push(item: T): void {
    this.#items.push(item);
  }

  pop(): T | undefined {
    return this.#items.pop();
  }
}

const prices = new Stack<number>();
prices.push(450);
prices.push("320");

const anything = new Stack();
anything.push(450);
anything.push("Dune");
const last = anything.pop();
