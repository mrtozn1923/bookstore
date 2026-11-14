// Development-only token issuer, the counterpart of `dotnet user-jwts`: node scripts/dev-issuer.ts
// The key pair lives in memory and is new on every start; nothing is written to disk
import http from "node:http";
import { exportJWK, generateKeyPair, SignJWT } from "jose";

if (process.env.NODE_ENV === "production") {
    console.error("dev-issuer must never run in production");
    process.exit(1);
}

const port = Number(process.env.DEV_ISSUER_PORT ?? 4001);
const issuer = `http://127.0.0.1:${port}`;
const kid = `dev-${Date.now()}`;
const { publicKey, privateKey } = await generateKeyPair("RS256");
const jwks = { keys: [{ ...(await exportJWK(publicKey)), kid, alg: "RS256", use: "sig" }] };

interface TokenRequest {
    sub: string;
    roles?: string[];
    audience?: string;
    expiresIn?: string;
}

async function readJson(req: http.IncomingMessage): Promise<TokenRequest> {
    let text = "";
    for await (const chunk of req) {
        text += chunk;
    }
    return JSON.parse(text) as TokenRequest;
}

http.createServer(async (req, res) => {
    if (req.method === "GET" && req.url === "/.well-known/jwks.json") {
        res.writeHead(200, { "content-type": "application/json" }).end(JSON.stringify(jwks));
        return;
    }
    if (req.method === "POST" && req.url === "/token") {
        // Audience and lifetime are overridable so that tests can mint wrong and expired tokens
        const { sub, roles = ["Customer"], audience = "bookstore-api", expiresIn = "15m" } = await readJson(req);
        const token = await new SignJWT({ roles })
            .setProtectedHeader({ alg: "RS256", kid })
            .setIssuer(issuer)
            .setAudience(audience)
            .setSubject(sub)
            .setIssuedAt()
            .setExpirationTime(expiresIn)
            .sign(privateKey);
        res.writeHead(200, { "content-type": "application/json" }).end(JSON.stringify({ access_token: token, token_type: "Bearer" }));
        return;
    }
    res.writeHead(404).end();
}).listen(port, "127.0.0.1", () => console.log(`dev issuer on ${issuer}, JWKS at ${issuer}/.well-known/jwks.json`));
