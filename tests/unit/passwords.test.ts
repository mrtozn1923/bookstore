import { it } from "node:test";
import assert from "node:assert/strict";
import { hashPassword, needsRehash, verifyPassword } from "../../src/security/passwords.ts";

it("verifies the right password and rejects a wrong one", async () => {
    const stored = await hashPassword("example-password-change-me");
    assert.match(stored, /^\$scrypt\$ln=17,r=8,p=1\$/);
    assert.equal(await verifyPassword("example-password-change-me", stored), true);
    assert.equal(await verifyPassword("example-password-change-mE", stored), false);
});

it("uses a new salt every time", async () => {
    assert.notEqual(await hashPassword("same"), await hashPassword("same"));
});

it("treats composed and decomposed Turkish letters as the same password", async () => {
    // "ş" as one code point and as "s" + combining cedilla
    const stored = await hashPassword("Çalıkuşuş");
    assert.equal(await verifyPassword("Çalıkuşuş", stored), true);
});

it("asks for a re-hash of hashes made with weaker parameters", () => {
    assert.equal(needsRehash("$scrypt$ln=14,r=8,p=1$c2FsdA$aGFzaA"), true);
    assert.equal(needsRehash("$scrypt$ln=17,r=8,p=1$c2FsdA$aGFzaA"), false);
});
