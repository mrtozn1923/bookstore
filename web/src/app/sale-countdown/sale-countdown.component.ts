import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';

@Component({
  selector: 'app-sale-countdown',
  template: `<p>Sale ends in {{ secondsLeft() }} s</p>`,
  standalone: true
})
export class SaleCountdownComponent implements OnInit {
  secondsLeft = signal(3600);
  private destroyRef = inject(DestroyRef);

  ngOnInit(): void {
    const id = window.setInterval(() => {
      this.secondsLeft.update(s => s - 1);
    }, 1000);
    this.destroyRef.onDestroy(() => window.clearInterval(id));
  }
}
