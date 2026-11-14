import { it } from "node:test";
import assert from "node:assert/strict";
import { UnknownBookError } from "#domain/errors";
import { createTestServices, DUNE } from "../../tests/helpers.ts";

// Dune is in the catalog, so placeOrder succeeds and this assertion is wrong. Does the test fail?
it("rejects an unknown ISBN", () => {
    const { orders } = createTestServices();
    assert.rejects(orders.placeOrder("ada", [{ isbn: DUNE, quantity: 1 }]), UnknownBookError);
});
