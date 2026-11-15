import { Params } from '@angular/router';

const FORMATS = ['hardcover', 'paperback', 'ebook'];
const SORTS = ['title', 'price', 'year'];

export type NormalizedQuery = { valid: false } | { valid: true; params: Params; changed: boolean };

// sunucudaki CatalogQuery.Parse ile aynı kurallar: tekrar eden ya da anlamsız filtre geçersiz,
// varsayılanlar (page=1, sort=title) atılır, parametre sırası format, sort, page
export function normalizeCatalogQuery(query: Params): NormalizedQuery {
  if (Object.values(query).some((v) => Array.isArray(v))) return { valid: false };
  const { format, sort, page } = query;
  if (format !== undefined && !FORMATS.includes(format)) return { valid: false };
  if (sort !== undefined && !SORTS.includes(sort)) return { valid: false };
  if (page !== undefined && !/^[1-9]\d*$/.test(page)) return { valid: false };

  const params: Params = {};
  if (format) params['format'] = format;
  if (sort && sort !== 'title') params['sort'] = sort;
  if (page && page !== '1') params['page'] = page;

  const known = Object.keys(query).filter((k) => ['format', 'sort', 'page'].includes(k));
  const changed = known.join('&') !== Object.keys(params).join('&') || known.some((k) => query[k] !== params[k]);
  return { valid: true, params, changed };
}
