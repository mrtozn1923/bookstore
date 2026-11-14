type Role = "admin" | "staff" | "customer";

const permissions: Record<Role, string[]> = {
  admin: ["read", "write", "delete"],
  staff: ["read", "write"],
  customer: ["read"],
};

// Error: Property 'customer' is missing in type '{ admin: string[]; staff: string[]; }' but required in type 'Record<Role, string[]>'.
const partialPermissions: Record<Role, string[]> = {
  admin: ["read"],
  staff: ["read"],
};

const visits: Record<string, number> = {};
visits["/books"] = 1;
visits["/cart"] = 3;
