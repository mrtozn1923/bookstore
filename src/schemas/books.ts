import { z } from "zod";
import { isValidIsbn13 } from "@bookstore/contracts";

// The regex ends up in JSON Schema / OpenAPI; the checksum refinement only runs in code
export const isbnSchema = z
    .string()
    // abort: when the format is wrong, skip the checksum and report one error, not two
    .regex(/^97[89]\d{10}$/, { message: "must be 13 digits starting with 978 or 979", abort: true })
    .refine(isValidIsbn13, { message: "must be a valid ISBN-13" });

export const bookParams = z.object({ isbn: isbnSchema });

// Query strings are text: z.coerce turns "2" into 2 before the number rules run
export const listBooksQuery = z.object({
    page: z.coerce.number().int().min(1).default(1),
    pageSize: z.coerce.number().int().min(1).max(50).default(10),
    q: z.string().trim().min(2).max(100).optional(),
    author: z.string().trim().min(1).max(100).optional(),
    sort: z.enum(["title", "price", "-price"]).default("title"),
});

export type ListBooksQuery = z.infer<typeof listBooksQuery>;
