class Registry<K extends string, V> {
  #entries = new Map<K, V>();

  set(key: K, value: V): void {
    this.#entries.set(key, value);
  }

  get(key: K): V | undefined {
    return this.#entries.get(key);
  }
}

const shippingFees = new Registry<"standard" | "express", number>();
shippingFees.set("express", 60);
console.log(shippingFees.get("express")); // 60
// Error: Argument of type '"overnight"' is not assignable to parameter of type '"express" | "standard"'.
shippingFees.set("overnight", 120);
