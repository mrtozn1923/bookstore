import { open } from "node:fs/promises";

// Reads the first bytes of a file; the handle is closed even if read() throws
async function readMagic(file: string): Promise<string> {
    await using handle = await open(file, "r");
    const { buffer, bytesRead } = await handle.read(Buffer.alloc(4), 0, 4, 0);
    return buffer.subarray(0, bytesRead).toString("hex");
}

// The older form of the same thing
async function readMagicFinally(file: string): Promise<string> {
    const handle = await open(file, "r");
    try {
        const { buffer, bytesRead } = await handle.read(Buffer.alloc(4), 0, 4, 0);
        return buffer.subarray(0, bytesRead).toString("hex");
    } finally {
        await handle.close();
    }
}

console.log(await readMagic("data/catalog-sample.csv"), await readMagicFinally("data/catalog-sample.csv"));

