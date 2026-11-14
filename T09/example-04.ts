function printLength(query: string | string[] | null) {
  if (query !== null && typeof query === "object") {
    // query: string[]
    console.log(query.length);
  }
}
