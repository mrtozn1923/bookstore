type Format = "hardcover" | "paperback" | "ebook";
type Rating = 1 | 2 | 3 | 4 | 5;

function setFormat(format: Format) {
  console.log(`Format set to ${format}`);
}

setFormat("ebook");
// Error: Argument of type '"audiobook"' is not assignable to parameter of type 'Format'.
setFormat("audiobook");
