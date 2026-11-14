fetch("https://openlibrary.org/isbn/9780441172710.json")
  .then((response) => console.log("then:", response.status))
  .catch(() => console.log("catch"));
