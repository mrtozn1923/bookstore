type DeepPartial<T> =
  T extends (...args: any[]) => unknown ? T :
  T extends readonly unknown[] ? T :
  T extends object ? { [K in keyof T]?: DeepPartial<T[K]> } :
  T;

interface StoreSettings {
  display: { currency: string; languages: string[] };
  onChange: (value: string) => void;
}

const current: StoreSettings = {
  display: { currency: "TRY", languages: ["tr", "en"] },
  onChange: (value) => console.log(value),
};

function applyPatch(patch: DeepPartial<StoreSettings>): StoreSettings {
  return { ...current, ...patch, display: { ...current.display, ...patch.display } };
}

const next = applyPatch({ display: { currency: "EUR" } });
console.log(next.display.currency, next.display.languages); // EUR [ 'tr', 'en' ]
