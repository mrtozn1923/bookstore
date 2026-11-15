import { randomBytes, scrypt, timingSafeEqual, type ScryptOptions } from "node:crypto";

// OWASP Password Storage Cheat Sheet: scrypt with N=2^17 (128 MiB), r=8, p=1
const CURRENT = { logN: 17, r: 8, p: 1 };
const KEY_LENGTH = 32;

function scryptAsync(password: string, salt: Buffer, options: ScryptOptions): Promise<Buffer> {
    return new Promise((resolve, reject) =>
        scrypt(password, salt, KEY_LENGTH, options, (err, key) => (err ? reject(err) : resolve(key))),
    );
}

function options(logN: number, r: number, p: number): ScryptOptions {
    const N = 2 ** logN;
    // The default maxmem is 32 MiB; scrypt needs about 128 * N * r bytes
    return { N, r, p, maxmem: 256 * N * r };
}

// PHC-style string: the parameters are stored with the hash, so they can be raised later
export async function hashPassword(password: string): Promise<string> {
    const salt = randomBytes(16);
    const { logN, r, p } = CURRENT;
    const key = await scryptAsync(password.normalize("NFC"), salt, options(logN, r, p));
    return `$scrypt$ln=${logN},r=${r},p=${p}$${salt.toString("base64url")}$${key.toString("base64url")}`;
}

const STORED = /^\$scrypt\$ln=(\d+),r=(\d+),p=(\d+)\$([\w-]+)\$([\w-]+)$/;

export async function verifyPassword(password: string, stored: string): Promise<boolean> {
    const match = STORED.exec(stored);
    if (!match) {
        return false;
    }
    const [, logN, r, p, salt, hash] = match;
    const expected = Buffer.from(hash, "base64url");
    const actual = await scryptAsync(password.normalize("NFC"), Buffer.from(salt, "base64url"), options(Number(logN), Number(r), Number(p)));
    return expected.length === actual.length && timingSafeEqual(expected, actual);
}

// True when the stored hash was made with weaker parameters than today's: re-hash it at the next sign-in
export function needsRehash(stored: string): boolean {
    const match = STORED.exec(stored);
    return !match || Number(match[1]) < CURRENT.logN || Number(match[2]) < CURRENT.r || Number(match[3]) < CURRENT.p;
}
