function pair<T>(x: T, y: T): [T, T] {
  return [x, y];
}

const p1 = pair(450, 320); // [number, number]
// Error: Argument of type 'string' is not assignable to parameter of type 'number'.
const p2 = pair(450, "320");
const p3 = pair<number | string>(450, "320"); // [string | number, string | number]
