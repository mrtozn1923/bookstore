import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ReservationTimerComponent } from './reservation-timer.component';

describe('ReservationTimerComponent', () => {
  let component: ReservationTimerComponent;
  let fixture: ComponentFixture<ReservationTimerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ReservationTimerComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ReservationTimerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
