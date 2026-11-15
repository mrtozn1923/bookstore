import { Router } from "express";
import { HttpError } from "#http/errors";
import type { Tracking } from "../integrations/carrierClient.ts";
import { currentRequestContext } from "../context/requestContext.ts";

// What the route needs from the carrier integration; tests pass a fake
export interface TrackingLookup {
    getTracking(trackingNumber: string, signal?: AbortSignal): Promise<Tracking>;
}

const TRACKING_NUMBER = /^[A-Z0-9]{6,20}$/;

// GET /api/shipments/:trackingNumber: back office staff look up a parcel at the carrier
export function shipmentsRouter(carrier: TrackingLookup): Router {
    return Router().get("/:trackingNumber", async (req, res) => {
        const { trackingNumber } = req.params;
        if (!TRACKING_NUMBER.test(trackingNumber)) {
            throw new HttpError(400, "tracking number must be 6-20 upper-case letters or digits");
        }
        // The request's signal: if the client leaves, the carrier call and its retries stop too
        res.json(await carrier.getTracking(trackingNumber, currentRequestContext()?.signal));
    });
}
