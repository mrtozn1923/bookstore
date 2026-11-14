type Nullable<T> = { [K in keyof T]: T[K] | null };

type Draft = Nullable<{ title: string; subtitle?: string }>;
// { title: string | null; subtitle?: string | null | undefined; }
