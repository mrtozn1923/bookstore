function echoAny(value: any): any {
  return value;
}

function echo<T>(value: T): T {
  return value;
}

const a = echoAny(450);
const b = echo(450);

console.log(a.toUpperCase());
console.log(b.toUpperCase());
