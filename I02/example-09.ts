type PropUnion<T> = T extends { a: infer U; b: infer U } ? U : never;
type U1 = PropUnion<{ a: string; b: number }>; // string | number

type ParamBoth<T> = T extends { a: (x: infer U) => void; b: (x: infer U) => void } ? U : never;
type U2 = ParamBoth<{ a: (x: { isbn: string }) => void; b: (x: { quantity: number }) => void }>; // { isbn: string; } & { quantity: number; }
