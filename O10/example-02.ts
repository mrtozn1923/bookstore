function wait(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function checkStock(isbn: string): Promise<number> {
  return new Promise((resolve) => {
    setTimeout(() => resolve(isbn === "9780441172719" ? 12 : 0), 100);
  });
}

const untyped = new Promise((resolve) => resolve(450)); // Promise<unknown>
const typed = new Promise<number>((resolve) => resolve(450)); // Promise<number>

// Error: Argument of type 'string' is not assignable to parameter of type 'number | PromiseLike<number>'.
const wrong = new Promise<number>((resolve) => resolve("450"));
