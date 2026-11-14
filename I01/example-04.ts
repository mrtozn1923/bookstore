type ElementType<T> = T extends readonly unknown[] ? T[number] : T;

// Error: Type 'null' is not assignable to type '1 | 2'.
const reveal: ElementType<readonly [1, 2]> = null;
