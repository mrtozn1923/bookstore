function logA(message?: string) {
  console.log(message ?? "(empty)");
}

function logB(message: string | undefined) {
  console.log(message ?? "(empty)");
}

logA();
// Error: Expected 1 arguments, but got 0.
logB();
