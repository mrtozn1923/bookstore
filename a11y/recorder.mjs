// Injected into the page: records ariaNotify() calls and text changes inside live regions in window.__announcements.
// A model of the ARIA rules, not a screen reader (see announce.mjs).
export function recorder() {
  window.__announcements = [];
  const log = entry => window.__announcements.push(entry);
  for (const proto of [Document.prototype, Element.prototype]) {
    const original = proto.ariaNotify;
    if (!original) continue;
    proto.ariaNotify = function (text, options) {
      log(`ariaNotify ${options?.priority ?? 'normal'}: "${text}"`);
      return original.call(this, text, options);
    };
  }
  const liveSelector = '[aria-live]:not([aria-live="off"]), [role="status"], [role="alert"], [role="log"]';
  const politeness = el => el.getAttribute('aria-live') ?? (el.getAttribute('role') === 'alert' ? 'assertive' : 'polite');
  // A region only reports changes if it was already in the DOM before the change
  const known = new WeakSet();
  const lastText = new WeakMap();
  const textOf = el => el.textContent.trim().replace(/\s+/g, ' ');
  const remember = () => document.querySelectorAll(liveSelector).forEach(el => {
    if (!known.has(el)) lastText.set(el, textOf(el));
    known.add(el);
  });
  const observer = new MutationObserver(mutations => {
    const changed = new Set();
    for (const m of mutations) {
      const node = m.target.nodeType === Node.ELEMENT_NODE ? m.target : m.target.parentElement;
      const region = node?.closest(liveSelector);
      if (region) changed.add(region);
      // aria-busy="false" on a container: the regions inside report what changed while it was busy
      if (m.attributeName === 'aria-busy') m.target.querySelectorAll(liveSelector).forEach(el => changed.add(el));
      for (const added of m.addedNodes) {
        if (added.nodeType === Node.ELEMENT_NODE && added.matches(liveSelector) && !known.has(added) && added.textContent.trim()) {
          log(`missed (region inserted with its text): ${added.getAttribute('role') ?? 'aria-live'} "${textOf(added)}"`);
        }
      }
    }
    for (const region of changed) {
      const text = textOf(region);
      if (known.has(region) && text && text !== lastText.get(region) && !region.closest('[aria-busy="true"]')) {
        lastText.set(region, text);
        log(`live ${politeness(region)}${region.getAttribute('aria-atomic') === 'true' ? ' atomic' : ''}: "${text}"`);
      }
    }
    remember();
  });
  document.addEventListener('DOMContentLoaded', () => {
    remember();
    observer.observe(document.body, { subtree: true, childList: true, characterData: true, attributes: true, attributeFilter: ['aria-busy'] });
  });
}
