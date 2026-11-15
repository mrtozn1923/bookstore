import { createCipheriv, createDecipheriv, randomBytes } from "node:crypto";

// AES-256-GCM for single fields (a customer's phone number...): "v1.<iv>.<ciphertext>.<tag>", base64url
const VERSION = "v1";

export function parseKey(base64: string): Buffer {
    const key = Buffer.from(base64, "base64");
    if (key.length !== 32) {
        throw new Error(`the field encryption key must be 32 bytes, got ${key.length}`);
    }
    return key;
}

// associatedData binds the ciphertext to its row: a value copied to another customer fails to decrypt
export function encryptField(plaintext: string, key: Buffer, associatedData: string): string {
    // A new random 96-bit IV for every value; never reuse an IV with the same key
    const iv = randomBytes(12);
    const cipher = createCipheriv("aes-256-gcm", key, iv);
    cipher.setAAD(Buffer.from(associatedData, "utf8"));
    const ciphertext = Buffer.concat([cipher.update(plaintext, "utf8"), cipher.final()]);
    return [VERSION, ...[iv, ciphertext, cipher.getAuthTag()].map((part) => part.toString("base64url"))].join(".");
}

export function decryptField(value: string, key: Buffer, associatedData: string): string {
    const [version, iv, ciphertext, tag] = value.split(".");
    if (version !== VERSION || !iv || !ciphertext || !tag) {
        throw new Error("not an encrypted field value");
    }
    const decipher = createDecipheriv("aes-256-gcm", key, Buffer.from(iv, "base64url"));
    decipher.setAAD(Buffer.from(associatedData, "utf8"));
    decipher.setAuthTag(Buffer.from(tag, "base64url"));
    // final() throws if a single bit of the ciphertext, the tag or the associated data changed
    return Buffer.concat([decipher.update(Buffer.from(ciphertext, "base64url")), decipher.final()]).toString("utf8");
}
