function wait(ms: number) {
  return new Promise<void>((resolve) => setTimeout(resolve, ms));
}

async function connect(url: string): Promise<AsyncDisposable> {
  await wait(10);
  console.log(`connected to ${url}`);
  return {
    async [Symbol.asyncDispose]() {
      await wait(10);
      console.log(`disconnected from ${url}`);
    },
  };
}

async function syncStock() {
  await using conn = await connect("db://bookstore");
  console.log("syncing stock");
}

syncStock().then(() => console.log("done"));
// connected to db://bookstore
// syncing stock
// disconnected from db://bookstore
// done
