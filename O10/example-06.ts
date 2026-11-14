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

main();
