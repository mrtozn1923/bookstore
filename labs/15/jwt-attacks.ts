// Run with the API (port 3001) and scripts/dev-issuer.ts (port 4001) started
import { createPublicKey, generateKeyPairSync } from "node:crypto";
import { SignJWT } from "jose";

const api = process.env.API_URL ?? "http://127.0.0.1:3001";
const issuer = process.env.ISSUER_URL ?? "http://127.0.0.1:4001";
const order = { lines: [{ isbn: "9780141439587", quantity: 1 }] };

async function issue(claims: { sub: string; roles?: string[]; audience?: string; expiresIn?: string }): Promise<string> {
    const res = await fetch(`${issuer}/token`, { method: "POST", body: JSON.stringify(claims) });
    return ((await res.json()) as { access_token: string }).access_token;
}

async function call(label: string, method: string, path: string, token?: string, body?: unknown): Promise<Response> {
    const res = await fetch(`${api}${path}`, {
        method,
        headers: { "content-type": "application/json", ...(token ? { authorization: `Bearer ${token}` } : {}) },
        body: body === undefined ? undefined : JSON.stringify(body),
    });
    console.log(`${label.padEnd(44)} ${res.status}`);
    return res;
}

const b64 = (value: object): string => Buffer.from(JSON.stringify(value)).toString("base64url");
const ada = await issue({ sub: "ada" });

await call("no token", "POST", "/api/orders", undefined, order);
const placed = await call("Ada places an order", "POST", "/api/orders", ada, order);
const location = placed.headers.get("location")!;
await call("Ada reads her order", "GET", location, ada);
await call("Alan reads Ada's order", "GET", location, await issue({ sub: "alan" }));
await call("Grace (StoreStaff) reads Ada's order", "GET", location, await issue({ sub: "grace", roles: ["StoreStaff"] }));
await call("Ada calls the export", "GET", "/api/orders/export", ada);
await call("Ada sends customerId: alan in the body", "POST", "/api/orders", ada, { ...order, customerId: "alan" });
await call("expired two minutes ago", "GET", location, await issue({ sub: "ada", expiresIn: "2 minutes ago" }));
await call("token for another audience", "GET", location, await issue({ sub: "ada", audience: "payments-api" }));

// Forged tokens: the payload claims to be Ada, the signature is the problem
const [header, payload] = ada.split(".").slice(0, 2).map((part) => JSON.parse(Buffer.from(part, "base64url").toString()));
await call('alg "none", no signature', "GET", location, `${b64({ alg: "none" })}.${b64(payload)}.`);

// Algorithm confusion: HS256 with the API's public RSA key as the HMAC secret
const jwks = (await (await fetch(`${issuer}/.well-known/jwks.json`)).json()) as { keys: JsonWebKey[] };
const publicPem = createPublicKey({ key: jwks.keys[0], format: "jwk" }).export({ type: "spki", format: "pem" });
const confused = await new SignJWT(payload).setProtectedHeader({ alg: "HS256", kid: header.kid }).sign(Buffer.from(publicPem));
await call("HS256 signed with the public key", "GET", location, confused);

// A key the attacker made, presented under the real key's kid
const { privateKey } = generateKeyPairSync("rsa", { modulusLength: 2048 });
const forged = await new SignJWT(payload).setProtectedHeader({ alg: "RS256", kid: header.kid }).sign(privateKey);
await call("RS256 signed with another key", "GET", location, forged);
