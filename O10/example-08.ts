// fetchBook (example-06) and fetchAuthor/searchBooks (example-07), which this example uses.
type OpenLibraryBook = { key: string; title: string; number_of_pages: number };

function isOpenLibraryBook(value: unknown): value is OpenLibraryBook {
  return (
    typeof value === "object" &&
    value !== null &&
    "key" in value && typeof value.key === "string" &&
    "title" in value && typeof value.title === "string" &&
    "number_of_pages" in value && typeof value.number_of_pages === "number"
  );
}

async function fetchBook(isbn: string): Promise<OpenLibraryBook> {
  const response = await fetch(`https://openlibrary.org/isbn/${isbn}.json`);
  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }
  const data: unknown = await response.json();
  if (!isOpenLibraryBook(data)) {
    throw new Error("Unexpected response shape");
  }
  return data;
}

async function main() {
  const book = await fetchBook("9780441172719");
  console.log(book.title, book.number_of_pages); // Dune 535

  try {
    await fetchBook("9780441172710");
  } catch (error) {
    console.log(error instanceof Error ? error.message : error); // Request failed with status 404
  }
}


type Author = { key: string; name: string };
type SearchDoc = { key: string; title: string };

function isAuthor(value: unknown): value is Author {
  return (
    typeof value === "object" &&
    value !== null &&
    "key" in value && typeof value.key === "string" &&
    "name" in value && typeof value.name === "string"
  );
}

function isSearchDoc(value: unknown): value is SearchDoc {
  return (
    typeof value === "object" &&
    value !== null &&
    "key" in value && typeof value.key === "string" &&
    "title" in value && typeof value.title === "string"
  );
}

async function fetchAuthor(authorKey: string): Promise<Author> {
  const response = await fetch(`https://openlibrary.org/authors/${authorKey}.json`);
  const data: unknown = await response.json();
  if (!isAuthor(data)) throw new Error("Unexpected author shape");
  return data;
}

async function searchBooks(authorKey: string): Promise<SearchDoc[]> {
  const response = await fetch(`https://openlibrary.org/search.json?author_key=${authorKey}&fields=key,title&limit=20`);
  const data: unknown = await response.json();
  const docs = typeof data === "object" && data !== null && "docs" in data ? data.docs : undefined;
  if (!Array.isArray(docs) || !docs.every(isSearchDoc)) throw new Error("Unexpected search shape");
  return docs;
}

async function loadAuthorPage(authorKey: string) {
  const [author, books] = await Promise.all([fetchAuthor(authorKey), searchBooks(authorKey)]);
  // author: Author, books: SearchDoc[]
  console.log(`${author.name}: ${books.length} books`); // Frank Herbert: 20 books
}

async function loadMany(isbns: string[]) {
  const results = await Promise.allSettled(isbns.map((isbn) => fetchBook(isbn)));
  for (const result of results) {
    if (result.status === "fulfilled") {
      console.log("ok", result.value.title);
    } else {
      console.log("failed", result.reason instanceof Error ? result.reason.message : result.reason);
    }
  }
}

loadAuthorPage("OL79034A").then(() => loadMany(["9780441172719", "9780441172710"]));
// ok Dune
// failed Request failed with status 404
