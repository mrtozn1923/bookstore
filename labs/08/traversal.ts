import path from "node:path";
import { resolveInsideRoot } from "#storage/safePath";

const root = "/srv/bookstore/uploads/covers";

// Naive check: does the resolved path start with the root string?
function naiveResolve(requested: string): string {
    const full = path.resolve(root, requested);
    if (!full.startsWith(root)) {
        throw new Error(`blocked ${requested}`);
    }
    return full;
}

function attempt(resolve: () => string): string {
    try {
        return `allowed ${resolve()}`;
    } catch {
        return "blocked";
    }
}

for (const requested of ["9780441172719.jpg", "../../../../etc/passwd", "../covers-backup/keys.txt", decodeURIComponent("..%2F..%2Fconfig.json")]) {
    console.log(requested);
    console.log(`  naive: ${attempt(() => naiveResolve(requested))}`);
    console.log(`  safe:  ${attempt(() => resolveInsideRoot(root, requested))}`);
}
