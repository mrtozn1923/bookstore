type DeepReadonlyNaive<T> = { readonly [K in keyof T]: DeepReadonlyNaive<T[K]> };

declare const handler: DeepReadonlyNaive<() => void>;

// Error: This expression is not callable.
handler();
