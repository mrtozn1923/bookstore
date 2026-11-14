const roles = ["customer", "staff", "admin"] as const;

type Role = (typeof roles)[number];
// "admin" | "customer" | "staff"

function canEditCatalog(role: Role) {
  return role === "admin" || role === "staff";
}

canEditCatalog("staff");
// Error: Argument of type '"guest"' is not assignable to parameter of type '"admin" | "customer" | "staff"'.
canEditCatalog("guest");
