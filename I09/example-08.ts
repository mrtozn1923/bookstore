function track(name: string): Disposable {
  console.log(`open ${name}`);
  return {
    [Symbol.dispose]() {
      console.log(`close ${name}`);
    },
  };
}

function startStockPolling(): DisposableStack {
  using stack = new DisposableStack();
  stack.adopt(setInterval(() => {}, 1000), (id) => {
    clearInterval(id);
    console.log("timer cleared");
  });
  stack.defer(() => console.log("polling stopped"));
  stack.use(track("stock-feed"));
  return stack.move();
}

{
  using polling = startStockPolling();
  console.log("polling stock...");
}
// open stock-feed
// polling stock...
// close stock-feed
// polling stopped
// timer cleared
