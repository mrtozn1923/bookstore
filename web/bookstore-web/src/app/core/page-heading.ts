import { Directive, ElementRef, afterNextRender, inject } from '@angular/core';
import { RouteFocus } from './route-focus';

// <h1 appPageHeading>: after a route change, focus moves to the new page's main heading.
// Works for headings that render later too (for example after an httpResource has loaded).
@Directive({
  selector: 'h1[appPageHeading]',
  host: { tabindex: '-1' },
})
export class PageHeading {
  constructor() {
    const heading = inject<ElementRef<HTMLElement>>(ElementRef);
    const routeFocus = inject(RouteFocus);
    afterNextRender(() => {
      if (routeFocus.claim()) {
        heading.nativeElement.focus();
      }
    });
  }
}
