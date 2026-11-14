async function getJSON(url) {
  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`HTTP ${response.status}: ${url}`);
  }
  return response.json();
}

try {
  const book = await getJSON("https://openlibrary.org/isbn/9780441172710.json");
  console.log(book);
} catch (error) {
  console.error(error.message); // "HTTP 404: https://openlibrary.org/isbn/9780441172710.json"
}
