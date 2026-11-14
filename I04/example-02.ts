type Name = "Ada";
type Greeting = `Hello, ${Name}!`; // "Hello, Ada!"

type Flag = `${boolean}`; // "false" | "true"
type Mixed = `${null}-${undefined}-${1n}`; // "null-undefined-1"
