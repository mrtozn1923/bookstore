type Expand<T> = { [K in keyof T]: T[K] } & {};

interface Book {
  title: string;
  price: number;
}

type Getters<T> = { [K in keyof T as `get${Capitalize<K & string>}`]: () => T[K] };
type BookGetters = Expand<Getters<Book>>;
// { getPrice: () => number; getTitle: () => string; }

interface StoreEvents {
  orderPlaced: { orderId: number };
  cartCleared: undefined;
}

type Handlers<E> = { [K in keyof E as `on${Capitalize<K & string>}`]: (payload: E[K]) => void };
type StoreHandlers = Expand<Handlers<StoreEvents>>;
// { onCartCleared: (payload: undefined) => void; onOrderPlaced: (payload: { orderId: number; }) => void; }
