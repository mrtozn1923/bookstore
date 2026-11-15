import { availableParallelism } from "node:os";
import { Piscina } from "piscina";
import type { CatalogParseResult, CatalogParseTask } from "../workers/catalogParseWorker.ts";

export type CatalogParser = (csv: Buffer, signal?: AbortSignal) => Promise<CatalogParseResult>;

export interface CatalogParsePool {
    parse: CatalogParser;
    close: () => Promise<void>;
}

// One pool per process, created at startup: threads stay warm instead of starting for every request
export function createCatalogParsePool(options: { maxThreads?: number } = {}): CatalogParsePool {
    const pool = new Piscina<CatalogParseTask, CatalogParseResult>({
        filename: new URL("../workers/catalogParseWorker.ts", import.meta.url).href,
        minThreads: 1,
        // Leave a core for the main thread's event loop
        maxThreads: options.maxThreads ?? Math.max(1, availableParallelism() - 1),
        idleTimeout: 60_000,
        // A full queue rejects new work instead of piling uploads up in memory
        maxQueue: 16,
    });
    return {
        parse: (csv, signal) => {
            // Transfer, do not copy: the ArrayBuffer moves to the worker. A small Buffer may share
            // Node's internal pool, which cannot be transferred, so only a Buffer that owns its memory moves as is
            const owned = csv.byteOffset === 0 && csv.byteLength === csv.buffer.byteLength;
            const bytes = (owned ? csv.buffer : csv.buffer.slice(csv.byteOffset, csv.byteOffset + csv.byteLength)) as ArrayBuffer;
            // "as never": piscina 5.3's TransferList type infers the wrong postMessage overload from @types/node 26
            return pool.run({ csv: bytes }, { transferList: [bytes] as never, signal });
        },
        close: () => pool.close(),
    };
}
