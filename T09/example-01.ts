function printLength(query: string | string[] | null) {
  if (typeof query === "object") {
    console.log(query.length);
  }
}
