import { tabLeavesPanel } from './overlay-focus';

function panel(): HTMLElement {
  const element = document.createElement('section');
  element.tabIndex = -1;
  element.innerHTML = '<a href="/books/1">View details</a><button type="button">Close</button>';
  document.body.append(element);
  return element;
}

const tab = (shiftKey = false) => new KeyboardEvent('keydown', { key: 'Tab', shiftKey });

describe('tabLeavesPanel', () => {
  afterEach(() => document.body.replaceChildren());

  it('is true for Tab on the last element and for Shift+Tab on the panel or its first element', () => {
    const element = panel();
    element.querySelector('button')!.focus();
    expect(tabLeavesPanel(tab(), element)).toBe(true);
    element.focus();
    expect(tabLeavesPanel(tab(true), element)).toBe(true);
    element.querySelector('a')!.focus();
    expect(tabLeavesPanel(tab(true), element)).toBe(true);
  });

  it('is false while Tab stays inside and for other keys', () => {
    const element = panel();
    element.querySelector('a')!.focus();
    expect(tabLeavesPanel(tab(), element)).toBe(false);
    expect(tabLeavesPanel(new KeyboardEvent('keydown', { key: 'Enter' }), element)).toBe(false);
  });
});
