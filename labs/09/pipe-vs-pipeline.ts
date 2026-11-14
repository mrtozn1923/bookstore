import { createReadStream } from "node:fs";
import { Transform, Writable } from "node:stream";
import { pipeline } from "node:stream/promises";

// A transform that fails on its third chunk (bad data in the middle of a file)
function failingTransform(): Transform {
    let chunks = 0;
    return new Transform({
        transform(chunk, encoding, callback) {
            chunks++;
            callback(chunks === 3 ? new Error("bad row in chunk 3") : null, chunk);
        },
    });
}

const input = import.meta.filename;
// Discards everything it receives
const sink = () => new Writable({ write: (chunk, encoding, callback) => callback() });

if (process.argv[2] === "pipe") {
    const source = createReadStream(input, { highWaterMark: 256 });
    const transform = failingTransform();
    transform.on("error", (err) => console.log(`pipe: transform error: ${err.message}`));
    source.pipe(transform).pipe(sink());
    setTimeout(() => console.log(`pipe: source destroyed? ${source.destroyed}, file closed? ${source.closed}`), 100);
} else {
    const source = createReadStream(input, { highWaterMark: 256 });
    try {
        await pipeline(source, failingTransform(), sink());
    } catch (err) {
        console.log(`pipeline: rejected with: ${(err as Error).message}`);
    }
    console.log(`pipeline: source destroyed? ${source.destroyed}, file closed? ${source.closed}`);
}
