console.log(/^x{,3}$/.test("xx"), /^x{,3}$/.test("x{,3}"));
try {
  new RegExp("^x{,3}$", "u");
} catch (e) {
  console.log(`${e.name}: ${e.message}`);
}
