const last = <T>(items: T[]): T | undefined => items[items.length - 1];

type Mapper = <T, R>(items: T[], fn: (item: T) => R) => R[];

const mapAll: Mapper = (items, fn) => items.map(fn);

const lengths = mapAll(["Dune", "The Hobbit"], (title) => title.length); // number[]
