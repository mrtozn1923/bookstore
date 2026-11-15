// Runs on a Piscina worker thread: CPU-bound CSV parsing, away from the main event loop
import { parseCatalogCsv } from "#services/catalogImport";

export interface CatalogParseTask {
    csv: ArrayBuffer;
}

// Errors lose their class and properties on the way back to the main thread, so the outcome is plain data
export type CatalogParseResult =
    | { ok: true; imported: number; skipped: number; errors: string[]; parseMs: number }
    | { ok: false; reason: string };

export default function parseCatalog({ csv }: CatalogParseTask): CatalogParseResult {
    const started = performance.now();
    let result;
    try {
        result = parseCatalogCsv(new TextDecoder().decode(csv));
    } catch (err) {
        return { ok: false, reason: (err as Error).message };
    }
    // Only a summary goes back: half a million Book objects would be copied into the main thread
    return {
        ok: true,
        imported: result.books.length,
        skipped: result.errors.length,
        errors: result.errors.slice(0, 20),
        parseMs: Math.round(performance.now() - started),
    };
}
