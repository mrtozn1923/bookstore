import { it } from "node:test";
import assert from "node:assert/strict";
import { randomBytes } from "node:crypto";
import { decryptField, encryptField, parseKey } from "../../src/security/fieldEncryption.ts";

const key = randomBytes(32);

it("round-trips and never produces the same ciphertext twice", () => {
    const a = encryptField("+90 500 000 00 01", key, "customer:ada");
    const b = encryptField("+90 500 000 00 01", key, "customer:ada");
    assert.notEqual(a, b);
    assert.equal(decryptField(a, key, "customer:ada"), "+90 500 000 00 01");
});

it("fails for another row's associated data", () => {
    const value = encryptField("+90 500 000 00 01", key, "customer:ada");
    assert.throws(() => decryptField(value, key, "customer:alan"), /unable to authenticate/);
});

it("rejects keys that are not 32 bytes", () => {
    assert.throws(() => parseKey(randomBytes(16).toString("base64")), /must be 32 bytes/);
});
