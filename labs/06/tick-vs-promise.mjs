Promise.resolve().then(() => console.log("promise.then"));
process.nextTick(() => console.log("process.nextTick"));
