import { Component, OnInit } from '@angular/core';
import { ReservationTimerService } from '../reservation-timer.service';

@Component({
    selector: 'app-reservation-timer',
    template: `<span>Reserved for {{ timer.secondsLeft }} s</span>`,
    providers: [ReservationTimerService],
    standalone: true
})
export class ReservationTimerComponent implements OnInit {
  constructor(public timer: ReservationTimerService) { }

  ngOnInit(): void {
    this.timer.start(600);
  }
}
