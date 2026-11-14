import { createRemoteJWKSet, jwtVerify, type JWTPayload } from "jose";

// Who is calling: the token's subject and roles, nothing else
export interface Principal {
    userId: string;
    roles: readonly string[];
}

export type TokenVerifier = (token: string) => Promise<Principal>;

declare global {
    namespace Express {
        // res.locals.user is set by the authenticate middleware
        interface Locals {
            user?: Principal;
        }
    }
}

export class InvalidTokenError extends Error {
    readonly code: string;

    constructor(code: string) {
        super(`invalid token: ${code}`);
        this.name = "InvalidTokenError";
        this.code = code;
    }
}

export interface JwtOptions {
    issuer: string;
    audience: string;
    jwksUrl: string;
}

function rolesOf(payload: JWTPayload): string[] {
    return Array.isArray(payload.roles) ? payload.roles.filter((r): r is string => typeof r === "string") : [];
}

// What AddJwtBearer does for you in ASP.NET Core, spelled out: keys from JWKS, signature, alg, iss, aud, exp
export function createJwtVerifier({ issuer, audience, jwksUrl }: JwtOptions): TokenVerifier {
    // Keys are fetched on first use, cached, and fetched again when a token names an unknown kid
    const keys = createRemoteJWKSet(new URL(jwksUrl));
    return async (token) => {
        try {
            const { payload } = await jwtVerify(token, keys, {
                issuer,
                audience,
                // Pinned: a token cannot choose "none" or HS256 for itself
                algorithms: ["RS256"],
                requiredClaims: ["sub", "exp"],
                // ASP.NET Core allows 5 minutes of clock skew by default; jose allows none
                clockTolerance: 30,
            });
            return { userId: payload.sub!, roles: rolesOf(payload) };
        } catch (err) {
            throw new InvalidTokenError((err as { code?: string }).code ?? "ERR_UNKNOWN");
        }
    };
}
