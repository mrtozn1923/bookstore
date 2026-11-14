const term = "Dune (1965).";
console.log(RegExp.escape(term));
console.log(new RegExp("^" + RegExp.escape(term)).test("Dune (1965). Frank Herbert"));

try {
  new RegExp("^Dune (1965");
} catch (e) {
  console.log(e.name + ": " + e.message);
}
