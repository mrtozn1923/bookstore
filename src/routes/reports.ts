import type { IncomingMessage, ServerResponse } from "node:http";
import { buildPriceReport, buildPriceReportChunked, type PriceReport } from "#services/priceReport";
import { buildPriceReportInWorker } from "#services/priceReportRunner";
import { currentRequestContext } from "../context/requestContext.ts";

// GET /api/reports/prices?mode=inline|chunked|worker&rows=20000000
export async function handlePriceReport(req: IncomingMessage, res: ServerResponse, url: URL): Promise<void> {
    const mode = url.searchParams.get("mode") ?? "worker";
    const rows = Number(url.searchParams.get("rows") ?? 20_000_000);
    const started = performance.now();

    let report: PriceReport;
    if (mode === "inline") {
        report = buildPriceReport(rows);
    } else if (mode === "chunked") {
        report = await buildPriceReportChunked(rows);
    } else {
        // The worker is terminated if the client disconnects before the report is ready
        report = await buildPriceReportInWorker(rows, currentRequestContext()?.signal);
    }

    res.writeHead(200, { "content-type": "application/json" });
    res.end(JSON.stringify({ mode, ...report, durationMs: Math.round(performance.now() - started) }));
}
