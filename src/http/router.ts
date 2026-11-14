import type { IncomingMessage, ServerResponse } from "node:http";
import { HttpError } from "#http/errors";

export type Params = Record<string, string>;
export type Handler = (req: IncomingMessage, res: ServerResponse, ctx: { url: URL; params: Params }) => unknown;

interface Route {
    method: string;
    pattern: RegExp;
    keys: string[];
    handler: Handler;
}

// "/api/books/:isbn" -> /^\/api\/books\/([^/]+)$/ with keys ["isbn"]
function compile(path: string): { pattern: RegExp; keys: string[] } {
    const keys: string[] = [];
    const source = path
        .split("/")
        .map((segment) => {
            if (segment.startsWith(":")) {
                keys.push(segment.slice(1));
                return "([^/]+)";
            }
            return segment.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
        })
        .join("/");
    return { pattern: new RegExp(`^${source}$`), keys };
}

function decodeSegment(segment: string): string {
    try {
        return decodeURIComponent(segment);
    } catch {
        // "%E0%A4" and similar broken escapes are the client's fault, not a 500
        throw new HttpError(400, `malformed path segment: ${segment}`);
    }
}

export class Router {
    #routes: Route[] = [];

    add(method: string, path: string, handler: Handler): this {
        this.#routes.push({ method, ...compile(path), handler });
        return this;
    }

    // Returns the handler, 405 when only the method is wrong, or undefined for 404
    match(method: string, pathname: string): { handler: Handler; params: Params } | { allowed: string[] } | undefined {
        const allowed: string[] = [];
        for (const route of this.#routes) {
            const m = route.pattern.exec(pathname);
            if (!m) {
                continue;
            }
            if (route.method !== method) {
                allowed.push(route.method);
                continue;
            }
            const params = Object.fromEntries(route.keys.map((key, i) => [key, decodeSegment(m[i + 1])]));
            return { handler: route.handler, params };
        }
        return allowed.length > 0 ? { allowed } : undefined;
    }
}
