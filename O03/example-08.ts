class Registry<K extends string, V> {
  #entries = new Map<K, V>();
}

// Error: Expected 2 type arguments, but got 1.
const fees = new Registry<"standard">();
