type Stringify<T> = { [K in keyof T]: string };

type S1 = Stringify<[number, boolean]>; // [string, string]
type S2 = Stringify<number[]>; // string[]

const pair: Stringify<[number, boolean]> = ["1", "true"];
