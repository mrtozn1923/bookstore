import { AsyncLocalStorage } from "node:async_hooks";

// Per-request values that follow the async call chain (HttpContext / Activity counterpart)
export interface RequestContext {
    requestId: string;
    method: string;
    path: string;
    // Aborted when the client disconnects before the response is complete (RequestAborted counterpart)
    signal: AbortSignal;
}

const storage = new AsyncLocalStorage<RequestContext>();

export function runWithRequestContext<T>(context: RequestContext, work: () => T): T {
    return storage.run(context, work);
}

export function currentRequestContext(): RequestContext | undefined {
    return storage.getStore();
}
