import { createCipheriv, randomBytes } from "node:crypto";

// The bug: one fixed IV for every value (for example a constant in the config file)
const key = randomBytes(32);
const fixedIv = Buffer.alloc(12, 7);
function encrypt(text: string): Buffer {
    const cipher = createCipheriv("aes-256-gcm", key, fixedIv);
    return Buffer.concat([cipher.update(text, "utf8"), cipher.final()]);
}

const adaPhone = encrypt("+90 500 000 00 01");
const alanPhone = encrypt("+90 500 999 12 34");

// GCM encrypts with a key stream (CTR mode): same key + same IV = same key stream.
// XOR of the two ciphertexts = XOR of the two plaintexts; knowing one value reveals the other
const knownAda = Buffer.from("+90 500 000 00 01", "utf8");
const recovered = Buffer.from(adaPhone.map((byte, i) => byte ^ alanPhone[i] ^ knownAda[i]));
console.log("recovered without the key:", recovered.toString("utf8"));
