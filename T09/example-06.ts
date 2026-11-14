function compare(a: string | number, b: string | boolean) {
  if (a === b) {
    // a: string, b: string
    return a.toUpperCase() === b.toUpperCase();
  }
  return false;
}
