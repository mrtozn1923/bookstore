// O07: example 12 needs the CatalogItem class from example 9, so both are here

// --- example 9 ---
class CatalogItem {
  constructor(title) {
    this.title = title;
  }

  describe() {
    return `${this.title} is in the catalog`;
  }
}

class EBook extends CatalogItem {
  constructor(title, fileFormat) {
    super(title);
    this.fileFormat = fileFormat;
  }

  describe() {
    return `${super.describe()}: download as ${this.fileFormat}`;
  }
}

const duneEbook = new EBook("Dune", "EPUB");
console.log(duneEbook.describe());
// "Dune is in the catalog: download as EPUB"

console.log(duneEbook instanceof EBook);       // true
console.log(duneEbook instanceof CatalogItem); // true

// --- example 12 ---
class AudioBook extends CatalogItem {
  constructor(title) {
    this.narrator = "Unknown"; // ReferenceError
    super(title);
  }
}

new AudioBook("Emma");
