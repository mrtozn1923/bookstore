export function formatPrice(value) {
  return new Intl.NumberFormat("tr-TR", { style: "currency", currency: "TRY" }).format(value);
}

function slugify(text) {
  return text.toLowerCase().trim().replaceAll(" ", "-");
}

export { slugify as toSlug };
