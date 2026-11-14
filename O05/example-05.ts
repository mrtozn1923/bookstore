const searchDefaults = { sort: "title", order: "asc" } as const;

type SearchDefaults = typeof searchDefaults;
// { readonly sort: "title"; readonly order: "asc"; }
