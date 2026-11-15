import { data, type LoaderFunctionArgs } from 'react-router';
import { bookQuery } from '@/features/catalog/catalogApi';
import { ApiError } from '@/shared/api';
import { queryClient } from '@/shared/queryClient';

// Like a resolver: the route renders only after the book is in the cache
export async function bookLoader({ params }: LoaderFunctionArgs) {
  try {
    await queryClient.ensureQueryData(bookQuery(params.isbn!));
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      throw data(`No book with ISBN ${params.isbn}`, { status: 404, statusText: 'Not Found' });
    }
    throw error;
  }
  return null;
}
