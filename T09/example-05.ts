function printLength(query: string | string[] | null) {
  if (query) {
    // query: string | string[]
    console.log(query.length);
  } else {
    // query: string | null
    console.log("no search query");
  }
}
