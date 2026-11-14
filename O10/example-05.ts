type OpenLibraryBook = { key: string; title: string; number_of_pages: number };

async function getBookUnsafe(isbn: string) {
  const response = await fetch(`https://openlibrary.org/isbn/${isbn}.json`);
  const data = await response.json(); // any
  console.log(data.titel); // undefined
  return data;
}

async function getBookTrusting(isbn: string): Promise<OpenLibraryBook> {
  const response = await fetch(`https://openlibrary.org/isbn/${isbn}.json`);
  return response.json();
}

getBookUnsafe("9780441172719");
