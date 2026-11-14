// 8 async pbkdf2 calls: all of them run on the libuv thread pool
import { pbkdf2 } from "node:crypto";

const started = performance.now();
for (let i = 1; i <= 8; i++) {
    pbkdf2("correct horse", "bookstore-salt", 3_000_000, 32, "sha256", () => {
        console.log(`hash ${i} done at ${Math.round(performance.now() - started)} ms`);
    });
}
console.log(`all 8 calls started in ${Math.round(performance.now() - started)} ms`);
