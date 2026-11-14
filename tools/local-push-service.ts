// A local stand-in for a browser push service (RFC 8030), for tests only: it checks VAPID (RFC 8292),
// decrypts aes128gcm payloads (RFC 8291) and keeps them for a test to deliver to the service worker.
// Run: node tools/local-push-service.ts   (PUSH_PORT defaults to 5291)
import { createDecipheriv, createECDH, createHmac, createPublicKey, randomBytes, verify } from "node:crypto";
import { createServer, type IncomingMessage, type ServerResponse } from "node:http";

const port = Number(process.env.PUSH_PORT ?? 5291);
const origin = `http://127.0.0.1:${port}`;
const b64 = (b: Buffer) => b.toString("base64url");
const hmac = (key: Buffer, data: Buffer) => createHmac("sha256", key).update(data).digest();

interface Subscription {
  serverKey: string; // applicationServerKey given to subscribe()
  ecdh: ReturnType<typeof createECDH>;
  auth: Buffer;
  gone: boolean;
  messages: string[];
}
const subscriptions = new Map<string, Subscription>();
let nextId = 1;
let requestNo = 0;

async function body(req: IncomingMessage) {
  const parts: Buffer[] = [];
  for await (const chunk of req) parts.push(chunk);
  return Buffer.concat(parts);
}

// RFC 8292: Authorization: vapid t=<JWT signed with ES256>, k=<public key>
function checkVapid(header: string | undefined, sub: Subscription): string {
  const match = /^vapid t=([\w-]+\.[\w-]+\.[\w-]+),\s*k=([\w-]+)$/.exec(header ?? "");
  if (!match) throw new Error("no vapid Authorization header");
  const [, jwt, k] = match;
  if (k !== sub.serverKey) throw new Error("k is not the applicationServerKey of this subscription");
  const [h, p, s] = jwt.split(".");
  const raw = Buffer.from(k, "base64url");
  const key = createPublicKey({ key: { kty: "EC", crv: "P-256", x: b64(raw.subarray(1, 33)), y: b64(raw.subarray(33)) }, format: "jwk" });
  const ok = verify("sha256", Buffer.from(`${h}.${p}`), { key, dsaEncoding: "ieee-p1363" }, Buffer.from(s, "base64url"));
  if (!ok) throw new Error("bad JWT signature");
  const claims = JSON.parse(Buffer.from(p, "base64url").toString());
  const now = Math.floor(Date.now() / 1000);
  // RFC 8292: aud is the origin of the push resource; some senders drop a non-default port, accepted here with a note
  const portless = origin.replace(/:\d+$/, "");
  if (claims.aud !== origin && claims.aud !== portless) throw new Error(`aud ${claims.aud} is not ${origin}`);
  const audNote = claims.aud === origin ? "" : " without the port";
  if (!(claims.exp > now && claims.exp <= now + 24 * 60 * 60)) throw new Error("exp must be within 24 hours");
  return `vapid ok (aud=${claims.aud}${audNote}, exp in ${Math.round((claims.exp - now) / 3600)} h, sub=${claims.sub})`;
}

// RFC 8291 + RFC 8188: one aes128gcm record
function decrypt(data: Buffer, sub: Subscription): string {
  const salt = data.subarray(0, 16);
  const idLength = data[20];
  const serverPublic = data.subarray(21, 21 + idLength);
  const ciphertext = data.subarray(21 + idLength);
  const userPublic = sub.ecdh.getPublicKey();
  const secret = sub.ecdh.computeSecret(serverPublic);
  const keyInfo = Buffer.concat([Buffer.from("WebPush: info\0"), userPublic, serverPublic, Buffer.from([1])]);
  const ikm = hmac(hmac(sub.auth, secret), keyInfo);
  const prk = hmac(salt, ikm);
  const cek = hmac(prk, Buffer.from("Content-Encoding: aes128gcm\0\x01")).subarray(0, 16);
  const nonce = hmac(prk, Buffer.from("Content-Encoding: nonce\0\x01")).subarray(0, 12);
  const decipher = createDecipheriv("aes-128-gcm", cek, nonce);
  decipher.setAuthTag(ciphertext.subarray(-16));
  const plain = Buffer.concat([decipher.update(ciphertext.subarray(0, -16)), decipher.final()]);
  return plain.subarray(0, plain.lastIndexOf(2)).toString("utf8"); // drop the padding delimiter
}

function reply(res: ServerResponse, status: number, json?: unknown) {
  res.writeHead(status, json ? { "Content-Type": "application/json" } : {}).end(json ? JSON.stringify(json) : undefined);
}

async function handle(req: IncomingMessage, res: ServerResponse) {
  const no = ++requestNo;
  const url = new URL(req.url ?? "/", origin);
  const [, kind, id, action] = url.pathname.split("/");
  const log = (message: string) => console.log(`[push] #${no} ${req.method} ${url.pathname} ${message}`);

  if (req.method === "POST" && kind === "subscriptions" && !id) {
    // what PushManager.subscribe() does inside the browser: a key pair and an auth secret per subscription
    const { applicationServerKey } = JSON.parse((await body(req)).toString());
    const ecdh = createECDH("prime256v1");
    ecdh.generateKeys();
    const sub: Subscription = { serverKey: applicationServerKey, ecdh, auth: randomBytes(16), gone: false, messages: [] };
    const subId = String(nextId++);
    subscriptions.set(subId, sub);
    log(`-> 201 subscription ${subId}`);
    return reply(res, 201, { endpoint: `${origin}/push/${subId}`, expirationTime: null, keys: { p256dh: b64(ecdh.getPublicKey()), auth: b64(sub.auth) } });
  }
  const sub = subscriptions.get(id ?? "");
  if (req.method === "POST" && kind === "subscriptions" && action === "expire" && sub) {
    sub.gone = true;
    log("-> 204 (the subscription is now gone)");
    return reply(res, 204);
  }
  if (req.method === "GET" && kind === "messages" && sub) {
    const messages = sub.messages.splice(0);
    return reply(res, 200, messages);
  }
  if (req.method === "POST" && kind === "push") {
    const data = await body(req);
    if (!sub || sub.gone) {
      log("-> 410 Gone");
      return reply(res, 410);
    }
    try {
      const vapid = checkVapid(req.headers.authorization, sub);
      if (req.headers["content-encoding"] !== "aes128gcm") throw new Error(`Content-Encoding ${req.headers["content-encoding"]}`);
      if (!req.headers.ttl) throw new Error("no TTL header");
      sub.messages.push(decrypt(data, sub));
      log(`${vapid}, aes128gcm ${data.length} bytes, TTL=${req.headers.ttl}, Topic=${req.headers.topic ?? "-"}, Urgency=${req.headers.urgency ?? "-"} -> 201`);
      res.setHeader("Location", `${origin}/messages/${id}`);
      return reply(res, 201);
    } catch (err) {
      log(`-> 400 ${(err as Error).message}`);
      return reply(res, 400);
    }
  }
  reply(res, 404);
}

createServer((req, res) => void handle(req, res).catch((err) => console.log(`[push] failed: ${err.message}`)))
  .listen(port, "127.0.0.1", () => console.log(`[push] local push service on ${origin}`));
