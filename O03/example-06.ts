class Stack<T> {
  #items: T[];

  constructor(initial: T[] = []) {
    this.#items = [...initial];
  }

  push(item: T): void {
    this.#items.push(item);
  }

  peek(): T | undefined {
    return this.#items.at(-1);
  }

  get size(): number {
    return this.#items.length;
  }
}

const recent = new Stack(["Dune", "Emma"]); // Stack<string>
const prices = new Stack<number>(); // Stack<number>
recent.push("The Hobbit");
console.log(recent.peek(), recent.size); // The Hobbit 3
