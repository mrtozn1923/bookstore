try {
  const response = await fetch("https://openlibrary.org/isbn/9780441172710.json");
  console.log("A", response.ok);
  const data = await response.json();
  console.log("B", data);
} catch (error) {
  console.log("C", error.name);
}
