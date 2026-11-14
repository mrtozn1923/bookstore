import { placeOrderBody } from "#schemas/orders";

// JSON.parse makes "__proto__" an ordinary own property; the danger starts when it is merged into another object
const body = JSON.parse('{"lines":[{"isbn":"9780441172719","quantity":1}],"__proto__":{"isStaff":true}}');

// The zod schema from post 12 rejects the unknown key before any code can merge it
const result = placeOrderBody.safeParse(body);
console.log("zod:", result.success ? "accepted" : result.error.issues.map((i) => i.message).join("; "));

// A naive recursive merge, the kind found in hand-written "deep merge" helpers
function merge(target: Record<string, any>, source: Record<string, any>): Record<string, any> {
    for (const key of Object.keys(source)) {
        if (typeof source[key] === "object" && source[key] !== null) {
            target[key] = merge(target[key] ?? {}, source[key]);
        } else {
            target[key] = source[key];
        }
    }
    return target;
}

merge({}, body);
const user = {};
console.log("after merge, any object is staff:", (user as { isStaff?: boolean }).isStaff);

