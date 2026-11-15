import { Component, computed, input } from '@angular/core';
import { extendTailwindMerge } from 'tailwind-merge';

export type ButtonIntent = 'primary' | 'secondary' | 'danger';
export type ButtonSize = 'sm' | 'md';

// Bookstore's custom font sizes, so that tailwind-merge does not read them as colors
const merge = extendTailwindMerge({ extend: { theme: { text: ['hero'] } } });

const BASE =
  'inline-flex items-center justify-center rounded-md font-medium focus-visible:outline-2 focus-visible:outline-offset-2 disabled:cursor-not-allowed disabled:opacity-50';

// one entry per variant, every class written in full
const INTENT: Record<ButtonIntent, string> = {
  primary: 'bg-brand-600 text-white hover:bg-brand-700 focus-visible:outline-brand-600',
  secondary:
    'border border-slate-300 bg-white text-ink-900 hover:bg-slate-50 focus-visible:outline-slate-400',
  danger: 'bg-red-600 text-white hover:bg-red-700 focus-visible:outline-red-600',
};

const SIZE: Record<ButtonSize, string> = {
  sm: 'px-3 py-1.5 text-sm',
  md: 'px-4 py-2 text-base',
};

export function buttonClasses(
  intent: ButtonIntent = 'primary',
  size: ButtonSize = 'md',
  extra = '',
): string {
  return merge(BASE, INTENT[intent], SIZE[size], extra);
}

@Component({
  selector: 'button[appButton]',
  template: '<ng-content />',
  host: { '[class]': 'classes()' },
})
export class Button {
  readonly intent = input<ButtonIntent>('primary');
  readonly size = input<ButtonSize>('md');
  // classes the caller adds; they win over the variant's classes
  readonly extra = input('', { alias: 'class' });

  protected readonly classes = computed(() =>
    buttonClasses(this.intent(), this.size(), this.extra()),
  );
}
