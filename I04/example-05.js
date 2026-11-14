console.log("start");

setTimeout(() => {
  console.log("timeout 1");
  Promise.resolve().then(() => console.log("micro in timeout"));
}, 0);

setTimeout(() => console.log("timeout 2"), 0);

Promise.resolve()
  .then(() => console.log("then 1"))
  .then(() => console.log("then 2"));

console.log("end");
