import { Injectable, OnDestroy } from '@angular/core';

@Injectable()
export class ReservationTimerService implements OnDestroy {
  secondsLeft = 0;
  private timerId = 0;

  start(seconds: number): void {
    this.stop();
    this.secondsLeft = seconds;
    this.timerId = window.setInterval(() => this.secondsLeft--, 1000);
  }

  stop(): void {
    window.clearInterval(this.timerId);
  }

  ngOnDestroy(): void {
    this.stop();
  }
}
