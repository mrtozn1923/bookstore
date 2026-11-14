type ToArrayAll<T> = [T] extends [unknown] ? T[] : never;
type A3 = ToArrayAll<string | number>; // (string | number)[]

type IsNever<T> = [T] extends [never] ? true : false;
type N1 = IsNever<never>; // true
type N2 = IsNever<string>; // false

type IsNeverWrong<T> = T extends never ? true : false;
type N3 = IsNeverWrong<never>; // never
