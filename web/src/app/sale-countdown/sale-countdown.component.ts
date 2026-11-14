import { Component, OnDestroy, OnInit } from '@angular/core';

@Component({
    selector: 'app-sale-countdown',
    template: `<p>Sale ends in {{ secondsLeft }} s</p>`,
    standalone: true
})
export class SaleCountdownComponent implements OnInit, OnDestroy {
  secondsLeft = 3600;
  private timerId = 0;

  ngOnInit(): void {
    this.timerId = window.setInterval(() => {
      this.secondsLeft--;
      console.log('tick');
    }, 1000);
  }

  ngOnDestroy(): void {
    window.clearInterval(this.timerId);
  }
}
