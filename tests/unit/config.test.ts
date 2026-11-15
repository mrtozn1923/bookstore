import { it } from "node:test";
import assert from "node:assert/strict";
import { ConfigError, loadConfig } from "../../src/config.ts";

const required = { DB_CONNECTION_STRING: "Server=db;Database=BookstoreDb", AUTH_ISSUER: "https://id.example", AUTH_JWKS_URL: "https://id.example/jwks", PAYMENT_WEBHOOK_SECRET: "test-webhook-secret-not-real" };

it("fills defaults and parses numbers and lists", () => {
    const config = loadConfig({ ...required, PORT: "8080", CORS_ORIGINS: "https://shop.example, http://localhost:4200" });
    assert.equal(config.port, 8080);
    assert.equal(config.host, "127.0.0.1");
    assert.deepEqual(config.http.corsOrigins, ["https://shop.example", "http://localhost:4200"]);
    assert.ok(Object.isFrozen(config));
});

it("lists every problem at once", () => {
    assert.throws(() => loadConfig({ PORT: "abc", CORS_ORIGINS: "*" }), (err: unknown) => {
        assert.ok(err instanceof ConfigError);
        for (const name of ["PORT", "CORS_ORIGINS", "DB_CONNECTION_STRING", "AUTH_ISSUER", "AUTH_JWKS_URL"]) {
            assert.match(err.message, new RegExp(`at ${name}`));
        }
        return true;
    });
});
