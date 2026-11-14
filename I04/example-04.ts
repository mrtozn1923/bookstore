type Digit = "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9";

type Pin = `${Digit}${Digit}${Digit}${Digit}`; // 10,000 members, no problem

// Error: Expression produces a union type that is too complex to represent.
type LongPin = `${Digit}${Digit}${Digit}${Digit}${Digit}`;
