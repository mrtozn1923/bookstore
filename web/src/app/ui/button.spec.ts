import { buttonClasses } from './button';

describe('buttonClasses', () => {
  it('uses the primary, medium variant by default', () => {
    expect(buttonClasses()).toContain('bg-brand-600');
    expect(buttonClasses()).toContain('px-4 py-2 text-base');
  });

  it('lets the caller override padding and color', () => {
    const classes = buttonClasses('primary', 'sm', 'bg-red-600 px-2');
    expect(classes).not.toContain('bg-brand-600');
    expect(classes).not.toContain('px-3');
    expect(classes).toContain('bg-red-600 px-2');
  });

  it('keeps a custom font size next to a text color', () => {
    const classes = buttonClasses('secondary', 'md', 'text-hero');
    expect(classes).toContain('text-ink-900');
    expect(classes).toContain('text-hero');
    expect(classes).not.toContain('text-base');
  });
});
