import type { z } from "zod";

// Thrown when a request does not match its schema; the error middleware turns it into a 400
export class RequestValidationError extends Error {
    readonly errors: Record<string, string[]>;

    constructor(errors: Record<string, string[]>) {
        super("One or more validation errors occurred.");
        this.name = "RequestValidationError";
        this.errors = errors;
    }
}

// ["lines", 0, "quantity"] -> "lines[0].quantity", the shape ASP.NET Core uses in "errors"
function fieldName(path: readonly PropertyKey[]): string {
    return path.reduce<string>((name, key) => (typeof key === "number" ? `${name}[${key}]` : name ? `${name}.${String(key)}` : String(key)), "") || "$";
}

export function parseRequest<T extends z.ZodType>(schema: T, value: unknown): z.infer<T> {
    const result = schema.safeParse(value);
    if (result.success) {
        return result.data;
    }
    const errors: Record<string, string[]> = {};
    for (const issue of result.error.issues) {
        (errors[fieldName(issue.path)] ??= []).push(issue.message);
    }
    throw new RequestValidationError(errors);
}
