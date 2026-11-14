interface StoreSettings {
  readonly currency: string;
  readonly freeShippingOver?: number;
}

type Copy<T> = { [K in keyof T]: T[K] };
type Copied = Copy<StoreSettings>; // { readonly currency: string; readonly freeShippingOver?: number | undefined; }

type ByHand = { [K in "currency" | "freeShippingOver"]: StoreSettings[K] }; // { currency: string; freeShippingOver: number | undefined; }

type CurrencyOnly = Pick<StoreSettings, "currency">; // { readonly currency: string; }
