type Mutable<T> = { -readonly [K in keyof T]: T[K] };
type Concrete<T> = { [K in keyof T]-?: T[K] };
type Frozen<T> = { +readonly [K in keyof T]+?: T[K] };

type C1 = Concrete<{ a?: string | undefined; b?: number }>; // { a: string; b: number; }
type C2 = Frozen<{ a: string }>; // { readonly a?: string | undefined; }
