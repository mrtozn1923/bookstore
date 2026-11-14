import type { Response } from "express";

// RFC 9457 Problem Details; type URIs and titles as ASP.NET Core writes them
const PROBLEMS: Record<number, { type: string; title: string }> = {
    400: { type: "https://tools.ietf.org/html/rfc9110#section-15.5.1", title: "Bad Request" },
    401: { type: "https://tools.ietf.org/html/rfc9110#section-15.5.2", title: "Unauthorized" },
    403: { type: "https://tools.ietf.org/html/rfc9110#section-15.5.4", title: "Forbidden" },
    404: { type: "https://tools.ietf.org/html/rfc9110#section-15.5.5", title: "Not Found" },
    409: { type: "https://tools.ietf.org/html/rfc9110#section-15.5.10", title: "Conflict" },
    413: { type: "https://tools.ietf.org/html/rfc9110#section-15.5.14", title: "Content Too Large" },
    415: { type: "https://tools.ietf.org/html/rfc9110#section-15.5.16", title: "Unsupported Media Type" },
    422: { type: "https://tools.ietf.org/html/rfc9110#section-15.5.21", title: "Unprocessable Content" },
    429: { type: "https://tools.ietf.org/html/rfc6585#section-4", title: "Too Many Requests" },
    500: { type: "https://tools.ietf.org/html/rfc9110#section-15.6.1", title: "An error occurred while processing your request." },
};

export interface ProblemDetails {
    type: string;
    title: string;
    status: number;
    detail?: string;
    instance?: string;
    [extension: string]: unknown;
}

export function sendProblem(res: Response, status: number, extra: Partial<ProblemDetails> = {}): void {
    const known = PROBLEMS[status] ?? { type: "about:blank", title: "Error" };
    const body: ProblemDetails = { type: known.type, title: known.title, status, ...extra };
    res.status(status).type("application/problem+json").send(JSON.stringify(body));
}
