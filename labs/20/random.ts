import { randomBytes, randomInt, randomUUID, randomUUIDv7 } from "node:crypto";

// All of these come from the operating system's CSPRNG; Math.random() does not
console.log("randomUUID   ", randomUUID());
console.log("randomUUIDv7 ", randomUUIDv7(), randomUUIDv7());
console.log("token        ", randomBytes(32).toString("base64url"));
console.log("order code   ", randomInt(100_000, 1_000_000));
