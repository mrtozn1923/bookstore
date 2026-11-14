// An old dependency that still reaches into Node's internal stream modules
const Readable = require("_stream_readable");
console.log(typeof Readable, Readable === require("node:stream").Readable);
