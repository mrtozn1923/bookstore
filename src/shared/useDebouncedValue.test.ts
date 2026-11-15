import { act, renderHook } from '@testing-library/react';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { useDebouncedValue } from '@/shared/useDebouncedValue';

beforeEach(() => vi.useFakeTimers());
afterEach(() => vi.useRealTimers());

it('returns the new value only after it has been stable for the delay', () => {
  const { result, rerender } = renderHook(({ value }) => useDebouncedValue(value, 300), { initialProps: { value: 'du' } });

  rerender({ value: 'dun' });
  act(() => vi.advanceTimersByTime(200));
  rerender({ value: 'dune' });
  act(() => vi.advanceTimersByTime(299));
  expect(result.current).toBe('du');

  act(() => vi.advanceTimersByTime(1));
  expect(result.current).toBe('dune');
});
