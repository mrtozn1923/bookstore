interface Book {
  id: number;
  title: string;
  subtitle?: string;
  price: number;
}

type PickByValue<T, V> = { [K in keyof T as T[K] extends V ? K : never]: T[K] };

type NumberFields = PickByValue<Book, number>; // { id: number; price: number; }
