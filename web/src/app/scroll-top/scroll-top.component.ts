import { Component, NgZone, OnDestroy, OnInit } from '@angular/core';

@Component({
  selector: 'app-scroll-top',
  template: `<button *ngIf="visible" (click)="scrollTop()">Back to top</button>`
})
export class ScrollTopComponent implements OnInit, OnDestroy {
  visible = false;

  constructor(private ngZone: NgZone) { }

  ngOnInit(): void {
    this.ngZone.runOutsideAngular(() => {
      window.addEventListener('scroll', this.onScroll);
    });
  }

  ngOnDestroy(): void {
    window.removeEventListener('scroll', this.onScroll);
  }

  scrollTop(): void {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  private onScroll = (): void => {
    const shouldShow = window.scrollY > 300;
    if (shouldShow !== this.visible) {
      this.ngZone.run(() => (this.visible = shouldShow));
    }
  };
}
