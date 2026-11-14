import path from "node:path";

export class PathOutsideRootError extends Error {
    constructor(requested: string) {
        super(`path escapes the storage root: ${requested}`);
        this.name = "PathOutsideRootError";
    }
}

// Resolves a user-supplied relative path and refuses anything outside root
export function resolveInsideRoot(root: string, requested: string): string {
    const absoluteRoot = path.resolve(root);
    const full = path.resolve(absoluteRoot, requested);
    if (full !== absoluteRoot && !full.startsWith(absoluteRoot + path.sep)) {
        throw new PathOutsideRootError(requested);
    }
    return full;
}
