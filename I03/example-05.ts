type Expand<T> = { [K in keyof T]: T[K] } & {};
type Mutable<T> = { -readonly [K in keyof T]: T[K] };

interface StoreSettings {
  readonly currency: string;
  readonly freeShippingOver?: number;
}

// Error: Type 'null' is not assignable to type '{ currency: string; freeShippingOver?: number | undefined; }'.
const reveal: Expand<Mutable<StoreSettings>> = null;
