// V8 14.6 (Node 26): Map.prototype.getOrInsert / getOrInsertComputed and Iterator.concat
interface Line {
    customerId: string;
    isbn: string;
    quantity: number;
}

const lines: Line[] = [
    { customerId: "ada", isbn: "9780441172719", quantity: 3 },
    { customerId: "alan", isbn: "9780547928227", quantity: 1 },
    { customerId: "ada", isbn: "9780547928227", quantity: 1 },
];

// Before: get, check for undefined, set
const before = new Map<string, Line[]>();
for (const line of lines) {
    let list = before.get(line.customerId);
    if (!list) {
        list = [];
        before.set(line.customerId, list);
    }
    list.push(line);
}

// After: one call; the factory runs only when the key is missing
const byCustomer = new Map<string, Line[]>();
for (const line of lines) {
    byCustomer.getOrInsertComputed(line.customerId, () => []).push(line);
}
console.log([...byCustomer].map(([id, l]) => `${id}: ${l.length}`).join(", "));

// getOrInsert takes a ready value: fine for a number, but an array literal would be built on every call
const quantities = new Map<string, number>();
for (const line of lines) {
    quantities.set(line.isbn, quantities.getOrInsert(line.isbn, 0) + line.quantity);
}
console.log(Object.fromEntries(quantities));

// TypeScript 7.0 does not know Iterator.concat yet: declare it until lib catches up
declare global {
    interface IteratorConstructor {
        concat<T>(...iterables: Iterable<T>[]): IteratorObject<T, undefined, unknown>;
    }
}

// Iterator.concat walks several sources lazily, without building a new array
const tryBooks = ["Dune", "The Hobbit"];
const eurBooks = new Set(["Emma", "Middlemarch"]);
const all = Iterator.concat(tryBooks.values(), eurBooks.values());
console.log(all.take(3).toArray());
