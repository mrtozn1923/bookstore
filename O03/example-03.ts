type Box<T> = { value: T };

function box<T>(value: T): Box<T> {
  return { value };
}

const boxedTitle = box("Dune"); // Box<string>
const boxedInStock = box(true); // Box<boolean>
