import { z } from "zod";
import { isbnSchema } from "#schemas/books";

const orderLine = z.object({
    isbn: isbnSchema,
    quantity: z.int().min(1).max(20),
});

// The request contract for POST /api/orders; unknown fields are rejected and the customer comes from the token
export const placeOrderBody = z.strictObject({
    lines: z
        .array(orderLine)
        .min(1)
        .max(50)
        .refine((lines) => new Set(lines.map((l) => l.isbn)).size === lines.length, { message: "each ISBN may appear only once" }),
});

// The TypeScript type is derived from the schema, so the two cannot drift apart
export type PlaceOrderBody = z.infer<typeof placeOrderBody>;
