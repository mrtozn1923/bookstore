type Id = string | number;

type SearchResult =
  | { titles: string[] }
  | { error: string };
