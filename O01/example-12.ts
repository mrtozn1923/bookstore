function parseJson<T>(text: string): T {
  return JSON.parse(text);
}

const book = parseJson<{ title: string }>('{"name":"Dune"}');
console.log(book.title.toUpperCase()); // Compiles; throws a TypeError when it runs
