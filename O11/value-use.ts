import type { formatBook } from "./book.ts";

// Error: 'formatBook' cannot be used as a value because it was imported using 'import type'.
formatBook({ title: "Dune", price: 450 });

// typeof stays in the type world, no problem
type Formatter = typeof formatBook;
