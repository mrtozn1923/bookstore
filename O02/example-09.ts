function pickFormat<F extends string>(formats: F[], fallback: NoInfer<F>): F {
  return formats[0] ?? fallback;
}

const format = pickFormat(["hardcover", "paperback"], "paperback"); // "hardcover" | "paperback"
// Error: Argument of type '"ebook"' is not assignable to parameter of type '"hardcover" | "paperback"'.
pickFormat(["hardcover", "paperback"], "ebook");
