interface Book {
  id: number;
  title: string;
  subtitle?: string;
  price: number;
}

type KeysOfType<T, V> = { [K in keyof T]: T[K] extends V ? K : never }[keyof T];

const key: KeysOfType<Book, string> = undefined; // compiles!
