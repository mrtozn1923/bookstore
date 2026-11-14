import { z } from "zod";
import { placeOrderBody } from "#schemas/orders";

// zod 4 can export a schema as JSON Schema, the raw material for an OpenAPI document
console.log(JSON.stringify(z.toJSONSchema(placeOrderBody), null, 2));
