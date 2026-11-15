'use client';

// Error boundaries run in the browser, so this file is a client component
export default function BookError({ error, reset }: { error: Error; reset: () => void }) {
  return (
    <>
      <p>Could not load this book: {error.message}</p>
      <button type="button" onClick={reset}>Try again</button>
    </>
  );
}
