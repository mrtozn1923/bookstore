import { currentRequestContext } from "../context/requestContext.ts";

type Level = "info" | "warn" | "error";

// One JSON line per entry; the request id comes from AsyncLocalStorage, not from a parameter
export function log(level: Level, msg: string, fields: Record<string, unknown> = {}): void {
    const entry = { time: new Date().toISOString(), level, requestId: currentRequestContext()?.requestId, msg, ...fields };
    const line = JSON.stringify(entry);
    if (level === "error") {
        console.error(line);
    } else {
        console.log(line);
    }
}
