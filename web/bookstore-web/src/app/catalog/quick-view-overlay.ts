import {
  ConnectedPosition, createFlexibleConnectedPositionStrategy, createOverlayRef, createRepositionScrollStrategy, OverlayRef,
} from '@angular/cdk/overlay';
import { ComponentPortal } from '@angular/cdk/portal';
import { Injector } from '@angular/core';
import { tabLeavesPanel } from '../core/overlay-focus';
import { BookQuickView } from './book-quick-view';

// Right of the card; if it does not fit, left of it; if neither fits, below it
const positions: ConnectedPosition[] = [
  { originX: 'end', originY: 'top', overlayX: 'start', overlayY: 'top', offsetX: 8 },
  { originX: 'start', originY: 'top', overlayX: 'end', overlayY: 'top', offsetX: -8 },
  { originX: 'center', originY: 'bottom', overlayX: 'center', overlayY: 'top', offsetY: 8 },
];

// Loaded on the first "Quick look" click: the CDK overlay code is not part of the catalog's initial bundle
export function attachQuickView(injector: Injector, isbn: string, card: HTMLElement, close: () => void): OverlayRef {
  const overlayRef = createOverlayRef(injector, {
    positionStrategy: createFlexibleConnectedPositionStrategy(injector, card)
      .withPositions(positions)
      .withViewportMargin(8)
      .withPush(false),
    // The panel follows the card while the page scrolls
    scrollStrategy: createRepositionScrollStrategy(injector),
    hasBackdrop: true,
    backdropClass: 'cdk-overlay-transparent-backdrop',
  });
  const view = overlayRef.attach(new ComponentPortal(BookQuickView, null, injector));
  view.setInput('isbn', isbn);
  view.instance.closed.subscribe(close);
  overlayRef.backdropClick().subscribe(close);
  const panel: HTMLElement = view.location.nativeElement;
  // Escape, or Tab out of the panel: close and give focus back to the button
  overlayRef.keydownEvents().subscribe(event => {
    if (event.key === 'Escape' || tabLeavesPanel(event, panel)) {
      event.preventDefault();
      close();
    }
  });
  // Keyboard and screen reader users land in the panel, not behind it
  panel.focus();
  return overlayRef;
}
