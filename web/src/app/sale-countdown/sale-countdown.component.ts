import { Component, DestroyRef, OnInit, inject } from '@angular/core';

@Component({
  selector: 'app-sale-countdown',
  template: `<p>Sale ends in {{ secondsLeft }} s</p>`,
  standalone: true
})
export class SaleCountdownComponent implements OnInit {
  secondsLeft = 3600;
  private destroyRef = inject(DestroyRef);

  ngOnInit(): void {
    const id = window.setInterval(() => {
      this.secondsLeft--;
      console.log('tick');
    }, 1000);
    this.destroyRef.onDestroy(() => window.clearInterval(id));
  }
}
