class TempFile implements Disposable {
  path: string;
  constructor(path: string) {
    this.path = path;
    console.log(`create ${path}`);
  }

  write(text: string) {
    console.log(`write "${text}" to ${this.path}`);
  }

  [Symbol.dispose]() {
    console.log(`delete ${this.path}`);
  }
}

function exportOrders() {
  using file = new TempFile("/tmp/orders.csv");
  file.write("id,total");
  return "exported";
}

console.log(exportOrders());
// create /tmp/orders.csv
// write "id,total" to /tmp/orders.csv
// delete /tmp/orders.csv
// exported
