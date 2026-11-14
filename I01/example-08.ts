type Wrap<T> = T[] extends string[] ? "strings" : "other";
type W1 = Wrap<string | number>; // "other"

type Direct = string | number extends string ? "yes" : "no"; // "no"
