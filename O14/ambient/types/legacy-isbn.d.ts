declare module "legacy-isbn" {
  export const isbnPrefixes: readonly string[];
  export function isValidIsbn(value: string): boolean;
}
