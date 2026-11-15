import { useEffect, useEffectEvent, useState } from 'react';
import type { BookSummary, PagedResult } from '@/features/catalog/types';
import { useDebouncedValue } from '@/shared/useDebouncedValue';

export type SearchResult =
  | { status: 'success'; query: string; books: BookSummary[]; total: number }
  | { status: 'error'; query: string; message: string };

// While loading, result may be missing or belong to an older query
export type BookSearch =
  | { isLoading: true; result: SearchResult | null }
  | { isLoading: false; result: SearchResult };

const debounceMs = 300;

// The search effect from post 06, moved out of the page into a reusable hook
export function useBookSearch(query: string, onFinished: (query: string, total: number) => void): BookSearch {
  const debouncedQuery = useDebouncedValue(query, debounceMs);
  const [result, setResult] = useState<SearchResult | null>(null);
  const handleFinished = useEffectEvent(onFinished);

  useEffect(() => {
    const controller = new AbortController();
    async function load() {
      try {
        const url = `/api/books?search=${encodeURIComponent(debouncedQuery.trim())}&pageSize=50`;
        const response = await fetch(url, { signal: controller.signal });
        if (!response.ok) {
          throw new Error(`HTTP ${response.status}`);
        }
        const page = (await response.json()) as PagedResult<BookSummary>;
        setResult({ status: 'success', query: debouncedQuery, books: page.items, total: page.total });
        handleFinished(debouncedQuery, page.total);
      } catch (error) {
        if (!controller.signal.aborted) {
          setResult({ status: 'error', query: debouncedQuery, message: error instanceof Error ? error.message : String(error) });
        }
      }
    }
    load();
    return () => controller.abort();
  }, [debouncedQuery]);

  // Still loading while the result belongs to another query than the one in the box
  if (result === null || result.query !== query) {
    return { isLoading: true, result };
  }
  return { isLoading: false, result };
}
