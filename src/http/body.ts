import type { IncomingMessage } from "node:http";
import { HttpError } from "#http/errors";

// Reads a JSON body as a stream and stops as soon as it grows past the limit
export async function readJsonBody(req: IncomingMessage, limitBytes = 64 * 1024): Promise<unknown> {
    const contentType = req.headers["content-type"] ?? "";
    if (!/^application\/json\b/i.test(contentType)) {
        throw new HttpError(415, "content-type must be application/json");
    }
    const declared = Number(req.headers["content-length"] ?? 0);
    if (declared > limitBytes) {
        throw new HttpError(413, `body larger than ${limitBytes} bytes`);
    }

    const chunks: Buffer[] = [];
    let received = 0;
    for await (const chunk of req as AsyncIterable<Buffer>) {
        received += chunk.length;
        if (received > limitBytes) {
            throw new HttpError(413, `body larger than ${limitBytes} bytes`);
        }
        chunks.push(chunk);
    }
    try {
        return JSON.parse(Buffer.concat(chunks).toString("utf8"));
    } catch {
        throw new HttpError(400, "body is not valid JSON");
    }
}
