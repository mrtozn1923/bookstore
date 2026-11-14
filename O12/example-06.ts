class HttpError extends Error {
  readonly status: number;

  constructor(status: number, message: string, options?: ErrorOptions) {
    super(message, options);
    this.name = "HttpError";
    this.status = status;
  }
}

function toError(value: unknown): Error {
  if (value instanceof Error) {
    return value;
  }
  return new Error(String(value));
}

async function loadBook(isbn: string): Promise<string> {
  if (isbn === "") {
    throw "isbn is required";
  }
  if (isbn !== "9780441172719") {
    throw new HttpError(404, `Book ${isbn} not found`);
  }
  return "Dune";
}

async function main() {
  for (const isbn of ["9780441172719", "9780441172710", ""]) {
    try {
      const title = await loadBook(isbn);
      console.log("loaded:", title);
    } catch (error) {
      if (error instanceof HttpError) {
        console.log("http:", error.status, error.message);
      } else {
        const err = toError(error);
        console.log("other:", err.name, err.message);
      }
    }
  }
}

main();
