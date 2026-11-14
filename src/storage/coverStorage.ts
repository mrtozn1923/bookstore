import { mkdir, readdir, stat } from "node:fs/promises";
import path from "node:path";
import { isValidIsbn13 } from "@bookstore/contracts";
import { writeFileAtomic } from "#storage/atomicWrite";
import { resolveInsideRoot } from "#storage/safePath";

export const coversRoot: string = path.resolve(process.env.COVERS_DIR ?? "uploads/covers");
const JPEG_MAGIC = [0xff, 0xd8, 0xff];

export class InvalidCoverError extends Error {
    constructor(message: string) {
        super(message);
        this.name = "InvalidCoverError";
    }
}

export interface CoverInfo {
    isbn: string;
    sizeBytes: number;
    modifiedUtc: string;
}

function coverPath(isbn: string): string {
    // First defence: only a valid ISBN-13 can become a file name
    if (!isValidIsbn13(isbn)) {
        throw new InvalidCoverError(`invalid ISBN-13: ${isbn}`);
    }
    // Second defence: the resolved path must stay under the covers folder
    return resolveInsideRoot(coversRoot, `${isbn}.jpg`);
}

export async function saveCover(isbn: string, bytes: Uint8Array): Promise<CoverInfo> {
    const target = coverPath(isbn);
    if (!JPEG_MAGIC.every((b, i) => bytes[i] === b)) {
        throw new InvalidCoverError("cover must be a JPEG file");
    }
    await mkdir(coversRoot, { recursive: true });
    await writeFileAtomic(target, bytes);
    const info = await stat(target);
    return { isbn, sizeBytes: info.size, modifiedUtc: info.mtime.toISOString() };
}

export async function listCovers(): Promise<CoverInfo[]> {
    const entries = await readdir(coversRoot, { withFileTypes: true }).catch((err: NodeJS.ErrnoException) => {
        if (err.code === "ENOENT") {
            return [];
        }
        throw err;
    });
    const files = entries.filter((e) => e.isFile() && e.name.endsWith(".jpg"));
    return Promise.all(
        files.map(async (e) => {
            const info = await stat(path.join(coversRoot, e.name));
            return { isbn: path.basename(e.name, ".jpg"), sizeBytes: info.size, modifiedUtc: info.mtime.toISOString() };
        }),
    );
}

export function coverFilePath(isbn: string): string {
    return coverPath(isbn);
}
