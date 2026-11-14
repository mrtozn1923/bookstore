setTimeout(() => console.log("timeout"), 0);

Promise.resolve().then(() => {
  console.log("micro 1");
  queueMicrotask(() => console.log("micro 2"));
});

// micro 1
// micro 2
// timeout
