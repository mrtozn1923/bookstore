import { timingSafeEqual } from "node:crypto";

// timingSafeEqual only compares buffers of the same length
const expected = Buffer.from("9f2c", "hex");
try {
    timingSafeEqual(expected, Buffer.from("9f2c00", "hex"));
} catch (err) {
    console.log(`${(err as { code: string }).code}: ${(err as Error).message}`);
}
