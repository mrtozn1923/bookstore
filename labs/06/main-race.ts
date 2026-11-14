// From the main module (not inside an I/O callback): who runs first?
setTimeout(() => console.log("timeout"), 0);
setImmediate(() => console.log("immediate"));
