function readSavedCart(text: string): unknown {
  try {
    return JSON.parse(text);
  } catch (error) {
    throw new Error("Saved cart could not be parsed", { cause: error });
  }
}

try {
  readSavedCart("{ broken");
} catch (error) {
  if (error instanceof Error) {
    console.log(error.message);
    console.log(error.cause instanceof SyntaxError);
  }
}
