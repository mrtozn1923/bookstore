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
