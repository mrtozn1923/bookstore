function track(name: string): Disposable {
  console.log(`open ${name}`);
  return {
    [Symbol.dispose]() {
      console.log(`close ${name}`);
    },
  };
}

try {
  using db = track("db");
  using cache = track("cache");
  throw new Error("payment declined");
} catch (error) {
  if (error instanceof Error) {
    console.log(`caught ${error.message}`);
  }
}
