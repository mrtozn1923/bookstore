for (const pattern of ["^\\d+0$", "^(?>\\d+)0$", "^\\d++0$", "^(?=(\\d+))\\1(?:0)$"]) {
  try {
    console.log(pattern.padEnd(20), new RegExp(pattern).test("1230"));
  } catch (e) {
    console.log(pattern.padEnd(20), `${e.name}: ${e.message}`);
  }
}
