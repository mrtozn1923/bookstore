import { argon2, createHash, randomBytes, scrypt } from "node:crypto";
import { promisify } from "node:util";

const scryptAsync = promisify(scrypt) as (pw: string, salt: Buffer, len: number, opts: object) => Promise<Buffer>;
const argon2Async = promisify(argon2);
const password = "correct horse battery staple";
const salt = randomBytes(16);

function perSecond(label: string, ms: number, count: number): void {
    console.log(`${label.padEnd(34)} ${(ms / count).toFixed(4).padStart(10)} ms/hash  ${Math.round((count / ms) * 1000).toLocaleString("en-US").padStart(11)} hashes/s`);
}

// Fast hashes: what an attacker with a stolen table can try per second on one core
for (const algorithm of ["md5", "sha1", "sha256"]) {
    const started = performance.now();
    for (let i = 0; i < 1_000_000; i++) {
        createHash(algorithm).update(password + i).digest();
    }
    perSecond(algorithm, performance.now() - started, 1_000_000);
}

// Password hashes: slow and memory-hard on purpose
async function measure(label: string, run: () => Promise<unknown>, count = 5): Promise<void> {
    const started = performance.now();
    for (let i = 0; i < count; i++) {
        await run();
    }
    perSecond(label, performance.now() - started, count);
}
await measure("scrypt N=2^14 r=8 p=1 (default)", () => scryptAsync(password, salt, 32, {}));
await measure("scrypt N=2^17 r=8 p=1 (OWASP)", () => scryptAsync(password, salt, 32, { N: 2 ** 17, r: 8, p: 1, maxmem: 256 * 2 ** 17 * 8 }));
await measure("argon2id m=19 MiB t=2 p=1 (OWASP)", () =>
    argon2Async("argon2id", { message: password, nonce: salt, parallelism: 1, tagLength: 32, memory: 19_456, passes: 2 }),
);
