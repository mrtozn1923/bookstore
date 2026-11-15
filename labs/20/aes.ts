import { randomBytes } from "node:crypto";
import { decryptField, encryptField } from "../../src/security/fieldEncryption.ts";

// In the API the key comes from configuration (a secret store); here it is generated for the run
const key = randomBytes(32);
const phone = "+90 500 000 00 01";

const stored = encryptField(phone, key, "customer:ada");
console.log("stored:   ", stored);
console.log("decrypted:", decryptField(stored, key, "customer:ada"));
console.log("again:    ", encryptField(phone, key, "customer:ada"));

function tryDecrypt(label: string, value: string, associatedData: string): void {
    try {
        decryptField(value, key, associatedData);
        console.log(`${label}: decrypted`);
    } catch (err) {
        console.log(`${label}: ${(err as Error).message}`);
    }
}

// One changed character in the ciphertext part
const parts = stored.split(".");
parts[2] = (parts[2][0] === "A" ? "B" : "A") + parts[2].slice(1);
tryDecrypt("tampered ciphertext", parts.join("."), "customer:ada");
// The same value copied to another customer's row
tryDecrypt("copied to alan     ", stored, "customer:alan");
