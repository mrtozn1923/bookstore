type Method<T> = T extends `${infer M} ${string}` ? M : never;
type M1 = Method<"GET /books">; // "GET"

type SplitDot<T> = T extends `${infer Head}.${infer Tail}` ? [Head, Tail] : [T];
type P1 = SplitDot<"order.shipping.city">; // ["order", "shipping.city"]
