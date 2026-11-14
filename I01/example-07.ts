type ToArray<T> = T extends unknown ? T[] : never;

type A1 = ToArray<string | number>; // string[] | number[]
type A2 = ToArray<boolean>; // false[] | true[]
