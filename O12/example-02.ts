function risky(n: number): void {
  if (n === 1) throw new Error("real error");
  if (n === 2) throw "just a string";
  if (n === 3) throw { code: 42 };
}
