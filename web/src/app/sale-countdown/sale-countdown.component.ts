import { Component, effect } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { interval, map } from 'rxjs';

@Component({
  selector: 'app-sale-countdown',
  template: `<p>Sale ends in {{ secondsLeft() }} s</p>`
})
export class SaleCountdownComponent {
  protected readonly secondsLeft = toSignal(
    interval(1000).pipe(map(i => 3600 - (i + 1))),
    { initialValue: 3600 }
  );

  constructor() {
    effect(() => console.log('Seconds left:', this.secondsLeft()));
  }
}
