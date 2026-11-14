import type { RequestHandler } from "express";
import { HttpError } from "#http/errors";
import { InvalidTokenError, type TokenVerifier } from "../auth/tokenVerifier.ts";
import { logger } from "../diagnostics/logger.ts";

// RFC 6750 bearer token: "Authorization: Bearer <token>"
const BEARER = /^Bearer ([A-Za-z0-9._~+/-]+=*)$/;

export function authenticate(verify: TokenVerifier): RequestHandler {
    return async (req, res, next) => {
        const match = BEARER.exec(req.headers.authorization ?? "");
        if (!match) {
            res.setHeader("www-authenticate", "Bearer");
            throw new HttpError(401, "a bearer token is required");
        }
        try {
            res.locals.user = await verify(match[1]);
        } catch (err) {
            // The reason goes to the log; the client only learns that the token was not accepted
            logger.info({ reason: err instanceof InvalidTokenError ? err.code : (err as Error).message }, "token rejected");
            res.setHeader("www-authenticate", 'Bearer error="invalid_token"');
            throw new HttpError(401, "the access token is invalid or expired");
        }
        next();
    };
}

// Function-level authorization: who may call this endpoint at all
export function requireRole(role: string): RequestHandler {
    return (req, res, next) => {
        if (!res.locals.user?.roles.includes(role)) {
            throw new HttpError(403, `this endpoint requires the ${role} role`);
        }
        next();
    };
}
