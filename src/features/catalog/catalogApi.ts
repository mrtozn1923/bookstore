import { keepPreviousData, queryOptions } from '@tanstack/react-query';
import type { BookDetails, BookSummary, PagedResult } from '@/features/catalog/types';
import { apiFetch } from '@/shared/api';

export const pageSize = 8;

// Every value the request depends on is part of the key
export function booksQuery(search: string, page: number) {
  return queryOptions({
    queryKey: ['books', { search, page }],
    queryFn: ({ signal }) =>
      apiFetch<PagedResult<BookSummary>>(
        `/api/books?search=${encodeURIComponent(search)}&page=${page}&pageSize=${pageSize}`, { signal }),
    // Keep showing the previous page while the next one loads
    placeholderData: keepPreviousData,
  });
}

export function bookQuery(isbn: string) {
  return queryOptions({
    queryKey: ['book', isbn],
    queryFn: ({ signal }) => apiFetch<BookDetails>(`/api/books/${isbn}`, { signal }),
  });
}
