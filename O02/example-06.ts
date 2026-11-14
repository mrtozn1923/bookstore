function createList<T = string>(): T[] {
  return [];
}

const titles = createList(); // string[]
const prices = createList<number>(); // number[]
