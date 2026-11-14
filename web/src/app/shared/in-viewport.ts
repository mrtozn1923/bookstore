import { DestroyRef, Directive, effect, ElementRef, inject, input, output } from '@angular/core';

@Directive({ selector: '[appInViewport]' })
export class InViewport {
  readonly appInViewport = input<unknown>();
  readonly enter = output<void>();

  constructor() {
    const element: HTMLElement = inject(ElementRef).nativeElement;
    const observer = new IntersectionObserver((entries) => {
      if (entries.some((entry) => entry.isIntersecting)) {
        this.enter.emit();
      }
    });
    effect(() => {
      // Değer değişince yeniden gözlemle: observe() o anki durumu tekrar bildirir
      this.appInViewport();
      observer.unobserve(element);
      observer.observe(element);
    });
    inject(DestroyRef).onDestroy(() => observer.disconnect());
  }
}
