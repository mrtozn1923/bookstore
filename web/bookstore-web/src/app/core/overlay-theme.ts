import { OverlayContainer } from '@angular/cdk/overlay';
import { DestroyRef, inject } from '@angular/core';

// Overlays (datepicker, autocomplete, snackbar) are rendered in the overlay container at the end of <body>, outside
// the page's theme class. Give the container the same class while the page is on screen. Call from an injection context.
export function useOverlayThemeClass(themeClass: string): void {
  const container = inject(OverlayContainer).getContainerElement();
  container.classList.add(themeClass);
  inject(DestroyRef).onDestroy(() => container.classList.remove(themeClass));
}
