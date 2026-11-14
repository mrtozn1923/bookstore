import { after, before, describe, it } from "node:test";
import assert from "node:assert/strict";
import http from "node:http";
import type { AddressInfo } from "node:net";
import { exportJWK, generateKeyPair, SignJWT, type CryptoKey } from "jose";
import { createJwtVerifier, InvalidTokenError, type TokenVerifier } from "../../src/auth/tokenVerifier.ts";

// The real verifier against a JWKS served by the test itself: keys are generated per run, never stored
describe("createJwtVerifier", () => {
    let server: http.Server;
    let privateKey: CryptoKey;
    let verify: TokenVerifier;
    const issuer = "https://id.test";

    // before/after: the counterpart of an xUnit class fixture
    before(async () => {
        const pair = await generateKeyPair("RS256");
        privateKey = pair.privateKey;
        const jwks = JSON.stringify({ keys: [{ ...(await exportJWK(pair.publicKey)), kid: "k1", alg: "RS256" }] });
        server = http.createServer((req, res) => res.end(jwks)).listen(0, "127.0.0.1");
        await new Promise((resolve) => server.once("listening", resolve));
        verify = createJwtVerifier({ issuer, audience: "bookstore-api", jwksUrl: `http://127.0.0.1:${(server.address() as AddressInfo).port}/jwks` });
    });

    after(() => {
        server.close();
    });

    const sign = (claims: { roles?: string[] }, audience = "bookstore-api", expiresIn = "5m") =>
        new SignJWT(claims).setProtectedHeader({ alg: "RS256", kid: "k1" }).setIssuer(issuer).setAudience(audience).setSubject("ada").setExpirationTime(expiresIn).sign(privateKey);

    it("accepts a valid token and reads the roles", async () => {
        assert.deepEqual(await verify(await sign({ roles: ["StoreStaff"] })), { userId: "ada", roles: ["StoreStaff"] });
    });

    it("rejects another audience, an expired token and alg none", async () => {
        const none = `${Buffer.from('{"alg":"none"}').toString("base64url")}.${Buffer.from(`{"sub":"ada","iss":"${issuer}","aud":"bookstore-api","exp":9999999999}`).toString("base64url")}.`;
        const cases: [string, string][] = [
            [await sign({}, "payments-api"), "ERR_JWT_CLAIM_VALIDATION_FAILED"],
            [await sign({}, "bookstore-api", "2 minutes ago"), "ERR_JWT_EXPIRED"],
            [none, "ERR_JOSE_ALG_NOT_ALLOWED"],
        ];
        for (const [token, code] of cases) {
            await assert.rejects(verify(token), (err: unknown) => err instanceof InvalidTokenError && err.code === code);
        }
    });
});
