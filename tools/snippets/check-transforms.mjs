// VS Code evaluates snippet transforms with JavaScript regular expressions,
// so the same regex can be checked in Node before it goes into the snippet.
const ns = (path) =>
  path.replace(/^\w+\W|\W[\w.]+$|(\W)/g, (_match, sep) => (sep ? '.' : ''));

for (const path of [
  'src/Bookstore.Api/BookEndpoints.cs',
  'src\\Bookstore.Catalog\\Persistence\\BookConfiguration.cs',
  'tests/Bookstore.Catalog.Tests/BookConfigurationTests.cs',
]) {
  console.log(`${path} -> ${ns(path)}`);
}
console.log('BookConfiguration ->', 'BookConfiguration'.replace(/(.*)Configuration$/, '$1'));
