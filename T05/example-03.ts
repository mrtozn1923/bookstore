const formats: readonly string[] = ["hardcover", "paperback", "ebook"];

// Error: Property 'push' does not exist on type 'readonly string[]'.
formats.push("audiobook");
// Error: Index signature in type 'readonly string[]' only permits reading.
formats[0] = "audiobook";
