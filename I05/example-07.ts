type DeepReadonly<T> =
  T extends (...args: any[]) => unknown ? T :
  T extends object ? { readonly [K in keyof T]: DeepReadonly<T[K]> } :
  T;

interface StoreSettings {
  display: { currency: string; languages: string[] };
  onChange: (value: string) => void;
}

const settings: DeepReadonly<StoreSettings> = {
  display: { currency: "TRY", languages: ["tr", "en"] },
  onChange: (value) => console.log(value),
};

// Error: Cannot assign to 'currency' because it is a read-only property.
settings.display.currency = "EUR";

// Error: Property 'push' does not exist on type 'readonly string[]'.
settings.display.languages.push("de");

settings.onChange("ok"); // ok
