import { Router } from "express";
import { buildPriceReport, buildPriceReportChunked, type PriceReport } from "#services/priceReport";
import { buildPriceReportInWorker } from "#services/priceReportRunner";
import { currentRequestContext } from "../context/requestContext.ts";

export const reportsRouter = Router();

// GET /api/reports/prices?mode=inline|chunked|worker&rows=20000000
reportsRouter.get("/prices", async (req, res) => {
    const mode = String(req.query.mode ?? "worker");
    const rows = Number(req.query.rows ?? 20_000_000);
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
    res.json({ mode, ...report, durationMs: Math.round(performance.now() - started) });
});
