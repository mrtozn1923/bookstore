// Error: A type predicate's type must be assignable to its parameter's type.
function isId(value: string): value is number {
  return true;
}

const assertArrow = (condition: unknown, message: string): asserts condition => {
  if (!condition) throw new Error(message);
};

function check(value: string | null) {
  // Error: Assertions require every name in the call target to be declared with an explicit type annotation.
  assertArrow(value, "value is required");
}

const assertTyped: (condition: unknown, message: string) => asserts condition = (condition, message) => {
  if (!condition) throw new Error(message);
};

function checkTyped(value: string | null) {
  assertTyped(value, "value is required");
  console.log(value.length);
}
