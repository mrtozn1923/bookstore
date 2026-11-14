import { TestBed } from '@angular/core/testing';

import { ReservationTimerService } from './reservation-timer.service';

describe('ReservationTimerService', () => {
  let service: ReservationTimerService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [ReservationTimerService] });
    service = TestBed.inject(ReservationTimerService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
