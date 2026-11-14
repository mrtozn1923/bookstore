function pickFormat<F extends string>(formats: F[], fallback: F): F {
  return formats[0] ?? fallback;
}

const format = pickFormat(["hardcover", "paperback"], "ebook"); // "ebook" | "hardcover" | "paperback"
