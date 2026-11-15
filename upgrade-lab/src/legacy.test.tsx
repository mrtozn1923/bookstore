import { act } from 'react';
import { afterEach, expect, it, vi } from 'vitest';
import { mount } from './legacy';

afterEach(() => vi.restoreAllMocks());

// createRoot renders asynchronously; act() waits for it (and expects this flag)
(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;

// console.error('... %s ...', arg) → one readable line
function firstLine(message: string, ...args: unknown[]) {
  let i = 0;
  return message.replace(/%s/g, () => String(args[i++])).trim().split('\n')[0];
}

it('renders the app and lists what React warns about', () => {
  const warnings: string[] = [];
  vi.spyOn(console, 'error').mockImplementation((message: string, ...args: unknown[]) => warnings.push(firstLine(message, ...args)));
  const container = document.createElement('div');

  act(() => mount(container));

  console.log(container.innerHTML);
  console.log(warnings.length === 0 ? '(no warnings)' : warnings.map((w) => '- ' + w).join('\n'));
  expect(container.querySelector('h2')?.dataset.ready).toBe('yes');
});
