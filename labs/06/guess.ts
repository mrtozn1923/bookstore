import { readFile } from "node:fs";

readFile(import.meta.filename, () => {
    setTimeout(() => console.log("setTimeout 0"), 0);
    setImmediate(() => console.log("setImmediate"));
    Promise.resolve().then(() => console.log("promise.then"));
    process.nextTick(() => console.log("process.nextTick"));
    console.log("readFile callback");
});
