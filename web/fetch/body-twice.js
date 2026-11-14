export async function bodyTwice() {
  const response = await fetch("/api/books");
  const books = await response.json();
  console.log("json():", books.length, "books, bodyUsed =", response.bodyUsed);
  try {
    const text = await response.text();
    console.log("text():", text.length, "chars");
  } catch (err) {
    console.log(`text(): ${err.name}: ${err.message}`);
  }
}

export async function bodyTwiceWithClone() {
  const response = await fetch("/api/books");
  const copy = response.clone(); // clone before the first read
  const books = await response.json();
  const text = await copy.text();
  console.log("json():", books.length, "books; clone text():", text.length, "chars");
}
