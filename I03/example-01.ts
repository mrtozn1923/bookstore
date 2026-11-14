type Mutable<T> = { -readonly [K in keyof T]: T[K] };

interface StoreSettings {
  readonly currency: string;
  readonly freeShippingOver?: number;
}

const settings: Mutable<StoreSettings> = { currency: "TRY" };
settings.currency = "EUR";
