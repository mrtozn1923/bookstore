import { writeFile, rename, rm } from "node:fs/promises";
import { randomUUID } from "node:crypto";

// Readers see either the old file or the new one, never a half-written file
export async function writeFileAtomic(target: string, data: string | Uint8Array): Promise<void> {
    const temp = `${target}.${randomUUID()}.tmp`;
    try {
        await writeFile(temp, data);
        await rename(temp, target);
    } catch (err) {
        await rm(temp, { force: true });
        throw err;
    }
}
