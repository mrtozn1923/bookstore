// An overlay is rendered at the end of <body>: Tab from its last element (or Shift+Tab from its first) would leave
// the page or jump to the last button of the page. True when this keydown would move focus out of the panel.
export function tabLeavesPanel(event: KeyboardEvent, panel: HTMLElement): boolean {
  if (event.key !== 'Tab') {
    return false;
  }
  const focusable = [...panel.querySelectorAll<HTMLElement>('a[href], button:not([disabled]), input, select, textarea')];
  const active = document.activeElement;
  return event.shiftKey
    ? active === panel || active === focusable[0]
    : focusable.length === 0 || active === focusable.at(-1);
}
