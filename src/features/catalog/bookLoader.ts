import { data, type LoaderFunctionArgs } from 'react-router';
import { bookQuery } from './catalogApi';
import { ApiError } from '@/shared/api';
import { useClientStore } from '@/shared/clientStore';
import { queryClient } from '@/shared/queryClient';

// Like a resolver: the route renders only after the book is in the cache
export async function bookLoader({ params }: LoaderFunctionArgs) {
  try {
    const book = await queryClient.ensureQueryData(bookQuery(params.isbn!));
    // Loaders run outside React: getState() reaches the store without a hook
    useClientStore.getState().addRecentlyViewed({ isbn: book.isbn, title: book.title });
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      throw data(`No book with ISBN ${params.isbn}`, { status: 404, statusText: 'Not Found' });
    }
    throw error;
  }
  return null;
}
