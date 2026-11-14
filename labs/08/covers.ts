import { glob } from "node:fs/promises";
import { saveCover, listCovers } from "#storage/coverStorage";

// Smallest possible stand-ins: a JPEG starts with FF D8 FF, a PNG with 89 50 4E 47
const fakeJpeg = (size: number) => Uint8Array.from({ length: size }, (_, i) => [0xff, 0xd8, 0xff, 0xe0][i] ?? 0);
const fakePng = Uint8Array.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);

for (const [isbn, size] of [["9780441172719", 48_213], ["9780547928227", 35_870]] as const) {
    const saved = await saveCover(isbn, fakeJpeg(size));
    console.log(`saved ${saved.isbn}: ${saved.sizeBytes} bytes`);
}

for (const [isbn, bytes] of [["9780141439600", fakePng], ["../../src/server", fakeJpeg(10)]] as const) {
    try {
        await saveCover(isbn, bytes);
    } catch (err) {
        console.log(`${(err as Error).name}: ${(err as Error).message}`);
    }
}

console.log(`listCovers: ${(await listCovers()).map((c) => c.isbn).join(", ")}`);
console.log(`glob:       ${(await Array.fromAsync(glob("uploads/**/*.jpg"))).join(", ")}`);
