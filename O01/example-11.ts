// Unnecessary: T appears only once
function printValue<T>(value: T): void {
  console.log(value);
}

// Does the same job
function printAnything(value: unknown): void {
  console.log(value);
}
