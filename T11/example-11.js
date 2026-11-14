const showBook = (title) => console.log(`Book: ${title}`);

["Dune", "Emma"].forEach(showBook);    // right: we pass the function
// ["Dune", "Emma"].forEach(showBook()); // wrong: we call it first and pass the result
