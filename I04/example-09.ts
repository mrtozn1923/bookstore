interface CartState {
  itemCount: number;
  coupon: string;
}

function createStore<T extends object>(state: T) {
  const listeners: Partial<Record<string, (value: unknown) => void>> = {};
  return {
    on<K extends keyof T & string>(event: `${K}Changed`, callback: (value: T[K]) => void) {
      listeners[event] = callback as (value: unknown) => void;
    },
    set<K extends keyof T & string>(key: K, value: T[K]) {
      state[key] = value;
      listeners[`${key}Changed`]?.(value);
    },
  };
}

const store = createStore<CartState>({ itemCount: 0, coupon: "" });
store.on("itemCountChanged", (count) => console.log(`cart has ${count.toFixed(0)} books`));
store.set("itemCount", 2); // cart has 2 books

// Error: Argument of type '"itemCountChange"' is not assignable to parameter of type '"couponChanged" | "itemCountChanged"'.
store.on("itemCountChange", () => {});
