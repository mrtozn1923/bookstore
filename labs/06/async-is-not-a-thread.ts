// An async function still runs on the same thread until its first await
import { pbkdf2Sync } from "node:crypto";

let ticks = 0;
const interval = setInterval(() => ticks++, 10);

async function hashPasswords(count: number): Promise<number> {
    for (let i = 0; i < count; i++) {
        pbkdf2Sync(`password-${i}`, "bookstore-salt", 600_000, 32, "sha256");
    }
    return count;
}

const started = performance.now();
const pending = hashPasswords(10);
console.log(`hashPasswords() returned a promise after ${Math.round(performance.now() - started)} ms, interval ticks so far: ${ticks}`);
await pending;
clearInterval(interval);
